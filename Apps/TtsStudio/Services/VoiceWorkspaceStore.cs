using System.Text;
using System.Text.Json;
using TTS_Create_By_DvaForDev.Models;

namespace TTS_Create_By_DvaForDev.Services;

public sealed record VoiceWorkspace(bool Favorite, string Tags, TtsGenerationSettings? Preset = null);
public sealed class VoiceWorkspaceStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string FilePath { get; }
    private readonly Dictionary<string, VoiceWorkspace> entries = new(StringComparer.OrdinalIgnoreCase);

    public VoiceWorkspaceStore(string? location = null)
    {
        FilePath = location ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DvaForDev", "TtsStudio", "voice_workspace.json");
        if (!File.Exists(FilePath)) return;
        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string,VoiceWorkspace>>(
                File.ReadAllText(FilePath), Json);
            if (loaded is null) return;
            foreach (var (key, value) in loaded)
                if (IsValidVoiceId(key) && value is not null) entries[key] = value;
        }
        catch (JsonException) { /* malformed sidecar does not damage original voice library */ }
        catch (IOException) { }
    }

    public VoiceWorkspace Get(string voiceId) =>
        entries.TryGetValue(voiceId, out var pref) ? pref : new VoiceWorkspace(false, "");
    public void Set(string voiceId, VoiceWorkspace item)
    {
        if (!IsValidVoiceId(voiceId)) throw new ArgumentException("Invalid voice id.", nameof(voiceId));
        entries[voiceId] = item;
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(entries, Json), new UTF8Encoding(false));
            File.Move(temporary, FilePath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public bool IsFavorite(string id) => Get(id).Favorite;
    public static bool IsValidVoiceId(string? id) => !string.IsNullOrWhiteSpace(id) &&
        id.Length <= 256 && id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
}
