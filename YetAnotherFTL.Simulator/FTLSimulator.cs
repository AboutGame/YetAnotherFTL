using Microsoft.VisualBasic;
using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play;
using YetAnotherFTL.Game.Utilities;

namespace YetAnotherFTL.Simulator;

public class FTLSimulator
{
    // public delegate void Deal(PlayerLocation location, List<CardValues> cards);
    // public delegate void Bidden(PlayerLocation landlord, List<CardValues> holeCards);
    // public delegate void Put(PlayerLocation side, List<CardValues> cards);
    // public delegate void Ended(PlayerLocation winner, int scores);
    //
    // public delegate int QueryBid();
    // public delegate List<CardValues> QueryPut();
    //
    // public event Deal OnDeal;
    // public event Bidden OnBidden;
    // public event Put OnPut;
    // public event Ended OnEnded;
    //
    // public event QueryBid OnQueryBid;
    // public event QueryPut OnQueryPut;

    private bool Logging { get; } = false;
    
    protected Random Random { get; } = new();

    public int Rounds { get; private set; } = 0;
    // public int Turns { get; private set; } = 0;

    public GameState State { get; private set; } = GameState.Ready;
    
    public Dictionary<PlayerLocation, FTLPlayer> Players { get; } = new();
    public Dictionary<PlayerLocation, List<CardValues>> HandCards { get; } = new();
    public List<CardValues> HoleCards { get; private set; } = new(3);
    
    public Dictionary<PlayerLocation, int> Bidding { get; private set; } = new();
    
    public FreeOutState CanFreeOut { get; private set; } = FreeOutState.Yes;
    public CardGroup? LastPut { get; private set; } = null;
    public Dictionary<PlayerLocation, List<CardGroup>> MoveHistory { get; private set; } = new();

    public PlayerLocation? Landlord { get; private set; } = null;
    
    public PlayerLocation NowPlayer { get; private set; } = PlayerLocation.East;

    public int Multiplier { get; private set; } = 1;
    public int BombUsed { get; private set; } = 0;
    public int RocketUsed { get; private set; } = 0;
    
    public FTLSimulator(FTLPlayer player1, FTLPlayer player2, FTLPlayer player3, bool logging = false)
    {
        Players[PlayerLocation.East] = player1;
        Players[PlayerLocation.South] = player2;
        Players[PlayerLocation.West] = player3;
        
        player1.SetSimulator(this);
        player2.SetSimulator(this);
        player3.SetSimulator(this);

        Logging = logging;
        
        Reset();
    }

    public void Reset()
    {
        Rounds += 1;
        State = GameState.Ready;
        
        var player1 = Players[PlayerLocation.East];
        var player2 = Players[PlayerLocation.South];
        var player3 = Players[PlayerLocation.West];
        Players[PlayerLocation.East] = player2;
        Players[PlayerLocation.South] = player3;
        Players[PlayerLocation.West] = player1;
        
        HandCards.Clear();
        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            HandCards.Add(location, []);
        }
        
        Bidding.Clear();
        
        
        HoleCards.Clear();
        
