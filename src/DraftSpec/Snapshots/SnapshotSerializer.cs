using System.Text.Json;

namespace DraftSpec.Snapshots;

/// <summary>
/// Handles JSON serialization for snapshot values.
/// Uses indented output for readable diffs.
/// </summary>
internal static class SnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serialize a value to JSON for snapshot storage.
    /// </summary>
    public static string Serialize<T>(T value)
    {
        try
        {
            return JsonSerializer.Serialize(value, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Cannot serialize value of type {typeof(T).Name} for snapshot. " +
                $"Ensure the type is JSON-serializable. Error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserialize JSON to a dictionary for snapshot storage.
    /// </summary>
    public static Dictionary<string, string>? DeserializeSnapshots(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json);
    }

    /// <summary>
    /// Serialize snapshots dictionary to JSON.
    /// </summary>
    public static string SerializeSnapshots(Dictionary<string, string> snapshots)
    {
        return JsonSerializer.Serialize(snapshots, Options);
    }
}
