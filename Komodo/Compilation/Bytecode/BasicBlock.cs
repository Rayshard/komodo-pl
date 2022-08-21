namespace Komodo.Compilation.Bytecode;

public class BasicBlock
{
    public string Name { get; }

    private List<Instruction> instructions = new List<Instruction>();
    public IEnumerable<Instruction> Instructions => instructions;

    public BasicBlock(string name) => Name = name;

    public void Append(Instruction instruction) => instructions.Add(instruction);

    public Instruction this[int idx] => instructions[idx];
}