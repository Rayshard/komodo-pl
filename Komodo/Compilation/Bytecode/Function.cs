namespace Komodo.Compilation.Bytecode;


public class Function
{
    public const string ENTRY_NAME = "__entry__";

    private Dictionary<string, BasicBlock> basicBlocks = new Dictionary<string, BasicBlock>();

    public Module Parent {get;}
    public string Name { get; }

    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;
    public BasicBlock Entry => basicBlocks[ENTRY_NAME];

    public Function(Module parent, string name)
    {
        Parent = parent;
        Name = name;

        CreateBasicBlock(ENTRY_NAME);
    }

    public BasicBlock CreateBasicBlock(string name) 
    {
        basicBlocks.Add(name, new BasicBlock(this, name));
        return GetBasicBlock(name);
    }

    public BasicBlock GetBasicBlock(string name) => basicBlocks[name];
}