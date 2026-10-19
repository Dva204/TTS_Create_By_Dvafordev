using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace TTS_Create_By_DvaForDev.Services;

public sealed record TimedAudioCue(int Index, TimeSpan Start, TimeSpan End,
    string Text, string AudioFile);

// Exact SRT timelines (including gaps) remain separate from generated TTS duration.
public static class SrtBatchTimelineService
{
    private const int Rate = 24000;
    private const int BytesPerSample = 2;
    private const int BytesPerSecond = Rate * BytesPerSample;

    public static IReadOnlyList<SubtitleCue> Parse(string source) =>
        SrtQualityService.Read(source).OrderBy(x => x.Start).ToArray();

    public static void Validate(IReadOnlyList<TimedAudioCue> cues)
    {
        if (cues.Count == 0) throw new InvalidDataException("Chưa có cue SRT.");
        long lastEnd = 0;
        foreach (var cue in cues)
        {
            if (cue.Start < TimeSpan.Zero || cue.End <= cue.Start || cue.Start.Ticks < lastEnd)
                throw new InvalidDataException($"Cue {cue.Index}: mốc SRT bị trùng hoặc không hợp lệ.");
            if (!File.Exists(cue.AudioFile))
                throw new FileNotFoundException($"Thiếu audio của cue {cue.Index}.", cue.AudioFile);
            lastEnd = cue.End.Ticks;
        }
        if (cues[^1].End.TotalHours > 6)
            throw new InvalidDataException("Timeline vượt 6 giờ; hãy chia thành nhiều dự án.");
    }

    private static long SamplePosition(TimeSpan time) =>
        (long)Math.Round(time.TotalSeconds * Rate, MidpointRounding.AwayFromZero);

    public static string BuildOriginalSrt(IEnumerable<TimedAudioCue> cues)
    {
        var result = new StringBuilder();int index=1;
        foreach (var cue in cues)
        {
            string Stamp(TimeSpan t) => t.ToString(@"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);
            result.Append(index++).Append('\n').Append(Stamp(cue.Start))
                  .Append(" --> ").Append(Stamp(cue.End)).Append('\n')
                  .Append(cue.Text).Append("\n\n");
        }
        return result.ToString();
    }

    private static async Task<byte[]> DecodePcmAsync(string input, CancellationToken token)
    {
        var psi = new ProcessStartInfo("ffmpeg") {
            UseShellExecute=false,CreateNoWindow=true,
            RedirectStandardOutput=true,RedirectStandardError=true
        };
        foreach (var arg in new[]{"-hide_banner","-v","error","-i",input,
                    "-vn","-ac","1","-ar",Rate.ToString(CultureInfo.InvariantCulture),
                    "-f","s16le","-acodec","pcm_s16le","pipe:1"})
            psi.ArgumentList.Add(arg);
        using var proc = Process.Start(psi) ??
            throw new InvalidOperationException("Không thể khởi động FFmpeg.");
        using var memory = new MemoryStream();
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(token);
        linked.CancelAfter(TimeSpan.FromMinutes(3));
        var errorTask=proc.StandardError.ReadToEndAsync(linked.Token);
        try
        {
            await proc.StandardOutput.BaseStream.CopyToAsync(memory,linked.Token);
            await proc.WaitForExitAsync(linked.Token);
        }
        catch
        {
            try { if(!proc.HasExited)proc.Kill(entireProcessTree:true); }catch{}
            throw;
        }
        string errors=await errorTask;
        if(proc.ExitCode!=0 || memory.Length<512)
            throw new InvalidDataException("Không đọc được audio: "+input+" "+errors);
        return memory.ToArray();
    }

    private static async Task PadToAsync(Stream output, long targetBytes, CancellationToken token)
    {
        long left=targetBytes-output.Position;
        if(left<0)throw new InvalidDataException("Audio vượt mốc bắt đầu câu tiếp theo.");
        byte[] silence=new byte[8192];
        while(left>0)
        {
            int amount=(int)Math.Min(left,silence.Length);
            await output.WriteAsync(silence.AsMemory(0,amount),token);
            left-=amount;
        }
    }

    public static async Task RenderAsync(IReadOnlyList<TimedAudioCue> cues,
        string outputPath, CancellationToken token = default)
    {
        Validate(cues);
        if (File.Exists(outputPath))
            throw new IOException("File đích đã tồn tại, hãy đổi tên để tránh ghi đè: "+outputPath);
        string destination=Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        // Write PCM body first, RIFF header afterward. An exception leaves only
        // a disposable .tmp file and never destroys an existing user recording.
        string raw=destination+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            await using (var output=new FileStream(raw,FileMode.CreateNew,FileAccess.ReadWrite,
                FileShare.None,81920,useAsync:true))
            {
                foreach (var cue in cues)
                {
                    token.ThrowIfCancellationRequested();
                    await PadToAsync(output,SamplePosition(cue.Start)*BytesPerSample,token);
                    var pcm=await DecodePcmAsync(cue.AudioFile,token);
                    long available=(SamplePosition(cue.End)-SamplePosition(cue.Start))*BytesPerSample;
                    // Accept a small rounding variation, never truncate words or
                    // silently let synthesized speech overlap a later SRT cue.
                    if(pcm.LongLength>available+Rate/5)
                        throw new InvalidDataException(
                            $"Cue {cue.Index}: TTS dài {pcm.LongLength/(double)BytesPerSecond:F2}s, " +
                            $"khung SRT chỉ {(cue.End-cue.Start).TotalSeconds:F2}s. " +
                            "Hãy rút ngắn câu hoặc tăng tốc độ đọc trước khi ghép.");
                    if (pcm.LongLength>available)
                        throw new InvalidDataException(
                            $"Cue {cue.Index}: audio dài hơn khung SRT. Hãy điều chỉnh trước khi ghép.");
                    await output.WriteAsync(pcm,token);
                    await PadToAsync(output,SamplePosition(cue.End)*BytesPerSample,token);
                }
                if (output.Length>uint.MaxValue-36)
                    throw new InvalidDataException("File WAV quá lớn (>4GB).");
            }
            // RIFF requires its 44-byte header at the front; stream a new WAV
            // rather than loading a potentially 30-minute project into memory.
            string wav=destination+"."+Guid.NewGuid().ToString("N")+".wav.tmp";
            try
            {
                await using var output=new FileStream(wav,FileMode.CreateNew,FileAccess.Write,
                    FileShare.None,81920,useAsync:true);
                long samples=new FileInfo(raw).Length;
                using (var header = new MemoryStream())
                using (var writer = new BinaryWriter(header,Encoding.ASCII,leaveOpen:true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write((uint)(36+samples));
                    writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);writer.Write((ushort)1);writer.Write((ushort)1);
                    writer.Write(Rate);writer.Write(BytesPerSecond);
                    writer.Write((ushort)BytesPerSample);writer.Write((ushort)16);
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write((uint)samples);
                    await output.WriteAsync(header.ToArray(),token);
                }
                await using(var source=File.OpenRead(raw))
                    await source.CopyToAsync(output,token);
                await output.FlushAsync(token);
                await output.DisposeAsync();
                File.Move(wav,destination);
            }
            finally{File.Delete(wav);}
        }
        finally{File.Delete(raw);}
    }
}
