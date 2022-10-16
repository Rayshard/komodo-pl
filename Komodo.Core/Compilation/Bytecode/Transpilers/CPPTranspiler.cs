using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode.Transpilers;

public class CPPTranspiler : Converter<string>
{
    private string Convert(DataType dt) => $"Komodo{dt}";

    public override string Convert(Program program)
    {
        var forwardDeclarations = Utility.Stringify(program.Modules.Values.Select(GetForwardDeclaration), Environment.NewLine + Environment.NewLine);
        var modules = Utility.Stringify(program.Modules.Values.Select(Convert), Environment.NewLine + Environment.NewLine);

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
        var functions = Utility.Stringify(module.Functions.Values.Select(Convert), Environment.NewLine + Environment.NewLine);

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
        var functions = Utility.Stringify(module.Functions.Values.Select(GetForwardDeclaration), Environment.NewLine);

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
        var parameters = function.Parameters.Select((p, i) => $"{Convert(p.DataType)} param{i}");
        var cppFunctionParams = parameters.AppendIf(!function.Returns.IsEmpty(), "Value* returns").Prepend("Interpreter& interpreter");

        return $"void {function.Name}({Utility.Stringify(cppFunctionParams, ", ")});";
    }

    public string Convert(Function function)
    {
        var parameters = function.Parameters.Select((p, i) => $"{Convert(p.DataType)} param{i}");
        var locals = function.Locals.Select((l, i) => $"auto local{i} = {Convert(l.DataType)}();");
        var bodyElements = Utility.Stringify(function.BodyElements.Select(Convert), Environment.NewLine);

        var cppFunctionParams = parameters.AppendIf(!function.Returns.IsEmpty(), "Value* returns").Prepend("Interpreter& interpreter");

        return
$@"
void {function.Name}({Utility.Stringify(cppFunctionParams, ", ")})
{{
{Utility.Stringify(locals, Environment.NewLine).WithIndent(delimiter: Environment.NewLine)}

{bodyElements}
}}
".Trim();
    }

    public string Convert(FunctionBodyElement fbe) => fbe switch
    {
        Label l => $"{l.Name}:",
        Instruction i => Convert(i),
        _ => throw new NotImplementedException(fbe.ToString())
    };

    public string Convert(Instruction instruction)
    {
        var result = instruction switch
        {
            Instruction.Dump i => Convert(i),
            Instruction.Call i => Convert(i),
            Instruction.Assert i => Convert(i),
            Instruction.Unop i => Convert(i),
            Instruction.Binop i => Convert(i),
            Instruction.Jump i => Convert(i),
            Instruction.Return i => Convert(i),
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

    public string Convert(Operand.Constant constant)
    {
        var v = constant switch
        {
            Operand.Constant.I8(var value) => value.ToString(),
            Operand.Constant.UI8(var value) => value.ToString(),
            Operand.Constant.I16(var value) => value.ToString(),
            Operand.Constant.UI16(var value) => value.ToString(),
            Operand.Constant.I32(var value) => value.ToString(),
            Operand.Constant.UI32(var value) => value.ToString(),
            Operand.Constant.I64(var value) => value.ToString(),
            Operand.Constant.UI64(var value) => value.ToString(),
            Operand.Constant.F32(var value) => value.ToString(),
            Operand.Constant.F64(var value) => value.ToString(),
            Operand.Constant.True => "true",
            Operand.Constant.False => "false",
            _ => throw new NotImplementedException(constant.ToString())
        };

        return $"{Convert(constant.DataType)}({v})";
    }

    public string Convert(Operand.Source source) => source switch
    {
        Operand.Constant constant => Convert(constant),
        Operand.Arg.Indexed(var index) => $"arg{index}",
        Operand.Local.Indexed(var index) => $"local{index}",
        Operand.Stack => "interpreter.PopStack()",
        _ => throw new NotImplementedException(source.ToString())
    };

    public string Convert(Operand.Destination destination, string rValue) => destination switch
    {
        Operand.Local(var index) => $"local{index} = {rValue};",
        Operand.Stack => "interpreter.PushStack(rValue);",
        _ => throw new NotImplementedException(destination.ToString())
    };

    public string Convert(Instruction.Dump instruction) => $"std::cout << ToString({Convert(instruction.Source)}) << std::endl;";

    public string Convert(Instruction.Call.Direct instruction)
    {
        var callArgsInitialization = Utility.Stringify(instruction.Args.Select((ca, i) => $"auto callArg{i} = {Convert(ca)};"), Environment.NewLine);
        var callArgs = instruction.Args.Select((_, i) => $"callArg{i}").Prepend("interpreter");

        return
    $@"
{callArgsInitialization}

{instruction.Module}::{instruction.Function}({Utility.Stringify(callArgs, ", ")});
".Trim();
    }

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

    public string Convert(Instruction.Unop instruction)
    {
        var source = Convert(instruction.Source);
        var rValue = instruction.Opcode switch
        {
            Opcode.Dec => $"{source} - 1",
            _ => throw new NotImplementedException(instruction.ToString())
        };

        return Convert(instruction.Destination, rValue);
    }

    public string Convert(Instruction.Jump instruction) => instruction.Condition is null
        ? $"goto {instruction.Label};"
        : $"if ({Convert(instruction.Condition)})\n    goto {instruction.Label};";

    public string Convert(Instruction.Return instruction) => throw new NotImplementedException();
}