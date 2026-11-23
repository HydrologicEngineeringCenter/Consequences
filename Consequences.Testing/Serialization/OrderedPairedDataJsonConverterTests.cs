using System.Text.Json;
using System.Text.Json.Nodes;
using Consequences.Serialization;
using Numerics.Data;

namespace Consequences.Testing.Serialization;

public class OrderedPairedDataJsonConverterTests
{
    private const string CurveJson = """
        {
          "StrictX": true,
          "StrictY": false,
          "OrderX": 0,
          "OrderY": 1,
          "Ordinates": [
            { "X": -1, "Y": 0.75 },
            { "X": 2, "Y": 0.25 }
          ]
        }
        """;

    [Fact]
    public void Read_RestoresOrdinatesAndOrdering()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new OrderedPairedDataJsonConverter() }
        };

        OrderedPairedData? curve = JsonSerializer.Deserialize<OrderedPairedData>(CurveJson, options);

        Assert.NotNull(curve);
        Assert.True(curve.StrictX);
        Assert.False(curve.StrictY);
        Assert.Equal(SortOrder.Ascending, curve.OrderX);
        Assert.Equal(SortOrder.Descending, curve.OrderY);
        Assert.Equal(2, curve.Count);
        Assert.Equal(-1d, curve[0].X);
        Assert.Equal(0.75, curve[0].Y);
        Assert.Equal(2d, curve[1].X);
        Assert.Equal(0.25, curve[1].Y);
        Assert.True(curve.IsValid);
        Assert.Equal(0.5, curve.GetYFromX(0.5));
    }

    [Fact]
    public void Write_EmitsOnlyOrdinatesAndOrdering()
    {
        var curve = new OrderedPairedData(
            [-1, 2], [0.75, 0.25],
            strictOnX: true, SortOrder.Ascending,
            strictOnY: false, SortOrder.Descending);
        var options = new JsonSerializerOptions
        {
            Converters = { new OrderedPairedDataJsonConverter() }
        };

        string json = JsonSerializer.Serialize(curve, options);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(CurveJson), JsonNode.Parse(json)));
    }
}
