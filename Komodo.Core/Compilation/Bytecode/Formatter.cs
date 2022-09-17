using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public class Formatter : Converter<string, string, string, string>
{
    public string Convert(Program program)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(program {program.Name}");
        builder.AppendLine($"    (entry {program.Entry.Module} {program.Entry.Function})");

        foreach (var module in program.Modules)
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
        builder.AppendLine($"(function {function.Name}");

        // Append arguments
        builder.Append($"    (args");

        foreach (var arg in function.Arguments)
            builder.Append($" {arg}");

        builder.AppendLine(")");

        // Append locals
        builder.Append($"    (locals");

        foreach (var local in function.Locals)
            builder.Append($" {local}");

        builder.AppendLine(")");

        // Append returns
        builder.Append($"    (returns");

        foreach (var ret in function.Returns)
            builder.Append($" {ret}");

        builder.AppendLine(")");

        // Append basic blocks
        foreach (var basicBlock in function.BasicBlocks)
            builder.AppendLine(Convert(basicBlock).WithIndent());

        //Append footer
        builder.Append(")");

        return builder.ToString();
    }

    public string Convert(BasicBlock basicBlock)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(basicBlock {basicBlock.Name}");

        foreach (var instruction in basicBlock.Instructions)
            builder.AppendLine(Convert(instruction).WithIndent());

        builder.Append(")");
        return builder.ToString();
    }

    public string Convert(Instruction instruction) => instruction.AsSExpression().ToString();
}