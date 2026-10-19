using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using TTS_Create_By_DvaForDev;
using TTS_Create_By_DvaForDev.Services;

static class SrtSegmentCleanupTests
{
    private static void RunTask(Func<Task> callback)
    {
        var task=callback();
        var elapsed=Stopwatch.StartNew();
        while(!task.IsCompleted && elapsed.Elapsed<TimeSpan.FromSeconds(45))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if(!task.IsCompleted)throw new TimeoutException("SRT merge timed out");
        task.GetAwaiter().GetResult();
    }

    public static void Run(Form1 form,string workspace,string shortTone,string longTone)
    {
        var grid=form.Controls.Find("dgvJobs",true).OfType<DataGridView>().Single();
        var outputBox=form.Controls.Find("txtBatchFolder",true).OfType<TextBox>().Single();
        var delete=form.Controls.Find("chkDeleteSegments",true).OfType<CheckBox>().Single();
        string formerOutput=outputBox.Text;
        bool formerDelete=delete.Checked;
        var mergeMethod=typeof(Form1).GetMethod("JoinTimedSrtAudioAsync",
            BindingFlags.NonPublic|BindingFlags.Instance)!;
        var srtMethod=typeof(Form1).GetMethod("GenerateSrtTask",
            BindingFlags.NonPublic|BindingFlags.Instance)!;
        try
        {
            string destination=Path.Combine(workspace,"delete-srt-success");
            Directory.CreateDirectory(destination);
            string first=Path.Combine(destination,"001.wav");
            string second=Path.Combine(destination,"002.wav");
            File.Copy(shortTone,first);
            File.Copy(shortTone,second);
            string originalSrt=Path.Combine(destination,"Bản ghi gốc.srt");
            string originalContents="1\n00:00:00,000 --> 00:00:01,000\nMở đầu\n";
            File.WriteAllText(originalSrt,originalContents);
            grid.Rows.Clear();
            void AddCue(string id,string file,string stem)
            {
                var position=grid.Rows.Add(id,file,"0.7","Nội dung "+id,
                    "Demo","vi","Completed",stem);
                grid.Rows[position].Tag=new SubtitleCue(int.Parse(id),
                    TimeSpan.Zero,TimeSpan.FromSeconds(1),"Nội dung "+id);
            }
            AddCue("001","001.wav","Bản ghi gốc");
            AddCue("002","002.wav","Video thứ hai");
            outputBox.Text=destination;
            delete.Checked=true;
            RunTask(()=> (Task)mergeMethod.Invoke(form,new object[]{
                destination,grid,CancellationToken.None})!);
            if(File.Exists(first)||File.Exists(second))
                throw new Exception("SRT Del Segments enabled but individual audio survived");
            if(!File.Exists(originalSrt)||
               File.ReadAllText(originalSrt)!=originalContents)
                throw new Exception("Source SRT was deleted or modified");
            var exports=Directory.GetFiles(destination)
                .Where(f=>Path.GetExtension(f) is ".mp3" or ".wav").ToArray();
            if(exports.Length!=2 || exports.Any(f=>new FileInfo(f).Length<1000))
                throw new Exception("Final SRT recordings are not preserved");
            RunTask(()=>(Task)srtMethod.Invoke(form,null)!);
            if(!File.Exists(Path.Combine(destination,"Bản ghi gốc_subtitles.srt")))
                throw new Exception("Create SRT fails after deleting audio segments");
            Console.WriteLine("PASS: enabled SRT cleanup deletes only segments after all exports; SRT intact");

            // A multi-group failure must not delete *any* source segments,
            // even when the first group has already rendered successfully.
            string failed=Path.Combine(workspace,"delete-srt-failure");
            Directory.CreateDirectory(failed);
            File.Copy(shortTone,Path.Combine(failed,"001.wav"));
            File.Copy(longTone,Path.Combine(failed,"002.wav"));
            outputBox.Text=failed;
            grid.Rows.Clear();
            AddCue("001","001.wav","A-first-succeeds");
            AddCue("002","002.wav","B-overlong-fails");
            bool rejected=false;
            try
            {
                RunTask(()=>(Task)mergeMethod.Invoke(form,new object[]{
                    failed,grid,CancellationToken.None})!);
            }
            catch(InvalidDataException){rejected=true;}
            if(!rejected)
                throw new Exception("Overlong group did not stop SRT cleanup");
            if(!File.Exists(Path.Combine(failed,"001.wav"))||
               !File.Exists(Path.Combine(failed,"002.wav")))
                throw new Exception("Source audio deleted despite failed SRT merge");
            Console.WriteLine("PASS: any failed SRT group prevents deletion of all source audio");
        }
        finally
        {
            delete.Checked=formerDelete;
            outputBox.Text=formerOutput;
        }
    }
}