        LastPut = null;
        CanFreeOut = FreeOutState.Yes;
        MoveHistory.Clear();
        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            MoveHistory.Add(location, []);
        }

        Landlord = null;
        NowPlayer = PlayerLocation.East;

        Multiplier = 1;
        BombUsed = 0;
        RocketUsed = 0;

        SpreadCards();
        
        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            Players[location].OnDeal(location, HandCards[location]);
        }

        if (Logging)
        {
            Console.WriteLine($"[R-{Rounds}] Shuffle and spread.");
            Console.WriteLine($"[R-{Rounds}] East {string.Join(", ", HandCards[PlayerLocation.East])}");
            Console.WriteLine($"[R-{Rounds}] South {string.Join(", ", HandCards[PlayerLocation.South])}");
            Console.WriteLine($"[R-{Rounds}] West {string.Join(", ", HandCards[PlayerLocation.West])}");
        }
    }

    private void SpreadCards()
    {
        var cards = new List<CardValues>();

        for (var i = 1; i <= 13; i++)
        {
            var c = (CardValues)i;
            cards.AddRange(Enumerable.Repeat(c, 4));
        }
        
        cards.Add(CardValues.CardJokerRed);
        cards.Add(CardValues.CardJokerBlack);

        var arr = cards.ToArray();
        Random.Shuffle(arr);
        
        HandCards[PlayerLocation.East].AddRange(arr[..17]);
        HandCards[PlayerLocation.South].AddRange(arr[17..34]);
        HandCards[PlayerLocation.West].AddRange(arr[34..51]);
        HoleCards.AddRange(arr[51..53]);
        
        CardHelper.SortCards(HandCards[PlayerLocation.East]);
        CardHelper.SortCards(HandCards[PlayerLocation.South]);
        CardHelper.SortCards(HandCards[PlayerLocation.West]);
        CardHelper.SortCards(HoleCards);
    }

    public void Bid()
    {
        State = GameState.Bidding;
        
        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            var bid = Players[location].QueryBid();
            CheckBid(location, bid);
            Bidding[location] = bid;
            
            if (Logging)
            {
                Console.WriteLine($"[R-{Rounds}] Player {location} bidded {bid}.");
            }

            if (bid == 3)
            {
                PickLandlord(location);
                break;
            }
        }
        
        var bid1 = Bidding[PlayerLocation.East];
        var bid2 = Bidding[PlayerLocation.South];
        var bid3 = Bidding[PlayerLocation.West];
        
        if (bid1 == 0 && bid2 == 0 && bid3 == 0)
        {
            throw new InvalidOperationException();
        }

        if (bid1 != 0 && bid2 == 0 && bid3 == 0)
        {
            PickLandlord(PlayerLocation.East);
            return;
        }
        
        if (bid1 != 0 && bid2 != 0 && bid3 == 0)
        {
            PickLandlord(PlayerLocation.South);
            return;
        }
        
        if (bid1 != 0 && bid2 != 0 && bid3 != 0)
        {
            PickLandlord(PlayerLocation.West);
        }
    }

    private void CheckBid(PlayerLocation location, int value)
    {
        NowPlayer = location;
        
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 3);

        if (value != 0 && value <= Bidding[location.PrevLocation()])
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        if (Bidding.Count > 3)
        {
            throw new InvalidOperationException();
        }
    }

    private void PickLandlord(PlayerLocation landlord)
    {
        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            Players[location].OnBidden(landlord, HoleCards);
        }
        
        HandCards[landlord].AddRange(HoleCards);
        CardHelper.SortCards(HandCards[landlord]);

        Landlord = landlord;
        NowPlayer = landlord;

        State = GameState.Playing;
        
        if (Logging)
        {
            Console.WriteLine($"[R-{Rounds}] Picked {Landlord} is landlord, leftover {string.Join(", ", HoleCards)}.");
        }
    }

    private void NextRound()
    {
        NowPlayer = NowPlayer.NextLocation();
    }

    public PlayResult Play()
    {
        var play = Players[NowPlayer].QueryPlay(CanFreeOut == FreeOutState.Yes);
        
        if (Logging)
        {
            Console.WriteLine($"[R-{Rounds}] Player {NowPlayer} played {play.Type} {string.Join(", ", play.Cards)}.");
        }

        if (play.Type is PlayTypes.Pass && CanFreeOut == FreeOutState.Yes)
        {
            throw new InvalidOperationException();
        }

        if (play.Type is PlayTypes.Pass)
        {
            if (CanFreeOut == FreeOutState.Cant)
            {
                CanFreeOut = FreeOutState.Cannt;
            }
            else if (CanFreeOut == FreeOutState.Cannt)
            {
                CanFreeOut = FreeOutState.Yes;
            }
            
            NextRound();
            return PlayResult.Next;
        }

        if (LastPut is not null && LastPut > play)
        {
            throw new ArgumentOutOfRangeException(nameof(play));
        }
        
        CardHelper.RemoveCards(HandCards[NowPlayer], play.Cards);
        
        MoveHistory[NowPlayer].Add(play);

        if (play.Type is PlayTypes.Bomb)
        {
            BombUsed += 1;
        } 
        else if (play.Type == PlayTypes.FrenchFries)
        {
            RocketUsed += 1;
        }

        foreach (var location in Enum.GetValues<PlayerLocation>())
        {
            Players[location].OnPut(NowPlayer, play);
        }
       
        LastPut = play;

        if (HandCards.Any(entry => entry.Value.Count == 0))
        {
            EndGame();
            return NowPlayer switch
            {
                PlayerLocation.East => PlayResult.EastWin,
                PlayerLocation.South => PlayResult.SouthWin,
                PlayerLocation.West => PlayResult.WestWin,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
        
        NextRound();
        return PlayResult.Next;
    }

    private void EndGame()
    {
        State = GameState.Ended;

        var landlordWin = NowPlayer == Landlord;

        if (BombUsed > 0)
        {
            Multiplier = 1 * (1 + BombUsed);
        }

        if (RocketUsed > 0)
        {
            Multiplier *= 2;
        }

        if (landlordWin)
        {
            if (MoveHistory[Landlord!.Value.PrevLocation()].Count == 0 
                && MoveHistory[Landlord!.Value.NextLocation()].Count == 0)
            {
                Multiplier *= 2;
            }
        }
        else
        {
            if (MoveHistory[Landlord!.Value].Count == 0)
            {
                Multiplier *= 2;
            }
        }
        
        var landlordScore = Math.Max(2 * (landlordWin ? 1 : -1)  * 100 * (Bidding[Landlord!.Value] == 0 ? 1 : Bidding[Landlord!.Value]) * Multiplier, 2100);
        var farmerPrevScore = Math.Max((landlordWin ? -1 : 1) * 100 * (Bidding[Landlord!.Value.PrevLocation()] == 0 ? 1 : Bidding[Landlord!.Value.PrevLocation()]) * Multiplier, 2100);
        var farmerNextScore = Math.Max((landlordWin ? -1 : 1) * 100 * (Bidding[Landlord!.Value.NextLocation()] == 0 ? 1 : Bidding[Landlord!.Value.NextLocation()]) * Multiplier, 2100);

        if (Logging)
        {
            Console.WriteLine($"[R-{Rounds}] Game ended, {NowPlayer} wins, {farmerPrevScore}/{landlordScore}/{farmerNextScore}.");
        }
        
        Players[Landlord!.Value].OnEnded(NowPlayer, landlordScore);
        Players[Landlord!.Value.PrevLocation()].OnEnded(NowPlayer, farmerPrevScore);
        Players[Landlord!.Value.NextLocation()].OnEnded(NowPlayer, farmerNextScore);
    }

    public enum PlayResult
    {
        Next,
        EastWin,
        SouthWin,
        WestWin
    }
    
    public enum GameState
    {
        Ready,
        Bidding,
        Playing,
        Ended
    }
    
    public enum FreeOutState
    {
        Yes, 
        Cant, 
        Cannt
    }
}