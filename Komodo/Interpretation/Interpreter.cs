using Komodo.Compilation.Bytecode;

namespace Komodo.Interpretation;

public interface Value
{
    public record I64(Int64 Value) : Value;
}

public record InstructionPointer(string Module, string Function, string BasicBlock, int Index)
{
    public override string ToString() => $"{Module}.{Function}.{BasicBlock}.{Index}";
}

public enum InterpreterState { NotStarted, Running, ShuttingDown, Terminated }

public class Interpreter
{
    public Program Program { get; }
    public InterpreterState State { get; private set; }

    private Queue<Request> requests = new Queue<Request>();

    public Interpreter(Program program)
    {
        State = InterpreterState.NotStarted;
        Program = program;
    }

    public void Run()
    {
        State = InterpreterState.Running;

        var thread = new Thread(this, new InstructionPointer(Program.Entry.Parent.Name, Program.Entry.Name, Program.Entry.Entry.Name, 0));
        thread.Run();

        while (State == InterpreterState.Running)
        {
            // Process requests
            if (requests.TryDequeue(out var request))
            {
                switch (request)
                {
                    case Request.Exit r:
                        {
                            State = InterpreterState.ShuttingDown;

                            //TODO: send each thread the terminate signal
                            r.Sender.Send(new Signal.Terminate());
                            r.Complete();
                        }
                        break;
                    default: request.Deny($"Unknown request: {request}"); break;
                }
            }
        }

        while (requests.TryDequeue(out var request))
            request.Deny("Interpreter has temrinated.");

        State = InterpreterState.Terminated;
    }

    public void Request(Request request)
    {
        if (State != InterpreterState.Running)
        {
            request.Deny($"Interpreter is not running. Current state is '{State}'.");
            return;
        }

        requests.Enqueue(request);

        while (!request.Processed)
            ;
    }
}