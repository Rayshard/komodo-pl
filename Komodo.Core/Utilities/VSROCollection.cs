using System.Collections.ObjectModel;

namespace Komodo.Core.Utilities;

public class VSROCollection<T> : ReadOnlyCollection<T>
{
    public VSROCollection(IEnumerable<T> values) : base(values.ToArray()) { }

    public static bool operator ==(VSROCollection<T> obj1, VSROCollection<T> obj2) => obj1.Equals(obj2);
    public static bool operator !=(VSROCollection<T> obj1, VSROCollection<T> obj2) => !(obj1 == obj2);

    public bool Equals(VSROCollection<T>? other)
        => other is not null && (ReferenceEquals(this, other) || other.SequenceEqual(this));

    public override bool Equals(object? obj) => Equals(obj as VSROCollection<T>);

    public override int GetHashCode() => this.GetOrderDependentHashCode();
}