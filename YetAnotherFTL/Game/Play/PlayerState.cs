using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play.Model;

namespace YetAnotherFTL.Game.Play;

public class PlayerState : GameSide
{
    public List<CardValues> HandCards { get; set; } = [];

    public CardGroup? LastPlayGroup { get; set; } = null;

    #region Memory

    public Dictionary<PlayerLocation, int> PlayerBidded { get; set; } = [];

    public List<CardValues> HoleCards { get; set; } = [];
    
    public List<CardValues> BombUsed { get; set; } = [];
    
    public List<CardValues> OtherHandCards { get; set; } = [];
    
    public GameSide Other1 { get; set; } = new();
    
    public GameSide Other2 { get; set; } = new();

    public Queue<List<CardValues>> MoveHistory { get; set; } = new(Enumerable.Repeat(new List<CardValues>(), 15));

    #endregion

    public override PlayerState Duplicate()
    {
        return new PlayerState
        {
            HandCards = [..HandCards], 
            PlayerBidded = new Dictionary<PlayerLocation, int>(PlayerBidded), 
            HoleCards = [..HoleCards], 
            BombUsed = [..BombUsed],
            Other1 = Other1.Duplicate(), 
            Other2 = Other2.Duplicate(), 
            MoveHistory = new Queue<List<CardValues>>(MoveHistory)
        };
    }
}