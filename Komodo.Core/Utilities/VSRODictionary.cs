using System.Collections.ObjectModel;

namespace Komodo.Core.Utilities;

public class VSRODictionary<K, V> : ReadOnlyDictionary<K, V> where K : notnull
{
    public VSRODictionary(IEnumerable<V> values, Func<V, K> keySelector) : base(values.ToDictionary(keySelector)) { }
    public VSRODictionary(IEnumerable<KeyValuePair<K, V>> values) : base(values.ToDictionary()) { }

    public static bool operator ==(VSRODictionary<K, V> obj1, VSRODictionary<K, V> obj2) => obj1.Equals(obj2);
    public static bool operator !=(VSRODictionary<K, V> obj1, VSRODictionary<K, V> obj2) => !(obj1 == obj2);

    public bool Equals(VSRODictionary<K, V>? other)
    {
        if (other is null) { return false; }
        else if (ReferenceEquals(this, other)) { return true; }
        else if (other.Count != this.Count) { return false; }

        foreach (var (key, value) in other)
        {
            if (TryGetValue(key, out var expected))
            {
                if (value is null && expected is null) { continue; }
                else if (value is not null && !value.Equals(expected)) { return false; }
            }
            else { return false; }
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as VSRODictionary<K, V>);
    public override int GetHashCode() => this.GetOrderIndependentHashCode();
}