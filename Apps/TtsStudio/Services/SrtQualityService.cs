using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TTS_Create_By_DvaForDev.Services;

public sealed record SubtitleCue(int Number, TimeSpan Start, TimeSpan End, string Text);
public static class SrtQualityService
{
    private static readonly Regex Timing = new(
        @"(?<start>\d{2}:\d{2}:\d{2}[,.]\d{3})\s*-->\s*(?<end>\d{2}:\d{2}:\d{2}[,.]\d{3})",
        RegexOptions.Compiled);

    private static TimeSpan ParseTime(string text) =>
        TimeSpan.ParseExact(text.Replace('.', ','), @"hh\:mm\:ss\,fff", CultureInfo.InvariantCulture);

    public static IReadOnlyList<SubtitleCue> Read(string content)
    {
        var result = new List<SubtitleCue>();
        var blocks = Regex.Split(content.TrimStart('\uFEFF').Replace("\r\n","\n"),
            @"\n\s*\n+");
        foreach(var block in blocks)
        {
            if(string.IsNullOrWhiteSpace(block))continue;
            var lines = block.Split('\n');
            int index = Array.FindIndex(lines,l => Timing.IsMatch(l));
            if(index<0) throw new InvalidDataException("Missing SRT time arrow.");
            var match=Timing.Match(lines[index]);
            string text=string.Join(" ",lines.Skip(index+1)).Trim();
            int.TryParse(lines[0].Trim(),out int number);
            var start=ParseTime(match.Groups["start"].Value);
            var end=ParseTime(match.Groups["end"].Value);
            if(end<=start || string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException("Invalid SRT duration or empty subtitle.");
            result.Add(new SubtitleCue(number==0?result.Count+1:number,start,end,text));
        }
        return result;
    }

    public static string Validate(IReadOnlyList<SubtitleCue> cues, double? audioDuration)
    {
        var issues=new List<string>();
        if(cues.Count==0)issues.Add("Không có dòng phụ đề nào.");
        for (int i=0;i<cues.Count;i++)
        {
            if(i>0 && cues[i].Start<cues[i-1].End)
                issues.Add($"Cue {i+1}: trùng thời gian với câu trước.");
            if(i>0 && (cues[i].Start-cues[i-1].End).TotalSeconds>3)
                issues.Add($"Cue {i+1}: khoảng trống phụ đề >3 giây.");
            if(cues[i].Start<TimeSpan.Zero)
                issues.Add($"Cue {i+1}: thời gian âm.");
        }
        if(audioDuration.HasValue && cues.Count>0 &&
            cues.Max(c=>c.End.TotalSeconds)>audioDuration+0.25)
            issues.Add("Phụ đề dài hơn audio gốc.");
        var output=new StringBuilder();
        output.AppendLine($"Số cue: {cues.Count}");
        if(cues.Count>0) output.AppendLine($"Kết thúc: {cues.Max(c=>c.End.TotalSeconds):F2}s");
        if(audioDuration.HasValue) output.AppendLine($"Audio: {audioDuration:F2}s");
        output.AppendLine(issues.Count==0 ? "Timeline không phát hiện lỗi cơ bản." :
            string.Join(Environment.NewLine, issues));
        output.AppendLine("Kiểm tra timeline không đồng nghĩa forced alignment.");
        return output.ToString();
    }

    public static async Task<string> InspectAsync(string srt, string audio, CancellationToken token)
    {
        if(!File.Exists(srt)) throw new FileNotFoundException("Chọn file .srt cần kiểm tra.",srt);
        var cues=Read(await File.ReadAllTextAsync(srt,token));
        double? duration = null;
        if(File.Exists(audio))
        {
            using var proc=new Process {StartInfo=new ProcessStartInfo("ffprobe"){
                RedirectStandardOutput=true, RedirectStandardError=true,
                CreateNoWindow=true,UseShellExecute=false
            }};
            foreach(var arg in new []{"-v","error","-show_entries","format=duration",
                "-of","default=noprint_wrappers=1:nokey=1",audio})
                proc.StartInfo.ArgumentList.Add(arg);
            try
            {
                proc.Start();
                using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(12));
                var data=proc.StandardOutput.ReadToEndAsync(timeout.Token);
                var err=proc.StandardError.ReadToEndAsync(timeout.Token);
                await proc.WaitForExitAsync(timeout.Token);
                await err;
                if(proc.ExitCode==0 &&
                    double.TryParse((await data).Trim(),NumberStyles.Float,
                        CultureInfo.InvariantCulture,out double length))duration=length;
            }
            catch(System.ComponentModel.Win32Exception){ /* ffprobe missing */ }
        }
        return Validate(cues,duration);
    }

    public static async Task<string> RunOptionalWhisperxAsync(string srt,string audio,
        CancellationToken token, string language="vi")
    {
        if(!File.Exists(srt) || !File.Exists(audio))
            throw new FileNotFoundException("Chọn cả SRT và audio để căn chỉnh.");
        // Model may need to download and can be slow; only execute on an explicit user click.
        string backend=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","backend"));
        if(!File.Exists(Path.Combine(backend,"main.py")))
            backend=Path.Combine(AppContext.BaseDirectory,"backend");
        string python=Path.Combine(backend,".venv-align","Scripts","python.exe");
        if(!File.Exists(python))
            return "Chưa cài môi trường WhisperX forced alignment.\r\n"+
                "Thiết lập riêng: py -3.11 -m venv backend\\.venv-align và cài whisperx.\r\n"+
                "Chế độ này KHÔNG tự nhận phụ đề suy đoán là đã căn chỉnh.";
        string worker=Path.Combine(backend,"tools","forced_align_srt.py");
        if(!File.Exists(worker)) return "Không tìm thấy script căn chỉnh WhisperX.";
        string output=Path.Combine(Path.GetDirectoryName(srt)!,
            Path.GetFileNameWithoutExtension(srt)+"_aligned.srt");
        if(File.Exists(output)) return "File _aligned.srt đã tồn tại; đổi tên/xóa thủ công trước khi chạy lại.";
        var psi=new ProcessStartInfo(python){
            RedirectStandardOutput=true,RedirectStandardError=true,
            UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=backend
        };
        foreach(var arg in new []{worker,"--audio",audio,"--srt",srt,"--language",language,"--output",output})
            psi.ArgumentList.Add(arg);
        using var proc=new Process {StartInfo=psi};
        proc.Start();
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(20));
        var stdout=proc.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderr=proc.StandardError.ReadToEndAsync(timeout.Token);
        try{await proc.WaitForExitAsync(timeout.Token);}
        catch { if(!proc.HasExited)proc.Kill(entireProcessTree:true);throw; }
        var err=(await stderr);
        if(proc.ExitCode!=0) return "WhisperX failed: "+err[^Math.Min(err.Length,900)..];
        return "Đã căn chỉnh thực tế: "+output+"\r\n"+(await stdout);
    }
}
