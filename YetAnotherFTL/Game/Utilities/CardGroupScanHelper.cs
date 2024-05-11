using YetAnotherFTL.Game.Card;
using YetAnotherFTL.Game.Play;

namespace YetAnotherFTL.Game.Utilities;

public class CardGroupScanHelper
{
    public static List<CardGroup> Scan(List<CardValues> cards)
    {
        CardHelper.SortCards(cards);
        
        var cardWithCount = new Dictionary<CardValues, int>();
        foreach (var card in cards)
        {
            cardWithCount.TryAdd(card, 0);
            cardWithCount[card] += 1;
        }
        
        var result = new List<CardGroup>();
        
        result.AddRange(ScanFrenchFries(cardWithCount));    // Rocket

        var singleGroup = ScanNormal(cardWithCount, 1, PlayTypes.Single).ToList();
        var doubleGroup = ScanNormal(cardWithCount, 2, PlayTypes.Double).ToList();
        var tripleGroup = ScanNormal(cardWithCount, 3, PlayTypes.Triple).ToList();
        var quadrupleGroup = ScanNormal(cardWithCount, 4, PlayTypes.Bomb).ToList();
        result.AddRange(singleGroup);       // Single
        result.AddRange(doubleGroup);       // Double
        result.AddRange(tripleGroup);       // Triple
        result.AddRange(quadrupleGroup);    // Bomb
        
        result.AddRange(ScanNormalDescartesProduct(tripleGroup, singleGroup));  // Triple With Single
        result.AddRange(ScanNormalDescartesProduct(tripleGroup, doubleGroup));  // Triple With Double
        
        var singleStraight = ScanStraight(cards, cardWithCount, 1, 5, PlayTypes.Straight).ToList();
        var doubleStraight = ScanStraight(cards, cardWithCount, 2, 3, PlayTypes.StraightDouble).ToList();
        var tripleStraight = ScanStraight(cards, cardWithCount, 3, 2, PlayTypes.StraightTriple).ToList();
        
        result.AddRange(singleStraight);    // Single Sequence
        result.AddRange(doubleStraight);    // Double Sequence
        result.AddRange(tripleStraight);    // Triple Sequence
        
        result.AddRange(ScanStraightDescartesProduct(tripleStraight, singleGroup, PlayTypes.StraightTripleWithExtra));
        result.AddRange(ScanStraightDescartesProduct(tripleStraight, doubleGroup, PlayTypes.StraightTripleWithExtra));
        result.AddRange(ScanNormalDescartesProduct(quadrupleGroup, doubleGroup));

        return result;
    }
    
    private static IEnumerable<CardGroup> ScanFrenchFries(Dictionary<CardValues, int> cardWithCount)
    {
        if (cardWithCount.Keys.Any(c => c is CardValues.CardJokerBlack) 
            && cardWithCount.Keys.Any(c => c is CardValues.CardJokerRed))
        {
            return [new CardGroup(PlayTypes.FrenchFries, [CardValues.CardJokerBlack, CardValues.CardJokerRed])];
        }

        return [];
    }

    private static IEnumerable<NormalCardGroup> ScanNormal(Dictionary<CardValues, int> cardWithCount, 
        int cell, PlayTypes type)
    {
        return cardWithCount.Where(entry => entry.Value >= cell)
            .SelectMany(entry =>
                Enumerable.Repeat(new NormalCardGroup(type, entry.Key, Enumerable.Repeat(entry.Key, cell).ToList()),
                    entry.Value / cell));
    }
    
    private static IEnumerable<ExtraCardGroup> ScanNormalDescartesProduct(IEnumerable<NormalCardGroup> main,
        IEnumerable<NormalCardGroup> joins)
    {
        return from m in main
            from j in joins
            where m.Value != j.Value
            select new ExtraCardGroup(PlayTypes.TripleWithExtra, m, [j], [..m.Cards, ..j.Cards]);
    }

    private static IEnumerable<StraightCardGroup> ScanStraight(List<CardValues> cards, 
        Dictionary<CardValues, int> cardWithCount, int cell, int minContinuous, PlayTypes type)
    {
        var result = new List<StraightCardGroup>();

        for (var i = 0; i < cards.Count - 5; i++)
        {
            CardValues? max = null;
            var continuous = 0;
            List<CardValues> cardGroup = [];

            for (var j = i; j < i + 5; j++)
            {
                if (j >= (int) CardValues.Card2)
                {
                    break;
                }
                
                if (cardWithCount[(CardValues) (j + 1)] >= cell)
                {
                    continuous += 1;

                    var thisCard = (CardValues) (j + 1);
                    if (max == null || max < thisCard)
                    {
                        max = thisCard;
                    }
                    
                    cardGroup.Add(thisCard);
                }
            }

            if (continuous >= minContinuous)
            {
                var group = new StraightCardGroup(type, continuous, max!.Value, cardGroup);
                result.Add(group);
            }
        }
        
        return result;
    }

