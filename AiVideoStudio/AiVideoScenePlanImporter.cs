using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Strict, side-effect-free import of Claude/ChatGPT scene plans.
/// The actual SRT is authoritative for cue timings and narration.
/// Never modifies an existing project, images, SRT or audio.
/// </summary>
public static class AiVideoScenePlanImporter
{
    private static readonly HashSet<string> RenderableStyles =
        new(StringComparer.Ordinal) {
            "whiteboard", "image-story", "image-motion", "layer", "text"
        };
    private static readonly HashSet<string> ImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) {
            ".png", ".jpg", ".jpeg", ".webp", ".bmp", ".svg"
        };
    private const long MaxJsonBytes = 8 * 1024 * 1024;
    private const long TimeToleranceMs = 150;

    public sealed record ImportPreview(
        IReadOnlyList<AiVideoScene> Scenes,
        int CueCount,
        long DurationMs,
        IReadOnlyList<string> MissingImages,
        IReadOnlyList<string> Warnings);

    public static ImportPreview Read(string jsonFile, string srtFile, string imageFolder)
    {
        if (!File.Exists(jsonFile)) throw new FileNotFoundException("Không tìm thấy scene_plan.json.", jsonFile);
        if (!Directory.Exists(imageFolder))
            throw new DirectoryNotFoundException("Chưa chọn thư mục ảnh có tồn tại: " + imageFolder);
        if (new FileInfo(jsonFile).Length > MaxJsonBytes)
            throw new InvalidDataException("Scene Plan JSON quá lớn (giới hạn 8 MB).");

        var cues = AiVideoProjectService.ReadSrt(srtFile);
        for (int i = 1; i < cues.Count; i++)
        {
            if (cues[i].StartMs < cues[i - 1].EndMs)
                throw new InvalidDataException(
                    $"SRT có cue bị chồng thời gian: {i} và {i + 1}.");
        }
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonFile),
            new JsonDocumentOptions { MaxDepth = 24, CommentHandling = JsonCommentHandling.Disallow });
        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Scene Plan JSON phải là một object.");
        if (!root.TryGetProperty("scenes", out var scenesJson) ||
            scenesJson.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("JSON thiếu mảng scenes.");
        int count = scenesJson.GetArrayLength();
        if (count < 1 || count > 200)
            throw new InvalidDataException("Scene Plan cần từ 1 đến 200 cảnh.");
        if (count > cues.Count)
            throw new InvalidDataException($"Có {count} cảnh nhưng chỉ có {cues.Count} câu SRT.");
        if (Has(root, "scene_count") && Integer(root, "scene_count") != count)
            throw new InvalidDataException("scene_count không khớp mảng scenes.");
        if (Has(root, "total_cues") && Integer(root, "total_cues") != cues.Count)
            throw new InvalidDataException("total_cues không khớp file SRT đang chọn.");
        if (Has(root, "total_duration_s"))
            MatchTime(root, "total_duration_s", cues[^1].EndMs, "total_duration_s");

        // Resolve file names by exact name (case-insensitive on Windows),
        // not by index, to avoid silently attaching the wrong illustration.
        var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string file in AiVideoProjectService.FindStoryboardImages(imageFolder))
        {
            var name = Path.GetFileName(file);
            if (!byName.TryAdd(name, file))
                throw new InvalidDataException("Tên ảnh trùng khi bỏ qua chữ hoa/thường: " + name);
        }

        var output = new List<AiVideoScene>(count);
        var missing = new List<string>();
        var warnings = new List<string>();
        var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int nextCue = 1;
        for (int i = 0; i < count; i++)
        {
            var item = scenesJson[i];
            int sceneNumber = i + 1;
            string prefix = $"Scene {sceneNumber:000}";
            if (item.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException($"{prefix}: scene phải là object.");
            var sceneId = RequiredString(item, "scene_id");
            if (sceneId != sceneNumber.ToString("000", CultureInfo.InvariantCulture))
                throw new InvalidDataException($"{prefix}: scene_id phải là {sceneNumber:000}.");

            string filename = RequiredString(item, "filename");
            if (filename is "." or ".." || Path.IsPathRooted(filename) ||
                filename.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 ||
                filename != Path.GetFileName(filename) ||
                !ImageExtensions.Contains(Path.GetExtension(filename)) ||
                !usedNames.Add(filename))
                throw new InvalidDataException($"{prefix}: filename không hợp lệ hoặc bị trùng: {filename}");
            int from = Integer(item, "cue_from");
            int to = Integer(item, "cue_to");
            if (from != nextCue || to < from || to > cues.Count)
                throw new InvalidDataException(
                    $"{prefix}: cue_from/cue_to không liên tiếp, sai thứ tự hoặc vượt quá SRT (mong đợi từ cue {nextCue}).");
            nextCue = to + 1;
            string style = RequiredString(item, "style_id");
            if (!RenderableStyles.Contains(style))
                throw new InvalidDataException($"{prefix}: style_id chưa hỗ trợ render: {style}");
            string transition = StringOr(item, "transition", "fade");
            if (transition is not ("fade" or "cut"))
                throw new InvalidDataException($"{prefix}: transition chỉ cho phép fade hoặc cut.");

            // Match Claude's optional timings against real SRT (never trust JSON
            // timecodes as an independent source of truth). Missing timecodes are
            // allowed for pre-TTS drafts; present values must match.
            if (Has(item, "start")) MatchTime(item, "start", cues[from-1].StartMs, prefix);
            if (Has(item, "end")) MatchTime(item, "end", cues[to-1].EndMs, prefix);
            if (Has(item, "start_ms")) MatchMs(item, "start_ms", cues[from-1].StartMs, prefix);
            if (Has(item, "end_ms")) MatchMs(item, "end_ms", cues[to-1].EndMs, prefix);
            if (Has(item, "duration_s"))
                MatchTime(item, "duration_s", cues[to-1].EndMs - cues[from-1].StartMs, prefix);
            string imagePath = byName.TryGetValue(filename, out string? existing)
                ? existing : Path.Combine(imageFolder, filename);
            if (existing is null) missing.Add(filename);
            string narration = string.Join(" ", cues.Skip(from-1).Take(to-from+1).Select(c => c.Text));
            string summary = StringOr(item, "visual_goal", "");
            string detail = StringOr(item, "visual_description_vi", "");
            string continuity = StringOr(item, "continuity", "");
            string notes = string.Join(Environment.NewLine,
                new[] { summary, detail, continuity }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var actualStart = i == 0 ? 0 : output[^1].EndMs;
            var actualEnd = to < cues.Count ? cues[to].StartMs : cues[to-1].EndMs;
            if (actualEnd <= actualStart)
                throw new InvalidDataException($"{prefix}: timecode SRT không tạo được cảnh có thời lượng dương.");
            output.Add(new AiVideoScene {
                Index = sceneNumber,
                CueFrom = from, CueTo = to,
                StartMs = actualStart, EndMs = actualEnd,
                StyleId = style, Transition = transition,
                ImagePath = imagePath, Narration = narration, Notes = notes
            });
        }
        if (nextCue != cues.Count + 1)
            throw new InvalidDataException($"Scene Plan chưa phủ hết SRT: dừng ở cue {nextCue-1}/{cues.Count}.");
        AiVideoProjectService.ValidateScenes(output);
        if (missing.Count > 0)
            warnings.Add($"Chưa có {missing.Count}/{count} ảnh: " +
                string.Join(", ", missing.Take(12)) + (missing.Count > 12 ? "..." : "") +
                ". Có thể import để xem timeline, nhưng phải bổ sung ảnh trước khi xuất.");
        if (cues[0].StartMs > 0)
            warnings.Add("SRT bắt đầu sau 0 ms; scene đầu sẽ có khoảng chờ trước lời đọc.");
        return new ImportPreview(output, cues.Count, output[^1].EndMs, missing, warnings);
    }

    private static bool Has(JsonElement item, string key) => item.TryGetProperty(key, out _);
    private static string RequiredString(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out var value) ||
            value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()))
            throw new InvalidDataException("Thiếu trường văn bản: " + key);
        return value.GetString()!.Trim();
    }
    private static string StringOr(JsonElement item, string key, string fallback)
    {
        if (!item.TryGetProperty(key, out var value) || value.ValueKind == JsonValueKind.Null)
            return fallback;
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException("Trường " + key + " phải là chuỗi.");
        return value.GetString() ?? fallback;
    }
    private static int Integer(JsonElement item, string key)
    {
        if (!item.TryGetProperty(key, out var value) ||
            value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int result))
            throw new InvalidDataException("Trường " + key + " phải là số nguyên.");
        return result;
    }
    private static void MatchTime(JsonElement item, string key, long expectedMs, string where)
    {
        if (!item.TryGetProperty(key, out var val) ||
            val.ValueKind != JsonValueKind.Number || !val.TryGetDouble(out double seconds) ||
            !double.IsFinite(seconds) || Math.Abs(seconds*1000.0 - expectedMs) > TimeToleranceMs)
            throw new InvalidDataException($"{where}: {key} lệch so với SRT thật ({expectedMs} ms).");
    }
    private static void MatchMs(JsonElement item, string key, long expectedMs, string where)
    {
        if (!item.TryGetProperty(key, out var val) ||
            val.ValueKind != JsonValueKind.Number || !val.TryGetInt64(out long ms) ||
            Math.Abs(ms - expectedMs) > TimeToleranceMs)
            throw new InvalidDataException($"{where}: {key} lệch so với SRT thật ({expectedMs} ms).");
    }
}
