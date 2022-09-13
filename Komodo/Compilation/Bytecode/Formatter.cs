using System.Text;
using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public static class Formatter
{
    public static string Format(Program program)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(program {program.Name}");
        builder.AppendLine($"    (entry {program.Entry.Module} {program.Entry.Function})");

        foreach (var module in program.Modules)
            builder.AppendLine(Format(module).WithIndent());

        builder.Append(")");

        return builder.ToString();
    }

    public static string Format(Module module)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(module {module.Name}");

        foreach (var function in module.Functions)
            builder.AppendLine(Format(function).WithIndent());

        builder.Append(")");
        return builder.ToString();
    }

    public static string Format(Function function)
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
            builder.AppendLine(Format(basicBlock).WithIndent());

        //Append footer
        builder.Append(")");

        return builder.ToString();
    }

    public static string Format(BasicBlock basicBlock)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"(basicBlock {basicBlock.Name}");

        foreach (var instruction in basicBlock.Instructions)
            builder.AppendLine(Format(instruction).WithIndent());

        builder.Append(")");
        return builder.ToString();
    }

    public static string Format(Instruction instruction) => instruction.AsSExpression().ToString();
}