    private static IEnumerable<ExtraCardGroup> ScanStraightDescartesProduct(IEnumerable<StraightCardGroup> main,
        IEnumerable<CardGroup> joins, PlayTypes types)
    {
        return main.SelectMany(m => 
                Enumerable.Repeat(joins, m.Length)
                    .Combinations()
                    .Select(g => new { g, m }))
            .Where(entry => !entry.m.Cards.Any(v => entry.g.Any(g => g.Cards.Contains(v))))
            .Select(entry => new ExtraCardGroup(types, entry.m, entry.g.ToList(),
                [..entry.m.Cards, ..entry.g.SelectMany(j => j.Cards)]));
    }

    public static CardGroup? MatchCardGroupExactly(List<CardValues> cards)
    {
        CardHelper.SortCards(cards);
        
        if (cards.Count == 0)
        {
            return new CardGroup(PlayTypes.Pass, [..cards]);
        }

        if (cards.Count == 2
            && cards.IndexOf(CardValues.CardJokerRed) != -1 
            && cards.IndexOf(CardValues.CardJokerBlack) != -1)
        {
            return new CardGroup(PlayTypes.FrenchFries, [..cards]);
        }

        if (cards.Count == 4 
            && cards[0] == cards[1]
            && cards[0] == cards[2] 
            && cards[0] == cards[3])
        {
            return new CardGroup(PlayTypes.Bomb, [..cards]);
        }
        
        var cardWithCount = new Dictionary<CardValues, int>();
        foreach (var card in cards)
        {
            cardWithCount.TryAdd(card, 0);
            cardWithCount[card] += 1;
        }

        if (cardWithCount.Count == 1)
        {
            var entry = cardWithCount.Take(1).First();
            if (entry.Value == 1)
            {
                return new NormalCardGroup(PlayTypes.Single, entry.Key, [..cards]);
            }
            else if (entry.Value == 2)
            {
                return new NormalCardGroup(PlayTypes.Double, entry.Key, [..cards]);
            }
            else if (entry.Value == 3)
            {
                return new NormalCardGroup(PlayTypes.Triple, entry.Key, [..cards]);
            }
        }

        if (cardWithCount.Count == 2)
        {
            var entry = cardWithCount.Take(1).First();
            var entry2 = cardWithCount.Skip(1).Take(1).First();
            var main = entry.Value > entry2.Value ? entry : entry2;
            var other = entry.Value > entry2.Value ? entry2 : entry;
            
            if (main.Value == 3)
            {
                if (other.Value == 1)
                {
                    return new ExtraCardGroup(PlayTypes.TripleWithExtra,
                        new NormalCardGroup(PlayTypes.Triple, main.Key, [main.Key, main.Key, main.Key]),
                        [new NormalCardGroup(PlayTypes.Single, other.Key, [other.Key])], [..cards]);
                }
                else if (other.Value == 2)
                {
                    return new ExtraCardGroup(PlayTypes.TripleWithExtra,
                        new NormalCardGroup(PlayTypes.Triple, main.Key, [main.Key, main.Key, main.Key]),
                        [new NormalCardGroup(PlayTypes.Double, other.Key, [other.Key, other.Key])], [..cards]);
                }
            }
        }

        var hasStraightButExtra = false;
        var straightType = PlayTypes.Straight;
        var straightLast = CardValues.Card3;
        var straightLength = 0;

        if (cardWithCount.Count > 5)
        {
            var straightEnd = CardValues.Card3;
            var straightLen = 0;
            
            for (var i = 1; i < 13; i++)
            {
                var v = (CardValues)i;
                
                if (cardWithCount.ContainsKey(v) && cardWithCount[v] == 1)
                {
                    straightLen += 1;
                }
                else
                {
                    if (straightLen < 5)
                    {
                        straightLen = 0;
                    }
                    else
                    {
                        hasStraightButExtra = true;
                        straightLast = straightEnd;
                        straightType = PlayTypes.Straight;
                        straightLength = straightLen;
                    }
                }
                
                straightEnd = v;
            }

            if (straightLen >= 5 && !hasStraightButExtra)
            {
                return new StraightCardGroup(PlayTypes.Straight, straightLen, straightEnd, [..cards]);
            }
        }
        
        if (cardWithCount.Count > 3)
        {
            var straightEnd = CardValues.Card3;
            var straightLen = 0;
            
            for (var i = 1; i < 13; i++)
            {
                var v = (CardValues)i;
                
                if (cardWithCount.ContainsKey(v) && cardWithCount[v] == 2)
                {
                    straightLen += 1;
                }
                else
                {
                    if (straightLen < 3)
                    {
                        straightLen = 0;
                    }
                    else
                    {
                        hasStraightButExtra = true;
                        straightLast = straightEnd;
                        straightType = PlayTypes.StraightDouble;
                        straightLength = straightLen;
                    }
                }
                
                straightEnd = v;
            }

            if (straightLen >= 3 && !hasStraightButExtra)
            {
                return new StraightCardGroup(PlayTypes.StraightDouble, straightLen, straightEnd, [..cards]);
            }
        }
        
        if (cardWithCount.Count > 2)
        {
            var straightEnd = CardValues.Card3;
            var straightLen = 0;
            
            for (var i = 1; i < 13; i++)
            {
                var v = (CardValues)i;
                
                if (cardWithCount.ContainsKey(v) && cardWithCount[v] == 3)
                {
                    straightLen += 1;
                }
                else
                {
                    if (straightLen < 2)
                    {
                        straightLen = 0;
                    }
                    else
                    {
                        hasStraightButExtra = true;
                        straightLast = straightEnd;
                        straightType = PlayTypes.StraightTriple;
                        straightLength = straightLen;
                    }
                }
                
                straightEnd = v;
            }

            if (straightLen >= 2 && !hasStraightButExtra)
            {
                return new StraightCardGroup(PlayTypes.StraightTriple, straightLen, straightEnd, [..cards]);
            }
        }

        if (hasStraightButExtra)
        {
            var vc = -1;
            var vl = new List<CardValues>();
            for (var i = 1; i < 15; i++)
            {
                var v = (CardValues)i;

                if (v <= straightLast && v > (straightLast - straightLength))
                {
                    continue;
                }

                if (cardWithCount.ContainsKey(v))
                {
                    if (vc == -1)
                    {
                        vc = cardWithCount[v];
                    }

                    if (vc != cardWithCount[v])
                    {
                        break;
                    }

                    vl.Add(v);
                }
            }

            if (vl.Count == straightLength)
            {
                var c = new List<CardValues>();
                for (var i = 0; i < straightLength; i++)
                {
                    var v = straightLast - i;
                    c.AddRange(Enumerable.Repeat(v, straightType switch
                    {
                        PlayTypes.Straight => 1,
                        PlayTypes.StraightDouble => 2, 
                        PlayTypes.StraightTripleWithExtra => 3, 
                        _ => 0
                    }));
                }

                return new ExtraCardGroup(PlayTypes.StraightTripleWithExtra,
                    new StraightCardGroup(straightType, straightLength, straightLast, c),
                    vl.Select(v => (CardGroup) new NormalCardGroup(vc switch
                    {
                        1 => PlayTypes.Single,
                        2 => PlayTypes.Double,
                        3 => PlayTypes.Triple,
                        _ => throw new ArgumentOutOfRangeException(nameof(vc))
                    }, v, [..Enumerable.Repeat(v, vc)])).ToList(), [..cards]);
            }
        }

        if (cardWithCount.Count == 3)
        {
            var entry1 = cardWithCount.Take(1).First();
            var entry2 = cardWithCount.Skip(1).Take(1).First();
            var entry3 = cardWithCount.Skip(2).Take(1).First();

            if (entry1.Value != 4 && entry2.Value != 4 && entry3.Value != 4)
            {
                return null;
            }

            var four = entry1.Value == 4 ? entry1 : entry2.Value == 4 ? entry2 : entry3;
            var other1 = entry1.Value == 4 ? entry2 : entry2.Value == 4 ? entry1 : entry2;
            var other2 = entry1.Value == 4 ? entry3 : entry2.Value == 4 ? entry3 : entry1;

            if (other1.Value == other2.Value && (other1.Value is 1 or 2))
            {
                return new ExtraCardGroup(PlayTypes.QuadrupleWithDouble,
                    new NormalCardGroup(PlayTypes.Bomb, four.Key, [..Enumerable.Repeat(four.Key, 4)]),
                    [
                        new NormalCardGroup(other1.Value switch
                        {
                            1 => PlayTypes.Single,
                            2 => PlayTypes.Double,
                            _ => throw new ArgumentOutOfRangeException(nameof(other1))
                        }, other1.Key, [..Enumerable.Repeat(other1.Key, other1.Value)]),
                        new NormalCardGroup(other2.Value switch
                        {
                            1 => PlayTypes.Single,
                            2 => PlayTypes.Double,
                            _ => throw new ArgumentOutOfRangeException(nameof(other2))
                        }, other2.Key, [..Enumerable.Repeat(other2.Key, other2.Value)]),
                    ], [..cards]);
            }
        }

        return null;
    }
}