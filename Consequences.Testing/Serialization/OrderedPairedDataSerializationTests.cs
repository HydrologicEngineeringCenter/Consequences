using System.Text.Json;
using System.Text.Json.Nodes;
using Numerics.Data;

namespace Consequences.Testing.Serialization;

public class OrderedPairedDataSerializationTests
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
        OrderedPairedData? curve = JsonSerializer.Deserialize<OrderedPairedData>(CurveJson);

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

        string json = JsonSerializer.Serialize(curve);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(CurveJson), JsonNode.Parse(json)));
    }
}
