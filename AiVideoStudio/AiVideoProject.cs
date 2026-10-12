using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>A saved, editable storyboard. Does not invoke any AI services.</summary>
public sealed class AiVideoProject
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "Dự án YouTube mới";
    public string Language { get; set; } = "en-US";
    public string AspectRatio { get; set; } = "16:9";
    public string AudioPath { get; set; } = "";
    public string SubtitlePath { get; set; } = "";
    public string ImageFolder { get; set; } = "";
    public List<AiVideoScene> Scenes { get; set; } = new();
    [JsonIgnore]
    public string FilePath { get; set; } = "";
}

public sealed class AiVideoScene
{
    public int Index { get; set; }
    public int CueFrom { get; set; }
    public int CueTo { get; set; }
    public long StartMs { get; set; }
    public long EndMs { get; set; }
    public string StyleId { get; set; } = "whiteboard";
    public string ImagePath { get; set; } = "";
    public string Narration { get; set; } = "";
    public string Notes { get; set; } = "";
    // Relative to the start of this scene; user-editable AI Director suggestions.
    public List<AiVideoDirectionEvent> DirectionEvents { get; set; } = new();
    [JsonIgnore]
    public string DirectorSummary =>
        DirectionEvents is { Count: > 0 }
            ? string.Join(" + ", DirectionEvents.Select(e =>
                e.Kind == "text" ? "Chữ: " + e.Text : $"Zoom {e.Zoom:0.00}x"))
            : "Chưa có";
}

public sealed class AiVideoDirectionEvent
{
    // "text" or "zoom"; only these event types are supported in V1.2.
    public string Kind { get; set; } = "text";
    public int StartMs { get; set; }
    public int DurationMs { get; set; } = 1400;
    public string Text { get; set; } = "";
    public double Zoom { get; set; } = 1.10;
    public double FocusX { get; set; } = .5;
    public double FocusY { get; set; } = .5;
}

public sealed record AiSubtitleCue(int Index, long StartMs, long EndMs, string Text);

