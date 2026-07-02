using Consequences.Buildings;
using Consequences.Network.DTOs;
using Consequences.Occupancy;

namespace Consequences.Network.Mapping;

/// <summary>
/// Projects an NSI structure onto a <see cref="Building"/>.
///
/// The JSON carries an occupancy type <em>name</em>; the domain needs an
/// <see cref="OccupancyType"/> with its damage curves. That lookup is why NSI cannot
/// be deserialized straight into <see cref="Building"/> — the mapper supplies what the
/// wire format doesn't.
/// </summary>
public sealed class BuildingMapper : INsiStructureMapper<Building>
{
    private readonly Dictionary<string, OccupancyType> _occupancyTypes;

    public BuildingMapper(IEnumerable<OccupancyType> occupancyTypes)
    {
        _occupancyTypes = occupancyTypes.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Uses the 40 shipped occupancy types, whose names match the NSI occtype strings
    /// </summary>
    public static BuildingMapper WithDefaultOccupancyTypes() => new(OccupancyTypeDefaults.GetDefaults());

    /// <summary>
    /// Stability thresholds are not an NSI attribute; supply one to attach it to every
    /// mapped building, or leave it null for damage-only runs.
    /// </summary>
    public Stability.StabilityThreshold? StabilityThreshold { get; init; }

    /// <exception cref="KeyNotFoundException">
    /// The structure's occtype is not in this mapper's occupancy type set. Dropping the
    /// structure instead would silently understate consequences, so use
    /// <see cref="TryMap"/> if you want to filter deliberately.
    /// </exception>
    public Building Map(NsiStructure structure) =>
        TryMap(structure, out Building building)
            ? building
            : throw new KeyNotFoundException(
                $"NSI structure {structure.FdId} has occupancy type '{structure.Occtype}', " +
                "which is not in this mapper's occupancy type set.");

    public bool TryMap(NsiStructure structure, out Building building)
    {
        if (!_occupancyTypes.TryGetValue(structure.Occtype, out OccupancyType? occupancyType))
        {
            building = default;
            return false;
        }

        building = new Building
        {
            OccupancyType = occupancyType,
            Value = (float)structure.ValStruct,
            ContentValue = (float)structure.ValCont,
            FoundationHeight = structure.FoundHt,
            StabilityThreshold = StabilityThreshold,
        };
        return true;
    }
}
