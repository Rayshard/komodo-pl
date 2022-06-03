namespace Komodo.Utilities
{
    public abstract class Result<TSuccess, TFailure>
    {
        public class Success : Result<TSuccess, TFailure>
        {
            public TSuccess Value { get; }

            public Success(TSuccess value) => Value = value;

            public void Deconstruct(out TSuccess value) => value = Value;
        }

        public class Failure : Result<TSuccess, TFailure>
        {
            public TFailure Value { get; }

            public Failure(TFailure value) => Value = value;

            public void Deconstruct(out TFailure value) => value = Value;
        }

        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failure;

        public TSuccess UnwrapSuccess() => (this as Success ?? throw new InvalidOperationException("Result is not a success!")).Value;
        public TFailure UnwrapFailure() => (this as Failure ?? throw new InvalidOperationException("Result is not a failure!")).Value;
    }
}