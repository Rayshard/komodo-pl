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

        // Append return
        builder.AppendLine($"    (ret {function.ReturnType})");

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

    public static string Format(Instruction instruction)
    {
        var items = new List<string>();

        items.Add(instruction.Opcode.ToString());
        items.AddRange(instruction switch
        {
            Instruction.PushI64 instr => new string[] { instr.Value.ToString() },
            Instruction.AddI64 instr => new string[] { },
            Instruction.Syscall instr => new string[] { instr.Code.ToString() },
            _ => throw new NotImplementedException(instruction.Opcode.ToString())
        });

        return Utility.Stringify(items, " ", ("(", ")"));
    }

    public static SExpression Serialize(Program program)
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("program"));
        nodes.Add(new SExpression.UnquotedSymbol(program.Name));

        var entryNode = new SExpression.List(new[]
        {
            new SExpression.UnquotedSymbol("entry"),
            new SExpression.UnquotedSymbol(program.Entry.Module),
            new SExpression.UnquotedSymbol(program.Entry.Function),
        });

        nodes.AddRange(program.Modules.Select(Serialize));

        return new SExpression.List(nodes);
    }

    public static SExpression Serialize(Module module)
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("module"));
        nodes.Add(new SExpression.UnquotedSymbol(module.Name));
        nodes.AddRange(module.Functions.Select(Serialize));

        return new SExpression.List(nodes);
    }

    public static SExpression Serialize(Function function)
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("function"));
        nodes.Add(new SExpression.UnquotedSymbol(function.Name));

        var argsNodes = new List<SExpression>();
        argsNodes.Add(new SExpression.UnquotedSymbol("args"));
        argsNodes.AddRange(function.Arguments.Select(arg => new SExpression.UnquotedSymbol(arg.ToString())));
        nodes.Add(new SExpression.List(argsNodes));

        var localsNodes = new List<SExpression>();
        localsNodes.Add(new SExpression.UnquotedSymbol("locals"));
        localsNodes.AddRange(function.Locals.Select(arg => new SExpression.UnquotedSymbol(arg.ToString())));
        nodes.Add(new SExpression.List(localsNodes));

        var retNode = new SExpression.List(new[]
        {
            new SExpression.UnquotedSymbol("ret"),
            new SExpression.UnquotedSymbol(function.ReturnType.ToString()),
        });
        nodes.Add(new SExpression.List(retNode));

        nodes.AddRange(function.BasicBlocks.Select(Serialize));

        return new SExpression.List(nodes);
    }

    public static SExpression Serialize(BasicBlock basicBlock)
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("basicBlock"));
        nodes.Add(new SExpression.UnquotedSymbol(basicBlock.Name));
        nodes.AddRange(basicBlock.Instructions.Select(Serialize));
        return new SExpression.List(nodes);
    }

    public static SExpression Serialize(Instruction instruction)
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol(instruction.Opcode.ToString()));

        switch (instruction)
        {
            case Instruction.Syscall instr: nodes.Add(new SExpression.UnquotedSymbol(instr.Code.ToString())); break;
            case Instruction.PushI64 instr: nodes.Add(new SExpression.UnquotedSymbol(instr.Value.ToString())); break;
            case Instruction.AddI64: break;
            default: throw new NotImplementedException(instruction.Opcode.ToString());
        }

        return new SExpression.List(nodes);
    }

    public static Program DeserializeProgram(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(3, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("program");

        var name = list[1].ExpectUnquotedSymbol().Value;

        var entryNode = list[2].ExpectList().ExpectLength(3);
        entryNode.ElementAt(0).ExpectUnquotedSymbol().ExpectValue("entry");

        var entryModule = entryNode.ElementAt(1).ExpectUnquotedSymbol().Value;
        var entryFunction = entryNode.ElementAt(2).ExpectUnquotedSymbol().Value;
        var program = new Program(name, (entryModule, entryFunction));

        foreach (var item in list.Skip(3))
            program.AddModule(DeserializeModule(item));

        return program;
    }

    public static Module DeserializeModule(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(2, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("module");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var module = new Module(name);

        foreach (var item in list.Skip(2))
            module.AddFunction(DeserializeFunction(item));

        return module;
    }

    public static Function DeserializeFunction(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(6, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("function");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var argsNode = list[2].ExpectList().ExpectLength(1, null);
        var localsNode = list[3].ExpectList().ExpectLength(1, null);
        var retNode = list[4].ExpectList().ExpectLength(2);

        argsNode[0].ExpectUnquotedSymbol().ExpectValue("args");
        localsNode[0].ExpectUnquotedSymbol().ExpectValue("locals");
        retNode[0].ExpectUnquotedSymbol().ExpectValue("ret");

        var args = argsNode.Skip(1).Select(node => node.AsEnum<DataType>());
        var locals = localsNode.Skip(1).Select(node => node.AsEnum<DataType>());
        var ret = retNode.ElementAt(1).AsEnum<DataType>();
        var function = new Function(name, args, locals, ret);

        foreach (var item in list.Skip(5))
            function.AddBasicBlock(DeserializeBasicBlock(item));

        return function;
    }

    public static BasicBlock DeserializeBasicBlock(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(3, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("basicBlock");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var basicBlock = new BasicBlock(name);

        foreach (var item in list.Skip(2))
            basicBlock.Append(DeserializeInstruction(item));

        return basicBlock;
    }

    public static Instruction DeserializeInstruction(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(1, null);
        var opcode = list[0].AsEnum<Opcode>();

        switch (opcode)
        {
            case Opcode.PushI64:
                {
                    list.ExpectLength(2, null);
                    return new Instruction.PushI64(list[1].AsInt64());
                }
            case Opcode.AddI64:
                {
                    list.ExpectLength(1, null);
                    return new Instruction.AddI64();
                }
            case Opcode.Syscall:
                {
                    list.ExpectLength(2, null);
                    return new Instruction.Syscall(list[1].AsEnum<SyscallCode>());
                }
            default: throw new Exception($"Unexpected opcode: {opcode}");
        }
    }
}