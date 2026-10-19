using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using TTS_Create_By_DvaForDev;
using TTS_Create_By_DvaForDev.Services;

static class BatchOutputNamingTests
{
    public static void Run(Form1 form, string workspace)
    {
        var folder=Path.Combine(workspace,"naming");
        Directory.CreateDirectory(folder);
        static void Assert(bool condition, string message)
        {
            if(!condition)throw new Exception(message);
        }
        Assert(BatchOutputNaming.FromSourcePath(@"D:\Content\Kịch bản số 01.txt")=="Kịch bản số 01",
            "TXT basename not preserved");
        Assert(BatchOutputNaming.FromSourcePath(@"D:\Content\Video.Part1.srt")=="Video.Part1",
            "SRT basename not preserved");
        Assert(BatchOutputNaming.SafeStem("  ")=="Full_Audio","Unexpected fallback name");
        Assert(BatchOutputNaming.SafeStem("CON")=="_CON","Reserved Windows name was not sanitized");
        Assert(BatchOutputNaming.SafeStem("../unsafe")=="_unsafe" ||
               !BatchOutputNaming.SafeStem("../unsafe").Contains(".."),
               "Unsafe filename was not sanitized");

        var candidate=BatchOutputNaming.AvailablePath(folder,"Video của tôi","mp3");
        Assert(Path.GetFileName(candidate)=="Video của tôi.mp3","Unexpected MP3 filename");
        File.WriteAllText(candidate,"previous export");
        var second=BatchOutputNaming.AvailablePath(folder,"Video của tôi","mp3");
        Assert(Path.GetFileName(second)=="Video của tôi_2.mp3","Existing output overwritten");
        Assert(File.ReadAllText(candidate)=="previous export","Existing output modified");

        var grid=form.Controls.Find("dgvJobs",true).OfType<DataGridView>().Single();
        var source=Path.Combine(folder,"Tên kênh tập 01.txt");
        File.WriteAllText(source,"Xin chào các bạn\nHôm nay chúng ta bắt đầu");
        var import=typeof(Form1).GetMethod("ImportBatchText",
            BindingFlags.Instance|BindingFlags.NonPublic)!;
        import.Invoke(form,new object[]{source});
        Assert(grid.Rows.Count>0,"TXT import did not populate rows");
        Assert(grid.Rows.Cast<DataGridViewRow>().All(r=>
            r.Cells[7].Value?.ToString()=="Tên kênh tập 01"),
            "Imported TXT did not retain source filename for export");

        var srt=Path.Combine(folder,"Tên kênh tập 02.srt");
        File.WriteAllText(srt,
            "1\n00:00:00,000 --> 00:00:01,000\nXin chào\n");
        var importSrt=typeof(Form1).GetMethod("ImportTimedSrt",
            BindingFlags.Instance|BindingFlags.NonPublic)!;
        importSrt.Invoke(form,new object[]{srt});
        Assert(grid.Rows.Count==1 && grid.Rows[0].Cells[7].Value?.ToString()=="Tên kênh tập 02",
            "SRT import did not retain source filename");

        Console.WriteLine("PASS: TXT/SRT basename, Vietnamese Unicode and collision-safe naming");
    }

    public static void VerifyTimedSrtJoin(Form1 form, string workspace, string tone)
    {
        var folder=Path.Combine(workspace,"join-names");
        Directory.CreateDirectory(folder);
        File.Copy(tone,Path.Combine(folder,"001.wav"));
        var grid=form.Controls.Find("dgvJobs",true).OfType<DataGridView>().Single();
        grid.Rows.Clear();
        const string basename="Tên video thử";
        int index=grid.Rows.Add("001","001.wav","0.7","Xin chào",
            "Demo","vi","Completed",basename);
        grid.Rows[index].Tag=new SubtitleCue(1,TimeSpan.Zero,
            TimeSpan.FromSeconds(1),"Xin chào");

        var method=typeof(Form1).GetMethod("JoinTimedSrtAudioAsync",
            BindingFlags.Instance|BindingFlags.NonPublic)!;
        void JoinAndPump()
        {
            var task=(Task)method.Invoke(form,new object[]{
                folder,grid,CancellationToken.None})!;
            var sw=Stopwatch.StartNew();
            while(!task.IsCompleted && sw.Elapsed<TimeSpan.FromSeconds(30))
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
            if(!task.IsCompleted)throw new TimeoutException("Timed SRT join test did not finish");
            task.GetAwaiter().GetResult();
        }

        var del=form.Controls.Find("chkDeleteSegments",true).OfType<CheckBox>().Single();
        bool previousDel=del.Checked;
        del.Checked=false; // Existing naming test verifies repeat export, not deletion.
        try
        {
        JoinAndPump();
        var outputs=Directory.GetFiles(folder,basename+".*")
            .Where(x=>Path.GetExtension(x) is ".mp3" or ".wav").ToArray();
        if(outputs.Length!=1)throw new Exception("Source-named SRT audio not created");
        long originalBytes=new FileInfo(outputs[0]).Length;
        JoinAndPump();
        if(new FileInfo(outputs[0]).Length!=originalBytes ||
            !File.Exists(Path.Combine(folder,basename+"_2"+Path.GetExtension(outputs[0]))))
            throw new Exception("Repeat SRT join overwrote its existing output");
        if(!File.Exists(Path.Combine(folder,"001.wav")))
            throw new Exception("Unchecked Del Segments deleted original SRT segment");
        Console.WriteLine("PASS: SRT join unchecked keeps segments and creates safe _2 suffix");
        }
        finally { del.Checked=previousDel; }
    }

