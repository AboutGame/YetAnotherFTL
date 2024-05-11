using Tensorflow;
using Tensorflow.Common.Types;
using Tensorflow.Keras.Engine;
using Tensorflow.NumPy;
using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play;
using static Tensorflow.Binding;
using static Tensorflow.KerasApi;

namespace YetAnotherFTL.Training;

public class PlayerModel
{
    protected string ModelFilePath { get; }

    protected IModel Model { get; }
    
    public PlayerModel(string modelPath)
    {
        ModelFilePath = modelPath;

        if (Directory.Exists(ModelFilePath))
        {
            Model = keras.models.load_model(ModelFilePath);
        }
        else
        {
            var nextAction = keras.layers.Input(54, name: "action", dtype: TF_DataType.TF_FLOAT);
            var bidAction = keras.layers.Input(3, name: "bid", dtype: TF_DataType.TF_FLOAT);
            var state = keras.layers.Input(521, name: "state", dtype: TF_DataType.TF_FLOAT);
            
            var moveHistory = keras.layers.Input(54 * 5 * 3, name: "history", dtype: TF_DataType.TF_FLOAT);
            var reshaped = keras.layers.Reshape((54, 5 * 3)).Apply(moveHistory);
            var lstm = keras.layers.LSTM(128);
            var lstmOut = lstm.Apply(reshaped);

            var playingInput = keras.layers.Concatenate(1).Apply(new Tensors([nextAction, state, lstmOut]));
            var playing = keras.layers.Dense(256, keras.activations.Relu).Apply(playingInput);
            playing = keras.layers.Dense(256, keras.activations.Relu).Apply(playing);
            playing = keras.layers.Dense(256, keras.activations.Relu).Apply(playing);
            playing = keras.layers.Dense(256, keras.activations.Relu).Apply(playing);
            playing = keras.layers.Dense(256, keras.activations.Relu).Apply(playing);
            playing = keras.layers.Dense(1, keras.activations.Sigmoid).Apply(playing);

            var biddingInput = keras.layers.Concatenate(1).Apply(new Tensors([bidAction, state]));
            var bidding = keras.layers.Dense(256, keras.activations.Relu).Apply(biddingInput);
            bidding = keras.layers.Dense(256, keras.activations.Relu).Apply(bidding);
            bidding = keras.layers.Dense(256, keras.activations.Relu).Apply(bidding);
            bidding = keras.layers.Dense(1, keras.activations.Sigmoid).Apply(bidding);
            
            var input = new Tensors([nextAction, bidAction, state, moveHistory]);
            Console.WriteLine(string.Join(", ", input[0].shape.dims));
            Console.WriteLine(string.Join(", ", input[1].shape.dims));
            Console.WriteLine(string.Join(", ", input[2].shape.dims));
            Console.WriteLine(string.Join(", ", input[3].shape.dims));
            var output = new Tensors([playing, bidding]);
            Model = keras.Model(input, output);
            Model.summary();
            Model.compile(keras.optimizers.RMSprop(), keras.losses.MeanSquaredError());
        }
    }

    public void Learn(Dictionary<Tuple<PlayerState, int>, float> bidding, 
        Dictionary<Tuple<PlayerState, CardGroup?>, float> playing, 
        int scores, bool win)
    {
        var yArr = playing.SelectMany(_ => Enumerable.Repeat((float)scores, 2)).ToArray();
        
        var playingXArr = playing
            .Select(t => StateHelper.PlayToNdArray(t.Key.Item1, t.Key.Item2!.Cards))
            .ToArray();
        Model.fit(playingXArr, yArr, batch_size: playingXArr.Length, epochs: 1000);
        
        var biddingXArr = bidding
            .Select(t => StateHelper.BidToNdArray(t.Key.Item1, t.Key.Item2))
            .ToArray();
        Model.fit(biddingXArr, yArr, batch_size: biddingXArr.Length, epochs: 1000);
    }

    public List<float> PredicateBid(PlayerState state, List<int> bidAction)
    {
        var x = bidAction.Select(b => StateHelper.BidToNdArray(state, b))
            .ToArray();
        
        var resultArr = new List<float>();
        var result = Model.predict(x, batch_size: bidAction.Count).numpy();
        // var res = Model.predict(x[0]);
        
        for (var i = 0; i < bidAction.Count; i++)
        {
            resultArr.Add(result[i * 2]);
        }

        return resultArr;
    }
    
    public List<float> PredicatePlay(PlayerState state, List<CardGroup> playAction)
    {
        var x = playAction.Select(a => StateHelper.PlayToNdArray(state, a.Cards)).ToArray();
        
        var resultArr = new List<float>();
        var result = Model.predict(x, batch_size: playAction.Count).numpy();
        
        for (var i = 0; i < x.Length; i++)
        {
            resultArr.Add(result[i * 2]);
        }

        return resultArr;
    }

    public void Save()
    {
        Model.save(ModelFilePath);
    }
}