using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TTS_Create_By_DvaForDev.Services;

public static class BackendDiagnosticsService
{
    public static async Task<string> SnapshotAsync(Uri baseUri, CancellationToken cancellation)
    {
        using var http=new HttpClient {BaseAddress=baseUri,Timeout=TimeSpan.FromSeconds(8)};
        var output=new StringBuilder();
        try
        {
            using var response=await http.GetAsync("api/health",cancellation);
            response.EnsureSuccessStatusCode();
            var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
            var obj=doc.RootElement;
            string Value(string key)=>obj.TryGetProperty(key,out var x)?x.ToString():"-";
            output.AppendLine("Backend: "+Value("status"));
            output.AppendLine("App: "+Value("application"));
            output.AppendLine("Version: "+Value("backend_version"));
            output.AppendLine("Root: "+Value("backend_root"));
            if(obj.TryGetProperty("engines",out var engines))
                foreach(var engine in engines.EnumerateObject())
                    output.AppendLine(engine.Name+" : "+engine.Value.ToString());
            using var system=await http.GetAsync("api/system",cancellation);
            if(system.IsSuccessStatusCode)
                output.AppendLine("System: "+await system.Content.ReadAsStringAsync(cancellation));
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {
            output.AppendLine("Không kết nối được backend: "+ex.Message);
        }
        try
        {
            using var gpu=new Process {StartInfo=new ProcessStartInfo("nvidia-smi") {
                RedirectStandardOutput=true,RedirectStandardError=true,
                UseShellExecute=false,CreateNoWindow=true
            }};
            foreach(var arg in new[]{"--query-gpu=name,memory.used,memory.total,utilization.gpu",
                      "--format=csv,noheader"})
                gpu.StartInfo.ArgumentList.Add(arg);
            gpu.Start();
            using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellation);
            timeout.CancelAfter(TimeSpan.FromSeconds(6));
            var data=gpu.StandardOutput.ReadToEndAsync(timeout.Token);
            var error=gpu.StandardError.ReadToEndAsync(timeout.Token);
            await gpu.WaitForExitAsync(timeout.Token);
            await error;
            output.AppendLine("GPU (name, used MiB, total MiB, usage %): "+(await data).Trim());
        }
        catch(Exception ex) when(ex is not OperationCanceledException)
        {output.AppendLine("GPU information unavailable: "+ex.Message);}
        output.AppendLine("Performance: model loaded by engine on demand; avoid GPU concurrency on RTX 3050 4GB.");
        return output.ToString();
    }
}