    public static void VerifyTxtJoin(Form1 form, string workspace, string tone)
    {
        var folder=Path.Combine(workspace,"txt-join");
        Directory.CreateDirectory(folder);
        // The batch output format is a user setting (MP3/WAV). Test the real
        // FFmpeg path without modifying the user's setting or project files.
        var userSettings=typeof(Form1).GetField("userSettings",
            BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(form)!;
        var outputFormat=userSettings.GetType().GetProperty("OutputFormat")!
            .GetValue(userSettings)?.ToString();
        string ext=string.Equals(outputFormat,"mp3",StringComparison.OrdinalIgnoreCase)
            ?"mp3":"wav";
        var first=Path.Combine(folder,"001."+ext);
        var second=Path.Combine(folder,"002."+ext);
        foreach(var target in new[]{first,second})
        {
            if(ext=="wav") File.Copy(tone,target);
            else
            {
                var psi=new ProcessStartInfo("ffmpeg"){
                    UseShellExecute=false,CreateNoWindow=true,
                    RedirectStandardError=true
                };
                foreach(var arg in new[]{"-hide_banner","-loglevel","error","-y",
                    "-i",tone,"-b:a","192k",target})psi.ArgumentList.Add(arg);
                using var proc=Process.Start(psi)!;
                string errors=proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                if(proc.ExitCode!=0)
                    throw new Exception("Cannot prepare test MP3: "+errors);
            }
        }
        var grid=form.Controls.Find("dgvJobs",true).OfType<DataGridView>().Single();
        grid.Rows.Clear();
        const string sourceStem="Video tiếng Việt.Part1";
        grid.Rows.Add("001",Path.GetFileName(first),".7","Câu một",
            "Demo","vi","Completed",sourceStem);
        grid.Rows.Add("002",Path.GetFileName(second),".7","Câu hai",
            "Demo","vi","Completed",sourceStem);
        var deleteBox=form.Controls.Find("chkDeleteSegments",true).OfType<CheckBox>().Single();
        var outputBox=form.Controls.Find("txtBatchFolder",true).OfType<TextBox>().Single();
        string previousFolder=outputBox.Text;
        bool previous=deleteBox.Checked;
        outputBox.Text=folder;
        deleteBox.Checked=false;
        try
        {
            var join=typeof(Form1).GetMethod("JoinMp3Task",
                BindingFlags.Instance|BindingFlags.NonPublic)!;
            void Run()
            {
                var task=(Task)join.Invoke(form,null)!;
                var sw=Stopwatch.StartNew();
                while(!task.IsCompleted && sw.Elapsed<TimeSpan.FromSeconds(30))
                {
                    Application.DoEvents();
                    Thread.Sleep(10);
                }
                if(!task.IsCompleted)throw new TimeoutException("TXT audio concat timed out");
                task.GetAwaiter().GetResult();
            }
            Run();
            var merged=Path.Combine(folder,sourceStem+"."+ext);
            if(!File.Exists(merged) || new FileInfo(merged).Length<1000)
                throw new Exception("TXT join did not export by source filename");
            long previousLength=new FileInfo(merged).Length;
            Run();
            if(!File.Exists(Path.Combine(folder,sourceStem+"_2."+ext)) ||
                new FileInfo(merged).Length!=previousLength)
                throw new Exception("TXT join overwrote the first recording");
            Console.WriteLine("PASS: actual TXT concat named from original file, creates safe _2");
        }
        finally{deleteBox.Checked=previous;outputBox.Text=previousFolder;}
    }
}
