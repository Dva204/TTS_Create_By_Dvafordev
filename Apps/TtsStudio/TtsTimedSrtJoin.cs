using System.Diagnostics;
using System.Globalization;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private async Task JoinTimedSrtAudioAsync(string folder, DataGridView grid, CancellationToken token)
    {
        var groups=grid.Rows.Cast<DataGridViewRow>()
            .Where(row=>!row.IsNewRow)
            .GroupBy(row=>row.Cells[7].Value?.ToString()??"SRT")
            .ToArray();
        // Snapshot the UI option before rendering. Keep all segment files until
        // EVERY SRT output succeeds, so a later failure cannot destroy inputs.
        bool deleteSegments=(Controls.Find("chkDeleteSegments",true)
            .FirstOrDefault() as CheckBox)?.Checked==true;
        var finishedOutputs=new List<string>();
        var renderedSegmentFiles=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            var rows=group.ToArray();
            if(rows.Any(row=>row.Tag is not SubtitleCue))
                throw new InvalidDataException("Không thể ghép lẫn câu TXT và SRT trong cùng một lượt.");
            if(rows.Any(row=>row.Cells[6].Value?.ToString()!="Completed" ||
                             string.IsNullOrWhiteSpace(row.Cells[1].Value?.ToString())))
                throw new InvalidDataException("Còn câu chưa hoàn thành. Hãy tạo đủ audio trước khi ghép SRT.");
            var cues=rows.Select(row =>
            {
                var timing=(SubtitleCue)row.Tag!;
                var filename=row.Cells[1].Value?.ToString()??"";
                if(Path.GetFileName(filename)!=filename)
                    throw new InvalidDataException("Tên file audio không hợp lệ.");
                return new TimedAudioCue(timing.Number,timing.Start,timing.End,
                    timing.Text,Path.Combine(folder,filename));
            }).OrderBy(x=>x.Start).ToArray();
            string name=BatchOutputNaming.SafeStem(group.Key);
            string format = userSettings.OutputFormat.Equals("mp3",StringComparison.OrdinalIgnoreCase)
                ?"mp3":"wav";
            string result=BatchOutputNaming.AvailablePath(folder,name,format);
            string waveTmp=Path.Combine(folder,
                name+"."+Guid.NewGuid().ToString("N")+".wav");
            string mp3Tmp=Path.Combine(folder,
                name+"."+Guid.NewGuid().ToString("N")+".mp3");
            try
            {
                await SrtBatchTimelineService.RenderAsync(cues,waveTmp,token,
                    (index,actual,target)=>WriteLog(
                        $"SRT câu {index}: tự căn audio {actual:F3}s → {target:F3}s " +
                        "(chỉnh rất nhẹ tốc độ, giữ toàn bộ lời)."));
                if(format=="wav")
                {
                    File.Move(waveTmp,result);
                }
                else
                {
                    string bitrate=System.Text.RegularExpressions.Regex.Match(
                        userSettings.Mp3Bitrate??"192",@"\d+").Value;
                    if(string.IsNullOrWhiteSpace(bitrate))bitrate="192";
                    var psi=new ProcessStartInfo("ffmpeg") {
                        UseShellExecute=false,CreateNoWindow=true,
                        RedirectStandardError=true
                    };
                    foreach(var arg in new[]{"-y","-i",waveTmp,"-b:a",bitrate+"k",
                        "-c:a","libmp3lame",mp3Tmp})
                        psi.ArgumentList.Add(arg);
                    using var proc=Process.Start(psi) ??
                        throw new InvalidOperationException("FFmpeg không chạy được.");
                    var stderr=proc.StandardError.ReadToEndAsync(token);
                    await proc.WaitForExitAsync(token);
                    string err=await stderr;
                    if(proc.ExitCode!=0 || !File.Exists(mp3Tmp))
                        throw new IOException("Lỗi mã hóa MP3: "+err);
                    File.Move(mp3Tmp,result);
                }
                // Only register sources for deletion after the final MP3/WAV
                // exists and is nonempty. The source SRT file is never included.
                if(!File.Exists(result) || new FileInfo(result).Length<=44)
                    throw new IOException("File ghép SRT đầu ra chưa hợp lệ, giữ lại audio từng câu.");
                finishedOutputs.Add(Path.GetFullPath(result));
                foreach(var cue in cues)
                    renderedSegmentFiles.Add(Path.GetFullPath(cue.AudioFile));
                WriteLog($"Đã ghép SRT giữ nguyên vị trí/ngắt nghỉ: {result}");
                // Keeping segment audio is safer for later script edits.
            }
            finally
            {
                try{File.Delete(waveTmp);}catch{}
                try{File.Delete(mp3Tmp);}catch{}
            }
        }

        if(deleteSegments && finishedOutputs.Count==groups.Length)
        {
            string outputFolder=Path.GetFullPath(folder)
                .TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)
                +Path.DirectorySeparatorChar;
            var protectedOutputs=new HashSet<string>(finishedOutputs,
                StringComparer.OrdinalIgnoreCase);
            int removed=0, errors=0;
            foreach(var segment in renderedSegmentFiles)
            {
                // Delete only plain audio filenames directly inside the batch
                // output directory, never external files or merged recordings.
                string full=Path.GetFullPath(segment);
                if(!full.StartsWith(outputFolder,StringComparison.OrdinalIgnoreCase) ||
                   !string.Equals(
                       Path.GetDirectoryName(full)?.TrimEnd(Path.DirectorySeparatorChar),
                       outputFolder.TrimEnd(Path.DirectorySeparatorChar),
                       StringComparison.OrdinalIgnoreCase) ||
                   protectedOutputs.Contains(full) ||
                   Path.GetExtension(full).ToLowerInvariant() is not (".mp3" or ".wav"))
                {
                    WriteLog("Bỏ qua đường dẫn audio không an toàn: "+full);
                    errors++;
                    continue;
                }
                try
                {
                    if(File.Exists(full))
                    {
                        File.Delete(full);
                        removed++;
                    }
                }
                catch(IOException ex){errors++;WriteLog("Không thể xóa đoạn tạm: "+ex.Message);}
                catch(UnauthorizedAccessException ex){errors++;WriteLog("Không có quyền xóa đoạn tạm: "+ex.Message);}
            }
            WriteLog($"SRT: Đã xóa {removed}/{renderedSegmentFiles.Count} file audio từng câu."
                +(errors>0?$" Còn {errors} file chưa thể xóa.":""));
        }
    }
}
