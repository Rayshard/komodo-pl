namespace Komodo.CLI;

public interface Parameter
{
    public string Name { get; }
    public string Value { get; }
}

public record Command(string Name, IEnumerable<Parameter> Parameters, IEnumerable<Command> Subcommands, Action<IEnumerable<string>, IDictionary<string, string>> Callback);

public static class Parser
{
    public static Action ParseCommand(Command template, IEnumerable<string> input)
    {
        List<string> flags = new List<string>();
        Dictionary<string, string> parameters = new Dictionary<string, string>();
        Action? subcommand = null;

        return () => {
            template.Callback.Invoke(flags, parameters);
            subcommand?.Invoke();
        };
    }
}
