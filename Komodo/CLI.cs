using System.Text;
using System.Text.RegularExpressions;
using Komodo.Utilities;

namespace Komodo.CLI;

public record Argument(string Name, Func<string, object> Parser)
{
    public object Parse(string value) => Parser(value);
}

public abstract record Option(string Name)
{
    static Regex Pattern = new Regex("--(?<Name>([a-zA-Z]+))(=(?<Value>([a-zA-Z]+)))?");

    public abstract object Parse(string value);

    public record Flag(string Name) : Option(Name)
    {
        public override object Parse(string value)
        {
            if (bool.TryParse(value, out var b))
                return b;

            throw new Exception($"Expected 'true' or 'false', but found '{value}'.");
        }
    }

    public record Parameter(string Name, bool Required, Func<string, object> Parser) : Option(Name)
    {
        public override object Parse(string value) => Parser(value);
    }

    public static (string Name, string Value) ParseArg(string arg)
    {
        var match = Pattern.Match(arg);

        if (!match.Success)
            throw new Exception($"Invalid option: {arg}");

        var name = match.Groups["Name"].Value;
        var value = match.Groups.ContainsKey("Value") ? match.Groups["Name"].Value : "true";

        return (name, value);
    }
}

public delegate void CommandHandler(Dictionary<string, object> options, Dictionary<string, object> arguments);

public record Command(string Name, CommandHandler Handler)
{
    private Dictionary<string, Option> options = new Dictionary<string, Option>();
    private Dictionary<string, Command> subcommands = new Dictionary<string, Command>();
    private List<Argument> arguments = new List<Argument>();

    public void AddOption(Option option) => options.Add(option.Name, option);
    public void AddSubcommand(Command subcommand) => subcommands.Add(subcommand.Name, subcommand);

    public void AddArgument(Argument argument)
    {
        if (arguments.Any(arg => arg.Name == argument.Name))
            throw new Exception($"An argument name '{argument.Name}' already exists for command '{Name}'.");

        arguments.Add(argument);
    }

    private Dictionary<string, object> ParseOptions(IEnumerable<string> args, out IEnumerable<string> remainingArgs)
    {
        var result = new Dictionary<string, object>();
        var items = args.TakeWhile(x => x.StartsWith('-')).ToHashSet();

        foreach (var item in items)
        {
            var (name, value) = Option.ParseArg(item);

            if (!options.TryGetValue(name, out var option))
                throw new Exception($"The option '{name}' does not exist for command '{Name}'.");

            if (!result.TryAdd(name, option.Parse(value)))
                throw new Exception($"The option '{name}' has already been set for command '{Name}'.");
        }

        remainingArgs = args.Skip(items.Count);
        return result;
    }

    private Dictionary<string, object> ParseArguments(IEnumerable<string> args, out IEnumerable<string> remainingArgs)
    {
        var result = new Dictionary<string, object>();
        var items = args.Take(arguments.Count);

        if (items.Count() != arguments.Count)
            throw new Exception($"Command '{Name}' expects {arguments.Count} arguments, but only {items.Count()} were given.");

        foreach (var (arg, item) in arguments.Zip(items))
            result.Add(arg.Name, arg.Parse(item));

        remainingArgs = args.Skip(items.Count());
        return result;
    }

    public void Run(IEnumerable<string> args)
    {
        IEnumerable<string> remainingArgs;

        var options = ParseOptions(args, out remainingArgs);
        var arguments = ParseArguments(remainingArgs, out remainingArgs);

        Handler(options, arguments);

        if (subcommands.Count == 0) { return; }
        else if (remainingArgs.Count() == 0) { throw new Exception("Expected a subcommand"); }
        else if (subcommands.TryGetValue(remainingArgs.First(), out var subcommand)) { subcommand.Run(remainingArgs.Skip(1)); }
        else { throw new Exception($"The subcommand '{remainingArgs.First()}' does not exist for command '{Name}'."); }
    }

    public void PrintUsage()
    {
        var builder = new StringBuilder();
        builder.Append(Name);

        if (options.Count != 0)
            builder.Append(" [options]");

        if (arguments.Count != 0)
            builder.Append(Utility.Stringify(arguments.Select(a => $"<{a.Name}>"), " ", (" ", "")));

        if (options.Count != 0)
            builder.Append(" <command> [<args>]");
    }
}
