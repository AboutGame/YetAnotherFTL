using YetAnotherFTL.Simulator;
using YetAnotherFTL.Training;

var model = new PlayerModel("ftl_model");
// model.Save();

var player1 = new FTLPlayer(model, true);
var player2 = new FTLPlayer(model, true);
var player3 = new FTLPlayer(model, true);

var simulator = new FTLSimulator(player1, player2, player3, true);

var exit = false;

Console.CancelKeyPress += (sender, e) =>
{
    model.Save();
    exit = true;
};

while (!exit)
{
    switch (simulator.State)
    {
        case FTLSimulator.GameState.Ended:
            simulator.Reset();
            break;
        case FTLSimulator.GameState.Ready:
            simulator.Bid();
            break;
        case FTLSimulator.GameState.Bidding:
            break;
        case FTLSimulator.GameState.Playing:
            simulator.Play();
            break;
        default:
            throw new ArgumentOutOfRangeException();
    }
}
