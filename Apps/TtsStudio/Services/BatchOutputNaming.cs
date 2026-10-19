namespace TTS_Create_By_DvaForDev.Services;

/// <summary>
/// User-visible finished recording filenames come from the imported TXT/SRT
/// basename. Per-segment WAV/MP3 filenames are intentionally unchanged so
/// existing Skip Existing jobs can still reuse already generated audio.
/// </summary>
public static class BatchOutputNaming
{
    private const string DefaultStem = "Full_Audio";
    private static readonly char[] UnsafeWindowsChars = "<>:\"/\\|?*".ToCharArray();
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase) {
        "CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5",
        "COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4",
        "LPT5","LPT6","LPT7","LPT8","LPT9"
    };

    public static string FromSourcePath(string filePath) =>
        SafeStem(Path.GetFileNameWithoutExtension(filePath));

    public static string SafeStem(string? importedSource)
    {
        string candidate = string.IsNullOrWhiteSpace(importedSource)
            ? DefaultStem : importedSource.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var chars = candidate.Select(c => char.IsControl(c) ||
            invalid.Contains(c) || UnsafeWindowsChars.Contains(c) ? '_' : c).ToArray();
        string cleaned = new string(chars).Trim(' ', '.');
        if (cleaned.Length > 120) cleaned = cleaned[..120].Trim(' ', '.');
        if (cleaned.Length == 0) cleaned = DefaultStem;
        if (Reserved.Contains(cleaned.Split('.')[0])) cleaned = "_" + cleaned;
        return cleaned;
    }

    public static string AvailablePath(string directory, string sourceStem, string extension)
    {
        if (extension is not ("mp3" or "wav" or "srt"))
            throw new ArgumentException("Unsupported output extension.",nameof(extension));
        var safe=SafeStem(sourceStem);
        var output=Path.Combine(directory, safe+"."+extension);
        for(int version=2; File.Exists(output); version++)
        {
            if(version>10000)
                throw new IOException("Quá nhiều file trùng tên trong thư mục đầu ra.");
            output=Path.Combine(directory,$"{safe}_{version}.{extension}");
        }
        return output;
    }
}
