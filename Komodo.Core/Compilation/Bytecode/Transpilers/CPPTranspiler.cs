using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode.Transpilers;

public class CPPTranspiler : Converter<string, string, string, string>
{
    private string Convert(DataType dt) => $"Komodo.Core{dt}";

    public string Convert(Program program)
    {
        var forwardDeclarations = Utility.Stringify(program.Modules.Select(GetForwardDeclaration), Environment.NewLine + Environment.NewLine);
        var modules = Utility.Stringify(program.Modules.Select(Convert), Environment.NewLine + Environment.NewLine);

        return
$@"
#pragma once

#include ""runtime.h""

namespace Program
{{
{forwardDeclarations.WithIndent(delimiter: Environment.NewLine)}

{modules.WithIndent(delimiter: Environment.NewLine)}

    static void Main(Interpreter& interpreter)
    {{
        {program.Entry.Module}::{program.Entry.Function}(interpreter);
    }}
}}
".Trim();
    }

    public string Convert(Module module)
    {
        var functions = Utility.Stringify(module.Functions.Select(Convert), Environment.NewLine + Environment.NewLine);

        return
$@"
namespace {module.Name}
{{
{functions.WithIndent(delimiter: Environment.NewLine)}
}}
".Trim();
    }

    private string GetForwardDeclaration(Module module)
    {
        var functions = Utility.Stringify(module.Functions.Select(GetForwardDeclaration), Environment.NewLine);

        return
$@"
namespace {module.Name}
{{
{functions.WithIndent(delimiter: Environment.NewLine)}
}}
".Trim();
    }

    private string GetForwardDeclaration(Function function)
    {
        var args = function.Arguments.Select((a, i) => $"{Convert(a)} arg{i}");
        var cppFunctionArgs = args.AppendIf(function.Returns.Count() > 0, "Value* returns").Prepend("Interpreter& interpreter");

        return $"void {function.Name}({Utility.Stringify(cppFunctionArgs, ", ")});";
    }

    public string Convert(Function function)
    {
        var args = function.Arguments.Select((a, i) => $"{Convert(a)} arg{i}");
        var locals = function.Locals.Select((local, i) => $"auto local{i} = {Convert(local)}();");
        var basicBlocks = Utility.Stringify(function.BasicBlocks.Select(Convert), Environment.NewLine + Environment.NewLine);

        var cppFunctionArgs = args.AppendIf(function.Returns.Count() > 0, "Value* returns").Prepend("Interpreter& interpreter");

        return
$@"
void {function.Name}({Utility.Stringify(cppFunctionArgs, ", ")})
{{
{Utility.Stringify(locals, Environment.NewLine).WithIndent(delimiter: Environment.NewLine)}

{basicBlocks}
}}
".Trim();
    }

    public string Convert(BasicBlock basicBlock)
    {
        var instructions = Utility.Stringify(basicBlock.Instructions.Select(Convert), Environment.NewLine + Environment.NewLine);

        return
$@"
{basicBlock.Name}:
    {{
{instructions.WithIndent("        ", delimiter: Environment.NewLine)}
    }}
".Trim();
    }

    public string Convert(Instruction instruction)
    {
        var result = instruction switch
        {
            Instruction.Load l => Convert(l),
            Instruction.Syscall s => Convert(s),
            Instruction.Print p => Convert(p),
            Instruction.Call c => Convert(c),
            Instruction.Store s => Convert(s),
            Instruction.Assert a => Convert(a),
            Instruction.Dec d => Convert(d),
            Instruction.Binop b => Convert(b),
            Instruction.CJump cj => Convert(cj),
            Instruction.Return r => Convert(r),
            _ => throw new NotImplementedException(instruction.ToString())
        };

        return
$@"
//{instruction.AsSExpression()}
{{
    //std::cout << ""{instruction.AsSExpression()}"" << std::endl;
{result.WithIndent(delimiter: Environment.NewLine)}
}}
".Trim();
    }

    public string Convert(Value value)
    {
        var v = value switch
        {
            Value.I64(var i) => i.ToString(),
            Value.Bool(var b) => b ? "true" : "false",
            _ => throw new NotImplementedException(value.ToString())
        };

        return $"{Convert(value.DataType)}({v})";
    }

    public string Convert(Operand.Source source) => source switch
    {
        Operand.Constant(var value) => Convert(value),
        Operand.Arg(var index) => $"arg{index}",
        Operand.Local(var index) => $"local{index}",
        Operand.Stack => "interpreter.PopStack()",
        _ => throw new NotImplementedException(source.ToString())
    };

    public string Convert(Operand.Destination destination, string rValue) => destination switch
    {
        Operand.Local(var index) => $"local{index} = {rValue};",
        Operand.Stack => "interpreter.PushStack(rValue);",
        _ => throw new NotImplementedException(destination.ToString())
    };


    public string Convert(Instruction.Load instruction) => $"interpreter.PushStack({Convert(instruction.Source)});";
    public string Convert(Instruction.Syscall instruction) => $"interpreter.Syscall(\"{instruction.Name}\");";
    public string Convert(Instruction.Print instruction) => $"std::cout << ToString({Convert(instruction.Source)}) << std::endl;";

    public string Convert(Instruction.Call instruction)
    {
        var returns = instruction.Returns.ToArray();

        var returnsInitialization = returns.Length == 0 ? "" : $"Value callReturns[{returns.Length}];";
        var callArgsInitialization = Utility.Stringify(instruction.Args.Select((ca, i) => $"auto callArg{i} = {Convert(ca)};"), Environment.NewLine);
        var callArgs = instruction.Args.Select((_, i) => $"callArg{i}").AppendIf(returns.Length != 0, "callReturns").Prepend("interpreter");
        var returnSets = Utility.Stringify(returns.Reverse().Select((r, i) => Convert(r, $"callReturns[{i}]")), Environment.NewLine);

        return
$@"
{returnsInitialization}
{callArgsInitialization}

{instruction.Module}::{instruction.Function}({Utility.Stringify(callArgs, ", ")});

{returnSets}
".Trim();
    }

    public string Convert(Instruction.Store instruction) => $"throw std::runtime_error(\"Not implemented\");";
    public string Convert(Instruction.Assert instruction) => $"throw std::runtime_error(\"Not implemented\");";

    public string Convert(Instruction.Binop instruction)
    {
        var source1 = Convert(instruction.Source1);
        var source2 = Convert(instruction.Source2);
        var rValue = instruction.Opcode switch
        {
            Opcode.Add => $"{source1} + {source2}",
            Opcode.Mul => $"{source1} * {source2}",
            Opcode.Eq => $"{source1} == {source2}",
            _ => throw new NotImplementedException(instruction.ToString())
        };

        return Convert(instruction.Destination, rValue);
    }

    public string Convert(Instruction.Dec instruction)
    {
        var rValue = $"{Convert(instruction.Source)} - 1";

        return Convert(instruction.Destination, rValue);
    }

    public string Convert(Instruction.CJump instruction)
    {
        var condition = Convert(instruction.Condtion);
        
        return
$@"
if ({condition})
    goto {instruction.BasicBlock};
".Trim();
    }

    public string Convert(Instruction.Return instruction)
        => Utility.Stringify(instruction.Sources.Select((s, i) => $"returns[{i}] = {Convert(s)};").Append("return;"), Environment.NewLine);
}