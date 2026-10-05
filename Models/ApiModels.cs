using System.Text.Json.Serialization;

namespace TTS_Create_By_DvaForDev.Models;

public sealed record HealthResponse(string Status, string Engine, bool Ready, string? Detail);

public sealed record SystemResponse(
    string Platform,
    string Python,
    [property: JsonPropertyName("cuda_available")] bool CudaAvailable,
    [property: JsonPropertyName("gpu_name")] string? GpuName,
    string Ffmpeg,
    IReadOnlyList<string> Engines);

public sealed record VoiceDto(
    string Id,
    string Name,
    string? Language,
    string? Gender,
    string? Description,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("reference_file")] string ReferenceFile);

public sealed record TtsGenerationSettings(
    [property: JsonPropertyName("speed")] double Speed,
    [property: JsonPropertyName("cfg_weight")] double CfgWeight,
    [property: JsonPropertyName("temperature")] double Temperature,
    [property: JsonPropertyName("exaggeration")] double Exaggeration,
    [property: JsonPropertyName("speaker_boost")] bool SpeakerBoost,
    [property: JsonPropertyName("seed")] int? Seed = null);

public sealed record GenerateRequest(
    string Text,
    [property: JsonPropertyName("voice_id")] string VoiceId,
    string Language,
    string Engine,
    TtsGenerationSettings? Settings = null);

public sealed record JobAccepted(string Id, string Status);

public sealed record JobDto(
    string Id,
    string Status,
    string? Error,
    [property: JsonPropertyName("audio_url")] string? AudioUrl,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);

public sealed class AppSettings
{
    public Uri BackendUri { get; init; } = new("http://127.0.0.1:7865");
    public string PythonExecutable { get; init; } = "python";
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(30);
}
