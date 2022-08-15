namespace Komodo.Compilation.Bytecode;

public class BasicBlock
{
    private List<Instruction> instructions = new List<Instruction>();

    public Function Parent { get; }
    public string Name { get; }

    public IEnumerable<Instruction> Instructions => instructions.AsEnumerable();

    public BasicBlock(Function parent, string name)
    {
        Parent = parent;
        Name = name;
    }

    public void Append(Instruction instruction) => instructions.Add(instruction);

    public Instruction this[int key] => instructions[key];
}