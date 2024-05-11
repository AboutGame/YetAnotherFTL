using Tensorflow;
using Tensorflow.NumPy;
using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play;

namespace YetAnotherFTL.Training;

public class StateHelper
{
    public static NDArray BidToNdArray(PlayerState state, int action)
    {
        var a = np.concatenate([np.ones(action), np.zeros(3 - action)]);
        var stateArr = StateToNdArray(state);
        var historyArr = HistoryMoveToNdArray(state);
        return np.concatenate([np.zeros(54), a, stateArr, historyArr]);
    }
    
    public static NDArray PlayToNdArray(PlayerState state, List<CardValues> action)
    {
        var stateArr = StateToNdArray(state);
        var historyArr = HistoryMoveToNdArray(state);
        var actionArr = ActionToNdArray(action);
        return np.concatenate([actionArr, np.zeros(3), stateArr, historyArr]);
    }
    
    public static NDArray ActionToNdArray(List<CardValues> action)
    {
        return CardsToArray(action);
    }
    
    /// <summary>
    /// Get player state tensors.
    /// </summary>
    /// <param name="state">Player state</param>
    /// <returns>An 521 length Tensor of the state.</returns>
    /// <remarks>
    /// 54 * 8 + 20 * 3 + 14 + 3 * 2 + 3 * 3 = 521
    /// handCards (54)
    /// otherCards (54)
    /// holeCards (54)
    /// lastMove (54)
    /// other1LastMove (54)
    /// other2LastMove (54)
    /// other1PutCards (54)
    /// other2PutCards (54)
    /// selfCardCount (20)
    /// other1CardCount (20)
    /// other2CardCount (20)
    /// bombUsed (14)
    /// landlordLocation (3)
    /// selfLocation (3)
    /// playerBidded (3 * 3)
    /// </remarks>
    public static NDArray StateToNdArray(PlayerState state)
    {
        var handCards = CardsToArray(state.HandCards);
        var otherCards = CardsToArray(state.OtherHandCards);
        var holeCards = CardsToArray(state.HoleCards);
        
        var lastMove = CardsToArray(state.LastMove);
        var lastMoveOther1 = CardsToArray(state.Other1.LastMove);
        var lastMoveOther2 = CardsToArray(state.Other2.LastMove);
        
        var puttedCardsOther1 = CardsToArray(state.Other1.PuttedCards);
        var puttedCardsOther2 = CardsToArray(state.Other2.PuttedCards);

        var i = state.HandCards.Count;
        var cardCountSelf = np.concatenate([np.ones(i), np.zeros(20 - i)]);

        i = state.Other1.CardsRemain;
        var cardCountOther1 = np.concatenate([np.ones(i), np.zeros(20 - i)]);
        
        i = state.Other2.CardsRemain;
        var cardCountOther2 = np.concatenate([np.ones(i), np.zeros(20 - i)]);

        var bombUsed = np.zeros(14);
        foreach (var b in state.BombUsed)
        {
            bombUsed[(int)b] = 1;
        }

        NDArray landlordLocation;
        if (state.Other1.IsLandlord)
        {
            landlordLocation = LocationToArray(state.Other1.Location);
        }
        else if (state.Other2.IsLandlord)
        {
            landlordLocation = LocationToArray(state.Other2.Location);
        }
        else if (state.IsLandlord)
        {
            landlordLocation = LocationToArray(state.Location);
        }
        else
        {
            landlordLocation = np.zeros(3);
        }
        
        var selfLocation = LocationToArray(state.Location);

        var playerBidded = new NDArray[3];

        {
            if (state.PlayerBidded.TryGetValue(PlayerLocation.East, out var bid))
            {
                playerBidded[0] = np.concatenate([np.ones(bid), np.zeros(3 - bid)]);
            }
            else
            {
                playerBidded[0] = np.zeros(3);
            }
        }

        {
            if (state.PlayerBidded.TryGetValue(PlayerLocation.South, out var bid))
            {
                playerBidded[1] = np.concatenate([np.ones(bid), np.zeros(3 - bid)]);
            }
            else
            {
                playerBidded[1] = np.zeros(3);
            }
        }
        
        {
            if (state.PlayerBidded.TryGetValue(PlayerLocation.West, out var bid))
            {
                playerBidded[2] = np.concatenate([np.ones(bid), np.zeros(3 - bid)]);
            }
            else
            {
                playerBidded[2] = np.zeros(3);
            }
        }

        var arr = np.concatenate([handCards, otherCards, holeCards, 
            lastMove, lastMoveOther1, lastMoveOther2, 
            puttedCardsOther1, puttedCardsOther2, 
            cardCountSelf, cardCountOther1, cardCountOther2, 
            bombUsed, landlordLocation, selfLocation, np.concatenate(playerBidded)]);

        return arr;
    }

    private static NDArray LocationToArray(PlayerLocation location)
    {
        var loc = location switch
        {
            PlayerLocation.East => 1,
            PlayerLocation.South => 2,
            PlayerLocation.West => 3,
            _ => throw new ArgumentOutOfRangeException()
        };
        return np.concatenate([np.ones(loc), np.zeros(3 - loc)]);
    }

    private static Dictionary<int, int> CreateCardMap()
    {
        var map = new Dictionary<int, int>();
        
        foreach (var i in Enumerable.Range(1, 15))
        {
            map[i] = 0;
        }

        return map;
    }

    private static Dictionary<int, int> ListToMap(List<CardValues> cards)
    {
        var map = CreateCardMap();
        foreach (var c in cards)
        {
            map[(int) c] += 1;
        }

        return map;
    }
    
    private static Dictionary<int, int> MergeMap(Dictionary<int, int> map1, Dictionary<int, int> map2)
    {
        var map = CreateCardMap();
        
        foreach (var (k, v) in map1)
        {
            map[k] += v;
        }
        
        foreach (var (k, v) in map2)
        {
            map[k] += v;
        }

        return map;
    }
    
    private static NDArray CountToArray(int count)
    {
        var result = np.zeros(4);

        for (var i = 0; i < count; i++)
        {
            result[i] = np.ones(1);
        }

        return result;
    }

    private static NDArray MapToArray(Dictionary<int, int> map)
    {
        var arr = np.zeros(54);
        
        for (var i = 1; i <= 13; i++)
        {
            var a = CountToArray(map[i]);
            arr[(i - 1) * 4] = a[0];
            arr[(i - 1) * 4 + 1] = a[1];
            arr[(i - 1) * 4 + 2] = a[2];
            arr[(i - 1) * 4 + 3] = a[3];
        }

        if (map[13] != 0)
        {
            arr[13] = np.ones(1);
        }
            
        if (map[14] != 0)
        {
            arr[14] = np.ones(1);
        }
        
        return arr;
    }

    private static NDArray CardsToArray(List<CardValues> cards)
    {
        return MapToArray(ListToMap(cards));
    }

    /// <summary>
    /// History move to NDArray.
    /// </summary>
    /// <param name="playerState">Player state</param>
    /// <returns>NDArray of 54 * 15 values.</returns>
    public static NDArray HistoryMoveToNdArray(PlayerState playerState)
    {
        var arr = playerState.MoveHistory.Select(CardsToArray).ToArray();
        return np.concatenate(arr);
    }
}