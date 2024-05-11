using YetAnotherFTL.Game.Play;

namespace YetAnotherFTL.Game.Utilities;

public static class PlayerLocationExtension
{
    public static PlayerLocation NextLocation(this PlayerLocation location)
    {
        return location switch
        {
            PlayerLocation.East => PlayerLocation.South,
            PlayerLocation.South => PlayerLocation.West,
            PlayerLocation.West => PlayerLocation.East,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
    }
    
    public static PlayerLocation PrevLocation(this PlayerLocation location)
    {
        return location switch
        {
            PlayerLocation.East => PlayerLocation.West,
            PlayerLocation.South => PlayerLocation.East,
            PlayerLocation.West => PlayerLocation.South,
            _ => throw new ArgumentOutOfRangeException(nameof(location), location, null)
        };
    }
}