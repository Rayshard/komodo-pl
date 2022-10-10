using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

namespace Komodo.Core.Interpretation;

public abstract record Sysfunc(VSROCollection<DataType> Parameters, VSROCollection<DataType> Returns, Address Address)
    : Value.Function(Parameters, Returns, Address)
{
    public abstract Value[] Call(Value[] args);

    public record Write(Address Address, Action<UI64, Array> Callback) : Sysfunc(
        new DataType[] { new DataType.UI64(), new DataType.Array(new DataType.UI8()) }.ToVSROCollection(),
        new DataType[] {}.ToVSROCollection(),
        Address
    )
    {
        public override Value[] Call(Value[] args)
        {
            var handle = args[0].As<UI64>();
            var data = args[1].As<Array>();

            Callback(handle, data);
            return new Value[0];
        }
    } 
}