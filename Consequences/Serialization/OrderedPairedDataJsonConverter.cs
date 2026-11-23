using System.Text.Json;
using System.Text.Json.Serialization;
using Numerics.Data;

namespace Consequences.Serialization;

public sealed class OrderedPairedDataJsonConverter : JsonConverter<OrderedPairedData>
{
    public override OrderedPairedData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        var ordinates = new List<Ordinate>();
        foreach (JsonElement point in GetRequiredProperty(root, "Ordinates").EnumerateArray())
        {
            ordinates.Add(new Ordinate(
                GetRequiredProperty(point, "X").GetDouble(),
                GetRequiredProperty(point, "Y").GetDouble()));
        }

        return new OrderedPairedData(
            ordinates,
            GetRequiredProperty(root, "StrictX").GetBoolean(),
            (SortOrder)GetRequiredProperty(root, "OrderX").GetInt32(),
            GetRequiredProperty(root, "StrictY").GetBoolean(),
            (SortOrder)GetRequiredProperty(root, "OrderY").GetInt32());
    }

    public override void Write(Utf8JsonWriter writer, OrderedPairedData value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("StrictX", value.StrictX);
        writer.WriteBoolean("StrictY", value.StrictY);
        writer.WriteNumber("OrderX", (int)value.OrderX);
        writer.WriteNumber("OrderY", (int)value.OrderY);

        writer.WriteStartArray("Ordinates");
        foreach (Ordinate point in value)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", point.X);
            writer.WriteNumber("Y", point.Y);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement property))
            throw new JsonException($"Required property '{name}' is missing.");

        return property;
    }
}
