using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play;
using YetAnotherFTL.Game.Play.Model;
using YetAnotherFTL.Game.Utilities;
using YetAnotherFTL.Training;

namespace YetAnotherFTL.Simulator;

public class FTLPlayer
{
    private Random Random { get; } = new();
    private bool Training { get; } = false;
    
    private PlayerModel Model { get; }
    private FTLSimulator Simulator { get; set; }
    
    private PlayerState Memory { get; set; }
    
    private Dictionary<Tuple<PlayerState, int>, float> CacheBiddingData { get; set; } = [];
    private Dictionary<Tuple<PlayerState, CardGroup?>, float> CachePlayData { get; set; } = [];
    
    public FTLPlayer(PlayerModel model, bool training)
    {
        Model = model;
        Training = training;
    }

    public void SetSimulator(FTLSimulator simulator)
    {
        Simulator = simulator;
    }
    
    public void OnDeal(PlayerLocation location, List<CardValues> cards)
    {
        Memory = new PlayerState
        {
            HandCards = cards, 
            Location = location, 
            CardsRemain = cards.Count,
            Other1 = new GameSide
            {
                Location = location.PrevLocation(),
                CardsRemain = 17
            },
            Other2 = new GameSide
            {
                Location = location.NextLocation(),
                CardsRemain = 17
            },
            OtherHandCards = CardHelper.AllCardsExpect(cards)
        };
        
        CacheBiddingData.Clear();
        CachePlayData.Clear();
    }

    public void OnBid(PlayerLocation location, int value)
    {
        Memory.PlayerBidded[location] = value;
    }
    
    public void OnBidden(PlayerLocation landlord, List<CardValues> holeCards)
    {
        Memory.HoleCards = holeCards;
        
        if (landlord == Memory.Location)
        {
            Memory.IsLandlord = true;
            Memory.CardsRemain = 20;
            Memory.HandCards.AddRange(holeCards);
            CardHelper.SortCards(Memory.HandCards);
        }
        else if (landlord == Memory.Other1.Location)
        {
            Memory.Other1.IsLandlord = true;
            Memory.Other1.CardsRemain = 20;
            Memory.OtherHandCards.AddRange(holeCards);
            CardHelper.SortCards(Memory.OtherHandCards);
        }
        else if (landlord == Memory.Other2.Location)
        {
            Memory.Other2.IsLandlord = true;
            Memory.Other2.CardsRemain = 20;
            Memory.OtherHandCards.AddRange(holeCards);
            CardHelper.SortCards(Memory.OtherHandCards);
        }
    }
    
    public void OnPut(PlayerLocation location, CardGroup? play)
    {
        if (play is null)
        {
            return;
        }
        
        while (Memory.MoveHistory.Count >= 15)
        {
            Memory.MoveHistory.Dequeue();
        }
        Memory.MoveHistory.Enqueue(play.Cards);

        Memory.LastPlayGroup = play;
        if (location == Memory.Location)
        {
            Memory.LastMove = play.Cards;
            CardHelper.RemoveCards(Memory.HandCards, play.Cards);
            Memory.CardsRemain -= play.Cards.Count;
        }
        else if (location == Memory.Other1.Location)
        {
            Memory.Other1.LastMove = play.Cards;
            Memory.Other1.PuttedCards.AddRange(play.Cards);
            Memory.Other1.CardsRemain -= play.Cards.Count;
            CardHelper.RemoveCards(Memory.OtherHandCards, play.Cards);
        }
        else if (location == Memory.Other2.Location)
        {
            Memory.Other2.LastMove = play.Cards;
            Memory.Other2.PuttedCards.AddRange(play.Cards);
            Memory.Other2.CardsRemain -= play.Cards.Count;
            CardHelper.RemoveCards(Memory.OtherHandCards, play.Cards);
        }
    }
    
    public async void OnEnded(PlayerLocation winner, int scores)
    {
        await Task.Run(() =>
        {
            Model.Learn(CacheBiddingData, CachePlayData, scores, winner == Memory.Location);
        });
    }

    public int QueryBid()
    {
        List<int> action = [0, 1, 2, 3];
        
        foreach (var (k, v) in Memory.PlayerBidded)
        {
            switch (v)
            {
                case 0:
                    break;
                case 1:
                    action.RemoveAt(1);
                    break;
                case 2:
                    action.RemoveAt(2);
                    action.RemoveAt(1);
                    break;
                case 3:
                    action.RemoveAt(3);
                    action.RemoveAt(2);
                    action.RemoveAt(1);
                    break;
            }
        }

        var result = Model.PredicateBid(Memory, action);
        
        if (Training)
        {
            var i = Random.Next(0, action.Count);
            CacheBiddingData.Add(new Tuple<PlayerState, int>(Memory.Duplicate(), action[i]), result[i]);
            
            return action[i];
        }

        var maxIndex = result.IndexOf(result.Max());
        return action[maxIndex];
    }

    public CardGroup QueryPlay(bool freeOut)
    {
        var action = CardGroupScanHelper.Scan(Memory.HandCards)
            .Where(g => Memory.LastPlayGroup == null || g > Memory.LastPlayGroup)
            .ToList();

        if (!freeOut)
        {
            action.Add(new CardGroup(PlayTypes.Pass, []));
        }

        var result = Model.PredicatePlay(Memory, action);
        
        if (Training)
        {
            var i = Random.Next(0, action.Count);
            CachePlayData.Add(new Tuple<PlayerState, CardGroup?>(Memory.Duplicate(), action[i]), result[i]);
            
            return action[i];
        }

        var maxIndex = result.IndexOf(result.Max());
        return action[maxIndex];
    }
}