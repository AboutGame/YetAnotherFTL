namespace YetAnotherFTL.Game.Utilities;

public static class EnumerableCartesianProductExtensions
{
    public static IEnumerable<IEnumerable<T>> Combinations<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) => 
                from seq in accumulator 
                from item in sequence.Except(seq)
                where !seq.Any() || Comparer<T>.Default.Compare(item, seq.Last()) > 0
                select seq.Concat(new[] {item})).ToArray();
    }
}