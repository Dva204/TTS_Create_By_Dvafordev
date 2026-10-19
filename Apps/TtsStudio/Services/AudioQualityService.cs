using System.Diagnostics;
using System.Globalization;

namespace TTS_Create_By_DvaForDev.Services;

public sealed record AudioMetrics(double DurationSeconds, double RmsDbfs,
    double PeakDbfs, double EstimatedPitchHz, double ClippedPercent, int SampleRate);

// Pitch estimation is an approximate acoustic measurement, not voice identity proof.
public static class AudioQualityService
{
    public static AudioMetrics AnalyzePcm16(ReadOnlySpan<byte> pcm, int rate)
    {
        if (rate < 8000 || rate > 192000 || pcm.Length < 2048)
            throw new InvalidDataException("Not enough PCM audio.");
        int len = pcm.Length / 2;
        var samples = new float[len];
        double sum = 0, peak = 0;
        int clipped = 0;
        for (int i = 0; i < len; ++i)
        {
            samples[i] = System.Buffers.Binary.BinaryPrimitives
                .ReadInt16LittleEndian(pcm.Slice(i * 2, 2)) / 32768f;
            double value = Math.Abs(samples[i]);
            peak = Math.Max(peak, value);
            sum += value * value;
            if (value >= 0.998) clipped++;
        }
        double rms = Math.Sqrt(sum / len);
        var pitches = new List<double>();
        int frame = (int)(rate * .04);
        int step = frame; // 40-ms windows keep analysis inexpensive.
        int minLag = Math.Max(1, rate / 420), maxLag = Math.Min(frame / 2, rate / 75);
        for (int start = 0; start + frame <= len && pitches.Count < 400; start += step)
        {
            double energy = 0;
            for (int j = 0; j < frame; j++) energy += samples[start + j] * samples[start + j];
            if (energy / frame < .00015) continue;
            double best = 0; int bestLag = 0;
            var peaks = new List<(int Lag, double Correlation)>();
            for (int lag = minLag; lag <= maxLag; lag++)
            {
                double correlate = 0, aa = 0, bb = 0;
                for (int j = 0; j + lag < frame; j++)
                {
                    double a = samples[start + j], b = samples[start + j + lag];
                    correlate += a*b; aa+=a*a; bb+=b*b;
                }
                double score = correlate / (Math.Sqrt(aa * bb) + 1e-12);
                peaks.Add((lag, score));
                if (score > best) {best=score;bestLag=lag;}
            }
            // A periodic waveform also correlates strongly at 2x/3x its
            // fundamental period. Prefer the first strong LOCAL maximum
            // near the global best to avoid estimating half the real pitch.
            if (best > .72)
            {
                for (int i=1; i<peaks.Count-1; i++)
                {
                    var candidate=peaks[i];
                    if (candidate.Correlation >= Math.Max(.72,best*.97) &&
                        candidate.Correlation >= peaks[i-1].Correlation &&
                        candidate.Correlation >= peaks[i+1].Correlation)
                    { bestLag=candidate.Lag;break; }
                }
                if (bestLag!=0) pitches.Add(rate/(double)bestLag);
            }
        }
        pitches.Sort();
        return new AudioMetrics(len / (double)rate,
            20*Math.Log10(Math.Max(rms,1e-9)),20*Math.Log10(Math.Max(peak,1e-9)),
            pitches.Count == 0 ? 0 : pitches[pitches.Count/2],
            100d*clipped/len,rate);
    }

    public static async Task<AudioMetrics> AnalyzeFileAsync(string file, CancellationToken token = default)
    {
        if (!File.Exists(file)) throw new FileNotFoundException("Audio file not found.",file);
        // Decoder avoids adding codec libraries to the WinForms application.
        using var proc = new Process { StartInfo = new ProcessStartInfo("ffmpeg") {
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,
            RedirectStandardOutput=true
        } };
        foreach (var arg in new[] {"-hide_banner","-v","error","-i",file,
                 "-ac","1","-ar","16000","-t","60","-f","s16le","-"})
            proc.StartInfo.ArgumentList.Add(arg);
        proc.Start();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(70));
        var errors = proc.StandardError.ReadToEndAsync(timeout.Token);
        using var memory = new MemoryStream();
        try
        {
            await proc.StandardOutput.BaseStream.CopyToAsync(memory,timeout.Token);
            await proc.WaitForExitAsync(timeout.Token);
        }
        catch
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree:true);
            throw;
        }
        if (proc.ExitCode != 0) throw new InvalidDataException("FFmpeg: " + await errors);
        return AnalyzePcm16(memory.ToArray(),16000);
    }
}
