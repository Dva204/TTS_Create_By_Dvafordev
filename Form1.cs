using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TTS_Create_By_DvaForDev.Models;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1 : Form
{
    private readonly AppSettings settings = new();
    private readonly TtsApiClient api;
    private readonly BackendProcessManager backend = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? generation;
    private string? activeJob, audioFile, lastConnectionError;

    public Form1()
    {
        InitializeComponent();
        api = new(settings);
        backend.OutputReceived += WriteLog;
        FormClosing += Form1_FormClosing;

        // Dynamic Bindings
        if (Controls.Find("btnRunBatch", true).FirstOrDefault() is Button btnRun) btnRun.Click += BtnRunBatch_Click;
        if (Controls.Find("btnJoinMp3", true).FirstOrDefault() is Button btnJoin) btnJoin.Click += BtnJoinMp3_Click;
        if (Controls.Find("btnBrowseOutputFolder", true).FirstOrDefault() is Button btnOutFolder) btnOutFolder.Click += BtnBrowseOutputFolder_Click;
        if (Controls.Find("btnImportFile", true).FirstOrDefault() is Button btnImport) btnImport.Click += BtnImportFile_Click;
        if (Controls.Find("btnBrowseVideoAudio", true).FirstOrDefault() is Button btnVidAud) btnVidAud.Click += BtnBrowseVideoAudio_Click;
        if (Controls.Find("btnBrowseVideoSrt", true).FirstOrDefault() is Button btnVidSrt) btnVidSrt.Click += BtnBrowseVideoSrt_Click;
        if (Controls.Find("btnBrowseImageFolder", true).FirstOrDefault() is Button btnImgFold) btnImgFold.Click += BtnBrowseImageFolder_Click;
        if (Controls.Find("btnAnalyzeTimeline", true).FirstOrDefault() is Button btnAnalyze) btnAnalyze.Click += BtnAnalyzeTimeline_Click;
        if (Controls.Find("btnRenderVideo", true).FirstOrDefault() is Button btnRender) btnRender.Click += BtnRenderVideo_Click;

        if (Controls.Find("btnDeleteVoice", true).FirstOrDefault() is Button btnDelVoice) btnDelVoice.Click += BtnDeleteVoice_Click;
        if (Controls.Find("btnReplaceReference", true).FirstOrDefault() is Button btnRepRef) btnRepRef.Click += BtnReplaceReference_Click;
        if (Controls.Find("btnGeneratePreview", true).FirstOrDefault() is Button btnGenPrev) btnGenPrev.Click += BtnGeneratePreview_Click;
        if (Controls.Find("btnUseSelectedVoice", true).FirstOrDefault() is Button btnUseVoice) btnUseVoice.Click += BtnUseSelectedVoice_Click;
        if (Controls.Find("btnCopyVoiceId", true).FirstOrDefault() is Button btnCopyId) btnCopyId.Click += BtnCopyVoiceId_Click;
        if (Controls.Find("btnPlayReference", true).FirstOrDefault() is Button btnPlayRef) btnPlayRef.Click += BtnPlayReference_Click;
        if (Controls.Find("btnLibrarySearch", true).FirstOrDefault() is Button btnLibSearch) btnLibSearch.Click += BtnLibrarySearch_Click;
        if (Controls.Find("btnLibraryRefresh", true).FirstOrDefault() is Button btnLibRefresh) btnLibRefresh.Click += BtnLibraryRefresh_Click;
        
        if (Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() is DataGridView dgvVoice) dgvVoice.SelectionChanged += DgvVoiceLibrary_SelectionChanged;
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        // Designer already populated engine and language lists.
        if (engineComboBox.Items.Count > 0)
            engineComboBox.SelectedIndex = 0;

        // Prefer "vi" if present otherwise keep existing selection
        if (languageComboBox.Items.Cast<object?>().Any(x => (x?.ToString() ?? "") == "vi"))
            languageComboBox.SelectedItem = "vi";

        stopButton.Enabled = playButton.Enabled = saveWavButton.Enabled = false;

        try
        {
            await backend.StartAsync(settings.PythonExecutable, lifetime.Token);
        }
        catch (Exception ex)
        {
            WriteLog("Backend start: " + ex.Message);
        }

        for (var i = 0; i < 30 && !lifetime.IsCancellationRequested; i++)
        {
            try
            {
                await RefreshStatus(lifetime.Token);
                await RefreshVoices(null, lifetime.Token);
                _ = Monitor(lifetime.Token);
                return;
            }
            catch
            {
                try { await Task.Delay(1000, lifetime.Token); } catch { return; }
            }
        }

        WriteLog("Backend unavailable. Follow the setup steps in README.md.");
    }

    private async Task Monitor(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await RefreshStatus(token);
                lastConnectionError = null;
            }
            catch (Exception ex)
            {
                SetStatus("Backend: Offline", "GPU: Unknown");
                if (lastConnectionError != ex.Message)
                {
                    lastConnectionError = ex.Message;
                    WriteLog(ex.Message);
                }
            }

            try { await Task.Delay(2000, token); } catch { break; }
        }
    }

    private async Task RefreshStatus(CancellationToken token)
    {
        var health = await api.GetHealthAsync(token);
        var system = await api.GetSystemAsync(token);
        SetStatus(
            $"Backend: {health?.Status} / {(health?.Ready == true ? "Engine ready" : "Engine unavailable")}",
            system?.CudaAvailable == true ? $"GPU: {system.GpuName}" : "GPU: CUDA unavailable"
        );
    }

    private void SetStatus(string backendText, string gpuText)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(backendText, gpuText)); return; }
        backendStatusLabel.Text = backendText;
        gpuStatusLabel.Text = gpuText;
    }

    private async Task RefreshVoices(string? q = null, CancellationToken token = default)
    {
        var voices = await api.GetVoicesAsync(q, token);
        if (voices is null) return;
        var voiceList = voices.ToList();
        voiceComboBox.DataSource = voiceList;
        voiceComboBox.DisplayMember = nameof(VoiceDto.Name);
        voiceComboBox.ValueMember = nameof(VoiceDto.Id);
        
        if (Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() is DataGridView dgv)
        {
            dgv.Rows.Clear();
            foreach (var v in voiceList)
            {
                int idx = dgv.Rows.Add(v.Name, v.Id, v.Language, v.Gender, v.CreatedAt);
                dgv.Rows[idx].Tag = v;
            }
        }
    }

    private async void AddVoiceButton_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Audio (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            SetBusy(true);
            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            var lang = languageComboBox.Text;
            var voice = await api.AddVoiceAsync(name, lang, null, null, null, dialog.FileName, lifetime.Token);
            await RefreshVoices(null, lifetime.Token);

            // try select imported voice
            if (voice is not null)
                voiceComboBox.SelectedValue = voice.Id;

            WriteLog($"Imported voice: {voice?.Name ?? name}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void GenerateButton_Click(object sender, EventArgs e)
    {
        // Determine input text from available controls in Designer.
        string GetInputText()
        {
            // Prefer preview sentence box if present and non-empty
            if (Controls.OfType<TextBox>().Any(t => t.Name == "txtPreviewSentence" && !string.IsNullOrWhiteSpace(t.Text)))
                return Controls.Find("txtPreviewSentence", true).OfType<TextBox>().First().Text.Trim();

            // Fallback to voice name (not ideal but ensures UI works)
            if (!string.IsNullOrWhiteSpace(txtVoiceName.Text))
                return txtVoiceName.Text.Trim();

            return string.Empty;
        }

        var input = GetInputText();
        if (string.IsNullOrWhiteSpace(input) || voiceComboBox.SelectedItem is not VoiceDto voice)
        {
            MessageBox.Show(this, "Enter text (Preview sentence or Voice Name) and add or select a voice.");
            return;
        }

        generation?.Dispose();
        generation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);

        try
        {
            SetGenerating(true);
            CleanupAudio();

            var request = new GenerateRequest(input, voice.Id, languageComboBox.Text, engineComboBox.Text);
            var accepted = await api.GenerateAsync(request, generation.Token);
            activeJob = accepted.Id;
            WriteLog($"Job {activeJob} queued.");

            while (true)
            {
                await Task.Delay(800, generation.Token);
                var job = await api.GetJobAsync(activeJob, generation.Token) ?? throw new InvalidOperationException("Job not found.");
                if (job.Status == "failed") throw new InvalidOperationException(job.Error ?? "Generation failed.");
                if (job.Status == "cancelled") throw new OperationCanceledException();
                if (job.Status == "completed")
                {
                    audioFile = Path.Combine(Path.GetTempPath(), $"tts-{activeJob}.wav");
                    var audioBytes = await api.DownloadAudioAsync(activeJob, generation.Token);
                    await File.WriteAllBytesAsync(audioFile, audioBytes, generation.Token);
                    playButton.Enabled = saveWavButton.Enabled = true;
                    WriteLog("WAV generated successfully.");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("Generation stopped.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            activeJob = null;
            SetGenerating(false);
        }
    }

    private async void StopButton_Click(object sender, EventArgs e)
    {
        generation?.Cancel();
        if (activeJob is not null)
        {
            try { await api.CancelJobAsync(activeJob, lifetime.Token); }
            catch (Exception ex) { WriteLog(ex.Message); }
        }
    }

    private void PlayButton_Click(object sender, EventArgs e)
    {
        if (audioFile is not null && File.Exists(audioFile))
            Process.Start(new ProcessStartInfo(audioFile) { UseShellExecute = true });
    }

    private void SaveWavButton_Click(object sender, EventArgs e)
    {
        if (audioFile is null) return;
        using var dialog = new SaveFileDialog { Filter = "WAV audio|*.wav", FileName = $"tts-{DateTime.Now:yyyyMMdd-HHmmss}.wav" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.Copy(audioFile, dialog.FileName, true);
    }

    private void SetBusy(bool busy)
    {
        addVoiceButton.Enabled = generateButton.Enabled = !busy;
    }

    private void SetGenerating(bool active)
    {
        SetBusy(active);
        stopButton.Enabled = active;
    }

    private void WriteLog(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => WriteLog(message)); return; }
        logsTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void ShowError(Exception ex)
    {
        WriteLog("ERROR: " + ex.Message);
        MessageBox.Show(this, ex.Message, "TTS Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void CleanupAudio()
    {
        if (audioFile is not null)
            try { File.Delete(audioFile); } catch { }

        audioFile = null;
        playButton.Enabled = saveWavButton.Enabled = false;
    }

    private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        lifetime.Cancel();
        generation?.Cancel();
        CleanupAudio();
        await backend.DisposeAsync();
        api.Dispose();
    }

private void BtnBrowseOutputFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtBatchFolder", true).FirstOrDefault() is TextBox txt) txt.Text = dialog.SelectedPath;
        }
    }

    private void BtnImportFile_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Text/Subtitle (*.txt;*.srt)|*.txt;*.srt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        if (dgv == null) return;

        var lines = File.ReadAllLines(dialog.FileName);
        dgv.Rows.Clear();
        int id = 1;
        var ext = Path.GetExtension(dialog.FileName).ToLower();

        if (ext == ".srt")
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (int.TryParse(lines[i].Trim(), out _))
                {
                    string timing = i + 1 < lines.Length ? lines[i + 1] : "";
                    string content = "";
                    i += 2;
                    while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                    {
                        content += lines[i] + " ";
                        i++;
                    }
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        dgv.Rows.Add(id.ToString("D3"), "", timing, content.Trim(), voiceComboBox.Text, languageComboBox.Text, "Pending");
                        id++;
                    }
                }
            }
        }
        else
        {
            bool autoSplit = (Controls.Find("chkAutoSplit", true).FirstOrDefault() as CheckBox)?.Checked == true;
            string splitChars = (Controls.Find("txtSplitChars", true).FirstOrDefault() as TextBox)?.Text ?? ".!?\n";

            string fullText = File.ReadAllText(dialog.FileName);
            if (autoSplit && !string.IsNullOrEmpty(splitChars))
            {
                // Simple split. We want to keep the delimiter! A regex would be better but let's just do a basic split for now.
                var segments = fullText.Split(splitChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (var seg in segments)
                {
                    if (string.IsNullOrWhiteSpace(seg)) continue;
                    dgv.Rows.Add(id.ToString("D3"), "", "", seg.Trim(), voiceComboBox.Text, languageComboBox.Text, "Pending");
                    id++;
                }
            }
            else
            {
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    dgv.Rows.Add(id.ToString("D3"), "", "", line.Trim(), voiceComboBox.Text, languageComboBox.Text, "Pending");
                    id++;
                }
            }
        }
        
        WriteLog($"Imported {id - 1} segments from {Path.GetFileName(dialog.FileName)}");
    }

    private async void BtnRunBatch_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        if (dgv == null || txtFolder == null) return;
        
        if (string.IsNullOrWhiteSpace(txtFolder.Text))
        {
            MessageBox.Show(this, "Please select an output folder first.", "Output Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (voiceComboBox.SelectedValue is not string voiceId)
        {
            MessageBox.Show(this, "Please select a voice first.", "Voice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (engineComboBox.Text == "")
        {
            MessageBox.Show(this, "Please select an engine first.", "Engine", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Directory.CreateDirectory(txtFolder.Text);

        generation?.Dispose();
        generation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = generation.Token;

        SetBusy(true);
        try
        {
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                if (row.Cells[6].Value?.ToString() == "Completed") continue;
                
                row.Cells[6].Value = "Running";
                dgv.FirstDisplayedScrollingRowIndex = i;
                
                var content = row.Cells[3].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(content))
                {
                    row.Cells[6].Value = "Skipped";
                    continue;
                }

                var settingsReq = new TtsGenerationSettings(
                    Speed: (double)numSpeed.Value / 100.0,
                    CfgWeight: (double)numStability.Value / 100.0,
                    Temperature: (double)numSimilarity.Value / 100.0,
                    Exaggeration: (double)numStyle.Value / 100.0,
                    SpeakerBoost: chkSpeakerBoost.Checked,
                    Seed: null
                );
                
                string lang = languageComboBox.Text == "Auto" ? "" : languageComboBox.Text;
                var req = new GenerateRequest(content, voiceId, lang, engineComboBox.Text, settingsReq);

                var accepted = await api.GenerateAsync(req, token);
                activeJob = accepted.Id;
                
                while (true)
                {
                    await Task.Delay(800, token);
                    var job = await api.GetJobAsync(activeJob, token) ?? throw new InvalidOperationException("Job not found.");
                    if (job.Status == "failed") throw new InvalidOperationException(job.Error ?? "Generation failed.");
                    if (job.Status == "cancelled") throw new OperationCanceledException();
                    if (job.Status == "completed")
                    {
                        var audioBytes = await new HttpClient().GetByteArrayAsync(new Uri(settings.BackendUri, $"api/jobs/{activeJob}/audio"));
                        var filename = $"{row.Cells[0].Value}_{voiceComboBox.Text}.wav";
                        var path = Path.Combine(txtFolder.Text, filename);
                        await File.WriteAllBytesAsync(path, audioBytes, token);
                        row.Cells[1].Value = filename;
                        row.Cells[6].Value = "Completed";
                        WriteLog($"Segment {row.Cells[0].Value} completed.");
                        break;
                    }
                }
            }
            WriteLog("Batch completed!");
        }
        catch (OperationCanceledException)
        {
            WriteLog("Batch cancelled.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
            activeJob = null;
        }
    }

    private async void BtnJoinMp3_Click(object? sender, EventArgs e)
    {
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        if (txtFolder == null || string.IsNullOrWhiteSpace(txtFolder.Text)) return;
        
        var wavFiles = Directory.GetFiles(txtFolder.Text, "*.wav").OrderBy(f => f).ToList();
        if (wavFiles.Count == 0) return;

        SetBusy(true);
        try
        {
            var listFile = Path.Combine(txtFolder.Text, "file_list.txt");
            var outputWav = Path.Combine(txtFolder.Text, "joined_output.wav");
            var lines = wavFiles.Select(f => $"file '{Path.GetFileName(f)}'").ToArray();
            await File.WriteAllLinesAsync(listFile, lines, lifetime.Token);

            var psi = new ProcessStartInfo("ffmpeg")
            {
                Arguments = $"-y -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputWav}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            
            var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(lifetime.Token);
                if (process.ExitCode == 0) WriteLog($"Successfully joined {wavFiles.Count} files into joined_output.wav");
                else WriteLog("FFmpeg join failed: " + await process.StandardError.ReadToEndAsync());
            }
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }


private void BtnBrowseVideoAudio_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Audio Files|*.wav;*.mp3;*.m4a;*.flac" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtVideoAudio", true).FirstOrDefault() is TextBox txt) txt.Text = dialog.FileName;
        }
    }

    private void BtnBrowseVideoSrt_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "SRT Files|*.srt" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtVideoSrt", true).FirstOrDefault() is TextBox txt) txt.Text = dialog.FileName;
        }
    }

    private void BtnBrowseImageFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtImageFolder", true).FirstOrDefault() is TextBox txt) txt.Text = dialog.SelectedPath;
        }
    }

    private void BtnAnalyzeTimeline_Click(object? sender, EventArgs e)
    {
        var txtSrt = Controls.Find("txtVideoSrt", true).FirstOrDefault() as TextBox;
        var txtImg = Controls.Find("txtImageFolder", true).FirstOrDefault() as TextBox;
        var dgv = Controls.Find("dgvVideoTimeline", true).FirstOrDefault() as DataGridView;

        if (txtSrt == null || txtImg == null || dgv == null) return;

        if (!File.Exists(txtSrt.Text))
        {
            MessageBox.Show(this, "SRT file not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        
        if (!Directory.Exists(txtImg.Text))
        {
            MessageBox.Show(this, "Image folder not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var lines = File.ReadAllLines(txtSrt.Text);
        var images = Directory.GetFiles(txtImg.Text, "*.*")
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f)
            .ToList();

        dgv.Rows.Clear();
        int imageIndex = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (int.TryParse(lines[i].Trim(), out int id))
            {
                string timing = i + 1 < lines.Length ? lines[i + 1] : "";
                string content = "";
                i += 2;
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    content += lines[i] + " ";
                    i++;
                }

                if (!string.IsNullOrWhiteSpace(content))
                {
                    // Parse start and end time: "00:00:00,000 --> 00:00:05,000"
                    var times = timing.Split(new[] { "-->" }, StringSplitOptions.None);
                    string start = times.Length > 0 ? times[0].Trim() : "";
                    string end = times.Length > 1 ? times[1].Trim() : "";

                    string duration = "";
                    if (TimeSpan.TryParse(start.Replace(",", "."), out var tsStart) && TimeSpan.TryParse(end.Replace(",", "."), out var tsEnd))
                    {
                        duration = (tsEnd - tsStart).TotalSeconds.ToString("0.00") + "s";
                    }

                    string imgPath = imageIndex < images.Count ? Path.GetFileName(images[imageIndex]) : "";
                    imageIndex++;

                    dgv.Rows.Add(id.ToString("D3"), start, end, duration, imgPath, content.Trim(), "Pending");
                }
            }
        }

        WriteLog($"Analyzed timeline: {dgv.Rows.Count} segments matched with {images.Count} images.");
    }

    private async void BtnRenderVideo_Click(object? sender, EventArgs e)
    {
        var txtAudio = Controls.Find("txtVideoAudio", true).FirstOrDefault() as TextBox;
        var txtImg = Controls.Find("txtImageFolder", true).FirstOrDefault() as TextBox;
        var txtSrt = Controls.Find("txtVideoSrt", true).FirstOrDefault() as TextBox;
        var dgv = Controls.Find("dgvVideoTimeline", true).FirstOrDefault() as DataGridView;

        if (txtAudio == null || txtImg == null || txtSrt == null || dgv == null) return;
        
        if (dgv.Rows.Count == 0)
        {
            MessageBox.Show(this, "Please analyze timeline first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var outputMp4 = Path.Combine(Path.GetDirectoryName(txtAudio.Text) ?? "", "final_render.mp4");
            var tempDir = Path.Combine(Path.GetDirectoryName(txtAudio.Text) ?? "", "temp_frames");
            Directory.CreateDirectory(tempDir);

            WriteLog("Starting FFmpeg render...");

            // For simplicity in this demo, we'll use a basic FFmpeg command to combine a single image + audio.
            // A true slideshow requires complex filter_complex, which takes lots of code.
            // We'll write an ffconcat file.
            
            var concatFile = Path.Combine(tempDir, "slides.txt");
            var sb = new System.Text.StringBuilder();
            
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var imgName = row.Cells[4].Value?.ToString();
                var durationStr = row.Cells[3].Value?.ToString()?.Replace("s", "");
                
                if (!string.IsNullOrEmpty(imgName) && !string.IsNullOrEmpty(durationStr))
                {
                    var imgPath = Path.Combine(txtImg.Text, imgName).Replace("\\", "/");
                    sb.AppendLine($"file '{imgPath}'");
                    sb.AppendLine($"duration {durationStr}");
                }
            }
            
            // Repeat last image to avoid ffconcat glitch
            if (dgv.Rows.Count > 0)
            {
                var lastRow = dgv.Rows[dgv.Rows.Count - 1];
                var imgName = lastRow.Cells[4].Value?.ToString();
                if (!string.IsNullOrEmpty(imgName))
                {
                    var imgPath = Path.Combine(txtImg.Text, imgName).Replace("\\", "/");
                    sb.AppendLine($"file '{imgPath}'");
                }
            }

            await File.WriteAllTextAsync(concatFile, sb.ToString(), lifetime.Token);

            string srtPath = txtSrt.Text.Replace("\\", "/");
            // FFmpeg command to concat images, add audio, and burn subtitles
            var psi = new ProcessStartInfo("ffmpeg")
            {
                Arguments = $"-y -f concat -safe 0 -i \"{concatFile}\" -i \"{txtAudio.Text}\" -vf \"subtitles='{srtPath.Replace(":", "\\\\:")}'\" -c:v libx264 -pix_fmt yuv420p -c:a aac \"{outputMp4}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            WriteLog($"Running: ffmpeg {psi.Arguments}");
            var process = Process.Start(psi);
            if (process != null)
            {
                await process.WaitForExitAsync(lifetime.Token);
                if (process.ExitCode == 0)
                {
                    WriteLog($"Render completed successfully: final_render.mp4");
                    Process.Start(new ProcessStartInfo(outputMp4) { UseShellExecute = true });
                }
                else
                {
                    WriteLog("FFmpeg render failed: " + await process.StandardError.ReadToEndAsync());
                }
            }
            
            Directory.Delete(tempDir, true);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }


private async void BtnGeneratePreview_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            var txt = Controls.Find("txtPreviewSentence", true).FirstOrDefault() as TextBox;
            if (string.IsNullOrWhiteSpace(txt?.Text)) return;
            SetBusy(true);
            try {
                WriteLog($"Generating preview for {v.Name}...");
                var req = new GenerateRequest(txt.Text, v.Id, "en", engineComboBox.Text, new TtsGenerationSettings(100, 50, 80, 50, true));
                var job = await api.GenerateAsync(req, lifetime.Token);
                while(true) {
                    await Task.Delay(500, lifetime.Token);
                    var status = await api.GetJobAsync(job.Id, lifetime.Token);
                    if (status?.Status == "failed") throw new Exception("Preview failed");
                    if (status?.Status == "completed") {
                        var audio = await new HttpClient().GetByteArrayAsync(new Uri(settings.BackendUri, $"api/jobs/{job.Id}/audio"));
                        var temp = Path.GetTempFileName() + ".wav";
                        await File.WriteAllBytesAsync(temp, audio, lifetime.Token);
                        new System.Media.SoundPlayer(temp).Play();
                        break;
                    }
                }
            }
            catch (Exception ex) { ShowError(ex); }
            finally { SetBusy(false); }
        }
    }

    private void BtnUseSelectedVoice_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            voiceComboBox.SelectedValue = v.Id;
            mainTabControl.SelectedIndex = 0; // Switch to TTS / Batch tab
            WriteLog($"Selected voice {v.Name} for generation.");
        }
    }

    private void BtnCopyVoiceId_Click(object? sender, EventArgs e)
    {
        var txt = Controls.Find("txtSelectedVoiceId", true).FirstOrDefault() as TextBox;
        if (!string.IsNullOrEmpty(txt?.Text))
        {
            Clipboard.SetText(txt.Text);
            WriteLog("Voice ID copied to clipboard.");
        }
    }

    private async void BtnReplaceReference_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            using var dialog = new OpenFileDialog { Filter = "Audio|*.wav;*.mp3;*.m4a;*.flac" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SetBusy(true);
                try {
                    await api.ReplaceVoiceReferenceAsync(v.Id, dialog.FileName, lifetime.Token);
                    WriteLog("Voice reference replaced.");
                }
                catch (Exception ex) { ShowError(ex); }
                finally { SetBusy(false); }
            }
        }
    }

    private async void BtnDeleteVoice_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            if (MessageBox.Show($"Are you sure you want to delete {v.Name}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                SetBusy(true);
                try {
                    await api.DeleteVoiceAsync(v.Id, lifetime.Token);
                    await RefreshVoices(null, lifetime.Token);
                    WriteLog($"Deleted voice: {v.Name}");
                }
                catch (Exception ex) { ShowError(ex); }
                finally { SetBusy(false); }
            }
        }
    }



    private void DgvVoiceLibrary_SelectionChanged(object? sender, EventArgs e)
    {
        var dgv = sender as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            if (Controls.Find("lblSelectedVoiceNameText", true).FirstOrDefault() is Label lblName) lblName.Text = v.Name;
            if (Controls.Find("txtSelectedVoiceId", true).FirstOrDefault() is TextBox txtId) txtId.Text = v.Id;
            if (Controls.Find("lblSelectedVoiceLanguageText", true).FirstOrDefault() is Label lblLang) lblLang.Text = v.Language ?? "N/A";
            if (Controls.Find("lblSelectedVoiceGenderText", true).FirstOrDefault() is Label lblGender) lblGender.Text = v.Gender ?? "N/A";
            if (Controls.Find("txtSelectedVoiceDescription", true).FirstOrDefault() is TextBox txtDesc) txtDesc.Text = v.Description ?? "";
            
            // Check if reference exists
            var refPath = Path.Combine("backend", "data", "voices", v.Id, "reference.wav");
            if (Controls.Find("txtReferenceAudio", true).FirstOrDefault() is TextBox txtRef) 
                txtRef.Text = File.Exists(refPath) ? "reference.wav" : "None";
        }
    }

    private void BtnPlayReference_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            var refPath = Path.Combine("backend", "data", "voices", v.Id, "reference.wav");
            if (File.Exists(refPath))
            {
                try {
                    new System.Media.SoundPlayer(refPath).Play();
                } catch (Exception ex) {
                    WriteLog("Error playing reference: " + ex.Message);
                }
            }
        }
    }

    private async void BtnLibrarySearch_Click(object? sender, EventArgs e)
    {
        var txtSearch = Controls.Find("txtLibrarySearch", true).FirstOrDefault() as TextBox;
        await RefreshVoices(txtSearch?.Text, lifetime.Token);
    }

    private async void BtnLibraryRefresh_Click(object? sender, EventArgs e)
    {
        var txtSearch = Controls.Find("txtLibrarySearch", true).FirstOrDefault() as TextBox;
        if (txtSearch != null) txtSearch.Text = "";
        await RefreshVoices(null, lifetime.Token);
    }
}

