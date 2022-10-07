namespace Komodo.Core.Interpretation;

using System.Text;
using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;

public record Chunk(Address Address, Byte[] Data)
{
    public UInt64 Size => (UInt64)Data.Length;
    public Address Next => Address + Size;

    public bool Contains(Address address) => address >= Address && address < Next;
}

public class Memory
{
    private List<Chunk> chunks = new List<Chunk> { new Chunk(Address.NULL, new Byte[1]) };

    // A map of chunk addresses to their indices in chunks
    private Dictionary<Address, int> allocatedChunks = new Dictionary<Address, int> { { Address.NULL, 0 } };

    // A map of chunk size to a set of indicies for chunks in memoery with that size
    private Dictionary<UInt64, HashSet<int>> freeChunks = new Dictionary<UInt64, HashSet<int>>();

    public Address Allocate(UInt64 size)
    {
        if (size == 0)
            throw new InterpreterException("Unable to allocate memory of size 0");

        Address address;

        if (freeChunks.TryGetValue(size, out var indices))
        {
            var index = indices.First();
            indices.Remove(index);

            if (indices.Count == 0)
                freeChunks.Remove(size);

            address = chunks[index].Address;
            chunks[index] = new Chunk(address, new Byte[size]);

            Console.WriteLine(address);
            allocatedChunks.Add(address, index);
        }
        else
        {
            //TODO: Try to find a fitting chunk

            address = chunks[chunks.Count - 1].Next;

            chunks.Add(new Chunk(address, new Byte[size]));
            allocatedChunks.Add(address, chunks.Count - 1);
        }

        return address;
    }

    public Address AllocateWrite(IEnumerable<Byte> data, bool prefixWithLength = false)
    {
        var bytes = data.ToArray();

        if (prefixWithLength)
            bytes = BitConverter.GetBytes((UInt64)bytes.Length).Concat(bytes).ToArray();

        var address = Allocate((UInt64)bytes.Length);

        chunks[allocatedChunks[address]] = new Chunk(address, bytes);
        return address;
    }

    public Address AllocateWrite(string data, bool prefixWithLength = false) => AllocateWrite(Encoding.UTF8.GetBytes(data), prefixWithLength);

    public Address AllocateWrite(Value value, bool prefixWithType = false)
    {
        if (prefixWithType)
        {
            var mangledString = value.DataType.AsMangledString();
            var typeData = BitConverter.GetBytes((UInt64)mangledString.Length).Concat(Encoding.UTF8.GetBytes(mangledString));

            return AllocateWrite(typeData.Concat(value.AsBytes()));
        }
        else { return AllocateWrite(value.AsBytes()); }
    }

    public Address AllocateWrite(IEnumerable<Value> values, bool prefixWithType = false)
    {
        if (prefixWithType)
        {
            IEnumerable<Byte> data = new Byte[0];

            foreach (Value value in values)
            {
                var mangledString = value.DataType.AsMangledString();
                var typeData = BitConverter.GetBytes((UInt64)mangledString.Length).Concat(Encoding.UTF8.GetBytes(mangledString));

                data = data.Concat(typeData).Concat(value.AsBytes());
            }

            return AllocateWrite(data);
        }
        else { return AllocateWrite(values.Select(value => value.AsBytes()).Flatten()); }
    }

    public void Free(Address address)
    {
        if (address.IsNull) { throw new InterpreterException("Unable to NULL address"); }
        if (!allocatedChunks.Remove(address, out var index)) { throw new InterpreterException($"Unable to free chunks: address {address} is not the start of an allocated chunk."); }

        var chunkSize = chunks[index].Size;

        if (freeChunks.TryGetValue(chunkSize, out var indices)) { indices.Add(index); }
        else { freeChunks.Add(chunkSize, new HashSet<int>() { index }); }
    }

    public Byte ReadByte(Address start)
    {
        var chunk = GetContainingChunk(start);
        return chunk.Data[(int)(start - chunk.Address)];
    }

