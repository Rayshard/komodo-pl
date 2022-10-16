using System.Text;
using Komodo.Core.Utilities;

namespace Komodo.Core.Compilation.Bytecode;

public record Data(string Name, VSROCollection<Byte> Bytes, bool IsReadonly)
{
    public static Data Deserialize(SExpression sexpr)
    {
        var list = sexpr.ExpectList()
                        .ExpectLength(4, null)
                        .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("data"))
                        .ExpectItem(1, item => item.ExpectUnquotedSymbol().ExpectValue("RO", "RW").Value == "RO", out var isReadonly)
                        .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var name);

        IEnumerable<Byte> bytes;

        if (list[3] is SExpression.QuotedSymbol qs) { bytes = Encoding.UTF8.GetBytes(qs.Value); }
        else
        {
            list.ExpectLength(5, null)
                .ExpectItem(3, DataType.DeserializePrimitive, out var dataType);

            Func<SExpression, IEnumerable<Byte>> deserializer = dataType switch
            {
                DataType.Primitive.I8 => sexpr => sexpr.ExpectInt8().GetBytes(),
                DataType.Primitive.UI8 => sexpr => sexpr.ExpectUInt8().GetBytes(),
                DataType.Primitive.I16 => sexpr => sexpr.ExpectInt16().GetBytes(),
                DataType.Primitive.UI16 => sexpr => sexpr.ExpectUInt16().GetBytes(),
                DataType.Primitive.I32 => sexpr => sexpr.ExpectInt32().GetBytes(),
                DataType.Primitive.UI32 => sexpr => sexpr.ExpectUInt32().GetBytes(),
                DataType.Primitive.I64 => sexpr => sexpr.ExpectInt64().GetBytes(),
                DataType.Primitive.UI64 => sexpr => sexpr.ExpectUInt64().GetBytes(),
                DataType.Primitive.F32 => sexpr => sexpr.ExpectFloat().GetBytes(),
                DataType.Primitive.F64 => sexpr => sexpr.ExpectDouble().GetBytes(),
                DataType.Primitive.Bool => sexpr => sexpr.ExpectBool().GetBytes(),
                var dt => throw new NotImplementedException(dt.ToString())
            };

            list.ExpectItems(deserializer, out var elements, 4);
            bytes = elements.Flatten();
        }

        return new Data(name, bytes.ToVSROCollection(), isReadonly);
    }
}

public record Global(string Name, DataType DataType)
{
    public static Global Deserialize(SExpression sexpr)
    {
        sexpr.ExpectList()
             .ExpectLength(3)
             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("global"))
             .ExpectItem(1, DataType.Deserialize, out var dataType)
             .ExpectItem(2, item => item.ExpectUnquotedSymbol().Value, out var name);

        return new Global(name, dataType);
    }
}

public record Module(
    string Name,
    VSRODictionary<string, Data> Data,
    VSRODictionary<string, Global> Globals,
    VSRODictionary<string, Function> Functions
)
{
    public bool HasData(string name) => Data.ContainsKey(name);
    public bool HasGlobal(string name) => Globals.ContainsKey(name);
    public bool HasFunction(string name) => Functions.ContainsKey(name);

    public SExpression AsSExpression()
    {
        var nodes = new List<SExpression>();
        nodes.Add(new SExpression.UnquotedSymbol("module"));
        nodes.Add(new SExpression.UnquotedSymbol(Name));
        nodes.AddRange(Functions.Values.Select(function => function.AsSExpression()));

        return new SExpression.List(nodes);
    }

    public static Module Deserialize(SExpression sexpr) => new ModuleBuilder(sexpr).Build();
}

public class ModuleBuilder
{
    private string? name;
    public Dictionary<string, Data> data = new Dictionary<string, Data>();
    private Dictionary<string, Global> globals = new Dictionary<string, Global>();
    private Dictionary<string, Function> functions = new Dictionary<string, Function>();

    public ModuleBuilder(SExpression sexpr)
    {
        var remaining = sexpr.ExpectList()
                             .ExpectLength(2, null)
                             .ExpectItem(0, item => item.ExpectUnquotedSymbol().ExpectValue("module"))
                             .ExpectItem(1, item => item.ExpectUnquotedSymbol().Value, out var name)
                             .Skip(2);

        SetName(name);

        foreach (var item in remaining)
        {
            item.ExpectList()
                .ExpectLength(1, null)
                .ExpectItem(0, item => item.ExpectUnquotedSymbol().Value, out var label);

            switch (label)
            {
                case "data": AddData(Data.Deserialize(item)); break;
                case "global": AddGlobal(Global.Deserialize(item)); break;
                case "function": AddFunction(Function.Deserialize(item)); break;
                default: throw new SExpression.FormatException($"Invalid element: {item}", item);
            }
        }
    }

    public void SetName(string? value) => name = value;

    public void AddData(Data data) => this.data.Add(data.Name, data);
    public bool HasData(string name) => data.ContainsKey(name);
    public Data GetData(string name) => data[name];

    public void AddGlobal(Global global) => globals.Add(global.Name, global);
    public bool HasGlobal(string name) => globals.ContainsKey(name);
    public Global GetGlobal(string name) => globals[name];

    public void AddFunction(Function function) => functions.Add(function.Name, function);
    public bool HasFunction(string name) => functions.ContainsKey(name);
    public Function GetFunction(string name) => functions[name];

    public Module Build()
    {
        var name = this.name ?? throw new Exception("Name is not set");
        var data = this.data.ToVSRODictionary();
        var globals = this.globals.ToVSRODictionary();
        var functions = this.functions.ToVSRODictionary();

        return new Module(name, data, globals, functions);
    }
}