namespace Komodo.Core.Compilation.Bytecode;

public abstract class Converter<T>
{
    public abstract T Convert(Program program);
}