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
        var locals = function.Locals.Select((l, i) => $"auto local{i} = {Convert(l.Value)}();");
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

    public string Convert(Instruction.Dump instruction) => $"std::cout << ToString(interpreter.PopStack()) << std::endl;";
    public string Convert(Instruction.Assert instruction) => $"throw std::runtime_error(\"Not implemented\");";
    public string Convert(Instruction.Dec instruction) => "interpreter.PushStack(interpreter.PopStack() - 1)";
    public string Convert(Instruction.Jump instruction) => $"goto {instruction.Label};";
    public string Convert(Instruction.CJump instruction) => $"if (interpreter.PopStack())\n    goto {instruction.Label};";
    public string Convert(Instruction.Return instruction) => throw new NotImplementedException();
}