using System.Text.Json.Nodes;
using System.Xml;

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

    public static IEnumerable<T> DeserializeArray<T>(JsonNode node, Func<JsonNode, T> deserializer)
    {
        if (node is not JsonArray)
            throw new Exception($"Unexpected node: {node}");

        return node.AsArray().Select(n => deserializer(n!));
    }

    private static T DeserializeEnum<T>(JsonNode node) where T : struct => Enum.Parse<T>(node.GetValue<string>());

    public static Program DeserializeProgram(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("name")) { throw new Exception($"Expected property: name"); }
        else if (!jsonObject.ContainsKey("entry")) { throw new Exception($"Expected property: entry"); }
        else if (!jsonObject.ContainsKey("modules")) { throw new Exception($"Expected property: modules"); }
        else if (jsonObject.Count != 3) { throw new Exception($"Object has unexpected properties."); }

        var name = jsonObject["name"]!.GetValue<string>();
        var entry = DeserializeProgramEntry(jsonObject["entry"]!);
        var modules = DeserializeArray(jsonObject["modules"]!, DeserializeModule);

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

    public static Module DeserializeModule(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("name")) { throw new Exception($"Expected property: name"); }
        else if (!jsonObject.ContainsKey("functions")) { throw new Exception($"Expected property: functions"); }
        else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

        var name = jsonObject["name"]!.GetValue<string>();
        var functions = DeserializeArray(jsonObject["functions"]!, DeserializeFunction);

        var module = new Module(name);

        foreach (var function in functions)
            module.AddFunction(function);

        return module;
    }

    public static Function DeserializeFunction(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("name")) { throw new Exception($"Expected property: name"); }
        else if (!jsonObject.ContainsKey("args")) { throw new Exception($"Expected property: args"); }
        else if (!jsonObject.ContainsKey("locals")) { throw new Exception($"Expected property: locals"); }
        else if (!jsonObject.ContainsKey("ret")) { throw new Exception($"Expected property: ret"); }
        else if (!jsonObject.ContainsKey("basicBlocks")) { throw new Exception($"Expected property: basicBlocks"); }
        else if (jsonObject.Count != 5) { throw new Exception($"Object has unexpected properties."); }

        var name = jsonObject["name"]!.GetValue<string>();
        var args = DeserializeArray(jsonObject["args"]!, DeserializeEnum<DataType>);
        var locals = DeserializeArray(jsonObject["locals"]!, DeserializeFunctionLocal);
        var ret = DeserializeEnum<DataType>(jsonObject["ret"]!);
        var basicBlocks = DeserializeArray(jsonObject["basicBlocks"]!, DeserializeBasicBlock);

        var function = new Function(name, args, locals, ret);

        foreach (var basicBlock in basicBlocks)
            function.AddBasicBlock(basicBlock);

        return function;
    }

    public static KeyValuePair<string, DataType> DeserializeFunctionLocal(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("name")) { throw new Exception($"Expected property: name"); }
        else if (!jsonObject.ContainsKey("type")) { throw new Exception($"Expected property: type"); }
        else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

        var name = jsonObject["name"]!.GetValue<string>();
        var dataType = DeserializeEnum<DataType>(jsonObject["type"]!);
        return new KeyValuePair<string, DataType>(name, dataType);
    }

    public static BasicBlock DeserializeBasicBlock(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("name")) { throw new Exception($"Expected property: name"); }
        else if (!jsonObject.ContainsKey("instructions")) { throw new Exception($"Expected property: instructions"); }
        else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

        var name = jsonObject["name"]!.GetValue<string>();
        var instructions = DeserializeArray(jsonObject["instructions"]!, DeserializeInstruction);

        var basicBlock = new BasicBlock(name);

        foreach (var instruction in instructions)
            basicBlock.Append(instruction);

        return basicBlock;
    }

    public static Instruction DeserializeInstruction(JsonNode node)
    {
        if (node is not JsonObject)
            throw new Exception($"Unexpected node: {node}");

        var jsonObject = node.AsObject();

        if (!jsonObject.ContainsKey("opcode"))
            throw new Exception($"Expected property: opcode");

        var opcode = Enum.Parse<Opcode>(jsonObject["opcode"]!.GetValue<string>());

        switch (opcode)
        {
            case Opcode.PushI64:
                {
                    if (!jsonObject.ContainsKey("value")) { throw new Exception($"Expected property: value"); }
                    else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

                    var value = jsonObject["value"]!.GetValue<Int64>();
                    return new Instruction.PushI64(value);
                }
            case Opcode.AddI64:
                {
                    if (jsonObject.Count != 1)
                        throw new Exception($"Object has unexpected properties.");

                    return new Instruction.AddI64();
                }
            case Opcode.Syscall:
                {
                    if (!jsonObject.ContainsKey("code")) { throw new Exception($"Expected property: code"); }
                    else if (jsonObject.Count != 2) { throw new Exception($"Object has unexpected properties."); }

                    var code = DeserializeEnum<SyscallCode>(jsonObject["code"]!);
                    return new Instruction.Syscall(code);
                }
            default: throw new Exception($"Unexpected opcode: {opcode}");
        }
    }
}