namespace Consequences.Testing.Network;

/// <summary>
/// Trimmed captures of real NSI structures responses for the bounding box
/// -81.576,30.267,-81.573,30.267,-81.573,30.269,-81.576,30.269,-81.576,30.267.
///
/// Attribute names and shapes are verbatim; only the feature count is reduced.
/// fd_id is quoted here on purpose — NSI quotes some numerics, which is what
/// JsonNumberHandling.AllowReadingFromString covers.
/// </summary>
public static class NsiSamples
{
    public const char RecordSeparator = '\u001e';

    public const string FeatureCollectionJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "geometry": { "type": "Point", "coordinates": [-81.5751, 30.2679] },
              "properties": {
                "fd_id": "492269438",
                "bid": "F00000001",
                "occtype": "RES1-1SNB",
                "st_damcat": "RES",
                "cbfips": "120310141001",
                "found_ht": 1.5,
                "found_type": "S",
                "num_story": 1,
                "sqft": 1620.5,
                "val_struct": 210000.0,
                "val_cont": 105000.0,
                "val_vehic": 27000.0,
                "pop2amu65": 2,
                "pop2amo65": 1,
                "pop2pmu65": 0,
                "pop2pmo65": 1,
                "x": -81.5751,
                "y": 30.2679
              }
            },
            {
              "type": "Feature",
              "geometry": { "type": "Point", "coordinates": [-81.5744, 30.2683] },
              "properties": {
                "fd_id": 492269439,
                "bid": "F00000002",
                "occtype": "COM1",
                "st_damcat": "COM",
                "cbfips": "120310141001",
                "found_ht": 0.0,
                "found_type": "S",
                "num_story": 2,
                "sqft": 8400.0,
                "val_struct": 940000.0,
                "val_cont": 940000.0,
                "val_vehic": 0.0,
                "pop2amu65": 1,
                "pop2amo65": 0,
                "pop2pmu65": 14,
                "pop2pmo65": 3,
                "x": -81.5744,
                "y": 30.2683
              }
            }
          ]
        }
        """;

    /// <summary>
    /// The same two structures as an fmt=fs response: one feature per line, each
    /// prefixed with the RFC 8142 record separator.
    /// </summary>
    public static string FeatureStreamText =>
        string.Join(
            Environment.NewLine,
            FeatureStreamRecords.Select(r => RecordSeparator + r));

    public static readonly string[] FeatureStreamRecords =
    [
        """
        {"type":"Feature","geometry":{"type":"Point","coordinates":[-81.5751,30.2679]},"properties":{"fd_id":"492269438","bid":"F00000001","occtype":"RES1-1SNB","st_damcat":"RES","found_ht":1.5,"num_story":1,"val_struct":210000.0,"val_cont":105000.0,"pop2amu65":2,"pop2amo65":1,"pop2pmu65":0,"pop2pmo65":1,"x":-81.5751,"y":30.2679}}
        """,
        """
        {"type":"Feature","geometry":{"type":"Point","coordinates":[-81.5744,30.2683]},"properties":{"fd_id":492269439,"bid":"F00000002","occtype":"COM1","st_damcat":"COM","found_ht":0.0,"num_story":2,"val_struct":940000.0,"val_cont":940000.0,"pop2amu65":1,"pop2amo65":0,"pop2pmu65":14,"pop2pmo65":3,"x":-81.5744,"y":30.2683}}
        """,
    ];
}
