using System.Text;
using System.Text.RegularExpressions;
using Komodo.Utilities;

namespace Komodo.CLI;

public record Option(string Name, bool Required, Func<string, object> Parser)
{
    static Regex Pattern = new Regex("^--[a-zA-Z]+(=[a-zA-Z]+)?$");

    public object Parse(string value)
    {
        try { return Parser(value); }
        catch (Exception) { throw new Exception($"Invalid value for option '{Name}'"); }
    }

    public static (string Name, string Value) ParseArgument(string arg)
    {
        var match = Pattern.Match(arg);

        if (!match.Success)
            throw new Exception($"Invalid option: {arg}");

        var parts = arg.Substring(2).Split("=");
        var name = parts[0];
        var value = parts.Length != 1 ? string.Join("=", parts.Skip(1).ToArray()) : "true";

        return (name, value);
    }

    public record Flag(string Name) : Option(Name, false, value => bool.Parse(value));
}

public record Parameter(string Name, Func<string, object> Parser)
{
    public object Parse(string value) => Parser(value);
}

public delegate void CommandHandler(Dictionary<string, object> options, Dictionary<string, object> parameters);

public record Command(string Name, CommandHandler Handler)
{
    private Dictionary<string, Option> options = new Dictionary<string, Option>();
    private Dictionary<string, Command> subcommands = new Dictionary<string, Command>();
    private List<Parameter> parameters = new List<Parameter>();

    public void AddOption(Option option) => options.Add(option.Name, option);
    public void AddSubcommand(Command subcommand) => subcommands.Add(subcommand.Name, subcommand);

    public void AddParameter(Parameter parameter)
    {
        if (parameters.Any(param => param.Name == parameter.Name))
            throw new Exception($" parameter named '{parameter.Name}' already exists for command '{Name}'.");

        parameters.Add(parameter);
    }

    private Dictionary<string, object> ParseOptions(IEnumerable<string> args, out IEnumerable<string> remainingArgs)
    {
        var result = new Dictionary<string, object>();
        var items = args.TakeWhile(x => x.StartsWith('-')).ToHashSet();

        foreach (var item in items)
        {
            var (name, value) = Option.ParseArgument(item);

            if (!options.TryGetValue(name, out var option))
                throw new Exception($"The option '{name}' does not exist for command '{Name}'.");

            if (!result.TryAdd(name, option.Parse(value)))
                throw new Exception($"The option '{name}' has already been set for command '{Name}'.");
        }

        foreach (var option in options.Values)
        {
            if (result.ContainsKey(option.Name)) { continue; }
            else if (option.Required) { throw new Exception($"Missing required option: {option.Name}"); }
        }

        remainingArgs = args.Skip(items.Count);
        return result;
    }

    private Dictionary<string, object> ParseParameters(IEnumerable<string> args, out IEnumerable<string> remainingArgs)
    {
        var result = new Dictionary<string, object>();
        var items = args.Take(parameters.Count);

        if (items.Count() != parameters.Count)
            throw new Exception($"Command '{Name}' expects {parameters.Count} parameters, but only {items.Count()} were given.");

        foreach (var (param, item) in parameters.Zip(items))
            result.Add(param.Name, param.Parse(item));

        remainingArgs = args.Skip(items.Count());
        return result;
    }

    public int Run(IEnumerable<string> args)
    {
        try
        {
            IEnumerable<string> remainingArgs = args;

            var options = ParseOptions(remainingArgs, out remainingArgs);
            var parameters = ParseParameters(remainingArgs, out remainingArgs);

            Handler(options, parameters);

            if (subcommands.Count == 0) { return 0; }
            else if (remainingArgs.Count() == 0) { throw new Exception("Expected a subcommand"); }
            else if (subcommands.TryGetValue(remainingArgs.First(), out var subcommand)) { return subcommand.Run(remainingArgs.Skip(1)); }
            else { throw new Exception($"The subcommand '{remainingArgs.First()}' does not exist for command '{Name}'."); }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            PrintUsage();
            return -1;
        }
    }

    public void PrintUsage()
    {
        var builder = new StringBuilder();
        builder.Append($"USAGE: {Name}");

        if (options.Count != 0)
            builder.Append(" [options]");

        if (parameters.Count != 0)
            builder.Append(Utility.Stringify(parameters.Select(a => $"<{a.Name}>"), " ", (" ", "")));

        if (options.Count != 0)
            builder.Append(" <command> [<args>]");

        Console.WriteLine(builder.ToString());
    }
}
