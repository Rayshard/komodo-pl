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

    public static Program Deserialize(XmlDocument document)
    {
        Program? program = null;

        foreach (XmlNode node in document.ChildNodes)
        {
            if (node is XmlDeclaration) { continue; }
            else if (node is XmlElement && node.Name == "program" && program is null) { program = DeserializeProgram((XmlElement)node); }
            else { throw new Exception($"Unexpected node: {node.Name}"); }
        }

        return program ?? throw new InvalidDataException("Expected one 'program' element");
    }

    public static Program DeserializeProgram(XmlElement element)
    {
        if (element.Name != "program") { throw new InvalidDataException("Expected 'program' element"); }
        else if (!element.HasAttribute("entryModule")) { throw new InvalidDataException("Element has no attribute 'entryModule'"); }
        else if (!element.HasAttribute("entryFunction")) { throw new InvalidDataException("Element has no attribute 'entryFunction'"); }

        var program = new Program(element.Name, (element.GetAttribute("entryModule"), element.GetAttribute("entryFunction")));

        foreach (XmlNode node in element.ChildNodes)
        {
            if (node is not XmlElement)
                throw new InvalidDataException($"Unexpected node: {node.Name}");

            program.AddModule(DeserializeModule((XmlElement)node));
        }

        return program;
    }

    public static Module DeserializeModule(XmlElement element)
    {
        if (element.Name != "module") { throw new InvalidDataException("Expected 'module' element"); }
        else if (!element.HasAttribute("name")) { throw new InvalidDataException("Element has no attribute 'name'"); }

        var module = new Module(element.GetAttribute("name"));

        foreach (XmlNode node in element.ChildNodes)
        {
            if (node is not XmlElement)
                throw new InvalidDataException($"Unexpected node: {node.Name}");

            module.AddFunction(DeserializeFunction((XmlElement)node));
        }

        return module;
    }

    public static Function DeserializeFunction(XmlElement element)
    {
        if (element.Name != "function") { throw new InvalidDataException("Expected 'function' element"); }
        else if (!element.HasAttribute("name")) { throw new InvalidDataException("Element has no attribute 'name'"); }

        var function = new Function(element.GetAttribute("name"));

        foreach (XmlNode node in element.ChildNodes)
        {
            if (node is not XmlElement)
                throw new InvalidDataException($"Unexpected node: {node.Name}");

            switch(node.Name)
            {
                case "basicBlock": function.AddBasicBlock(DeserializeBasicBlock((XmlElement)node)); break;
                case "basicBlock": function.AddBasicBlock(DeserializeBasicBlock((XmlElement)node)); break;
                default: throw new InvalidDataException($"Unexpected node: {node.Name}");
            }
            
        }

        return function;
    }

    public static BasicBlock DeserializeBasicBlock(XmlElement element)
    {
        if (element.Name != "basicBlock") { throw new InvalidDataException("Expected 'basicBlock' element"); }
        else if (!element.HasAttribute("name")) { throw new InvalidDataException("Element has no attribute 'name'"); }

        var basicBlock = new BasicBlock(element.GetAttribute("name"));

        foreach (XmlNode node in element.ChildNodes)
        {
            if (node is not XmlElement)
                throw new InvalidDataException($"Unexpected node: {node.Name}");

            basicBlock.Append(DeserializeInstruction((XmlElement)node));
        }

        return basicBlock;
    }

    public static Instruction DeserializeInstruction(XmlElement element)
    {
        switch (element.Name)
        {
            case "PushI64":
                {
                    if (!element.HasAttribute("value")) { throw new InvalidDataException("Element has no attribute 'value'"); }
                    else if (element.Attributes.Count != 1) { throw new InvalidDataException("Element has too many attributes"); }

                    return new Instruction.PushI64(Int64.Parse(element.GetAttribute("value")));
                }
            case "AddI64":
                {
                    if (element.Attributes.Count != 0)
                        throw new InvalidDataException("Element has too many attributes");

                    return new Instruction.AddI64();
                }
            case "Syscall":
                {
                    if (!element.HasAttribute("code")) { throw new InvalidDataException("Element has no attribute 'code'"); }
                    else if (element.Attributes.Count != 1) { throw new InvalidDataException("Element has too many attributes"); }

                    try { return new Instruction.Syscall(Enum.Parse<SyscallCode>(element.GetAttribute("code"))); }
                    catch { throw new InvalidDataException($"'{element.GetAttribute("code")}' is not a valid Syscall code"); }
                }
            default: throw new InvalidDataException($"Unexpected element: {element}");
        }
    }
}