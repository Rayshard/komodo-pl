using System.Text.Json.Nodes;
using System.Xml;
using Komodo.Utilities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Komodo.Compilation.Bytecode;

public static class Formatter
{

    public static XmlElement Serialize(XmlDocument document, Bytecode.Instruction instruction)
    {
        XmlElement element = document.CreateElement(instruction.Opcode.ToString());

        switch (instruction)
        {
            case Bytecode.Instruction.Syscall instr: element.SetAttribute("code", instr.Code.ToString()); break;
            case Bytecode.Instruction.PushI64 instr: element.SetAttribute("value", instr.Value.ToString()); break;
            case Bytecode.Instruction.AddI64: break;
            default: throw new NotImplementedException(instruction.Opcode.ToString());
        }

        return element;
    }

    public static XmlElement Serialize(XmlDocument document, Bytecode.BasicBlock basicBlock)
    {
        XmlElement element = document.CreateElement("basicBlock");
        element.SetAttribute("name", basicBlock.Name);

        foreach (var instruction in basicBlock.Instructions)
            element.AppendChild(Serialize(document, instruction));

        return element;
    }

    public static XmlElement Serialize(XmlDocument document, Bytecode.Function function)
    {
        XmlElement element = document.CreateElement("function");
        element.SetAttribute("name", function.Name);

        foreach (var bb in function.BasicBlocks)
            element.AppendChild(Serialize(document, bb));

        return element;
    }

    public static XmlElement Serialize(XmlDocument document, Bytecode.Module module)
    {
        XmlElement element = document.CreateElement("module");
        element.SetAttribute("name", module.Name);

        foreach (var function in module.Functions)
            element.AppendChild(Serialize(document, function));

        return element;
    }

    public static XmlDocument Serialize(Bytecode.Program program)
    {
        XmlDocument document = new XmlDocument();
        XmlElement element = document.CreateElement("program");

        element.SetAttribute("name", program.Name);
        element.SetAttribute("entryModule", program.Entry.Module);
        element.SetAttribute("entryFunction", program.Entry.Function);

        foreach (var module in program.Modules)
            element.AppendChild(Serialize(document, module));

        document.AppendChild(element);
        return document;
    }

    private static IEnumerable<T> DeserializeArray<T>(JToken json, Func<JToken, T> deserializer) => json.Select(t => deserializer(t!));
    private static T DeserializeEnum<T>(JToken json) where T : struct, Enum => Enum.Parse<T>(json.ToString());

    public static Program DeserializeProgram(JToken json)
    {
        Utility.ValidateJSON(json, Schemas.Program());

        var name = json["name"]!.ToString();
        var entry = (json["entry"]!["module"]!.ToString(), json["entry"]!["function"]!.ToString());
        var modules = DeserializeArray(json["modules"]!, DeserializeModule);

        var program = new Program(name, entry);

        foreach (var module in modules)
            program.AddModule(module);

        return program;
    }

    public static (string Module, string Function) DeserializeProgramEntry(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("module")) { throw new Exception($"Expected property: module"); }
        else if (!jsonObject.ContainsKey("function")) { throw new Exception($"Expected property: function"); }
        else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

        var module = jsonObject["module"]!.GetValue<string>();
        var function = jsonObject["function"]!.GetValue<string>();
        return (module, function);
    }

    public static Module DeserializeModule(JToken json)
    {
        Utility.ValidateJSON(json, Schemas.Module());

        var name = json["name"]!.ToString();
        var functions = DeserializeArray(json["functions"]!, DeserializeFunction);

        var module = new Module(name);

        foreach (var function in functions)
            module.AddFunction(function);

        return module;
    }

    public static Function DeserializeFunction(JToken json)
    {
        Utility.ValidateJSON(json, Schemas.Function());

        var name = json["name"]!.ToString();
        var args = DeserializeArray(json["args"]!, DeserializeEnum<DataType>);
        var locals = DeserializeArray(json["locals"]!, DeserializeEnum<DataType>);
        var ret = DeserializeEnum<DataType>(json["ret"]!);
        var basicBlocks = DeserializeArray(json["basicBlocks"]!, DeserializeBasicBlock);

        var function = new Function(name, args, locals, ret);

        foreach (var basicBlock in basicBlocks)
            function.AddBasicBlock(basicBlock);

        return function;
    }

    public static BasicBlock DeserializeBasicBlock(JToken json)
    {
        Utility.ValidateJSON(json, Schemas.BasicBlock());

        var name = json["name"]!.ToString();
        var instructions = DeserializeArray(json["instructions"]!, DeserializeInstruction);

        var basicBlock = new BasicBlock(name);

        foreach (var instruction in instructions)
            basicBlock.Append(instruction);

        return basicBlock;
    }

    public static Instruction DeserializeInstruction(JToken json)
    {
        Utility.ValidateJSON(json, Schemas.Instruction());

        var opcode = DeserializeEnum<Opcode>(json["opcode"]!);

        switch (opcode)
        {
            case Opcode.PushI64: return new Instruction.PushI64((Int64)json["value"]!);
            case Opcode.AddI64: return new Instruction.AddI64();
            case Opcode.Syscall: return new Instruction.Syscall(DeserializeEnum<SyscallCode>(json["code"]!));
            default: throw new Exception($"Unexpected opcode: {opcode}");
        }
    }
}