    public Byte[] ReadBytes(Address start, UInt64 count)
    {
        if (count == 0)
            return new Byte[0];

        var chunk = GetContainingChunk(start);
        var end = new Address(start + count - 1);

        if (end >= chunk.Next)
            throw new InterpreterException($"Unable to read memory: address range [{start}, {end}] crosses a chunk boundary.");

        return chunk.Data.SubArray((int)(start - chunk.Address), (int)count); ;
    }

    public Value ReadValue(Address start, DataType dataType)
    {
        var bytes = ReadBytes(start, dataType.ByteSize);

        return dataType switch
        {
            DataType.UI8 => new Value.UI8(bytes[0]),
            DataType.I64 => new Value.I64(BitConverter.ToInt64(bytes)),
            DataType.UI64 => new Value.UI64(BitConverter.ToUInt64(bytes)),
            DataType.Bool => new Value.Bool(bytes[0] == 0),
            DataType.Array(var elementType) => new Value.Array(elementType, BitConverter.ToUInt64(bytes), BitConverter.ToUInt64(bytes, 8)),
            _ => throw new NotImplementedException(dataType.ToString())
        };
    }

    public Value ReadValue(Value.Reference reference) => ReadValue(reference.Address, reference.ValueType);

    public Value[] ReadValues(Address start, DataType dataType, UInt64 count)
    {
        var buffer = new Value[count];
        var address = start;

        for (UInt64 i = 0; i < count; i++)
        {
            buffer[i] = ReadValue(address, dataType);
            address += dataType.ByteSize;
        }

        return buffer;
    }

    public UInt64 ReadUInt64(Address start) => BitConverter.ToUInt64(ReadBytes(start, 8));
    public string ReadString(Address start) => Encoding.UTF8.GetString(ReadBytes(start + 8, ReadUInt64(start)));

    public Value ReadTypePrefixedValue(Address start)
    {
        var mangledString = ReadString(start);
        return ReadValue(start + (UInt64)Encoding.UTF8.GetByteCount(mangledString), DataType.Demangle(mangledString));
    }

    public void Write(Address start, Byte data)
    {
        if (start.IsNull)
            throw new InterpreterException("Unable to write to NULL");

        var chunk = GetContainingChunk(start);
        chunk.Data[(int)(start - chunk.Address)] = data;
    }

    public void Write(Address start, IEnumerable<Byte> data)
    {
        if (start.IsNull)
            throw new InterpreterException("Unable to write to NULL");

        var dataAsArray = data.ToArray();
        var chunk = GetContainingChunk(start);
        var end = new Address(start + ((UInt64)dataAsArray.Length - 1));

        if (end >= chunk.Next)
            throw new InterpreterException($"Unable to write memory: destination address range [{start}, {end}] crosses a chunk boundary.");

        Array.Copy(dataAsArray, chunk.Data, dataAsArray.Length);
    }

    public void Write(Address start, Value value, bool prefixWithType = false)
    {
        IEnumerable<Byte> data = value.AsBytes();

        if (prefixWithType)
        {
            var mangledString = value.DataType.AsMangledString();
            var typeData = BitConverter.GetBytes((UInt64)mangledString.Length).Concat(Encoding.UTF8.GetBytes(mangledString));

            data = typeData.Concat(data);
        }

        Write(start, data);
    }

    public void Write(Address start, IEnumerable<Value> values, bool prefixWithType = false)
    {
        UInt64 offset = 0;

        foreach (var value in values)
        {
            Write(start + offset, value);
            offset += value.ByteSize;
        }
    }

    public void Write(Address start, UInt64 data) => Write(start, BitConverter.GetBytes(data));

    private Chunk GetContainingChunk(Address address)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.Contains(address))
                return chunk;
        }

        throw new InterpreterException($"Unable to get containing chunk: address {address} is not in memory range.");
    }
}