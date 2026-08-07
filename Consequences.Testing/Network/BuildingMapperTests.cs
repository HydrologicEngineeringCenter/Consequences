using Consequences.Buildings;
using Consequences.Network;
using Consequences.Network.DTOs;
using Consequences.Network.Mapping;
using Consequences.Occupancy;
using Consequences.Receptors;
using Consequences.Stability;
using Numerics.Data;

namespace Consequences.Testing.Network;

public class BuildingMapperTests
{
    private static NsiStructure Residence() =>
        NsiJsonParser.ParseFeatureCollection(NsiSamples.FeatureCollectionJson)[0];

    private static NsiStructure Office() =>
        NsiJsonParser.ParseFeatureCollection(NsiSamples.FeatureCollectionJson)[1];

    [Fact]
    public void Map_ResolvesOccupancyTypeByName()
    {
        Building building = BuildingMapper.WithDefaultOccupancyTypes().Map(Residence());

        Assert.Equal("RES1-1SNB", building.OccupancyType.Name);
    }

    [Fact]
    public void Map_CarriesValuesAndFoundationHeight()
    {
        Building building = BuildingMapper.WithDefaultOccupancyTypes().Map(Residence());

        Assert.Equal(210000f, building.Value);
        Assert.Equal(105000f, building.ContentValue);
        Assert.Equal(1.5f, building.FoundationHeight);
        Assert.Null(building.StabilityThreshold);
    }

    [Fact]
    public void Map_AttachesTheConfiguredStabilityThreshold()
    {
        StabilityThreshold threshold = new(new OrderedPairedData(
            [0, 10], [10, 1],
            strictOnX: true, SortOrder.Ascending, strictOnY: true, SortOrder.Descending));

        BuildingMapper mapper = new(OccupancyTypeDefaults.GetDefaults())
        {
            StabilityThreshold = threshold,
        };

        Assert.Same(threshold, mapper.Map(Residence()).StabilityThreshold);
    }

    [Fact]
    public void Map_ThrowsOnAnUnknownOccupancyType()
    {
        NsiStructure unknown = Residence() with { Occtype = "NOT-AN-OCCTYPE" };

        KeyNotFoundException e = Assert.Throws<KeyNotFoundException>(
            () => BuildingMapper.WithDefaultOccupancyTypes().Map(unknown));

        Assert.Contains("NOT-AN-OCCTYPE", e.Message);
        Assert.Contains(unknown.FdId.ToString(), e.Message);
    }

    [Fact]
    public void TryMap_ReportsAnUnknownOccupancyTypeWithoutThrowing()
    {
        NsiStructure unknown = Residence() with { Occtype = "NOT-AN-OCCTYPE" };

        Assert.False(BuildingMapper.WithDefaultOccupancyTypes().TryMap(unknown, out _));
    }

    [Fact]
    public void Map_ProducesABuildingThatComputesDamage()
    {
        Building building = BuildingMapper.WithDefaultOccupancyTypes().Map(Residence());

        // 4 ft of water, 1.5 ft of it absorbed by the foundation.
        DamageResult damage = building.Compute(4f);

        Assert.True(damage.Structure > 0);
        Assert.True(damage.Structure < building.Value);
    }

    [Fact]
    public void Map_ResolvesNonResidentialOccupancyTypesToo()
    {
        Building building = BuildingMapper.WithDefaultOccupancyTypes().Map(Office());

        Assert.Equal("COM1", building.OccupancyType.Name);
        Assert.Equal(940000f, building.Value);
        Assert.Equal(0f, building.FoundationHeight);
    }
}
