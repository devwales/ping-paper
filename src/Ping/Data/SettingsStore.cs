using System.Text.Json;
using Ping.Models;

namespace Ping.Data;

/// <summary>
/// App settings, kept as one JSON document in the settings table.
/// </summary>
public class SettingsStore
{
    private const string Key = "app";
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public AppSettings Current { get; private set; }

    public SettingsStore()
    {
        Current = Load();
    }

    private AppSettings Load()
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = $key";
        cmd.Parameters.AddWithValue("$key", Key);
        var raw = cmd.ExecuteScalar() as string;
        if (raw == null) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(raw) ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO settings (key, value) VALUES ($key, $value)
            ON CONFLICT(key) DO UPDATE SET value = $value
            """;
        cmd.Parameters.AddWithValue("$key", Key);
        cmd.Parameters.AddWithValue("$value", JsonSerializer.Serialize(Current, JsonOpts));
        cmd.ExecuteNonQuery();
    }
}
