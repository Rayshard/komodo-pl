namespace Komodo.Compilation.Bytecode;

public interface Converter<TProgram, TModule, TFunction, TInstruction>
{
    public TProgram Convert(Program program);
    public TModule Convert(Module module);
    public TFunction Convert(Function function);
    public TInstruction Convert(Instruction instruction);
}