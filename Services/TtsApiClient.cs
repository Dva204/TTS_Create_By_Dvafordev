using System.Net.Http.Json;
using System.Text.Json;
using TTS_Create_By_DvaForDev.Models;

namespace TTS_Create_By_DvaForDev.Services;

public sealed class TtsApiClient : IDisposable
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public TtsApiClient(AppSettings settings)
    {
        _http = new HttpClient { BaseAddress = settings.BackendUri, Timeout = settings.RequestTimeout };
    }

    public Task<HealthResponse?> GetHealthAsync(CancellationToken token) =>
        _http.GetFromJsonAsync<HealthResponse>("api/health", Json, token);

    public Task<SystemResponse?> GetSystemAsync(CancellationToken token) =>
        _http.GetFromJsonAsync<SystemResponse>("api/system", Json, token);

    public async Task<IReadOnlyList<VoiceDto>> GetVoicesAsync(string? q, CancellationToken token)
    {
        var url = string.IsNullOrWhiteSpace(q) ? "api/voices" : $"api/voices?q={Uri.EscapeDataString(q)}";
        return await _http.GetFromJsonAsync<List<VoiceDto>>(url, Json, token) ?? [];
    }

    public async Task<VoiceDto> AddVoiceAsync(string name, string? language, string? gender, string? description, string? id, string filePath, CancellationToken token)
    {
        await using var stream = File.OpenRead(filePath);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        if (!string.IsNullOrWhiteSpace(language)) form.Add(new StringContent(language), "language");
        if (!string.IsNullOrWhiteSpace(gender)) form.Add(new StringContent(gender), "gender");
        if (!string.IsNullOrWhiteSpace(description)) form.Add(new StringContent(description), "description");
        if (!string.IsNullOrWhiteSpace(id)) form.Add(new StringContent(id), "id");
        form.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));
        using var response = await _http.PostAsync("api/voices", form, token);
        await EnsureSuccessAsync(response, token);
        return (await response.Content.ReadFromJsonAsync<VoiceDto>(Json, token))!;
    }

    public async Task DeleteVoiceAsync(string id, CancellationToken token)
    {
        using var response = await _http.DeleteAsync($"api/voices/{Uri.EscapeDataString(id)}", token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task<VoiceDto> ReplaceVoiceReferenceAsync(string id, string filePath, CancellationToken token)
    {
        await using var stream = File.OpenRead(filePath);
        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(stream), "file", Path.GetFileName(filePath));
        using var response = await _http.PutAsync($"api/voices/{Uri.EscapeDataString(id)}/reference", form, token);
        await EnsureSuccessAsync(response, token);
        return (await response.Content.ReadFromJsonAsync<VoiceDto>(Json, token))!;
    }

    public async Task<JobAccepted> PreviewVoiceAsync(string id, CancellationToken token)
    {
        using var response = await _http.PostAsync($"api/voices/{Uri.EscapeDataString(id)}/preview", null, token);
        await EnsureSuccessAsync(response, token);
        return (await response.Content.ReadFromJsonAsync<JobAccepted>(Json, token))!;
    }

    public async Task<JobAccepted> GenerateAsync(GenerateRequest request, CancellationToken token)
    {
        using var response = await _http.PostAsJsonAsync("api/tts/generate", request, Json, token);
        await EnsureSuccessAsync(response, token);
        return (await response.Content.ReadFromJsonAsync<JobAccepted>(Json, token))!;
    }

    public Task<JobDto?> GetJobAsync(string id, CancellationToken token) =>
        _http.GetFromJsonAsync<JobDto>($"api/jobs/{Uri.EscapeDataString(id)}", Json, token);

    public async Task CancelJobAsync(string id, CancellationToken token)
    {
        using var response = await _http.DeleteAsync($"api/jobs/{Uri.EscapeDataString(id)}", token);
        await EnsureSuccessAsync(response, token);
    }

    public async Task<byte[]> DownloadAudioAsync(string id, CancellationToken token) =>
        await _http.GetByteArrayAsync($"api/jobs/{Uri.EscapeDataString(id)}/audio", token);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken token)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(token);
        try
        {
            var detail = JsonDocument.Parse(body).RootElement.GetProperty("detail").GetString();
            throw new InvalidOperationException(detail ?? body);
        }
        catch (JsonException) { throw new HttpRequestException(body, null, response.StatusCode); }
        catch (KeyNotFoundException) { throw new HttpRequestException(body, null, response.StatusCode); }
    }

    public void Dispose() => _http.Dispose();
}
