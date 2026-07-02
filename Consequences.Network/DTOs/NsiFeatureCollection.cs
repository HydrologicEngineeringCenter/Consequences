using System.Text.Json.Serialization;

namespace Consequences.Network.DTOs;

/// <summary>
/// GeoJSON envelope returned by the NSI structures endpoint when fmt=fc.
/// </summary>
public sealed record NsiFeatureCollection
{
    [JsonPropertyName("type")]     public string Type              { get; init; } = "";
    [JsonPropertyName("features")] public List<NsiFeature> Features { get; init; } = [];
}

public sealed record NsiFeature
{
    [JsonPropertyName("type")]       public string Type          { get; init; } = "";
    [JsonPropertyName("geometry")]   public NsiGeometry? Geometry { get; init; }
    [JsonPropertyName("properties")] public NsiStructure? Properties { get; init; }
}

/// <summary>
/// Always a Point for NSI structures; coordinates are [longitude, latitude].
/// Duplicates the x/y properties on <see cref="NsiStructure"/>.
/// </summary>
public sealed record NsiGeometry
{
    [JsonPropertyName("type")]        public string Type              { get; init; } = "";
    [JsonPropertyName("coordinates")] public double[] Coordinates     { get; init; } = [];
}
