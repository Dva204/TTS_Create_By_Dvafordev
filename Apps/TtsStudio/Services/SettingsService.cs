using System.Text.Json;
using TTS_Create_By_DvaForDev.Models;

namespace TTS_Create_By_DvaForDev.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public AppSettings Load()
    {
        if (!File.Exists(SettingsPath)) return new AppSettings();
        try { return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), Json) ?? new AppSettings(); }
        catch (JsonException) { return new AppSettings(); }
    }

    public void Save(AppSettings settings) => File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, Json));
}
