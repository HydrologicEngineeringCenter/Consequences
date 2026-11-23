using System.Text.Json;
using Consequences.Occupancy;
using Numerics.Data;

namespace Consequences.Testing.Serialization;

public class OccupancyTypeSerializationTests
{
    [Fact]
    public void RoundTrip_RestoresOccupancyAndDamageFunctions()
    {
        var original = new OccupancyType
        {
            Name = "Example",
            StructureDamageFunction = new OrderedPairedData(
                [-1, 2], [0.25, 0.75],
                strictOnX: true, SortOrder.Ascending,
                strictOnY: false, SortOrder.Ascending),
            ContentDamageFunction = new OrderedPairedData(
                [-2, 4], [0.875, 0.125],
                strictOnX: false, SortOrder.Ascending,
                strictOnY: true, SortOrder.Descending),
            FoundationHeightOffset = 0.5f,
            StructureValuePercentageOfTheMean = 1.25f,
            ContentValuePercentageOfTheMean = 0.75f,
            CollectivelyWarned = true,
            CollectivelyMobilize = true,
            VehicleOccupancyRate = 4,
            FromFloorDepthThreshold = 2.5f,
            FromCeilingDepthThreshold = 3.5f
        };

        string json = JsonSerializer.Serialize(original);
        OccupancyType? restored = JsonSerializer.Deserialize<OccupancyType>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.FoundationHeightOffset, restored.FoundationHeightOffset);
        Assert.Equal(original.StructureValuePercentageOfTheMean, restored.StructureValuePercentageOfTheMean);
        Assert.Equal(original.ContentValuePercentageOfTheMean, restored.ContentValuePercentageOfTheMean);
        Assert.Equal(original.CollectivelyWarned, restored.CollectivelyWarned);
        Assert.Equal(original.CollectivelyMobilize, restored.CollectivelyMobilize);
        Assert.Equal(original.VehicleOccupancyRate, restored.VehicleOccupancyRate);
        Assert.Equal(original.FromFloorDepthThreshold, restored.FromFloorDepthThreshold);
        Assert.Equal(original.FromCeilingDepthThreshold, restored.FromCeilingDepthThreshold);
        AssertCurve(original.StructureDamageFunction, restored.StructureDamageFunction);
        AssertCurve(original.ContentDamageFunction, restored.ContentDamageFunction);
    }

    private static void AssertCurve(OrderedPairedData expected, OrderedPairedData actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.StrictX, actual.StrictX);
        Assert.Equal(expected.StrictY, actual.StrictY);
        Assert.Equal(expected.OrderX, actual.OrderX);
        Assert.Equal(expected.OrderY, actual.OrderY);
        Assert.Equal(expected.Count, actual.Count);
        Assert.True(actual.IsValid);

        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].X, actual[i].X);
            Assert.Equal(expected[i].Y, actual[i].Y);
        }

        double midpoint = (expected[0].X + expected[1].X) / 2;
        Assert.Equal(expected.GetYFromX(midpoint), actual.GetYFromX(midpoint));
    }
}
