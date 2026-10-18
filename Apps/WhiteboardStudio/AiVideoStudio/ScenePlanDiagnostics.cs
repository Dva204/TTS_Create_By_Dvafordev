using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>Read-only diagnostic report: never changes SRT, scene JSON or project.</summary>
public static class ScenePlanDiagnostics
{
    public sealed record Report(int Cues, int Scenes, IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings)
    {
        public bool CanImport => Errors.Count == 0;
        public string DisplayText()
        {
            var b = new StringBuilder();
            b.AppendLine($"Câu SRT: {Cues} | Phân cảnh: {Scenes}");
            b.AppendLine($"Lỗi: {Errors.Count} | Cảnh báo: {Warnings.Count}");
            b.AppendLine();
            b.AppendLine(Errors.Count == 0 ? "✓ Không phát hiện lỗi cấu trúc." : "LỖI CẦN SỬA:");
            foreach (var issue in Errors.Take(40)) b.AppendLine("• " + issue);
            if (Errors.Count > 40) b.AppendLine($"… và {Errors.Count - 40} lỗi khác.");
            if (Warnings.Count > 0)
            {
                b.AppendLine();
                b.AppendLine("CẢNH BÁO:");
                foreach (var issue in Warnings.Take(40)) b.AppendLine("• " + issue);
            }
            b.AppendLine();
            b.AppendLine("Chỉ kiểm tra dữ liệu. Không chỉnh sửa JSON, SRT hay ảnh.");
            return b.ToString();
        }
    }

    private static string Normalize(string value) =>
        Regex.Replace(value.Normalize(System.Text.NormalizationForm.FormC).ToLowerInvariant(),
            @"[\p{P}\p{S}\s]+", " ").Trim();

    private static bool Number(JsonElement element, string property, out int result)
    {
        result = 0;
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.Number &&
            p.TryGetInt32(out result);
    }

    private static string String(JsonElement element, string key) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(key, out var field) &&
        field.ValueKind == JsonValueKind.String ? field.GetString() ?? "" : "";

    public static Report Check(string srtFile, string jsonFile, string imageFolder)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        if (!File.Exists(srtFile) || !File.Exists(jsonFile))
            throw new FileNotFoundException("Hãy chọn file SRT và Scene Plan JSON hợp lệ.");
        var cues = AiVideoProjectService.ReadSrt(srtFile);
        for (int i = 1; i < cues.Count; i++)
            if (cues[i].StartMs < cues[i - 1].EndMs)
                errors.Add($"SRT: cue {i} và {i + 1} bị chồng thời gian.");
        if (!Directory.Exists(imageFolder))
            warnings.Add("Chưa chọn thư mục ảnh có tồn tại.");
        var images = AiVideoProjectService.FindStoryboardImages(imageFolder);
        var exact = new HashSet<string>(
            images.Select(Path.GetFileName).Select(x => x!), StringComparer.OrdinalIgnoreCase);
        var stems = images.GroupBy(path => Path.GetFileNameWithoutExtension(path),
            StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key,
            g => g.Select(Path.GetFileName).ToArray(), StringComparer.OrdinalIgnoreCase);
        if (new FileInfo(jsonFile).Length > 8 * 1024 * 1024)
            throw new InvalidDataException("Scene Plan vượt giới hạn 8 MB.");
        using var document = JsonDocument.Parse(File.ReadAllText(jsonFile),
            new JsonDocumentOptions { MaxDepth = 24 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("scenes", out var scenes) ||
            scenes.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("JSON không có mảng scenes hợp lệ.");
        int count = scenes.GetArrayLength();
        if (count is < 1 or > 200)
            errors.Add($"Số cảnh {count} nằm ngoài giới hạn 1–200.");
        if (Number(root, "total_cues", out int declared) && declared != cues.Count)
            errors.Add($"total_cues={declared} nhưng SRT có {cues.Count} cue.");
        if (Number(root, "scene_count", out declared) && declared != count)
            errors.Add($"scene_count={declared} nhưng mảng có {count} cảnh.");
        int expectedCue = 1;
        for (int i = 0; i < count; i++)
        {
            var scene = scenes[i];
            string label = $"Cảnh {i + 1:000}";
            if (scene.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{label}: phải là object.");
                continue;
            }
            if (String(scene, "scene_id") != (i + 1).ToString("000"))
                errors.Add($"{label}: scene_id không đúng thứ tự.");
            if (!Number(scene, "cue_from", out int from) ||
                !Number(scene, "cue_to", out int to) || from != expectedCue ||
                to < from || to > cues.Count)
            {
                errors.Add($"{label}: khoảng cue sai; cần bắt đầu tại cue {expectedCue}.");
                continue;
            }
            expectedCue = to + 1;
            string narration = String(scene, "narration_excerpt");
            var actual = string.Join(" ", cues.Skip(from - 1).Take(to - from + 1).Select(c => c.Text));
            if (!string.IsNullOrWhiteSpace(narration) &&
                Normalize(narration) != Normalize(actual))
                errors.Add($"{label}: narration_excerpt không khớp lời đọc SRT (cue {from}–{to}).");
            long start = cues[from - 1].StartMs;
            long end = cues[to - 1].EndMs;
            if (scene.TryGetProperty("start_ms", out var startField) &&
                startField.ValueKind == JsonValueKind.Number &&
                startField.TryGetInt64(out long startMs) && Math.Abs(startMs - start) > 150)
                warnings.Add($"{label}: start_ms lệch SRT {startMs - start:+#;-#;0} ms.");
            if (scene.TryGetProperty("end_ms", out var endField) &&
                endField.ValueKind == JsonValueKind.Number &&
                endField.TryGetInt64(out long endMs) && Math.Abs(endMs - end) > 150)
                warnings.Add($"{label}: end_ms lệch SRT {endMs - end:+#;-#;0} ms.");
            string filename = String(scene, "filename");
            if (string.IsNullOrWhiteSpace(filename) || filename != Path.GetFileName(filename) ||
                filename.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
                errors.Add($"{label}: filename trống hoặc không hợp lệ.");
            else if (!exact.Contains(filename))
            {
                string stem = Path.GetFileNameWithoutExtension(filename);
                if (stems.TryGetValue(stem, out var matches) && matches.Length == 1)
                    warnings.Add($"{label}: {filename} có thể dùng {matches[0]} (khác đuôi ảnh).");
                else if (matches is { Length: > 1 })
                    errors.Add($"{label}: có nhiều ảnh cùng tên gốc {stem}, cần chọn rõ.");
                else warnings.Add($"{label}: thiếu ảnh {filename}.");
            }
        }
        if (expectedCue != cues.Count + 1)
            errors.Add($"Scene Plan chỉ phủ tới cue {expectedCue - 1}/{cues.Count}.");
        return new Report(cues.Count, count, errors, warnings);
    }
}
