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

            // Verify exact SRT timings using real PCM WAVs decoded by FFmpeg.
            string tone1=Path.Combine(root,"tone_1.wav");
            string tone2=Path.Combine(root,"tone_2.wav");
            static void CreateTone(string file, double seconds)
            {
                int rate=24000;
                int count=(int)(rate*seconds);
                using var stream=File.Create(file);
                using var wr=new BinaryWriter(stream);
                wr.Write(Encoding.ASCII.GetBytes("RIFF"));wr.Write(36+count*2);
                wr.Write(Encoding.ASCII.GetBytes("WAVEfmt "));wr.Write(16);
                wr.Write((short)1);wr.Write((short)1);wr.Write(rate);wr.Write(rate*2);
                wr.Write((short)2);wr.Write((short)16);
                wr.Write(Encoding.ASCII.GetBytes("data"));wr.Write(count*2);
                for(int i=0;i<count;i++)
                    wr.Write((short)(Math.Sin(2*Math.PI*200*i/rate)*12000));
            }
            CreateTone(tone1,.8); CreateTone(tone2,.7);
            var cuesInput=SrtQualityService.Read(
                "1\n00:00:01,000 --> 00:00:02,500\nXin chao\n\n"+
                "2\n00:00:04,000 --> 00:00:05,500\nTam biet\n");
            var plan=cuesInput.Select((x,i)=>new TimedAudioCue(
                x.Number,x.Start,x.End,x.Text,i==0?tone1:tone2)).ToArray();
            var merge=Path.Combine(root,"with_pauses.wav");
            SrtBatchTimelineService.RenderAsync(plan,merge).GetAwaiter().GetResult();
            byte[] wave=File.ReadAllBytes(merge);
            if(wave.Length!=44+24000*2*5.5)
                throw new Exception("WAV length does not match SRT timeline: "+wave.Length);
            short At(double time) =>
                BitConverter.ToInt16(wave,44+2*(int)(time*24000));
            if(At(.5)!=0 || At(3.0)!=0 || At(3.8)!=0 || At(5.3)!=0)
                throw new Exception("SRT pause contains unexpected audio");
            if(At(1.103)==0 || At(4.103)==0)
                throw new Exception("Expected spoken segments missing");
            var roundTrip=SrtBatchTimelineService.BuildOriginalSrt(plan);
            if(!roundTrip.Contains("00:00:04,000 --> 00:00:05,500"))
                throw new Exception("Original subtitle timestamps shifted");
            bool refusedLong=false;
            CreateTone(tone1,2);
            try {
                SrtBatchTimelineService.RenderAsync(plan,Path.Combine(root,"should_not_exist.wav"))
                    .GetAwaiter().GetResult();
            } catch(InvalidDataException){refusedLong=true;}
            if(!refusedLong || File.Exists(Path.Combine(root,"should_not_exist.wav")))
                throw new Exception("Overlong generated cue silently altered SRT timeline");
            Console.WriteLine("PASS: SRT audio silence and absolute timestamps, overlong cues rejected");
            var timedProject=model with {Segments=new[]{
                segments[0] with {SrtStartMilliseconds=1000,SrtEndMilliseconds=2500}}};
            var savedTimed=Path.Combine(root,"timed.ttsproject.json");
            StudioProjectStore.SaveAsync(savedTimed,timedProject).GetAwaiter().GetResult();
            var restoredTimed=StudioProjectStore.LoadAsync(savedTimed).GetAwaiter().GetResult();
            if(restoredTimed.Segments[0].SrtStartMilliseconds!=1000 ||
                restoredTimed.Segments[0].SrtEndMilliseconds!=2500)
                throw new Exception("Lost SRT timing when saving a project");
            Console.WriteLine("PASS: project JSON retains SRT start/end times");

            using var form=new Form1();
            var tab=Find<TabControl>(form);
            if(tab==null||tab.TabPages.Count!=6)throw new Exception("Expected 6 standalone TTS UI tabs");
            var names=tab.TabPages.Cast<TabPage>().Select(x=>x.Text).ToArray();
            if(names.Any(x=>x.Contains("Clone",StringComparison.OrdinalIgnoreCase)))
                throw new Exception("Clone tab was not removed");
            var importSrt=form.Controls.Find("btnImportSrt",true).OfType<Button>().Single();
            if(importSrt.Text!="Nhập SRT")
                throw new Exception("SRT import is not a distinct TTS action");
            Console.WriteLine("PASS: Clone tab removed; TXT and SRT input actions visible");
            var example=Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,"..","..","..","..","..","Examples",
                "SRT_tam_nghi_giua_cac_cau.srt"));
            if(!File.Exists(example))
                throw new FileNotFoundException("Example SRT file missing.",example);
            var importMethod=typeof(Form1).GetMethod("ImportTimedSrt",
                BindingFlags.Instance|BindingFlags.NonPublic)!;
            importMethod.Invoke(form,new object[]{example});
            var queue=form.Controls.Find("dgvJobs",true).OfType<DataGridView>().Single();
            if(queue.Rows.Count!=4)
                throw new Exception("SRT importer lost cues: "+queue.Rows.Count);
            var first=queue.Rows[0].Tag as SubtitleCue;
            var last=queue.Rows[3].Tag as SubtitleCue;
            if(first is null||last is null||
                first.Start!=TimeSpan.FromSeconds(1)||
                last.End!=TimeSpan.FromSeconds(35))
                throw new Exception("SRT importer discarded absolute cue times");
            if(!queue.Rows[2].Cells["SrtStart"].Value!.ToString()!.Contains("00:00:18"))
                throw new Exception("SRT time columns were not populated");
            Console.WriteLine("PASS: import real four-cue SRT preserving gaps and visible columns");

            foreach(var title in new[]{"Dự án & Xuất","SRT nâng cao","Hệ thống V3"})
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

            // Three primary sections remain visible. Each shows only its
            // contextual subtabs; changing sections must preserve TabPage indexes.
            var expectedGroups = new[] {
                new[]{0,3,4}, new[]{1}, new[]{2,5}
            };
            for(int g=0;g<3;g++)
            {
                var mainNav=form.Controls.Find("btnNavGroup"+g,true).OfType<Button>().Single();
                if(!mainNav.Visible)throw new Exception("Missing main navigation group "+g);
                mainNav.PerformClick();
                Application.DoEvents();
                if(tab.SelectedIndex!=new[]{0,1,2}[g])
                    throw new Exception("Wrong default landing page for section "+g);
                var actual=Enumerable.Range(0,6).Where(i=>
                    form.Controls.Find("btnNavPage"+i,true).OfType<Button>().Single().Visible).ToArray();
                if(!actual.SequenceEqual(expectedGroups[g]))
                    throw new Exception("Incorrect contextual navigation for group "+g+
                        ": "+string.Join(",",actual));
            }
            tab.SelectedIndex=3;
            Application.DoEvents();
            form.Controls.Find("btnNavGroup1",true).OfType<Button>().Single().PerformClick();
            form.Controls.Find("btnNavGroup0",true).OfType<Button>().Single().PerformClick();
            if(tab.SelectedIndex!=3)
                throw new Exception("Switching group lost the last selected project subtab");
            form.Controls.Find("btnNavPage4",true).OfType<Button>().Single().PerformClick();
            if(tab.SelectedIndex!=4)throw new Exception("Contextual SRT subtab does not navigate");
            Console.WriteLine("PASS: 3 grouped menus, contextual subtabs and last-visited navigation");

            foreach(var (index,key) in new[]{(0,"tts"),(1,"voice"),(3,"project"),
                                         (4,"srt"),(5,"backend")})
            {
                tab.SelectedIndex=index;
                Application.DoEvents();
                using var bitmap=new Bitmap(form.Width,form.Height);
                form.DrawToBitmap(bitmap,new Rectangle(0,0,bitmap.Width,bitmap.Height));
                bitmap.Save(Path.Combine(root,key+"_v4.png"));
            }
            // Verify contextual navigation at the smallest supported shell size.
            form.Size=new Size(1050,700);
            foreach(int index in new[]{0,1,2,3,4,5})
            {
                tab.SelectedIndex=index;
                Application.DoEvents();
                var row=form.Controls.Find("ttsSubNavigation",true)
                    .OfType<FlowLayoutPanel>().Single();
                var chosen=form.Controls.Find("btnNavPage"+index,true)
                    .OfType<Button>().Single();
                if(!row.Visible || !chosen.Visible || chosen.Width<=60 ||
                   row.Width<500 || !chosen.Enabled)
                    throw new Exception("Small-window submenu clipped/inaccessible for tab "+index);
            }
            tab.SelectedIndex=0;
            Application.DoEvents();
            using(var small=new Bitmap(form.Width,form.Height))
            {
                form.DrawToBitmap(small,new Rectangle(0,0,small.Width,small.Height));
                small.Save(Path.Combine(root,"tts_compact.png"));
            }
            var txt=Path.Combine(root,"sample.txt");
            File.WriteAllText(txt,"First line.\nSecond line.\n");
            var importTxt=typeof(Form1).GetMethod("ImportBatchText",
                BindingFlags.Instance|BindingFlags.NonPublic)!;
            importTxt.Invoke(form,new object[]{txt});
            if(queue.Rows.Count==0||queue.Rows.Cast<DataGridViewRow>().Any(x=>x.Tag is SubtitleCue))
                throw new Exception("TXT import did not reset SRT metadata");
            form.Hide();
            Console.WriteLine("PASS: TXT input still supported and clears previous SRT timing");
            Console.WriteLine("PASS: compact 1050x700 layout, 6 navigation targets");
            Console.WriteLine("PASS: rendered 5 WinForms screens with backend startup disabled");
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
