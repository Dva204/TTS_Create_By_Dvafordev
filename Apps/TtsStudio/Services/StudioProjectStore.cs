using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TTS_Create_By_DvaForDev.Services;

// All user projects are user-selected JSON files, never stored beside models or voices.
public sealed record StudioSegment(
    string Id, string Text, string VoiceId, string Language,
    string Engine, string Status, string? AudioFile, string? Source,
    double DurationSeconds, long? SrtStartMilliseconds = null,
    long? SrtEndMilliseconds = null);

public sealed record StudioProject(
    int SchemaVersion, string Name, DateTimeOffset UpdatedAt,
    string VoiceId, string Engine, string Language, string OutputFolder,
    IReadOnlyList<StudioSegment> Segments);

public static class StudioProjectStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static async Task SaveAsync(string file, StudioProject project, CancellationToken cancellation = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        if (project.SchemaVersion != 1) throw new InvalidDataException("Unknown project version.");
        string path = Path.GetFullPath(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(project, Json), new UTF8Encoding(false), cancellation);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static async Task<StudioProject> LoadAsync(string file, CancellationToken cancellation = default)
    {
        using var stream = File.OpenRead(file);
        var result = await JsonSerializer.DeserializeAsync<StudioProject>(stream, Json, cancellation)
                     ?? throw new InvalidDataException("Empty TTS project.");
        if (result.SchemaVersion != 1 || result.Segments is null || result.Segments.Count > 100000)
            throw new InvalidDataException("Invalid/unsupported TTS project.");
        return result;
    }

    // Hash binds text + language + engine + selected voice + engine parameters + voice-reference content.
    // Never reuse generated audio across voice or settings changes.
    public static string Fingerprint(string text, string voiceId, string language,
        string engine, string settingsJson, string referenceSha256 = "")
    {
        var payload = JsonSerializer.Serialize(new[] {
            "cache-v1", text, voiceId, language, engine, settingsJson, referenceSha256
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public static async Task<string> ExportAsync(StudioProject project, string destination,
        CancellationToken cancellation = default)
    {
        // Non-destructive, explicit export to an empty destination. Keeps private
        // reference voices and engine caches outside export.
        string output = Path.GetFullPath(destination);
        if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
            throw new IOException("Thư mục xuất phải trống để không ghi đè video/audio cũ.");
        var sourceFolder = Path.GetFullPath(project.OutputFolder);
        if (!Directory.Exists(sourceFolder)) throw new DirectoryNotFoundException(sourceFolder);
        Directory.CreateDirectory(output);
        string audio = Path.Combine(output, "audio");
        Directory.CreateDirectory(audio);
        var exported = new List<StudioSegment>();
        foreach (var segment in project.Segments)
        {
            cancellation.ThrowIfCancellationRequested();
            string? file = segment.AudioFile;
            if (string.IsNullOrWhiteSpace(file)) { exported.Add(segment); continue; }
            if (Path.GetFileName(file) != file ||
                file.Contains("..", StringComparison.Ordinal) ||
                Path.GetInvalidFileNameChars().Any(c => file.Contains(c)))
                throw new InvalidDataException("Unsafe audio filename: " + file);
            string source = Path.GetFullPath(Path.Combine(sourceFolder, file));
            if (!source.StartsWith(sourceFolder.TrimEnd(Path.DirectorySeparatorChar) +
                Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Audio file outside project folder.");
            if (!File.Exists(source)) { exported.Add(segment with { Status = "Missing", AudioFile = null }); continue; }
            string dest = Path.Combine(audio, file);
            if (!File.Exists(dest))
            {
                await using var from = File.OpenRead(source);
                await using var to = new FileStream(dest, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await from.CopyToAsync(to, cancellation);
            }
            exported.Add(segment);
        }
        foreach (var subtitle in Directory.EnumerateFiles(sourceFolder, "*_subtitles.srt", SearchOption.TopDirectoryOnly))
            File.Copy(subtitle, Path.Combine(output, Path.GetFileName(subtitle)), overwrite: false);
        await SaveAsync(Path.Combine(output, "project.ttsproject.json"),
            project with { OutputFolder = "audio", Segments = exported }, cancellation);
        return output;
    }
}
