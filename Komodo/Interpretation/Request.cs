namespace Komodo.Interpretation;

public abstract record Request(Action<string> OnDenied)
{
    public bool Processed { get; private set; }

    public abstract record Get<T>(Action<T> OnCompleted, Action<string> OnDenied) : Request(OnDenied)
    {
        public void Complete(T result)
        {
            OnCompleted(result);
            Processed = true;
        }
    }

    public abstract record Post(Action<string> OnDenied) : Request(OnDenied)
    {
        public void Complete() => Processed = true;
    }

    public void Deny(string reason) 
    {
        OnDenied(reason);
        Processed = true;
    }

    public record Exit(Thread Sender, int Code) : Post(reason => throw new Exception($"Exit requests cannot be denied! Supplied reason: {reason}"));
    public record MemoryRead(int address, Action<int> OnCompleted, Action<string> OnDenied) : Get<int>(OnCompleted, OnDenied);
}