public static class AiVideoProjectService
{
    public static readonly string[] SupportedStyles = {
        "whiteboard", "layer", "map", "timeline", "image-motion",
        "infographic", "text", "image-story"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex TimeLineRegex = new(
        @"(?<sh>\d{2,}):(?<sm>\d{2}):(?<ss>\d{2})[,.](?<sms>\d{3})\s*-->\s*(?<eh>\d{2,}):(?<em>\d{2}):(?<es>\d{2})[,.](?<ems>\d{3})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static List<AiSubtitleCue> ReadSrt(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Không thấy file SRT.", path);
        var text = File.ReadAllText(path).TrimStart('\uFEFF').Replace("\r\n", "\n").Trim();
        var chunks = Regex.Split(text, @"\n\s*\n");
        var cues = new List<AiSubtitleCue>();
        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;
            var lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int idx = Array.FindIndex(lines, line => line.Contains("-->", StringComparison.Ordinal));
            if (idx < 0) throw new InvalidDataException("Phụ đề thiếu dòng thời gian: " + chunk[..Math.Min(chunk.Length, 80)]);
            var match = TimeLineRegex.Match(lines[idx]);
            if (!match.Success) throw new InvalidDataException("Định dạng thời gian SRT không hợp lệ: " + lines[idx]);
            long start = Time(match, "s"), end = Time(match, "e");
            if (start < 0 || end <= start) throw new InvalidDataException("Một mục SRT có thời lượng không hợp lệ.");
            if (cues.Count > 0 && start < cues[^1].StartMs)
                throw new InvalidDataException("SRT không được sắp xếp theo thời gian.");
            string narration = string.Join(" ", lines.Skip(idx + 1));
            cues.Add(new AiSubtitleCue(cues.Count + 1, start, end, narration));
        }
        if (cues.Count == 0) throw new InvalidDataException("Không tìm thấy phụ đề SRT.");
        return cues;
    }

    private static long Time(Match match, string key) =>
        ((long)int.Parse(match.Groups[key + "h"].Value, CultureInfo.InvariantCulture) * 3600000)
        + ((long)int.Parse(match.Groups[key + "m"].Value, CultureInfo.InvariantCulture) * 60000)
        + ((long)int.Parse(match.Groups[key + "s"].Value, CultureInfo.InvariantCulture) * 1000)
        + int.Parse(match.Groups[key + "ms"].Value, CultureInfo.InvariantCulture);

    public static List<AiVideoScene> GenerateStoryboard(
        IReadOnlyList<AiSubtitleCue> cues, int cuesPerScene, string? imageFolder, string styleId = "whiteboard")
    {
        if (cues.Count == 0) throw new ArgumentException("Cần ít nhất một đoạn SRT.", nameof(cues));
        if (cuesPerScene < 1 || cuesPerScene > 20) throw new ArgumentOutOfRangeException(nameof(cuesPerScene));
        if (!SupportedStyles.Contains(styleId)) throw new ArgumentException("Kiểu video chưa được hỗ trợ.", nameof(styleId));
        var images = Directory.Exists(imageFolder)
            ? Directory.EnumerateFiles(imageFolder!)
                .Where(file => new[] { ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".svg" }
                    .Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : Array.Empty<string>();
        var scenes = new List<AiVideoScene>();
        for (int first = 0; first < cues.Count; first += cuesPerScene)
        {
            int last = Math.Min(first + cuesPerScene, cues.Count) - 1;
            int sequence = scenes.Count;
            scenes.Add(new AiVideoScene {
                Index = sequence + 1,
                CueFrom = cues[first].Index,
                CueTo = cues[last].Index,
                StartMs = first == 0 ? 0 : cues[first].StartMs,
                EndMs = last + 1 < cues.Count ? cues[last + 1].StartMs : cues[last].EndMs,
                StyleId = styleId,
                ImagePath = sequence < images.Length ? images[sequence] : "",
                Narration = string.Join(" ", cues.Skip(first).Take(last - first + 1).Select(c => c.Text))
            });
        }
        ValidateScenes(scenes);
        return scenes;
    }

    public static void ValidateScenes(IReadOnlyList<AiVideoScene> scenes)
    {
        int expectedCue = 1;
        long previousEnd = 0;
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            if (scene.Index != i + 1 || scene.CueFrom != expectedCue ||
                scene.CueTo < scene.CueFrom || scene.StartMs < previousEnd ||
                scene.EndMs <= scene.StartMs || !SupportedStyles.Contains(scene.StyleId))
                throw new InvalidDataException($"Cảnh {i + 1} có mốc SRT/thời gian/kiểu hiệu ứng không hợp lệ.");
            if ((scene.DirectionEvents?.Count ?? 0) > 4)
                throw new InvalidDataException($"Cảnh {i + 1} có quá nhiều hiệu ứng Director (tối đa 4).");
            if ((scene.DirectionEvents?.Count(e => e.Kind == "zoom") ?? 0) > 1)
                throw new InvalidDataException($"Cảnh {i + 1} chỉ hỗ trợ một zoom.");
            foreach (var shot in scene.DirectionEvents ?? new())
            {
                if (shot.Kind is not ("text" or "zoom") ||
                    shot.StartMs < 0 || shot.DurationMs <= 0 ||
                    (long)shot.StartMs + shot.DurationMs > scene.EndMs - scene.StartMs ||
                    shot.Zoom < 1 || shot.Zoom > 1.22 ||
                    shot.FocusX < 0 || shot.FocusX > 1 ||
                    shot.FocusY < 0 || shot.FocusY > 1 ||
                    shot.Text.Length > 90)
                    throw new InvalidDataException($"Hiệu ứng Director ở cảnh {i + 1} không hợp lệ.");
            }
            expectedCue = scene.CueTo + 1;
            previousEnd = scene.EndMs;
        }
    }

    public static void Save(AiVideoProject project, string targetFile)
    {
        if (project.SchemaVersion != 1) throw new InvalidDataException("Chỉ hỗ trợ project schema v1.");
        ValidateScenes(project.Scenes);
        var path = Path.GetFullPath(targetFile);
        string root = Path.GetDirectoryName(path) ?? throw new InvalidDataException("Thư mục project không hợp lệ.");
        Directory.CreateDirectory(root);
        // Do not mutate in-memory absolute paths; serialize a detached clone.
        var clone = JsonSerializer.Deserialize<AiVideoProject>(JsonSerializer.Serialize(project, JsonOptions), JsonOptions)!;
        clone.FilePath = "";
        clone.AudioPath = ToRelative(root, clone.AudioPath);
        clone.SubtitlePath = ToRelative(root, clone.SubtitlePath);
        clone.ImageFolder = ToRelative(root, clone.ImageFolder);
        foreach (var scene in clone.Scenes)
            scene.ImagePath = ToRelative(root, scene.ImagePath);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(clone, JsonOptions));
            File.Move(temp, path, overwrite: true);
            project.FilePath = path;
        }
        finally
        {
            if (File.Exists(temp)) File.Delete(temp);
        }
    }

    public static AiVideoProject Load(string projectFile)
    {
        var path = Path.GetFullPath(projectFile);
        var project = JsonSerializer.Deserialize<AiVideoProject>(File.ReadAllText(path), JsonOptions)
                      ?? throw new InvalidDataException("JSON project trống.");
        if (project.SchemaVersion != 1) throw new InvalidDataException("Phiên bản JSON chưa hỗ trợ.");
        project.Scenes ??= new();
        ValidateScenes(project.Scenes);
        string root = Path.GetDirectoryName(path)!;
        project.AudioPath = ToAbsolute(root, project.AudioPath);
        project.SubtitlePath = ToAbsolute(root, project.SubtitlePath);
        project.ImageFolder = ToAbsolute(root, project.ImageFolder);
        foreach (var scene in project.Scenes)
            scene.ImagePath = ToAbsolute(root, scene.ImagePath);
        project.FilePath = path;
        return project;
    }

    private static string ToRelative(string root, string? path) =>
        string.IsNullOrWhiteSpace(path) ? "" : Path.GetRelativePath(root, Path.GetFullPath(path));

    private static string ToAbsolute(string root, string? path) =>
        string.IsNullOrWhiteSpace(path) ? "" : Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
}

