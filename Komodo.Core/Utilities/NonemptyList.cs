using System.Collections;

namespace Komodo.Core.Utilities;

public record NonemptyList<T> : IEnumerable<T>
{
    private T[] Values { get; }

    public NonemptyList(IEnumerable<T> values) => Values = values.ToArray();

    public void Deconstruct(out T first, out NonemptyList<T>? rest)
    {
        first = Values[0];
        rest = Values.Length == 1 ? null : new NonemptyList<T>(Values.Skip(1));
    }

    public IEnumerator<T> GetEnumerator() =>  ((IEnumerable<T>)Values).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Values.GetEnumerator();
}