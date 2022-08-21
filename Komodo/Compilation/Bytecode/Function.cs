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

    private DataType[] locals;
    public IEnumerable<DataType> Locals => locals;

    public Function(string name, IEnumerable<DataType> arguments, IEnumerable<DataType> locals, DataType returnType)
    {
        Name = name;
        ReturnType = returnType;

        this.arguments = arguments.ToArray();
        this.locals = locals.ToArray();
    }

    public void AddBasicBlock(BasicBlock basicBlock) => basicBlocks.Add(basicBlock.Name, basicBlock);
    public BasicBlock GetBasicBlock(string name) => basicBlocks[name];

    public DataType GetArgument(int idx) => arguments[idx];
    public DataType GetLocal(int idx) => locals[idx];
}