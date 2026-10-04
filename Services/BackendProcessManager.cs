using System.Diagnostics;

namespace TTS_Create_By_DvaForDev.Services;

public sealed class BackendProcessManager : IAsyncDisposable
{
    private Process? _process;
    public event Action<string>? OutputReceived;
    public bool IsRunning => _process is { HasExited: false };

    public Task StartAsync(string pythonExecutable, CancellationToken token)
    {
        if (IsRunning) return Task.CompletedTask;
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
        return Task.CompletedTask;
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
