using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace TTS_Create_By_DvaForDev.Services;

public sealed class BackendProcessManager : IAsyncDisposable
{
    private Process? _process;
    public event Action<string>? OutputReceived;
    public bool IsRunning => _process is { HasExited: false };

    public async Task StartAsync(string pythonExecutable, CancellationToken token)
    {
        if (IsRunning) return;
        if (await ProbeHealthAsync(token))
        {
            OutputReceived?.Invoke(await DescribeBackendOriginAsync(token));
            return;
        }
        // Never spawn a duplicate server against a port owned by another process.
        // In particular, an unresponsive old Uvicorn must not generate Errno 10048.
        if (PortIsListening())
            throw new InvalidOperationException(
                "Cổng 7865 đang được sử dụng nhưng backend TTS không phản hồi. " +
                "Hãy kiểm tra tiến trình đang giữ cổng trước khi khởi động lại.");
        var developmentBackend = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "backend"));
        var copiedBackend = Path.Combine(AppContext.BaseDirectory, "backend");
        var backend = File.Exists(Path.Combine(developmentBackend, "main.py"))
            ? developmentBackend
            : copiedBackend;
        var main = Path.Combine(backend, "main.py");
        if (!File.Exists(main)) throw new FileNotFoundException("Backend main.py was not found.", main);
        var venvPython = Path.Combine(backend, ".venv", "Scripts", "python.exe");
        var python = File.Exists(venvPython) ? venvPython : pythonExecutable;
        var info = new ProcessStartInfo
        {
            FileName = python,
            WorkingDirectory = backend,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        info.ArgumentList.Add("-u");
        info.ArgumentList.Add("-m");
        info.ArgumentList.Add("uvicorn");
        info.ArgumentList.Add("main:app");
        info.ArgumentList.Add("--host"); info.ArgumentList.Add("127.0.0.1");
        info.ArgumentList.Add("--port"); info.ArgumentList.Add("7865");
        _process = new Process { StartInfo = info, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) OutputReceived?.Invoke(e.Data); };
        if (!_process.Start()) throw new InvalidOperationException("Could not start the Python backend.");
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
        OutputReceived?.Invoke($"Started backend (PID {_process.Id}) with {python}");
    }

    private static async Task<string> DescribeBackendOriginAsync(CancellationToken token)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var reply = await http.GetAsync("http://127.0.0.1:7865/api/health", token);
            await using var body = await reply.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(body, cancellationToken: token);
            var root = json.RootElement;
            bool hasApp = root.TryGetProperty("application", out var app);
            if (!hasApp || app.GetString() != "DvaForDev.TtsStudio")
                return "CẢNH BÁO: Đang dùng backend cũ trên cổng 7865. " +
                       "Các thay đổi Python trong TTS Studio mới chưa có hiệu lực.";
            if (root.TryGetProperty("backend_root", out var directory))
            {
                string running = Path.GetFullPath(directory.GetString() ?? "");
                string development = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "..", "..", "..", "backend"));
                string deployed = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory, "backend"));
                if (!running.Equals(development, StringComparison.OrdinalIgnoreCase) &&
                    !running.Equals(deployed, StringComparison.OrdinalIgnoreCase))
                    return "CẢNH BÁO: Backend trả lời từ thư mục khác: " + running;
            }
            return "Đã nhận diện backend TTS Studio độc lập trên cổng 7865.";
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return "Backend trên cổng 7865 phản hồi chậm; chưa xác nhận được nguồn chạy.";
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return "Backend trên cổng 7865 hoạt động nhưng chưa đọc được nguồn: " + e.Message;
        }
    }

    public static async Task<bool> ProbeHealthAsync(CancellationToken token)
    {
        using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await probe.GetAsync("http://127.0.0.1:7865/api/health", token);
            if (!response.IsSuccessStatusCode) return false;
            await using var data = await response.Content.ReadAsStreamAsync(token);
            using var json = await JsonDocument.ParseAsync(data, cancellationToken: token);
            return json.RootElement.TryGetProperty("status", out var status) &&
                   status.GetString() == "ok" &&
                   json.RootElement.TryGetProperty("engines", out var engines) &&
                   engines.ValueKind == JsonValueKind.Object;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
            or JsonException or InvalidOperationException) { return false; }
    }

    private static bool PortIsListening() =>
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            .Any(ep => ep.Port == 7865 && (ep.Address.Equals(System.Net.IPAddress.Loopback) ||
                                            ep.Address.Equals(System.Net.IPAddress.Any)));

    public async Task ReconnectAsync(string pythonExecutable, CancellationToken token)
    {
        if (await ProbeHealthAsync(token))
        {
            OutputReceived?.Invoke(await DescribeBackendOriginAsync(token));
            return;
        }
        // Only restart a child process owned by this application, not arbitrary
        // Python jobs on the machine.
        if (IsRunning) await StopAsync();
        await StartAsync(pythonExecutable, token);
    }

    public async Task StopAsync()
    {
        if (_process is not { HasExited: false } process) return;
        try
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex) { OutputReceived?.Invoke($"Backend shutdown: {ex.Message}"); }
        finally { _process?.Dispose(); _process = null; }
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
