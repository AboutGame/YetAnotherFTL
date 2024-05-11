using YetAnotherFTL.Game.Card;
using static YetAnotherFTL.Game.Card.CardValues;

namespace YetAnotherFTL.Game.Utilities;

public class CardHelper
{
    public static CardValues ToCardValue(Cards cards)
    {
        return cards switch
        {
            Cards.Heart3 => Card3,
            Cards.Diamond3 => Card3,
            Cards.Spade3 => Card3,
            Cards.Club3 => Card3,
            Cards.Heart4 => Card4,
            Cards.Diamond4 => Card4,
            Cards.Spade4 => Card4,
            Cards.Club4 => Card4,
            Cards.Heart5 => Card5,
            Cards.Diamond5 => Card5,
            Cards.Spade5 => Card5,
            Cards.Club5 => Card5,
            Cards.Heart6 => Card6,
            Cards.Diamond6 => Card6,
            Cards.Spade6 => Card6,
            Cards.Club6 => Card6,
            Cards.Heart7 => Card7,
            Cards.Diamond7 => Card7,
            Cards.Spade7 => Card7,
            Cards.Club7 => Card7,
            Cards.Heart8 => Card8,
            Cards.Diamond8 => Card8,
            Cards.Spade8 => Card8,
            Cards.Club8 => Card8,
            Cards.Heart9 => Card9,
            Cards.Diamond9 => Card9,
            Cards.Spade9 => Card9,
            Cards.Club9 => Card9,
            Cards.Heart10 => Card10,
            Cards.Diamond10 => Card10,
            Cards.Spade10 => Card10,
            Cards.Club10 => Card10,
            Cards.HeartJack => CardJack,
            Cards.DiamondJack => CardJack,
            Cards.SpadeJack => CardJack,
            Cards.ClubJack => CardJack,
            Cards.HeartQueen => CardQueen,
            Cards.DiamondQueen => CardQueen,
            Cards.SpadeQueen => CardQueen,
            Cards.ClubQueen => CardQueen,
            Cards.HeartKing => CardKing,
            Cards.DiamondKing => CardKing,
            Cards.SpadeKing => CardKing,
            Cards.ClubKing => CardKing,
            Cards.HeartA => CardAce,
            Cards.DiamondA => CardAce,
            Cards.SpadeA => CardAce,
            Cards.ClubA => CardAce,
            Cards.Heart2 => Card2,
            Cards.Diamond2 => Card2,
            Cards.Spade2 => Card2,
            Cards.Club2 => Card2,
            Cards.JokerBlack => CardJokerBlack,
            Cards.JokerRed => CardJokerRed,
            _ => throw new ArgumentOutOfRangeException(nameof(cards), cards, null)
        };
    }

    public static void SortCards(List<CardValues> cards)
    {
        cards.Sort((a, b) => (int)a - (int)b);
    }

    public static void RemoveCards(List<CardValues> cards, List<CardValues> toRemove)
    {
        foreach (var c in toRemove)
        {
            var index = cards.IndexOf(c);
            if (index != -1)
            {
                cards.RemoveAt(index);
            }
        }
    }
    
    public static List<CardValues> AllCardsExpect(List<CardValues> cards)
    {
        var all = new List<CardValues>();
        foreach (var v in Enum.GetValues<CardValues>())
        {
            if (v is not (CardJokerBlack or CardJokerRed))
            {
                all.AddRange(Enumerable.Repeat(v, 4));
            }
            else
            {
                all.Add(v);
            }
        }

        RemoveCards(all, cards);
        return all;
    }
}