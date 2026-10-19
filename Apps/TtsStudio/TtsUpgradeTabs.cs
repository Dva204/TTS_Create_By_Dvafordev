using System.Diagnostics;
using System.Drawing;
using TTS_Create_By_DvaForDev.Models;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private readonly VoiceWorkspaceStore _voiceWorkspace = new();
    private TextBox _projectName = null!, _referencePath = null!, _clonePath = null!,
                    _cloneText = null!, _voiceTags = null!, _srtAudioPath = null!, _srtPath = null!;
    private Label _cloneResult = null!, _projectInfo = null!;
    private TextBox _diagnosticsText = null!, _srtResult = null!;
    private CheckBox _favoriteVoice = null!;
    private CheckBox? _favoritesOnly;
    private string? _activeProjectFile;

    private static Button ActionButton(string caption, Action click, int width = 145)
    {
        var button = new Button { Text = caption, Width = width, Height = 35,
            Margin = new Padding(4), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderColor = Color.FromArgb(205, 219, 225);
        button.Click += (_, _) => click();
        return button;
    }

    private static Panel WorkspacePage(TabPage page, string title, string subtitle)
    {
        var content = new Panel {
            Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 12, 18, 18),
            BackColor = Color.FromArgb(247, 249, 251)
        };
        var top = new Panel { Dock = DockStyle.Top, Height = 92,
            BackColor = Color.FromArgb(247, 249, 251), Padding = new Padding(20,8,16,6) };
        var heading = new Label { Text = title, Dock = DockStyle.Top, Height = 41,
            Font = new Font("Segoe UI Semibold", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(35, 65, 88) };
        var hint = new Label { Text = subtitle, Dock = DockStyle.Bottom, Height = 30,
            ForeColor = Color.FromArgb(88, 106, 120),
            Font = new Font("Segoe UI",9) };
        top.Controls.Add(heading);
        top.Controls.Add(hint);
        // Keeping the title in a separate docked header prevents subsequent
        // Dock=Top content controls from pushing it BELOW the controls.
        page.Controls.Add(content);
        page.Controls.Add(top);
        return content;
    }

    private static FlowLayoutPanel Toolbar(int height = 52) => new()
    {
        Dock = DockStyle.Top, AutoSize = false, Height = height,
        FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
        AutoScroll = true, BackColor = Color.White, Padding = new Padding(6, 5, 6, 4)
    };

    private VoiceDto? CurrentLibraryVoice()
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).OfType<DataGridView>().FirstOrDefault();
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto voice) return voice;
        return voiceComboBox.SelectedItem as VoiceDto;
    }

    private void InitializeUpgradeTabs()
    {
        var cloneTab = new TabPage("Clone giọng V2");
        var projectTab = new TabPage("Dự án & Xuất");
        var srtTab = new TabPage("SRT nâng cao");
        var backendTab = new TabPage("Hệ thống V3");
        mainTabControl.TabPages.Add(cloneTab);
        mainTabControl.TabPages.Add(projectTab);
        mainTabControl.TabPages.Add(srtTab);
        mainTabControl.TabPages.Add(backendTab);
        BuildCloneTab(cloneTab);
        BuildVoiceLibraryV3Toolbar();
        BuildProjectTab(projectTab);
        BuildSrtTab(srtTab);
        BuildBackendTab(backendTab);
    }

    private void BuildVoiceLibraryV3Toolbar()
    {
        var table=Controls.Find("dgvVoiceLibrary",true).OfType<DataGridView>().FirstOrDefault();
        if(table?.Parent is null)return;
        var bar=Toolbar(43);
        bar.BackColor=Color.FromArgb(250,252,253);
        _favoritesOnly=new CheckBox {Text="Chỉ giọng yêu thích",
            AutoSize=true,Margin=new Padding(15,10,18,4)};
        _favoritesOnly.CheckedChanged += (_,_) =>
            _ = RefreshVoices((Controls.Find("txtLibrarySearch",true)
                .OfType<TextBox>().FirstOrDefault()?.Text),lifetime.Token);
        bar.Controls.Add(_favoritesOnly);
        bar.Controls.Add(ActionButton("Sao lưu thư viện giọng",()=>_ = BackupVoiceLibraryAsync(),185));
        var parent=table.Parent;
        parent.Controls.Remove(table);
        var host=new TableLayoutPanel {
            Dock=DockStyle.Fill,RowCount=2,ColumnCount=1,
            BackColor=Color.White,Padding=Padding.Empty,Margin=Padding.Empty
        };
        host.RowStyles.Add(new RowStyle(SizeType.Absolute,46));
        host.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        table.Dock=DockStyle.Fill;
        bar.Dock=DockStyle.Fill;
        host.Controls.Add(bar,0,0);
        host.Controls.Add(table,0,1);
        parent.Controls.Add(host);
    }

    private async Task BackupVoiceLibraryAsync()
    {
        var backend=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..","..","..","backend","data","voices"));
        if(!Directory.Exists(backend))
        {
            MessageBox.Show(this,"Thư mục voice riêng chưa sẵn sàng.");
            return;
        }
        if(MessageBox.Show(this,"Bản ZIP chứa audio giọng cá nhân; chỉ lưu ở vị trí riêng tư.\n"+
            "Bạn có muốn sao lưu toàn bộ thư viện giọng hiện tại?",
            "Sao lưu Voice Library",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        using var choose=new SaveFileDialog{Filter="ZIP archive|*.zip",FileName="DvaForDev_Voices_Backup.zip"};
        if(choose.ShowDialog(this)!=DialogResult.OK)return;
        if(File.Exists(choose.FileName))
        {
            MessageBox.Show(this,"Chọn tên ZIP mới, không ghi đè bản sao lưu trước.");
            return;
        }
        try
        {
            await Task.Run(()=>System.IO.Compression.ZipFile.CreateFromDirectory(backend,
                choose.FileName,System.IO.Compression.CompressionLevel.Optimal,
                includeBaseDirectory:true),lifetime.Token);
            MessageBox.Show(this,"Đã sao lưu Voice Library: "+choose.FileName);
        }
        catch(Exception ex){ShowError(ex);}
    }

    private void BuildCloneTab(TabPage tab)
    {
        var p=WorkspacePage(tab,"Voice Cloning Studio V2",
            "Nghe A/B, phân tích cao độ ước tính, clipping và lưu preset cho từng giọng. Không thay đổi preset mặc định.");
        _cloneResult = new Label { Dock = DockStyle.Top, Height=100, Padding=new Padding(12),
            BackColor=Color.White,Text="Chọn giọng ở Thư viện giọng hoặc TTS, rồi bấm Tải giọng gốc.",
            AutoEllipsis=true };
        _referencePath = new TextBox { ReadOnly=true, Dock=DockStyle.Top, Height=29,
            PlaceholderText="File giọng mẫu..." };
        _clonePath = new TextBox { ReadOnly=true, Dock=DockStyle.Top, Height=29,
            PlaceholderText="File clone vừa tạo..." };
        _cloneText = new TextBox { Dock=DockStyle.Top, Height=32,
            Text="Xin chào, đây là bài kiểm tra chất lượng giọng đọc." };
        var prefs=Toolbar(46);
        _favoriteVoice = new CheckBox {Text="Yêu thích",AutoSize=true,Margin=new Padding(12,13,16,4)};
        prefs.Controls.Add(_favoriteVoice);
        prefs.Controls.Add(new Label { Text="Thẻ:",AutoSize=true,Margin=new Padding(8,14,3,3) });
        _voiceTags = new TextBox { Width=245,PlaceholderText="Ví dụ: kể chuyện, trầm, Việt",Margin=new Padding(2,9,8,3) };
        prefs.Controls.Add(_voiceTags);
        prefs.Controls.Add(ActionButton("Lưu yêu thích / thẻ", SaveVoiceWorkspace,165));
        prefs.Controls.Add(ActionButton("Lưu preset của giọng",SaveSelectedVoicePreset,166));
        prefs.Controls.Add(ActionButton("Áp dụng preset giọng",ApplySelectedVoicePreset,166));
        var controls=Toolbar();
        controls.Controls.Add(ActionButton("Tải giọng gốc",()=>_ = LoadReferenceForABAsync()));
        controls.Controls.Add(ActionButton("Tạo bản clone",()=>_ = GenerateCloneABAsync()));
        controls.Controls.Add(ActionButton("Nghe A",()=>PlayAudio(_referencePath.Text),90));
        controls.Controls.Add(ActionButton("Nghe B",()=>PlayAudio(_clonePath.Text),90));
        controls.Controls.Add(ActionButton("Phân tích A/B",()=>_ = AnalyzeABAsync(),145));
        p.Controls.Add(_cloneResult);
        p.Controls.Add(_clonePath);
        p.Controls.Add(_referencePath);
        p.Controls.Add(controls);
        p.Controls.Add(_cloneText);
        p.Controls.Add(prefs);
    }

    private void SaveVoiceWorkspace()
    {
        var voice=CurrentLibraryVoice();
        if(voice is null){ MessageBox.Show(this,"Hãy chọn một giọng trước.");return; }
        var old=_voiceWorkspace.Get(voice.Id);
        _voiceWorkspace.Set(voice.Id,old with {Favorite=_favoriteVoice.Checked, Tags=_voiceTags.Text.Trim()});
        _cloneResult.Text=$"Đã lưu thông tin riêng cho giọng: {voice.Name}. Không sửa voice.json.";
        _ = RefreshVoices((Controls.Find("txtLibrarySearch",true).OfType<TextBox>()
            .FirstOrDefault()?.Text),lifetime.Token);
    }
    private TtsGenerationSettings CurrentPreset() => new(
        (double)numSpeed.Value,(double)numStability.Value/100,
        (double)numSimilarity.Value/100,(double)numStyle.Value/100,
        chkSpeakerBoost.Checked,null,(double)numDenoise.Value/100);
    private void SaveSelectedVoicePreset()
    {
        var voice=CurrentLibraryVoice();
        if(voice is null){MessageBox.Show(this,"Hãy chọn một giọng trước.");return;}
        _voiceWorkspace.Set(voice.Id,_voiceWorkspace.Get(voice.Id) with {Preset=CurrentPreset()});
        _cloneResult.Text="Đã lưu preset riêng cho "+voice.Name+"; preset mặc định của app không đổi.";
    }
    private void ApplySelectedVoicePreset()
    {
        var voice=CurrentLibraryVoice();
        var preset=voice is null?null:_voiceWorkspace.Get(voice.Id).Preset;
        if(preset is null){MessageBox.Show(this,"Giọng chưa có preset riêng.");return;}
        numSpeed.Value=Math.Clamp((decimal)preset.Speed,numSpeed.Minimum,numSpeed.Maximum);
        numSimilarity.Value=Math.Clamp((decimal)(preset.Temperature*100),numSimilarity.Minimum,numSimilarity.Maximum);
        numStability.Value=Math.Clamp((decimal)(preset.CfgWeight*100),numStability.Minimum,numStability.Maximum);
        numStyle.Value=Math.Clamp((decimal)(preset.Exaggeration*100),numStyle.Minimum,numStyle.Maximum);
        numDenoise.Value=Math.Clamp((decimal)(preset.Denoise*100),numDenoise.Minimum,numDenoise.Maximum);
        chkSpeakerBoost.Checked=preset.SpeakerBoost;
        _cloneResult.Text="Đã áp dụng preset của giọng được chọn cho phiên làm việc này.";
    }
    private void SyncVoiceWorkspace()
    {
        var voice=CurrentLibraryVoice();
        if(voice is null)return;
        var data=_voiceWorkspace.Get(voice.Id);
        _favoriteVoice.Checked=data.Favorite;_voiceTags.Text=data.Tags;
    }

    private static void PlayAudio(string path)
    {
        if(string.IsNullOrWhiteSpace(path)||!File.Exists(path))return;
        Process.Start(new ProcessStartInfo(path){UseShellExecute=true});
    }
    private async Task LoadReferenceForABAsync()
    {
        var voice=CurrentLibraryVoice();
        if(voice is null){MessageBox.Show(this,"Chọn một giọng trong thư viện trước.");return;}
        try
        {
            var data=await api.DownloadReferenceAsync(voice.Id,lifetime.Token);
            string dir=Path.Combine(Path.GetTempPath(),"DvaForDev","TtsStudio","AB");
            Directory.CreateDirectory(dir);
            // Reference API can return WAV or MP3. Preserve its actual format
            // for Windows playback even if legacy voice metadata is inconsistent.
            bool riff=data.Length>12 && data[0]=='R' && data[1]=='I' &&
                      data[2]=='F' && data[3]=='F';
            bool flac=data.Length>4 && data[0]=='f' && data[1]=='L' &&
                      data[2]=='a' && data[3]=='C';
            string ext=riff?".wav":flac?".flac":".mp3";
            string path=Path.Combine(dir,"reference-"+Guid.NewGuid().ToString("N")+ext);
            await File.WriteAllBytesAsync(path,data,lifetime.Token);
            _referencePath.Text=path;_cloneResult.Text="Đã tải giọng mẫu: "+voice.Name;
            SyncVoiceWorkspace();
        }
        catch(Exception ex){ShowError(ex);}
    }
    private async Task GenerateCloneABAsync()
    {
        var voice=CurrentLibraryVoice();
        if(voice is null || string.IsNullOrWhiteSpace(_cloneText.Text))
        {MessageBox.Show(this,"Chọn giọng và nhập một câu thử.");return;}
        try
        {
            _cloneResult.Text="Đang tạo bản clone…";
            bool vi=voice.Language?.Contains("vi",StringComparison.OrdinalIgnoreCase)==true ||
                    voice.Language?.Contains("Vietnam",StringComparison.OrdinalIgnoreCase)==true;
            string engine=vi?"vieneu_v3_turbo":"chatterbox_multilingual";
            string lang=vi?"vi":GetLangCode(voice.Language??"en");
            var request=new GenerateRequest(_cloneText.Text,voice.Id,lang,engine,CurrentPreset());
            var job=await api.GenerateAsync(request,lifetime.Token);
            for(int i=0;i<180;i++)
            {
                await Task.Delay(1500,lifetime.Token);
                var status=await api.GetJobAsync(job.Id,lifetime.Token);
                if(status?.Status is "failed" or "cancelled")
                    throw new InvalidOperationException(status.Error??"Suy luận thất bại.");
                if(status?.Status=="completed")
                {
                    var bytes=await api.DownloadAudioAsync(job.Id,lifetime.Token);
                    string dir=Path.Combine(Path.GetTempPath(),"DvaForDev","TtsStudio","AB");
                    Directory.CreateDirectory(dir);
                    string path=Path.Combine(dir,"clone-"+Guid.NewGuid().ToString("N")+".wav");
                    await File.WriteAllBytesAsync(path,bytes,lifetime.Token);
                    _clonePath.Text=path;
                    try{await api.CancelJobAsync(job.Id,lifetime.Token);}catch{}
                    _cloneResult.Text="Clone đã tạo xong. Nghe A/B rồi xem các thông số âm học.";
                    return;
                }
            }
            throw new TimeoutException("Hết thời gian đợi bản clone.");
        }
        catch(Exception ex){ShowError(ex);}
    }

    private async Task AnalyzeABAsync()
    {
        if(!File.Exists(_referencePath.Text)||!File.Exists(_clonePath.Text))
        {MessageBox.Show(this,"Cần tải giọng mẫu và tạo clone trước.");return;}
        try
        {
            var a=await AudioQualityService.AnalyzeFileAsync(_referencePath.Text,lifetime.Token);
            var b=await AudioQualityService.AnalyzeFileAsync(_clonePath.Text,lifetime.Token);
            _cloneResult.Text=$"A: {a.DurationSeconds:F1}s | Pitch ~{a.EstimatedPitchHz:F0} Hz | RMS {a.RmsDbfs:F1} dBFS | Clip {a.ClippedPercent:F2}%\n"+
                $"B: {b.DurationSeconds:F1}s | Pitch ~{b.EstimatedPitchHz:F0} Hz | RMS {b.RmsDbfs:F1} dBFS | Clip {b.ClippedPercent:F2}%\n"+
                "Pitch chỉ là ước tính; so sánh nghe A/B vẫn quan trọng nhất.";
        }
        catch(Exception ex){ShowError(ex);}
    }

    private StudioProject CaptureProject()
    {
        var dgv=Controls.Find("dgvJobs",true).OfType<DataGridView>().First();
        var dir=(Controls.Find("txtBatchFolder",true).OfType<TextBox>().FirstOrDefault()?.Text ?? "").Trim();
        var rows=dgv.Rows.Cast<DataGridViewRow>().Where(x=>!x.IsNewRow).Select(row =>
        {
            string Cell(int i)=>row.Cells.Count>i?row.Cells[i].Value?.ToString()??"":"";
            _=double.TryParse(Cell(2),System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,out double duration);
            return new StudioSegment(Cell(0),Cell(3),voiceComboBox.SelectedValue?.ToString()??"",
                Cell(5),engineComboBox.Text,Cell(6),
                string.IsNullOrWhiteSpace(Cell(1))?null:Cell(1),Cell(7),duration);
        }).ToArray();
        return new StudioProject(1,_projectName.Text.Trim(),
            DateTimeOffset.UtcNow,voiceComboBox.SelectedValue?.ToString()??"",
            engineComboBox.Text,languageComboBox.Text,dir,rows);
    }

    private void BuildProjectTab(TabPage tab)
    {
        var p=WorkspacePage(tab,"Project & Export Manager",
            "Lưu danh sách câu TTS thành file .ttsproject.json; xuất audio/SRT cho Whiteboard. Không đụng dữ liệu giọng.");
        _projectInfo=new Label {Dock=DockStyle.Top,Height=170,BackColor=Color.White,
            Padding=new Padding(15),Text="Chưa mở dự án.\nDự án được lưu độc lập dưới dạng JSON.\nBạn có thể lưu khi đang tạo audio, rồi tiếp tục vào lần sau."};
        _projectName=new TextBox {Dock=DockStyle.Top,Height=32,Text="Du an YouTube moi"};
        var bar=Toolbar();
        bar.Controls.Add(ActionButton("Lưu dự án",()=>_ = SaveStudioProjectAsync()));
        bar.Controls.Add(ActionButton("Mở dự án",()=>_ = OpenStudioProjectAsync()));
        bar.Controls.Add(ActionButton("Xuất cho Whiteboard",()=>_ = ExportStudioProjectAsync(),183));
        p.Controls.Add(_projectInfo);p.Controls.Add(bar);p.Controls.Add(_projectName);
    }

    private async Task SaveStudioProjectAsync()
    {
        if(string.IsNullOrWhiteSpace(_projectName.Text)){MessageBox.Show(this,"Nhập tên dự án.");return;}
        if(string.IsNullOrWhiteSpace(_activeProjectFile))
        {
            using var choose=new SaveFileDialog {Filter="DvaForDev Project|*.ttsproject.json",
                DefaultExt="ttsproject.json",FileName="project.ttsproject.json"};
            if(choose.ShowDialog(this)!=DialogResult.OK)return;
            _activeProjectFile=choose.FileName;
        }
        try
        {
            var model=CaptureProject();
            await StudioProjectStore.SaveAsync(_activeProjectFile,model,lifetime.Token);
            _projectInfo.Text=$"Đã lưu: {_activeProjectFile}\n{model.Segments.Count} câu TTS\nThư mục audio: {model.OutputFolder}";
        }
        catch(Exception ex){ShowError(ex);}
    }
    private async Task OpenStudioProjectAsync()
    {
        using var choose=new OpenFileDialog {Filter="DvaForDev Project|*.ttsproject.json"};
        if(choose.ShowDialog(this)!=DialogResult.OK)return;
        try
        {
            var model=await StudioProjectStore.LoadAsync(choose.FileName,lifetime.Token);
            var dgv=Controls.Find("dgvJobs",true).OfType<DataGridView>().First();
            if(dgv.Rows.Count>0 && MessageBox.Show(this,"Thay danh sách TTS đang hiển thị?",
                "Mở dự án",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            dgv.Rows.Clear();
            foreach(var seg in model.Segments)
                dgv.Rows.Add(seg.Id,seg.AudioFile??"",
                    seg.DurationSeconds>0?seg.DurationSeconds.ToString(
                        System.Globalization.CultureInfo.InvariantCulture):"",
                    seg.Text,seg.VoiceId,seg.Language,seg.Status,seg.Source??"");
            var folder=Controls.Find("txtBatchFolder",true).OfType<TextBox>().FirstOrDefault();
            if(folder is not null) folder.Text=model.OutputFolder;
            var selected=allVoices.FirstOrDefault(v=>v.Id==model.VoiceId);
            if(selected is not null) voiceComboBox.SelectedValue=selected.Id;
            if(engineComboBox.Items.Contains(model.Engine))engineComboBox.SelectedItem=model.Engine;
            _projectName.Text=model.Name;
            _activeProjectFile=choose.FileName;
            _projectInfo.Text=$"Đã mở: {choose.FileName}\n{model.Segments.Count} câu\nSẵn sàng kiểm tra và tiếp tục Batch.";
        }
        catch(Exception ex){ShowError(ex);}
    }
    private async Task ExportStudioProjectAsync()
    {
        try
        {
            var model=CaptureProject();
            if(!Directory.Exists(model.OutputFolder))
            {MessageBox.Show(this,"Chọn thư mục audio chứa những file đã tạo.");return;}
            using var choose=new FolderBrowserDialog {Description="Chọn thư mục cha để tạo bản xuất mới"};
            if(choose.ShowDialog(this)!=DialogResult.OK)return;
            string name=string.Concat(model.Name.Select(c=>Path.GetInvalidFileNameChars().Contains(c)?'_':c));
            if(string.IsNullOrWhiteSpace(name))name="TTS";
            string dir=Path.Combine(choose.SelectedPath,
                name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string output=await StudioProjectStore.ExportAsync(model,dir,lifetime.Token);
            var exported=await StudioProjectStore.LoadAsync(
                Path.Combine(output,"project.ttsproject.json"),lifetime.Token);
            int missing=exported.Segments.Count(x=>x.Status=="Missing");
            _projectInfo.Text=$"Đã xuất:\n{output}\nAudio/SRT/project JSON. "+
                (missing>0?$"CẢNH BÁO: {missing} câu thiếu file audio.":"Đã kiểm tra danh sách file có sẵn.");
            if(missing>0)
                MessageBox.Show(this,$"{missing} câu chưa có audio. Hãy tạo lại trước khi dựng video.",
                    "Thiếu audio",MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
        catch(Exception ex){ShowError(ex);}
    }
    private void BuildSrtTab(TabPage tab)
    {
        var p=WorkspacePage(tab,"Smart SRT & Alignment",
            "Kiểm tra timeline SRT và chuẩn bị cho forced alignment. Model căn chỉnh cần cài riêng, không tự tải.");
        _srtResult=new TextBox {Dock=DockStyle.Top,Height=210,Multiline=true,
            ScrollBars=ScrollBars.Vertical,ReadOnly=true,BackColor=Color.White,
            Text="Chọn file audio và SRT để kiểm tra thời gian."};
        _srtAudioPath=new TextBox {Dock=DockStyle.Top,Height=32,PlaceholderText="Đường dẫn audio"};
        _srtPath=new TextBox {Dock=DockStyle.Top,Height=32,PlaceholderText="Đường dẫn .srt"};
        var bar=Toolbar();
        bar.Controls.Add(ActionButton("Chọn audio",()=>ChooseFile(_srtAudioPath,"Audio|*.wav;*.mp3;*.m4a")));
        bar.Controls.Add(ActionButton("Chọn SRT",()=>ChooseFile(_srtPath,"SRT|*.srt")));
        bar.Controls.Add(ActionButton("Kiểm tra SRT",()=>_ = CheckSrtAsync()));
        bar.Controls.Add(ActionButton("Forced alignment",()=>_ = AlignSrtAsync(),166));
        p.Controls.Add(_srtResult);p.Controls.Add(bar);p.Controls.Add(_srtPath);p.Controls.Add(_srtAudioPath);
    }
    private void ChooseFile(TextBox target,string filter)
    {
        using var dialog=new OpenFileDialog {Filter=filter};
        if(dialog.ShowDialog(this)==DialogResult.OK)target.Text=dialog.FileName;
    }
    private async Task CheckSrtAsync()
    {
        try { _srtResult.Text=await SrtQualityService.InspectAsync(_srtPath.Text,_srtAudioPath.Text,lifetime.Token); }
        catch(Exception ex){_srtResult.Text=ex.Message;}
    }
    private async Task AlignSrtAsync()
    {
        try {
            if(MessageBox.Show(this,
                "WhisperX cần môi trường riêng và có thể tải model căn chỉnh ở lần chạy đầu."+
                "\nTiếp tục thử căn chỉnh trên CPU?",
                "Forced alignment",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)
                return;
            _srtResult.Text=await SrtQualityService.RunOptionalWhisperxAsync(
                _srtPath.Text,_srtAudioPath.Text,lifetime.Token,
                GetLangCode(languageComboBox.Text));
        }
        catch(Exception ex){_srtResult.Text=ex.Message;}
    }

    private void BuildBackendTab(TabPage tab)
    {
        var p=WorkspacePage(tab,"Backend Manager V3 & Performance",
            "Chẩn đoán backend, nguồn Python, engine và VRAM. Không dừng tiến trình backend đang xử lý job.");
        _diagnosticsText=new TextBox { Dock=DockStyle.Fill, Multiline=true,
            ReadOnly=true,ScrollBars=ScrollBars.Both,Font=new Font("Consolas",10),
            BackColor=Color.White,Text="Bấm Kiểm tra để xem backend và GPU." };
        var bar=Toolbar();
        bar.Controls.Add(ActionButton("Kiểm tra backend / GPU",()=>_ = ShowBackendDiagnosticsAsync(),203));
        bar.Controls.Add(ActionButton("Kết nối lại",()=>_ = ReconnectBackendAsync(),145));
        var cache=new CheckBox {Text="Cache audio (lần khởi động sau)", AutoSize=true,
            Checked=BackendProcessManager.AudioCacheEnabledNextLaunch,
            Margin=new Padding(15,12,3,2)};
        cache.CheckedChanged += (_,_) =>
        {
            BackendProcessManager.SetAudioCacheForNextLaunch(cache.Checked);
            _diagnosticsText.Text="Đã lưu cấu hình cache cho lần khởi động backend tiếp theo. "+
                "Không restart backend hoặc thay đổi preset hiện tại.";
        };
        bar.Controls.Add(cache);
        p.Controls.Add(_diagnosticsText);p.Controls.Add(bar);
    }
    private async Task ShowBackendDiagnosticsAsync()
    {
        try {_diagnosticsText.Text=await BackendDiagnosticsService.SnapshotAsync(settings.BackendUri,lifetime.Token);}
        catch(Exception ex){_diagnosticsText.Text=ex.Message;}
    }
    private async Task ReconnectBackendAsync()
    {
        try
        {
            await backend.ReconnectAsync(settings.PythonExecutable,lifetime.Token);
            await RefreshStatus(lifetime.Token);
            await RefreshVoices(null,lifetime.Token);
            await ShowBackendDiagnosticsAsync();
        }
        catch(Exception ex){_diagnosticsText.Text="Không kết nối được: "+ex.Message;}
    }
}
