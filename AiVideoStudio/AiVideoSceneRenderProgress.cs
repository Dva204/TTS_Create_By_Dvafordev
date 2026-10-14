using System;
using System.Globalization;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>Structured, newline-delimited renderer events written by ai_video_pipeline.py.</summary>
public sealed record AiVideoSceneRenderProgress(
    string Stage, int Ordinal, int Total, int SceneIndex)
{
    public static bool TryParse(string? line, out AiVideoSceneRenderProgress? entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(line)) return false;
        const string prefix = "[SCENE_PROGRESS] ";
        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;
        string[] parts = line[prefix.Length..].Trim().Split(' ',
            StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is not (2 or 3)) return false;
        string stage = parts[0];
        if (stage is not ("start" or "done" or "cached" or "mux" or "complete"))
            return false;
        string[] fraction = parts[1].Split('/');
        if (fraction.Length != 2 ||
            !int.TryParse(fraction[0], NumberStyles.None, CultureInfo.InvariantCulture,
                         out int ordinal) ||
            !int.TryParse(fraction[1], NumberStyles.None, CultureInfo.InvariantCulture,
                         out int total) ||
            total < 1 || total > 200 || ordinal < 1 || ordinal > total)
            return false;
        int sceneIndex = 0;
        if (stage is "start" or "done" or "cached")
        {
            if (parts.Length != 3 || !parts[2].StartsWith("scene=", StringComparison.Ordinal) ||
                !int.TryParse(parts[2]["scene=".Length..], NumberStyles.None,
                              CultureInfo.InvariantCulture, out sceneIndex) ||
                sceneIndex < 1 || sceneIndex > 200)
                return false;
        }
        else if (parts.Length != 2 || ordinal != total) return false;

        entry = new AiVideoSceneRenderProgress(stage, ordinal, total, sceneIndex);
        return true;
    }
}
