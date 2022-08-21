using System.Text.Json.Nodes;

namespace Komodo.Utilities;

public interface JsonSchema
{
    public void Validate(JsonNode? node);

    public class String : JsonSchema
    {
        private Func<string, bool>? validator;

        public String(Func<string, bool>? validator = null) => this.validator = validator;

        public void Validate(JsonNode? node)
        {
            string? value = null;

            try { value = node!.GetValue<string>(); }
            catch { throw new Exception($"{node} is not a string"); }

            if (validator is not null && !validator(value))
                throw new Exception($"Invalid value: {value}");
        }

        public static String OfEnum<T>() where T : struct, Enum => new String(s => Enum.GetNames<T>().Contains(s));
    }

    public class Object : JsonSchema
    {
        public record Property(string Name, JsonSchema Schema, bool Required);

        private Dictionary<string, Property> properties;

        public Object(IEnumerable<Property> properties)
            => this.properties = new Dictionary<string, Property>(properties.Select(p => new KeyValuePair<string, Property>(p.Name, p)));

        public void Validate(JsonNode? node)
        {
            JsonObject? value = null;

            try { value = node!.AsObject(); }
            catch { throw new Exception($"{node} is not an object"); }

            var remainingProperties = properties.Keys.ToHashSet();

            foreach (var (name, propertyNode) in value)
            {
                if (!properties.ContainsKey(name))
                    throw new Exception($"Unexpected property: {name}");

                var property = properties[name];
                if (!remainingProperties.Remove(name))
                    throw new Exception($"Repeated property: {name}");

                property.Schema.Validate(propertyNode);
            }

            if (remainingProperties.Count != 0)
                throw new Exception($"Missing required properties: {Utility.Stringify(remainingProperties, ", ")}");
        }
    }
}