namespace Komodo.Core.Interpretation;

using Komodo.Core.Compilation.Bytecode;
using Komodo.Core.Utilities;
using Address = UInt64;

public record Chunk(Address Address, Byte[] Data)
{
    public UInt64 Size => (UInt64)Data.Length;
    public Address Next => Address + Size;

    public bool Contains(Address address) => address >= Address && address < Next;
}

public class Memory
{
    public static Address NULL = 0;

    private List<Chunk> chunks = new List<Chunk> { new Chunk(NULL, new Byte[1]) };

    // A map of chunk addresses to their indices in chunks
    private Dictionary<Address, int> allocatedChunks = new Dictionary<Address, int> { { NULL, 0 } };

    // A map of chunk size to a set of indicies for chunks in memoery with that size
    private Dictionary<UInt64, HashSet<int>> freeChunks = new Dictionary<UInt64, HashSet<int>>();

    public Address Allocate(UInt64 size)
    {
        if (size == 0)
            throw new Exception("Unable to allocate memory of size 0");

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

    public Address Allocate(IEnumerable<Byte> data)
    {
        var bytes = data.ToArray();
        var address = Allocate((UInt64)bytes.Length);

        chunks[allocatedChunks[address]] = new Chunk(address, bytes);
        return address;
    }

    public void Free(Address address)
    {
        if (address == 0) { throw new Exception("Unable to free address: 0x0000000000000000"); }
        if (!allocatedChunks.Remove(address, out var index)) { throw new Exception($"Unable to free chunks: address 0x{address.ToString("X")} is not the start of an allocated chunk."); }

        var chunkSize = chunks[index].Size;

        if (freeChunks.TryGetValue(chunkSize, out var indices)) { indices.Add(index); }
        else { freeChunks.Add(chunkSize, new HashSet<int>() { index }); }
    }

    public Value Read(DataType dataType, Address address)
    {
        var chunk = GetContainingChunk(address);

        if (address + dataType.ByteSize - 1 >= chunk.Next)
            throw new Exception($"Unable to read memory: address range [0x{address.ToString("X")}, 0x{(address + dataType.ByteSize - 1).ToString("X")}] crosses a chunk boundary.");

        var bytes = chunk.Data.SubArray((int)(address - chunk.Address), (int)dataType.ByteSize);

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

    public Value[] Read(DataType dataType, Address start, UInt64 count)
    {
        var buffer = new Value[count];
        var address = start;

        for (UInt64 i = 0; i < count; i++)
        {
            buffer[i] = Read(dataType, address);
            address += dataType.ByteSize;
        }

        return buffer;
    }


    private Chunk GetContainingChunk(Address address)
    {
        foreach (var chunk in chunks)
        {
            if (chunk.Contains(address))
                return chunk;
        }

        throw new Exception($"Unable to get containing chunk: address 0x{address.ToString("X")} is not in memory range.");
    }
}