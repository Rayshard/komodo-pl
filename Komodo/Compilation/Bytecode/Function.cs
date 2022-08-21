namespace Komodo.Compilation.Bytecode;

public class Function
{
    public const string ENTRY_NAME = "__entry__";

    public string Name { get; }
    public DataType ReturnType { get; }

    private Dictionary<string, BasicBlock> basicBlocks = new Dictionary<string, BasicBlock>();
    public IEnumerable<BasicBlock> BasicBlocks => basicBlocks.Values;

    private DataType[] arguments;
    public IEnumerable<DataType> Arguments => arguments;

    private Dictionary<string, DataType> locals;

    public Function(string name, IEnumerable<DataType> args, IEnumerable<KeyValuePair<string, DataType>> locals, DataType returnType)
    {
        Name = name;
        ReturnType = returnType;

        arguments = args.ToArray();
        this.locals = new Dictionary<string, DataType>(locals);
    }

    public void AddBasicBlock(BasicBlock basicBlock) => basicBlocks.Add(basicBlock.Name, basicBlock);
    public BasicBlock GetBasicBlock(string name) => basicBlocks[name];

    public DataType GetLocal(string name) => locals[name];
}