using System.Collections.ObjectModel;

namespace Komodo.Core.Utilities;

public class Stream<T> where T : class
{
    public ReadOnlyCollection<T> Tokens { get; }
    public int Offset { get; private set; }

    public T? Current => Offset == Tokens.Count ? null : Tokens[Offset];

    public Stream(IEnumerable<T> tokens)
    {
        Tokens = new ReadOnlyCollection<T>(tokens.ToArray());
        Offset = 0;
    }

    public Stream<T> Next(out T? token)
    {
        if (Offset == Tokens.Count) { token = null; }
        else { token = Tokens[Offset++]; }

        return this;
    }

    public Stream<T> Next<Result>(Func<T, Result> validator, out Result result)
    {
        var start = Offset;
        Next(out var item);

        if (item is null)
            throw new Exception("Unexpectedly encounted EOS");

        try
        {
            result = validator(item);
            return this;
        }
        catch (Exception e)
        {
            Offset = start;
            throw new Exception($"Error at {start}: {e}");
        }
    }

    public Stream<T> Next(Action<T> validator, out T token) => Next(t => { validator(t); return t; }, out token);

    public Stream<T> Next(T expected, out T result)
        => Next(token => token == expected ? token : throw new Exception($"Expected {expected}, but found {token}"), out result);

    public Stream<T> Next<Result>(Func<Stream<T>, Result> parser, out Result result)
    {
        result = parser(this);
        return this;
    }

    public Stream<T> Next<Result>(Action<Stream<T>> parser, out T[] result)
    {
        var start = Offset;
        parser(this);
        var end = Offset;

        result = new T[end - start];
        for (int i = 0; i < result.Length; i++)
            result[i] = Tokens[start + i];

        return this;
    }
}