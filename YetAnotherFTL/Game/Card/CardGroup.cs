using System.Diagnostics;
using YetAnotherFTL.Game.Play;
using static YetAnotherFTL.Game.Play.PlayTypes;

namespace YetAnotherFTL.Game.Card;

public record CardGroup(PlayTypes Type, List<CardValues> Cards)
{
    public static bool operator >(CardGroup a, CardGroup b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        if (a.Type == b.Type && a.Cards.SequenceEqual(b.Cards))
        {
            return false;
        }

        return !(a < b);
    }

    public static bool operator <(CardGroup a, CardGroup b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        
        if (a.Type is Pass && b.Type is not Pass)
        {
            return true;
        }

        if (a.Type == b.Type && a.Cards.Count == b.Cards.Count)
        {
            if (a.Type is PlayTypes.Single or PlayTypes.Double or Triple or Bomb)
            {
                var nA = a as NormalCardGroup;
                var nB = b as NormalCardGroup;
                Debug.Assert(nA != null, $"{nameof(nA)} != null");
                Debug.Assert(nB != null, $"{nameof(nB)} != null");
                
                return nA.Value < nB.Value;
            }

            if (a.Type is Straight or StraightDouble or StraightTriple)
            {
                var sA = a as StraightCardGroup;
                var sB = b as StraightCardGroup;
                Debug.Assert(sA != null, $"{nameof(sA)} != null");
                Debug.Assert(sB != null, $"{nameof(sB)} != null");
                Debug.Assert(sA.Length == sB.Length, $"{nameof(sA)}.Length == {nameof(sB)}.Length");

                return sA.MaxValue < sB.MaxValue;
            }
            
            if (a.Type is TripleWithExtra or StraightTripleWithExtra or QuadrupleWithDouble)
            {
                var eA = a as ExtraCardGroup;
                var eB = b as ExtraCardGroup;
                Debug.Assert(eA != null, $"{nameof(eA)} != null");
                Debug.Assert(eB != null, $"{nameof(eB)} != null");
                Debug.Assert(eA.ExtraGroup.Count == eB.ExtraGroup.Count, 
                    $"{nameof(eA)}.ExtraGroup.Count == {nameof(eB)}.ExtraGroup.Count");

                return eA.MainGroup < eB.MainGroup;
            }

            return false;
        }

        if (b.Type is Bomb && a.Type is not (Bomb or FrenchFries))
        {
            return true;
        }

        if (a.Type is FrenchFries)
        {
            return true;
        }

        return false;
    }

    public virtual CardGroup Duplicate()
    {
        return new CardGroup(Type, [..Cards]);
    }
}

public record NormalCardGroup(PlayTypes Type, CardValues Value, List<CardValues> Cards)
    : CardGroup(Type, Cards)
{
    public override CardGroup Duplicate()
    {
        return new NormalCardGroup(Type, Value, [..Cards]);
    }
}

public record StraightCardGroup(PlayTypes Type, int Length, CardValues MaxValue, List<CardValues> Cards)
    : CardGroup(Type, Cards)
{
    public override CardGroup Duplicate()
    {
        return new StraightCardGroup(Type, Length, MaxValue, [..Cards]);
    }
}

public record ExtraCardGroup(PlayTypes Type, CardGroup MainGroup, List<CardGroup> ExtraGroup, List<CardValues> Cards)
    : CardGroup(Type, Cards)
{
    public override CardGroup Duplicate()
    {
        var list = ExtraGroup.Select(g => g.Duplicate()).ToList();
        return new ExtraCardGroup(Type, MainGroup.Duplicate(), list, [..Cards]);
    }
}
