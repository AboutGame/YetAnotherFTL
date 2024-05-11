using YetAnotherFTL.Game.Card;

namespace YetAnotherFTL.Game.Play.Model;

public class GameSide
{
    public bool IsLandlord { get; set; }
    
    public PlayerLocation Location { get; set; }

    public List<CardValues> LastMove { get; set; } = [];
    
    public List<CardValues> PuttedCards { get; set; } = [];
        
    public int CardsRemain { get; set; }
    
    public virtual GameSide Duplicate()
    {
        return new GameSide
        {
            IsLandlord = IsLandlord, 
            Location = Location, 
            LastMove = [..LastMove], 
            PuttedCards = [..PuttedCards],
            CardsRemain = CardsRemain
        };
    }
}