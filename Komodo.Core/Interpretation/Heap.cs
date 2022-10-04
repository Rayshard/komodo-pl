namespace Komodo.Core.Interpretation;

using Address = UInt64;

public record Chunk(Address Address, Byte[] Data)
{
    public UInt64 Size => (UInt64)Data.Length;
    public Address Next => Address + Size;
}

public class Heap
{
    private List<Chunk> memory = new List<Chunk> { new Chunk(0, new Byte[1]) };

    // A map of chunk addresses to their indices in memory
    private Dictionary<Address, int> allocatedChunks = new Dictionary<Address, int>();

    // A map of chunk size to a set of indicies for chunks in memoery with that size
    private Dictionary<UInt64, HashSet<int>> freeChunks = new Dictionary<UInt64, HashSet<int>>();

    public Address Allocate(UInt64 size)
    {
        Address address;

        if (freeChunks.TryGetValue(size, out var indices))
        {
            var index = indices.First();
            indices.Remove(index);

            if (indices.Count == 0)
                freeChunks.Remove(size);

            address = memory[index].Address;
            memory[index] = new Chunk(address, new Byte[size]);

            allocatedChunks.Add(address, index);
        }
        else
        {
            //TODO: Try to find a fitting chunk

            address = memory[memory.Count - 1].Next;

            memory.Add(new Chunk(address, new Byte[size]));
            allocatedChunks.Add(address, memory.Count - 1);
        }

        return address;
    }

    public Address Allocate(IEnumerable<Byte> data)
    {
        var bytes = data.ToArray();
        var address = Allocate((UInt64)bytes.Length);

        memory[allocatedChunks[address]] = new Chunk((UInt64)bytes.Length, bytes);
        return address;
    }

    public bool Free(Address address)
    {
        if (allocatedChunks.Remove(address, out var index))
        {
            var chunkSize = memory[index].Size;

            if (freeChunks.TryGetValue(chunkSize, out var indices)) { indices.Add(index); }
            else { freeChunks.Add(chunkSize, new HashSet<int>() { index }); }
        }

        return false;
    }
}