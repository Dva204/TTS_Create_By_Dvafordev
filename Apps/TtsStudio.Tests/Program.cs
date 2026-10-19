using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TTS_Create_By_DvaForDev;
using TTS_Create_By_DvaForDev.Models;
using TTS_Create_By_DvaForDev.Services;
using System.Drawing;

class V4Smoke
{
    [STAThread] static void Main()
    {
        ApplicationConfiguration.Initialize();
        string root=Path.Combine(Path.GetTempPath(),"DvaForDev","V4-smoke-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var store=new VoiceWorkspaceStore(Path.Combine(root,"pref.json"));
            var p=new VoiceWorkspace(true,"narrator, deep",new TtsGenerationSettings(1, .65, .65, .5,true));
            store.Set("voice123",p);
            var restored=new VoiceWorkspaceStore(Path.Combine(root,"pref.json"));
            if(!restored.IsFavorite("voice123")||restored.Get("voice123").Preset?.Temperature!=.65)
                throw new Exception("Voice preset/favorite not restored");
            Console.WriteLine("PASS: sidecar voice favorites and per-voice preset");

            var source=Path.Combine(root,"source");Directory.CreateDirectory(source);
            File.WriteAllBytes(Path.Combine(source,"001.wav"),new byte[512]);
            File.WriteAllText(Path.Combine(source,"demo_subtitles.srt"),
                "1\n00:00:00,000 --> 00:00:01,000\nXin chao\n");
            var segments=new[]{new StudioSegment("001","Hello","voice123","vi",
                "vieneu_v3_turbo","Completed","001.wav",null,1.0)};
            var model=new StudioProject(1,"Demo",DateTimeOffset.UtcNow,
                "voice123","vieneu_v3_turbo","vi",source,segments);
            var file=Path.Combine(root,"demo.ttsproject.json");
            StudioProjectStore.SaveAsync(file,model).GetAwaiter().GetResult();
            var loaded=StudioProjectStore.LoadAsync(file).GetAwaiter().GetResult();
            if(loaded.Segments.Count!=1 || loaded.Segments[0].Text!="Hello")
                throw new Exception("Project roundtrip failed");
            var exported=StudioProjectStore.ExportAsync(loaded,
                Path.Combine(root,"export")).GetAwaiter().GetResult();
            if(!File.Exists(Path.Combine(exported,"audio","001.wav"))||
                !File.Exists(Path.Combine(exported,"demo_subtitles.srt")))
                throw new Exception("Export incomplete");
            if(StudioProjectStore.Fingerprint("a","voice","en","model","{}")==
               StudioProjectStore.Fingerprint("b","voice","en","model","{}"))
                throw new Exception("Cache fingerprints not content-dependent");
            var unsafeProject=loaded with { Segments=new[]{segments[0] with {AudioFile="../secret.wav"}} };
            bool refused=false;
            try {StudioProjectStore.ExportAsync(unsafeProject,Path.Combine(root,"unsafe"))
                .GetAwaiter().GetResult();}
            catch(InvalidDataException){refused=true;}
            if(!refused)throw new Exception("Unsafe export path accepted");
            Console.WriteLine("PASS: project save/load/export and unsafe path refusal");

            string srt="1\n00:00:00,000 --> 00:00:01,000\nHello\n\n"+
                       "2\n00:00:00,900 --> 00:00:02,000\nWorld\n";
            var cues=SrtQualityService.Read(srt);
            if(cues.Count!=2||!SrtQualityService.Validate(cues,1.5).Contains("trùng"))
                throw new Exception("SRT overlap detection failed");
            Console.WriteLine("PASS: SRT validation detects overlap/length");

            int rate=16000;
            var pcm=new byte[rate*2];
            for(int i=0;i<rate;i++)
            {
                short value=(short)(Math.Sin(2*Math.PI*185*i/rate)*10000);
                BitConverter.GetBytes(value).CopyTo(pcm,2*i);
            }
            var m=AudioQualityService.AnalyzePcm16(pcm,rate);
            if(Math.Abs(m.DurationSeconds-1)>1e-4 ||
               m.EstimatedPitchHz<140||m.EstimatedPitchHz>240||m.ClippedPercent>0.01)
                throw new Exception($"Audio metrics incorrect: {m}");
            Console.WriteLine("PASS: PCM audio pitch/RMS and clipping analysis");

            using var form=new Form1();
            var tab=Find<TabControl>(form);
            if(tab==null||tab.TabPages.Count!=7)throw new Exception("Expected 7 standalone TTS UI tabs");
            var names=tab.TabPages.Cast<TabPage>().Select(x=>x.Text).ToArray();
            foreach(var title in new[]{"Clone giọng V2","Dự án & Xuất","SRT nâng cao","Hệ thống V3"})
                if(!names.Contains(title))throw new Exception("UI tab missing: "+title);
            form.Size=new Size(1550,920);
            // Disable only the real startup handler for an offline UI smoke:
            // do not launch Python or touch live inference/voice data.
            var method=typeof(Form1).GetMethod("Form1_Load",
                BindingFlags.Instance|BindingFlags.NonPublic)!;
            var handler=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),form,method);
            form.Load -= handler;
            form.Show();
            Application.DoEvents();
            foreach(var (index,key) in new[]{(1,"voice"),(3,"clone"),(4,"project"),
                                         (5,"srt"),(6,"backend")})
            {
                tab.SelectedIndex=index;
                Application.DoEvents();
                using var bitmap=new Bitmap(form.Width,form.Height);
                form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));
                bitmap.Save(Path.Combine(root,key+"_v4.png"));
            }
            form.Hide();
            Console.WriteLine("PASS: rendered 4 WinForms V4 screens with backend startup disabled");
        }
        finally
        {
            // Keep temporary screenshot for review, no user-owned project data touched.
            Console.WriteLine("SMOKE_ROOT "+root);
        }
    }
    static T? Find<T>(Control root) where T:Control
    {
        if(root is T t)return t;
        foreach(Control c in root.Controls)
        {
            var v=Find<T>(c);if(v!=null)return v;
        }
        return null;
    }
}
