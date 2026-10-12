using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// GDI+ on Windows cannot reliably open WebP. Convert only the UI preview via
/// the existing whiteboard Python/Pillow environment; source remains WebP.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private readonly Dictionary<string, (long Stamp, string Preview)> webpPreviews =
        new(StringComparer.OrdinalIgnoreCase);

    private static long ImageStamp(string source)
    {
        var info = new FileInfo(source);
        return info.LastWriteTimeUtc.Ticks ^ info.Length;
    }

    private string? ResolveDisplayImage(string? source)
    {
        if (string.IsNullOrWhiteSpace(source)) return source;
        if (!Path.GetExtension(source).Equals(".webp", StringComparison.OrdinalIgnoreCase))
            return source;
        if (!File.Exists(source)) return source;

        var full = Path.GetFullPath(source);
        long stamp = ImageStamp(full);
        if (!webpPreviews.TryGetValue(full, out var cached) ||
            cached.Stamp != stamp || !File.Exists(cached.Preview))
            WarmPreviewImages(new[] { full });
        return webpPreviews.TryGetValue(full, out cached) &&
               cached.Stamp == stamp && File.Exists(cached.Preview)
            ? cached.Preview : source;
    }

    private void WarmPreviewImages(IEnumerable<string> sourceFiles)
    {
        var pending = new List<string>();
        foreach (string item in sourceFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(item) ||
                !Path.GetExtension(item).Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(item)) continue;
            var full = Path.GetFullPath(item);
            long stamp = ImageStamp(full);
            if (!webpPreviews.TryGetValue(full, out var current) ||
                current.Stamp != stamp || !File.Exists(current.Preview))
                pending.Add(full);
        }
        if (pending.Count == 0) return;

        var script = RenderScriptPath();
        var pyScript = script is null ? null :
            Path.Combine(Path.GetDirectoryName(script)!, "preview_image_bridge.py");
        var python = Path.Combine(Path.GetDirectoryName(rendererExecutable)!, "python.exe");
        if (pyScript == null || !File.Exists(pyScript) || !File.Exists(python))
        {
            status.Text = "Không tìm thấy Python/Pillow của renderer để tạo preview WebP.";
            return;
        }

        string temporary = Path.Combine(Path.GetTempPath(), "DvaForDevStudio",
            "webp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        string input = Path.Combine(temporary, "images.json");
        string output = Path.Combine(temporary, "result.json");
        string cache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DvaForDev", "TTSStudio", "PreviewCache");
        try
        {
            File.WriteAllText(input, JsonSerializer.Serialize(pending), Encoding.UTF8);
            var processSettings = new ProcessStartInfo(python) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            foreach (var arg in new[] {
                pyScript, "--input-json", input, "--output-json", output,
                "--cache-dir", cache
            }) processSettings.ArgumentList.Add(arg);

            using var process = Process.Start(processSettings)
                ?? throw new IOException("Không khởi chạy được bộ tạo preview WebP.");
            if (!process.WaitForExit(45000))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("Tạo preview WebP quá thời gian 45 giây.");
            }
            if (process.ExitCode != 0)
                throw new IOException("Python/Pillow: " + process.StandardError.ReadToEnd().Trim());

            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(output, Encoding.UTF8));
            if (result != null)
                foreach (var entry in result)
                    if (File.Exists(entry.Value))
                        webpPreviews[entry.Key] = (ImageStamp(entry.Key), entry.Value);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or TimeoutException or JsonException)
        {
            status.Text = "Lỗi tạo thumbnail WebP: " + ex.Message;
        }
        finally
        {
            try { Directory.Delete(temporary, recursive: true); }
            catch (IOException) { }
        }
    }
}
