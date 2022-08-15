using System.Xml;

namespace Komodo.Compilation;

public static class XMLSerializer
{
    public static XmlElement Serialize(XmlDocument document, Bytecode.Instruction instruction)
    {
        XmlElement element = document.CreateElement(instruction.Opcode.ToString());

        switch (instruction)
        {
            case Bytecode.Instruction.Halt: break;
            case Bytecode.Instruction.PushI64(var value): element.SetAttribute("value", value.ToString()); break;
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
        document.AppendChild(document.CreateXmlDeclaration("1.0", "UTF-8", string.Empty));

        XmlElement element = document.CreateElement("program");
        element.SetAttribute("name", program.Name);
        element.SetAttribute("entryModule", program.Entry.Parent.Name);
        element.SetAttribute("entryFunction", program.Entry.Name);

        foreach (var module in program.Modules)
            element.AppendChild(Serialize(document, module));

        document.AppendChild(element);
        return document;
    }
}