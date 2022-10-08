using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public class Formatter : Converter<string>
{
    public override string Convert(Program program)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(program {program.Name}");
        builder.AppendLine($"    (entry {program.Entry.Module} {program.Entry.Function})");

        foreach (var module in program.Modules.Values)
            builder.AppendLine(Convert(module).WithIndent());

        builder.Append(")");

        return builder.ToString();
    }

    public string Convert(Module module)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(module {module.Name}");

        foreach (var function in module.Functions)
            builder.AppendLine(Convert(function).WithIndent());

        builder.Append(")");
        return builder.ToString();
    }

    public string Convert(Function function)
    {
        var builder = new StringBuilder();

        //Append header
        builder.Append($"(function {function.Name}");

        // Append parameters
        if (!function.Parameters.IsEmpty())
            builder.Append(function.Parameters.Stringify(", ", ("(params ", ")")));

        // Append returns
        if (!function.Returns.IsEmpty())
            builder.Append(function.Returns.Stringify(", ", ("(returns ", ")")));

        builder.AppendLine();

        // Append locals
        if (!function.Locals.IsEmpty())
        {
            builder.Append($"    (locals");

            foreach (var local in function.Locals)
                builder.Append($" {local}");

            builder.AppendLine(")");
        }

        // Append body elements
        foreach (var bodyElement in function.BodyElements)
        {
            builder.AppendLine(bodyElement switch
            {
                Label l => Convert(l).WithIndent("  "),
                Instruction i => Convert(i).WithIndent("   "),
                _ => throw new NotImplementedException(bodyElement.ToString())
            });
        }

        //Append footer
        builder.Append(")");

        return builder.ToString();
    }

    public string Convert(Label label) => $"(label {label.Name})";

    public string Convert(Instruction instruction) => instruction.AsSExpression().ToString();
}