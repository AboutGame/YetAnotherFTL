namespace YetAnotherFTL.Training.Messenger;

public class TrainMessageModel
{
    public enum Command
    {
        Register,
        Info,
        Deal,
        Bid,
        QueryBid,
        Leftover,
        Play,
        QueryPlay,
        GameOver
    }
}