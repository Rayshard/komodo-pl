using Komodo.Utilities;

namespace Komodo.Compilation.Bytecode;

public class BasicBlock
{
    public string Name { get; }

    private List<Instruction> instructions = new List<Instruction>();
    public IEnumerable<Instruction> Instructions => instructions;

    public BasicBlock(string name) => Name = name;

    public void Append(Instruction instruction) => instructions.Add(instruction);

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("basicBlock"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));
        nodes.AddRange(Instructions.Select(instr => instr.AsSExpression()));
        return new SExpression.List(nodes);
    }

    public Instruction this[int idx] => instructions[idx];

    public static BasicBlock Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList().ExpectLength(3, null);
        list[0].ExpectUnquotedSymbol().ExpectValue("basicBlock");

        var name = list[1].ExpectUnquotedSymbol().Value;
        var basicBlock = new BasicBlock(name);

        foreach (var item in list.Skip(2))
            basicBlock.Append(Instruction.Deserialize(item));

        return basicBlock;
    }

}