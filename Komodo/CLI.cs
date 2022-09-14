using System.Collections;
using System.Text;
using Komodo.Utilities;

namespace Komodo.CLI;

public abstract record Parameter(string Name, string Description, object? DefaultValue)
{
    public bool Required => DefaultValue is null;

    public record Boolean(string Name, string Description) : Parameter(Name, Description, false);
    public record Option(string Name, string Description, Func<string, object>? Parser = null, object? DefaultValue = null) : Parameter(Name, Description, DefaultValue);
    public record Positional(string Name, string Description, Func<string, object>? Parser = null) : Parameter(Name, Description, null);

    public static class Parsers
    {
        public static object Enmeration<T>(string value) where T : struct, Enum => Enum.Parse<T>(value);
    }
}

public record Arguments(Dictionary<string, object> arguments) : IEnumerable<KeyValuePair<string, object>>
{
    public T Get<T>(string name) => (T)arguments[name];

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => arguments.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public record Command
{
    public string Name { get; }
    public string Description { get; }
    public Func<Arguments, int> Callback { get; }

    private Dictionary<string, Parameter> parameters = new Dictionary<string, Parameter>();
    private List<Parameter.Positional> positionalParameterOrder = new List<Parameter.Positional>();
    private Dictionary<string, Command> subcommands = new Dictionary<string, Command>();

    public Command(string name, string description, IEnumerable<Parameter> parameters, IEnumerable<Command> subcommands, Func<Arguments, int> callback)
    {
        Name = name;
        Description = description;
        Callback = callback;

        // Add parameters
        foreach (var parameter in parameters)
        {
            if (!this.parameters.TryAdd(parameter.Name, parameter))
                throw new Exception($"Parameter named '{parameter.Name}' already exists for command '{Name}'.");

            if (parameter is Parameter.Positional positional)
                positionalParameterOrder.Add(positional);
        }

        // Add subcommands
        foreach (var subcommand in subcommands)
        {
            if (!this.subcommands.TryAdd(subcommand.Name, subcommand))
                throw new Exception($"Subcommand named '{subcommand.Name}' already exists for command '{Name}'.");
        }
    }

    private Arguments ParseArguments(IEnumerable<string> args, out IEnumerable<string> remainingArgs)
    {
        var result = new Dictionary<string, object>();
        var remainingPositionalParameters = new Queue<Parameter.Positional>(positionalParameterOrder);

        remainingArgs = args;

        while (true)
        {
            if (remainingArgs.Count() == 0 || result.Count == parameters.Count)
                break;

            var arg = remainingArgs.First();

            if (arg.StartsWith("--"))
            {
                var name = arg.Substring(2);

                if (!parameters.ContainsKey(name)) { throw new Exception($"The parameter '{name}' does not exist for command '{Name}'."); }
                else if (result.ContainsKey(name)) { throw new Exception($"The parameter '{name}' has already been set for command '{Name}'."); }
                else { remainingArgs = remainingArgs.Skip(1); }

                switch (parameters[name])
                {
                    case Parameter.Boolean boolean: result[boolean.Name] = true; break;
                    case Parameter.Option option:
                        {
                            if (remainingArgs.Count() == 0)
                                throw new Exception($"Missing value for parameter: {option.Name}");

                            var valueArg = remainingArgs.First();

                            try
                            {
                                result[option.Name] = option.Parser is null ? valueArg : option.Parser(valueArg);
                                remainingArgs = remainingArgs.Skip(1);
                            }
                            catch { throw new Exception($"Invalid value for option: {option.Name}"); }
                        }
                        break;
                    case Parameter.Positional positional: throw new Exception($"Invalid format for positional parameter: {positional.Name}");
                    default: throw new NotImplementedException();
                }
            }
            else if (remainingPositionalParameters.Count() != 0)
            {
                var positional = remainingPositionalParameters.Dequeue();

                try
                {
                    result[positional.Name] = positional.Parser is null ? arg : positional.Parser(arg);
                    remainingArgs = remainingArgs.Skip(1);
                }
                catch { throw new Exception($"Invalid value for positional parameter '{positional.Name}'"); }
            }
            else { break; }
        }

        // Set default values for parameters that were not set and raise error for required parameters that were not set
        foreach (var parameter in parameters.Values)
        {
            if (!result.ContainsKey(parameter.Name))
            {
                if (parameter.DefaultValue is null) { throw new Exception($"Missing required parameter: {parameter.Name}"); }
                else { result[parameter.Name] = parameter.DefaultValue; }
            }
        }

        return new Arguments(result);
    }

    public int Run(IEnumerable<string> args)
    {
        try
        {
            var arguments = ParseArguments(args, out var remainingArgs);
            var callbackValue = Callback(arguments);

            if (callbackValue != 0) { return callbackValue; }
            else if (subcommands.Count == 0) { return 0; }
            else if (remainingArgs.Count() == 0) { throw new Exception("Expected a subcommand"); }
            else if (subcommands.TryGetValue(remainingArgs.First(), out var subcommand)) { return subcommand.Run(remainingArgs.Skip(1)); }
            else { throw new Exception($"The subcommand '{remainingArgs.First()}' does not exist for command '{Name}'."); }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message + Environment.NewLine);
            Console.WriteLine(GetUsage());
            return -1;
        }
    }

    public string GetUsage()
    {
        var builder = new StringBuilder();
        builder.Append($"USAGE: {Name}");

        var booleans = parameters.Values.Where(p => p is Parameter.Boolean).Select(p => (Parameter.Boolean)p);
        var options = parameters.Values.Where(p => p is Parameter.Option).Select(p => (Parameter.Option)p);
        var positionals = parameters.Values.Where(p => p is Parameter.Positional).Select(p => (Parameter.Positional)p);

        if (booleans.Count() != 0)
            builder.Append(" [flags]");

        if (options.Count() != 0)
            builder.Append(" [options]");

        if (positionals.Count() != 0)
            builder.Append(Utility.Stringify(positionals.Select(p => $"<{p.Name}>"), " ", (" ", "")));

        if (subcommands.Count() != 0)
            builder.Append(" <command> [<args>]");

        builder.AppendLine();
        builder.AppendLine();
        builder.Append(Description);

        if (parameters.Count() != 0)
        {
            builder.AppendLine();
            builder.AppendLine();

            if (booleans.Count() != 0)
            {
                builder.AppendLine("Flags:");

                foreach (var flag in booleans.OrderBy(b => b.Name))
                    builder.AppendLine($"    --{flag.Name}\t\t{flag.Description}");
            }

            if (options.Count() != 0)
            {
                builder.AppendLine("Options:");

                foreach (var option in options.OrderBy(o => o.Name))
                    builder.AppendLine($"    --{option.Name} <value>\t\t{option.Description}");
            }

            if (positionals.Count() != 0)
            {
                builder.AppendLine();

                foreach (var positional in positionals.OrderBy(p => p.Name))
                    builder.AppendLine($"{positional.Name}\t\t{positional.Description}");
            }
        }

        if (subcommands.Count() != 0)
        {
            builder.AppendLine();
            builder.AppendLine("Commands:");

            foreach (var command in subcommands.Values.OrderBy(sc => sc.Name))
                builder.AppendLine($"    {command.Name}\t\t{command.Description}");
        }

        return builder.ToString().TrimEnd();
    }
}
