using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Consequences.Network.DTOs;

namespace Consequences.Network;

/// <summary>
/// Reads the two NSI structures response formats: the GeoJSON feature collection
/// (fmt=fc) and the RFC 8142 record-separated feature stream (fmt=fs).
///
/// Answers "what did NSI send" and stops there — projecting onto a domain type is
/// the mapper's job. Nothing here touches the network, so every format decision is
/// testable offline against a string or a <see cref="TextReader"/>.
/// </summary>
public static class NsiJsonParser
{
    /// <summary>
    /// RFC 8142 record separator, prefixed to every feature in an fmt=fs response.
    /// </summary>
    public const char RecordSeparator = '\u001e';

    /// <summary>
    /// NSI quotes some numeric attributes, so reading numbers from strings is required.
    /// </summary>
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    public static List<NsiStructure> ParseFeatureCollection(string json) =>
        Structures(JsonSerializer.Deserialize<NsiFeatureCollection>(json, _serializerOptions));

    public static async Task<List<NsiStructure>> ParseFeatureCollectionAsync(
        Stream json,
        CancellationToken cancellationToken = default) =>
        Structures(
            await JsonSerializer.DeserializeAsync<NsiFeatureCollection>(json, _serializerOptions, cancellationToken));

    public static async IAsyncEnumerable<NsiStructure> ParseFeatureStreamAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await reader.ReadLineAsync(cancellationToken) is string line)
        {
            if (TryParseFeature(line, out NsiStructure? structure))
                yield return structure;
        }
    }

    /// <summary>
    /// Parses one record of an fmt=fs response, with or without its leading separator.
    /// Returns false for blank records and for features that carry no properties.
    /// </summary>
    public static bool TryParseFeature(ReadOnlySpan<char> record, [NotNullWhen(true)] out NsiStructure? structure)
    {
        ReadOnlySpan<char> trimmed = record.Trim().Trim(RecordSeparator);
        if (trimmed.IsEmpty)
        {
            structure = null;
            return false;
        }

        structure = JsonSerializer.Deserialize<NsiFeature>(trimmed, _serializerOptions)?.Properties;
        return structure is not null;
    }

    private static List<NsiStructure> Structures(NsiFeatureCollection? collection)
    {
        if (collection is null)
            return [];

        return collection.Features
            .Select(f => f.Properties)
            .OfType<NsiStructure>()
            .ToList();
    }
}
