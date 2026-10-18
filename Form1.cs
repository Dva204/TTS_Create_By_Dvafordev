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
        InitializeWhiteboardTab();
        InitializeAiVideoStudioTab();
        InitializeModernUi();
        api = new(settings);
        backend.OutputReceived += WriteLog;
        FormClosing += Form1_FormClosing;

        // Dynamic Bindings
        if (Controls.Find("btnRunBatch", true).FirstOrDefault() is Button btnRun) btnRun.Click += BtnRunBatch_Click;
        if (Controls.Find("btnJoinMp3", true).FirstOrDefault() is Button btnJoin) btnJoin.Click += BtnJoinMp3_Click;
        if (Controls.Find("btnBrowseOutputFolder", true).FirstOrDefault() is Button btnOutFolder) btnOutFolder.Click += BtnBrowseOutputFolder_Click;
        if (Controls.Find("btnBrowseFolder", true).FirstOrDefault() is Button btnBrowseFold) btnBrowseFold.Click += BtnBrowseOutputFolder_Click;
        if (Controls.Find("btnImportFile", true).FirstOrDefault() is Button btnImport) btnImport.Click += BtnImportFile_Click;
        if (Controls.Find("btnImportFolder", true).FirstOrDefault() is Button btnImpFolder) btnImpFolder.Click += BtnImportFolder_Click;
        if (Controls.Find("btnClearList", true).FirstOrDefault() is Button btnClr) btnClr.Click += (s, e) => {
            if (Controls.Find("dgvJobs", true).FirstOrDefault() is DataGridView dgv) dgv.Rows.Clear();
        };
        if (Controls.Find("btnImportVoice", true).FirstOrDefault() is Button btnImpVoice) btnImpVoice.Click += AddVoiceButton_Click;
        if (Controls.Find("btnOpenAudioOutput", true).FirstOrDefault() is Button btnOpenOut) btnOpenOut.Click += BtnOpenAudioOutput_Click;
        if (Controls.Find("btnCreateSrt", true).FirstOrDefault() is Button btnCreateSrt) btnCreateSrt.Click += BtnCreateSrt_Click;
        if (Controls.Find("btnSaveSettings", true).FirstOrDefault() is Button btnSave) btnSave.Click += (s, e) => SaveSettings();
        if (Controls.Find("btnBrowseVideoAudio", true).FirstOrDefault() is Button btnVidAud) btnVidAud.Click += BtnBrowseVideoAudio_Click;
        if (Controls.Find("btnBrowseVideoSrt", true).FirstOrDefault() is Button btnVidSrt) btnVidSrt.Click += BtnBrowseVideoSrt_Click;
        if (Controls.Find("btnBrowseImageFolder", true).FirstOrDefault() is Button btnImgFold) btnImgFold.Click += BtnBrowseImageFolder_Click;
        if (Controls.Find("btnAnalyzeTimeline", true).FirstOrDefault() is Button btnAnalyze) btnAnalyze.Click += BtnAnalyzeTimeline_Click;
        if (Controls.Find("btnRenderVideo", true).FirstOrDefault() is Button btnRender) btnRender.Click += BtnRenderVideo_Click;

        if (Controls.Find("btnDeleteVoice", true).FirstOrDefault() is Button btnDelVoice) btnDelVoice.Click += BtnDeleteVoice_Click;
        if (Controls.Find("btnReplaceReference", true).FirstOrDefault() is Button btnRepRef) btnRepRef.Click += BtnReplaceReference_Click;
        if (Controls.Find("btnGeneratePreview", true).FirstOrDefault() is Button btnGenPrev) btnGenPrev.Click += BtnGeneratePreview_Click;
        if (Controls.Find("btnUseSelectedVoice", true).FirstOrDefault() is Button btnUseVoice) btnUseVoice.Click += BtnUseSelectedVoice_Click;
        if (Controls.Find("btnResetVoiceSettings", true).FirstOrDefault() is Button btnReset) btnReset.Click += (s, e) => {
            if (Controls.Find("numSpeed", true).FirstOrDefault() is NumericUpDown numSp) numSp.Value = 1.00m;
            if (Controls.Find("numStability", true).FirstOrDefault() is NumericUpDown numSt) numSt.Value = 65m;
            if (Controls.Find("numSimilarity", true).FirstOrDefault() is NumericUpDown numSim) numSim.Value = 60m;
            if (Controls.Find("numStyle", true).FirstOrDefault() is NumericUpDown numSty) numSty.Value = 50m;
            if (Controls.Find("chkSpeakerBoost", true).FirstOrDefault() is CheckBox chkBoost) chkBoost.Checked = true;
            if (Controls.Find("numDenoise", true).FirstOrDefault() is NumericUpDown numDen) numDen.Value = 0m;
        };
        if (Controls.Find("btnAdvancedSettings", true).FirstOrDefault() is Button btnAdvSet) btnAdvSet.Click += (s, e) => MessageBox.Show(this, "Advanced Settings will be available in the next update!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        if (Controls.Find("btnCopyVoiceId", true).FirstOrDefault() is Button btnCopyId) btnCopyId.Click += BtnCopyVoiceId_Click;
        if (Controls.Find("btnPlayReference", true).FirstOrDefault() is Button btnPlayRef) btnPlayRef.Click += BtnPlayReference_Click;
        if (Controls.Find("btnLibrarySearch", true).FirstOrDefault() is Button btnLibSearch) btnLibSearch.Click += BtnLibrarySearch_Click;
        if (Controls.Find("btnLibraryRefresh", true).FirstOrDefault() is Button btnLibRefresh) btnLibRefresh.Click += BtnLibraryRefresh_Click;
        
        if (Controls.Find("btnBrowseVoiceFile", true).FirstOrDefault() is Button btnBrowseVoice) btnBrowseVoice.Click += BtnBrowseVoiceFile_Click;
        if (Controls.Find("btnPreviewNewVoice", true).FirstOrDefault() is Button btnPreviewNew) btnPreviewNew.Click += BtnPreviewNewVoice_Click;
        if (Controls.Find("btnSaveNewVoice", true).FirstOrDefault() is Button btnSaveNew) btnSaveNew.Click += BtnSaveNewVoice_Click;
        if (Controls.Find("btnClearNewVoice", true).FirstOrDefault() is Button btnClearNew) btnClearNew.Click += BtnClearNewVoice_Click;
        if (Controls.Find("btnLibraryAddVoice", true).FirstOrDefault() is Button btnLibAddVoice) btnLibAddVoice.Click += BtnLibraryAddVoice_Click;
        if (Controls.Find("btnOpenVoiceLibrary", true).FirstOrDefault() is Button btnOpenLib) btnOpenLib.Click += (s, e) => mainTabControl.SelectedTab = tabVoiceLibrary;
        if (Controls.Find("btnSearchVoice", true).FirstOrDefault() is Button btnSearchVoice) btnSearchVoice.Click += BtnSearchVoice_Click;
        if (Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() is ComboBox cmbLibLang) cmbLibLang.SelectedIndexChanged += async (s, e) => {
            var txtSearch = Controls.Find("txtLibrarySearch", true).FirstOrDefault() as TextBox;
            await RefreshVoices(txtSearch?.Text, lifetime.Token);
        };
        
        if (Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() is DataGridView dgvVoice) dgvVoice.SelectionChanged += DgvVoiceLibrary_SelectionChanged;

        if (Controls.Find("dgvJobs", true).FirstOrDefault() is DataGridView dgvJobs)
        {
            dgvJobs.RowsAdded += (s, e) => UpdateToolbarState();
            dgvJobs.RowsRemoved += (s, e) => UpdateToolbarState();
        }
        if (Controls.Find("txtBatchFolder", true).FirstOrDefault() is TextBox txtBatchFolder)
        {
            txtBatchFolder.TextChanged += (s, e) => UpdateToolbarState();
        }

        // Set Default Video Settings
        if (Controls.Find("cmbResolution", true).FirstOrDefault() is ComboBox cmbRes && cmbRes.Items.Count > 0) cmbRes.SelectedIndex = 0; // 1920x1080
        if (Controls.Find("cmbFps", true).FirstOrDefault() is ComboBox cmbF && cmbF.Items.Count > 1) cmbF.SelectedIndex = 1; // 30
        if (Controls.Find("cmbAspectRatio", true).FirstOrDefault() is ComboBox cmbAspect && cmbAspect.Items.Count > 0) cmbAspect.SelectedIndex = 0; // 16:9
        if (Controls.Find("cmbEncoder", true).FirstOrDefault() is ComboBox cmbEnc && cmbEnc.Items.Count > 0) cmbEnc.SelectedIndex = 0; // Auto
        if (Controls.Find("cmbVideoQuality", true).FirstOrDefault() is ComboBox cmbQual && cmbQual.Items.Count > 2) cmbQual.SelectedIndex = 2; // Maximum Quality
        if (Controls.Find("cmbTimelineMode", true).FirstOrDefault() is ComboBox cmbTime && cmbTime.Items.Count > 0) cmbTime.SelectedIndex = 0; // From SRT
        if (Controls.Find("cmbImageMapping", true).FirstOrDefault() is ComboBox cmbImgMap && cmbImgMap.Items.Count > 0) cmbImgMap.SelectedIndex = 0;
    }







































    // ==========================================
    // Form Lifecycle & Common
    // ==========================================
    #region Form Lifecycle & Common

    private UserSettings userSettings = new();
    private readonly string userSettingsPath = "user_settings.json";

    private void LoadSettings()
    {
        if (File.Exists(userSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(userSettingsPath);
                userSettings = System.Text.Json.JsonSerializer.Deserialize<UserSettings>(json) ?? new();
            }
            catch { }
        }

        if (Controls.Find("txtBatchFolder", true).FirstOrDefault() is TextBox txtBatch) txtBatch.Text = userSettings.OutputFolder;
        if (Controls.Find("txtOutputFolder", true).FirstOrDefault() is TextBox txtFolder) txtFolder.Text = userSettings.OutputFolder;
        if (Controls.Find("numMaxChars", true).FirstOrDefault() is NumericUpDown numChars) numChars.Value = userSettings.MaxChars;
        if (Controls.Find("numSilence", true).FirstOrDefault() is NumericUpDown numSilence) numSilence.Value = userSettings.Silence;
        if (Controls.Find("cmbOutputFormat", true).FirstOrDefault() is ComboBox cmbFormat) cmbFormat.Text = userSettings.OutputFormat;
        if (Controls.Find("cmbMp3Bitrate", true).FirstOrDefault() is ComboBox cmbBitrate) cmbBitrate.Text = userSettings.Mp3Bitrate;
    }

    private void SaveSettings()
    {
        if (Controls.Find("txtOutputFolder", true).FirstOrDefault() is TextBox txtFolder) userSettings.OutputFolder = txtFolder.Text;
        if (Controls.Find("numMaxChars", true).FirstOrDefault() is NumericUpDown numChars) userSettings.MaxChars = (int)numChars.Value;
        if (Controls.Find("numSilence", true).FirstOrDefault() is NumericUpDown numSilence) userSettings.Silence = (int)numSilence.Value;
        if (Controls.Find("cmbOutputFormat", true).FirstOrDefault() is ComboBox cmbFormat) userSettings.OutputFormat = cmbFormat.Text;
        if (Controls.Find("cmbMp3Bitrate", true).FirstOrDefault() is ComboBox cmbBitrate) userSettings.Mp3Bitrate = cmbBitrate.Text;

        if (Controls.Find("txtBatchFolder", true).FirstOrDefault() is TextBox txtBatch) txtBatch.Text = userSettings.OutputFolder;

        try
        {
            File.WriteAllText(userSettingsPath, System.Text.Json.JsonSerializer.Serialize(userSettings));
            MessageBox.Show(this, "Settings saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private async void Form1_Load(object? sender, EventArgs e)
    {
        // Optional local VieNeu TTS engine. Existing engines remain unchanged.
        if (!engineComboBox.Items.Contains("vieneu_v3_turbo"))
            engineComboBox.Items.Add("vieneu_v3_turbo");

        // Designer already populated engine and language lists.
        if (engineComboBox.Items.Count > 0)
            engineComboBox.SelectedIndex = 0;

        string[] friendlyLangs = new[] { "Auto", "English (en)", "Tiếng Việt (vi)", "Spanish (es)", "French (fr)", "German (de)", "Italian (it)", "Portuguese (pt)", "Russian (ru)", "Japanese (ja)", "Korean (ko)", "Chinese (zh)" };
        languageComboBox.Items.Clear();
        languageComboBox.Items.AddRange(friendlyLangs);
        languageComboBox.SelectedItem = "Tiếng Việt (vi)";

        // Vietnamese voice cloning uses the local VieNeu engine by default.
        if (engineComboBox.Items.Contains("vieneu_v3_turbo"))
        {
            engineComboBox.SelectedItem = "vieneu_v3_turbo";
            numSimilarity.Value = 65;
        }

        languageComboBox.SelectedIndexChanged += (s, ev) =>
        {
            if (languageComboBox.SelectedItem?.ToString()?.Contains("(vi)") == true)
            {
                // Never override an explicitly selected VieNeu cloning engine.
                // Keep the explicitly selected engine; language must not override it.
            }
        };

        // VieNeu currently synthesizes Vietnamese only. Keep the language in sync
        // when users switch engines, without changing language for other engines.
        engineComboBox.SelectedIndexChanged += (s, ev) =>
        {
            if (engineComboBox.SelectedItem?.ToString() == "vieneu_v3_turbo")
            {
                languageComboBox.SelectedItem = "Tiếng Việt (vi)";
                numSimilarity.Value = 65; // Tested VieNeu C profile; adjustable in UI.
            }
        };


        if (Controls.Find("cmbNewVoiceLanguage", true).FirstOrDefault() is ComboBox cmbNewLang)
        {
            string[] newVoiceLangs = new[] { "English (en)", "Tiếng Việt (vi)", "Spanish (es)", "French (fr)", "German (de)", "Italian (it)", "Portuguese (pt)", "Russian (ru)", "Japanese (ja)", "Korean (ko)", "Chinese (zh)" };
            cmbNewLang.Items.Clear();
            cmbNewLang.Items.AddRange(newVoiceLangs);
        }
        
        if (Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() is ComboBox cmbLibLang)
        {
            var libLangs = new List<string> { "All Languages" };
            libLangs.AddRange(friendlyLangs.Where(x => x != "Auto"));
            cmbLibLang.Items.Clear();
            cmbLibLang.Items.AddRange(libLangs.ToArray());
            cmbLibLang.SelectedIndex = 0;
        }

        stopButton.Enabled = false;

        LoadSettings();
        
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

    private List<VoiceDto> allVoices = new();

    private async Task RefreshVoices(string? q = null, CancellationToken token = default)
    {
        // Fetch from API only if it's a full refresh (q == null) or if we don't have voices yet
        if (q == null || allVoices.Count == 0)
        {
            try 
            {
                var voices = await api.GetVoicesAsync(null, token);
                if (voices != null)
                {
                    allVoices = voices.ToList();
                    var currentVoiceId = voiceComboBox.SelectedValue?.ToString();
                    
                    voiceComboBox.DataSource = allVoices.ToList();
                    voiceComboBox.DisplayMember = nameof(VoiceDto.Name);
                    voiceComboBox.ValueMember = nameof(VoiceDto.Id);
                    
                    if (currentVoiceId != null) voiceComboBox.SelectedValue = currentVoiceId;
                }
            }
            catch (Exception)
            {
                // Ignore backend offline exception to prevent crash
            }
        }

        if (Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() is DataGridView dgv)
        {
            dgv.Rows.Clear();
            var filtered = allVoices.AsEnumerable();
            
            if (!string.IsNullOrWhiteSpace(q))
            {
                var lowerQ = q.ToLower();
                filtered = filtered.Where(v => 
                    (v.Name?.ToLower().Contains(lowerQ) ?? false) ||
                    (v.Id?.ToLower().Contains(lowerQ) ?? false) ||
                    (v.Description?.ToLower().Contains(lowerQ) ?? false)
                );
            }
            
            var cmbLibLang = Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() as ComboBox;
            if (cmbLibLang != null && cmbLibLang.SelectedIndex > 0)
            {
                var targetCode = GetLangCode(cmbLibLang.Text).ToLower();
                filtered = filtered.Where(v => v.Language?.ToLower() == targetCode);
            }

            foreach (var v in filtered)
            {
                int idx = dgv.Rows.Add(v.Name, v.Id, v.Language, v.Gender, v.CreatedAt);
                dgv.Rows[idx].Tag = v;
            }
        }
    }

    private string GetLangCode(string display)
    {
        if (string.IsNullOrWhiteSpace(display) || display == "Auto") return "";
        int start = display.LastIndexOf('(');
        int end = display.LastIndexOf(')');
        if (start >= 0 && end > start) return display.Substring(start + 1, end - start - 1);
        return display;
    }

    private void SetBusy(bool busy)
    {

        UpdateToolbarState();
    }

    private void UpdateToolbarState()
    {
        if (InvokeRequired) { BeginInvoke(() => UpdateToolbarState()); return; }

        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        bool hasJobs = dgv != null && dgv.Rows.Count > 0;
        
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        bool hasFolder = !string.IsNullOrWhiteSpace(txtFolder?.Text);

        bool isBusy = activeJob != null || generation != null;

        if (Controls.Find("btnImportFile", true).FirstOrDefault() is Button btnImpFile) btnImpFile.Enabled = !isBusy;
        if (Controls.Find("btnImportFolder", true).FirstOrDefault() is Button btnImpFolder) btnImpFolder.Enabled = !isBusy;
        if (Controls.Find("btnClearList", true).FirstOrDefault() is Button btnClr) btnClr.Enabled = hasJobs && !isBusy;
        
        if (Controls.Find("btnJoinMp3", true).FirstOrDefault() is Button btnJoin) btnJoin.Enabled = hasJobs && hasFolder && !isBusy;
        if (Controls.Find("btnCreateSrt", true).FirstOrDefault() is Button btnSrt) btnSrt.Enabled = hasJobs && hasFolder && !isBusy;
        if (Controls.Find("btnOpenAudioOutput", true).FirstOrDefault() is Button btnOpenOut) btnOpenOut.Enabled = hasFolder;
        if (Controls.Find("btnRunBatch", true).FirstOrDefault() is Button btnRun) btnRun.Enabled = hasJobs && hasFolder && !isBusy;
    }

    private void WriteLog(string message)
    {
        if (IsDisposed || string.IsNullOrWhiteSpace(message)) return;
        if (message.Contains("GET /api/health") || message.Contains("GET /api/system") || message.Contains("GET /api/voices")) return;

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
    }

    private async void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        lifetime.Cancel();
        generation?.Cancel();
        CleanupAudio();
        await backend.DisposeAsync();
        api.Dispose();
    }
    #endregion


    // ==========================================
    // TAB: TTS / Batch
    // ==========================================
    #region TAB: TTS / Batch

    private async void AddVoiceButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Audio (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            SetBusy(true);
            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            var lang = GetLangCode(languageComboBox.Text);
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

    private async void GenerateButton_Click(object? sender, EventArgs e)
    {
        // Determine input text from available controls in Designer.
        string GetInputText()
        {
            // Try to get text from the selected row in dgvJobs
            var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
            if (dgv != null && dgv.SelectedRows.Count > 0)
            {
                var content = dgv.SelectedRows[0].Cells[3].Value?.ToString();
                if (!string.IsNullOrWhiteSpace(content)) return content.Trim();
            }

            // Prefer preview sentence box if present and non-empty
            if (Controls.OfType<TextBox>().Any(t => t.Name == "txtPreviewSentence" && !string.IsNullOrWhiteSpace(t.Text)))
                return Controls.Find("txtPreviewSentence", true).OfType<TextBox>().First().Text.Trim();

            return string.Empty;
        }

        var input = GetInputText();
        if (string.IsNullOrWhiteSpace(input) || voiceComboBox.SelectedItem is not VoiceDto voice)
        {
            MessageBox.Show(this, "Please select a row in the Subtitles list to test its voice, and ensure a Voice is selected.", "TTS Studio", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string langCode = GetLangCode(languageComboBox.Text);
        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        if (dgv != null && dgv.SelectedRows.Count > 0)
        {
            var rowLang = dgv.SelectedRows[0].Cells[5].Value?.ToString();
            if (!string.IsNullOrWhiteSpace(rowLang)) langCode = GetLangCode(rowLang);
        }
        
        if (string.IsNullOrWhiteSpace(langCode)) langCode = "en"; // Fallback to avoid 422 if empty

        TtsGenerationSettings? settings = null;
        if (Controls.Find("chkChangeVoiceSettings", true).FirstOrDefault() is CheckBox chkChange && chkChange.Checked)
        {
            settings = new TtsGenerationSettings(
                Speed: (double)((Controls.Find("numSpeed", true).FirstOrDefault() as NumericUpDown)?.Value ?? 1m),
                CfgWeight: (double)((Controls.Find("numStability", true).FirstOrDefault() as NumericUpDown)?.Value ?? 65m) / 100.0,
                Temperature: (double)((Controls.Find("numSimilarity", true).FirstOrDefault() as NumericUpDown)?.Value ?? 60m) / 100.0,
                Exaggeration: (double)((Controls.Find("numStyle", true).FirstOrDefault() as NumericUpDown)?.Value ?? 50m) / 100.0,
                SpeakerBoost: (Controls.Find("chkSpeakerBoost", true).FirstOrDefault() as CheckBox)?.Checked ?? true,
                Seed: null,
                Denoise: (double)((Controls.Find("numDenoise", true).FirstOrDefault() as NumericUpDown)?.Value ?? 0m) / 100.0
            );
        }

        generation?.Dispose();
        generation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);

        try
        {
            SetGenerating(true);
            CleanupAudio();

            var request = new GenerateRequest(input, voice.Id, langCode, engineComboBox.Text, settings);
            var accepted = await api.GenerateAsync(request, generation.Token);
            activeJob = accepted.Id;
            WriteLog($"Job {activeJob} queued.");

            while (true)
            {
                await Task.Delay(1500, generation.Token);
                var job = await api.GetJobAsync(activeJob, generation.Token) ?? throw new InvalidOperationException("Job not found.");
                if (job.Status == "failed") throw new InvalidOperationException(job.Error ?? "Generation failed.");
                if (job.Status == "cancelled") throw new OperationCanceledException();
                if (job.Status == "completed")
                {
                    audioFile = Path.Combine(Path.GetTempPath(), $"tts-{activeJob}.wav");
                    var audioBytes = await api.DownloadAudioAsync(activeJob, generation.Token);
                    await File.WriteAllBytesAsync(audioFile, audioBytes, generation.Token);
                    try { await api.CancelJobAsync(activeJob, generation.Token); } catch { }

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
            generation?.Dispose();
            generation = null;
            SetGenerating(false);
        }
    }

    private async void StopButton_Click(object? sender, EventArgs e)
    {
        generation?.Cancel();
        if (activeJob is not null)
        {
            try { await api.CancelJobAsync(activeJob, lifetime.Token); }
            catch (Exception ex) { WriteLog(ex.Message); }
        }
    }

    private void PlayButton_Click(object? sender, EventArgs e)
    {
        if (audioFile is not null && File.Exists(audioFile))
            Process.Start(new ProcessStartInfo(audioFile) { UseShellExecute = true });
    }

    private void SaveWavButton_Click(object? sender, EventArgs e)
    {
        if (audioFile is null) return;
        using var dialog = new SaveFileDialog { Filter = "WAV audio|*.wav", FileName = $"tts-{DateTime.Now:yyyyMMdd-HHmmss}.wav" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.Copy(audioFile, dialog.FileName, true);
    }

private void BtnBrowseOutputFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtBatchFolder", true).FirstOrDefault() is TextBox txtBatch) txtBatch.Text = dialog.SelectedPath;
            if (Controls.Find("txtOutputFolder", true).FirstOrDefault() is TextBox txtOut) txtOut.Text = dialog.SelectedPath;
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
                // Use SmartSplit to ensure sentences aren't too short
                var segments = SmartSplit(fullText, splitChars, userSettings.MaxChars);
                foreach (var seg in segments)
                {
                    dgv.Rows.Add(id.ToString("D3"), "", "", seg, voiceComboBox.Text, languageComboBox.Text, "Pending");
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

        SetGenerating(true);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int totalJobs = 0, done = 0, processing = 0, failedCount = 0;
        
        System.Windows.Forms.Timer progressTimer = new System.Windows.Forms.Timer { Interval = 500 };
        progressTimer.Tick += (s, ev) => {
            lblSubtitleStatus.Text = $"Subtitles | Done: {done} | Processing: {processing} | Total: {totalJobs} | Failed: {failedCount} | Elapsed: {sw.Elapsed:mm\\:ss}";
            int progressVal = done + failedCount;
            if (progressVal <= progressTotal.Maximum) progressTotal.Value = progressVal;
        };
        progressTimer.Start();

        try
        {
            var settingsReq = new TtsGenerationSettings(
                Speed: (double)numSpeed.Value,
                CfgWeight: (double)numStability.Value / 100.0,
                Temperature: (double)numSimilarity.Value / 100.0,
                Exaggeration: (double)numStyle.Value / 100.0,
                SpeakerBoost: chkSpeakerBoost.Checked,
                Seed: null,
                Denoise: (double)numDenoise.Value / 100.0
            );
            string lang = GetLangCode(languageComboBox.Text);
            string engineTxt = engineComboBox.Text;
            bool retryFailed = (Controls.Find("chkRetryFailed", true).FirstOrDefault() as CheckBox)?.Checked == true;
            bool skipExisting = (Controls.Find("chkSkipExisting", true).FirstOrDefault() as CheckBox)?.Checked == true;

            // Pre-count total jobs
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var statusStr = dgv.Rows[i].Cells[6].Value?.ToString();
                if (statusStr == "Completed") continue;
                if (statusStr == "Failed" && !retryFailed) continue;
                totalJobs++;
            }

            if (totalJobs > 0)
            {
                progressTotal.Maximum = totalJobs;
                progressTotal.Value = 0;
            }

            var semaphore = new SemaphoreSlim(3);
            var tasks = new List<Task>();
            string ext = userSettings.OutputFormat.ToLower() == "mp3" ? "mp3" : "wav";
            string bitrate = userSettings.Mp3Bitrate;

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                var statusStr = row.Cells[6].Value?.ToString();
                if (statusStr == "Completed") continue;
                if (statusStr == "Failed" && !retryFailed) continue;
                
                var filename = $"{row.Cells[0].Value}_{voiceComboBox.Text}.{ext}";
                var path = Path.Combine(txtFolder.Text, filename);

                if (skipExisting && File.Exists(path))
                {
                    double dur = await GetAudioDurationSecs(path);
                    row.Cells[1].Value = filename;
                    row.Cells[2].Value = dur.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    row.Cells[6].Value = "Completed";
                    WriteLog($"Segment {row.Cells[0].Value} skipped (already exists).");
                    Interlocked.Increment(ref done);
                    continue;
                }
                
                var content = row.Cells[3].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(content))
                {
                    row.Cells[6].Value = "Skipped";
                    Interlocked.Increment(ref done);
                    continue;
                }

                row.Cells[6].Value = "Pending";
                var req = new GenerateRequest(content, voiceId, lang, engineTxt, settingsReq);
                string cell0 = row.Cells[0].Value?.ToString() ?? "";
                int rowIndex = i;

                await semaphore.WaitAsync(token);
                
                tasks.Add(Task.Run(async () =>
                {
                    string? jobId = null;
                    try
                    {
                        token.ThrowIfCancellationRequested();
                        Interlocked.Increment(ref processing);
                        Invoke(() => { 
                            row.Cells[6].Value = "Running";
                            string source = row.Cells[7].Value?.ToString() ?? "File";
                            lblCurrentJob.Text = $"Current: {cell0} ({source})";
                            try { dgv.FirstDisplayedScrollingRowIndex = rowIndex; } catch { }
                        });

                        var accepted = await api.GenerateAsync(req, token);
                        jobId = accepted.Id;
                        
                        while (true)
                        {
                            await Task.Delay(1500, token);
                            var job = await api.GetJobAsync(jobId, token) ?? throw new InvalidOperationException("Job not found.");
                            if (job.Status == "failed") throw new InvalidOperationException(job.Error ?? "Generation failed.");
                            if (job.Status == "cancelled") throw new OperationCanceledException();
                            if (job.Status == "completed")
                            {
                                var audioBytes = await new HttpClient().GetByteArrayAsync(new Uri(settings.BackendUri, $"api/jobs/{jobId}/audio"), token);
                                if (ext == "mp3")
                                {
                                    var tempWav = path + ".tmp.wav";
                                    await File.WriteAllBytesAsync(tempWav, audioBytes, token);
                                    var psi = new ProcessStartInfo("ffmpeg") { Arguments = $"-y -i \"{tempWav}\" -b:a {bitrate} \"{path}\"", UseShellExecute = false, CreateNoWindow = true };
                                    var proc = Process.Start(psi);
                                    if (proc != null) await proc.WaitForExitAsync(token);
                                    try { File.Delete(tempWav); } catch { }
                                }
                                else
                                {
                                    await File.WriteAllBytesAsync(path, audioBytes, token);
                                }
                                try { await api.CancelJobAsync(jobId, token); } catch { }
                                double dur = await GetAudioDurationSecs(path);
                                Invoke(() => {
                                    row.Cells[1].Value = filename;
                                    row.Cells[2].Value = dur.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                    row.Cells[6].Value = "Completed";
                                });
                                WriteLog($"Segment {cell0} completed.");
                                Interlocked.Increment(ref done);
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Invoke(() => { row.Cells[6].Value = "Pending"; });
                        if (jobId != null) { try { await api.CancelJobAsync(jobId, lifetime.Token); } catch { } }
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedCount);
                        Invoke(() => { row.Cells[6].Value = "Failed"; });
                        WriteLog($"Segment {cell0} failed: {ex.Message}");
                    }
                    finally
                    {
                        Interlocked.Decrement(ref processing);
                        semaphore.Release();
                    }
                }, token));
            }
            
            await Task.WhenAll(tasks);
            WriteLog("Batch completed!");
            var chkSrt = Controls.Find("chkAutoSrt", true).FirstOrDefault() as CheckBox;
            if (chkSrt?.Checked == true) await GenerateSrtTask();

            var chkJoin = Controls.Find("chkAutoJoin", true).FirstOrDefault() as CheckBox;
            if (chkJoin?.Checked == true) await JoinMp3Task();
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
            sw.Stop();
            progressTimer.Stop();
            progressTimer.Dispose();
            lblSubtitleStatus.Text = $"Subtitles | Done: {done} | Processing: 0 | Total: {totalJobs} | Failed: {failedCount} | Elapsed: {sw.Elapsed:mm\\:ss}";
            activeJob = null;
            generation?.Dispose();
            generation = null;
            SetGenerating(false);
        }
    }

    private async void BtnJoinMp3_Click(object? sender, EventArgs e)
    {
        await JoinMp3Task();
    }

    private async Task JoinMp3Task()
    {
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        if (txtFolder == null || string.IsNullOrWhiteSpace(txtFolder.Text) || dgv == null) return;
        
        SetBusy(true);
        try
        {
            var groups = new Dictionary<string, List<string>>();
            string ext = userSettings.OutputFormat.ToLower() == "mp3" ? "mp3" : "wav";

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                var filename = row.Cells[1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(filename)) continue;
                
                var path = Path.Combine(txtFolder.Text, filename);
                if (!File.Exists(path)) continue;

                var sourceFile = row.Cells[7].Value?.ToString();
                if (string.IsNullOrWhiteSpace(sourceFile)) sourceFile = "000_Full_Audio";
                else sourceFile = "000_" + sourceFile;

                if (!groups.ContainsKey(sourceFile)) groups[sourceFile] = new List<string>();
                groups[sourceFile].Add(path);
            }

            if (groups.Count == 0) return;

            foreach (var group in groups)
            {
                var sourceName = group.Key;
                var files = group.Value;
                if (files.Count == 0) continue;

                var listFile = Path.Combine(txtFolder.Text, $"file_list_{sourceName}.txt");
                var outputAudio = Path.Combine(txtFolder.Text, $"{sourceName}.{ext}");
                var lines = files.Select(f => $"file '{Path.GetFileName(f)}'").ToArray();
                await File.WriteAllLinesAsync(listFile, lines, lifetime.Token);

                var psi = new ProcessStartInfo("ffmpeg")
                {
                    Arguments = $"-y -f concat -safe 0 -i \"{listFile}\" -c copy \"{outputAudio}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                };
                
                var process = Process.Start(psi);
                if (process != null)
                {
                    await process.WaitForExitAsync(lifetime.Token);
                    if (process.ExitCode == 0)
                    {
                        WriteLog($"Successfully joined {files.Count} files into {sourceName}.{ext}");
                        try { File.Delete(listFile); } catch { }
                    }
                    else WriteLog($"FFmpeg join failed for {sourceName}: " + await process.StandardError.ReadToEndAsync());
                }
            }

            if (Controls.Find("chkDeleteSegments", true).FirstOrDefault() is CheckBox chkDel && chkDel.Checked)
            {
                foreach (var group in groups)
                {
                    foreach (var file in group.Value)
                    {
                        try { File.Delete(file); } catch { }
                    }
                }
                WriteLog("Deleted individual segment files.");
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

    private async void BtnImportFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        if (dgv == null) return;

        var files = Directory.GetFiles(dialog.SelectedPath, "*.txt").OrderBy(f => f).ToList();
        if (files.Count == 0) {
            MessageBox.Show(this, "No .txt files found in the selected folder.");
            return;
        }

        bool autoSplit = (Controls.Find("chkAutoSplit", true).FirstOrDefault() as CheckBox)?.Checked == true;
        string splitChars = (Controls.Find("txtSplitChars", true).FirstOrDefault() as TextBox)?.Text ?? ".!?\n";
        
        int id = dgv.Rows.Count > 0 ? dgv.Rows.Count + 1 : 1;
        int imported = 0;

        foreach (var file in files)
        {
            string sourceName = Path.GetFileNameWithoutExtension(file);
            lblSubtitleStatus.Text = $"Subtitles | Importing: {sourceName}.txt";
            await Task.Delay(10); // Let UI refresh

            string fullText = await File.ReadAllTextAsync(file);
            if (autoSplit && !string.IsNullOrEmpty(splitChars))
            {
                var segments = SmartSplit(fullText, splitChars, userSettings.MaxChars);
                foreach (var seg in segments)
                {
                    dgv.Rows.Add(id.ToString("D3"), "", "", seg, voiceComboBox.Text, languageComboBox.Text, "Pending", sourceName);
                    id++; imported++;
                }
            }
            else
            {
                var lines = await File.ReadAllLinesAsync(file);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    dgv.Rows.Add(id.ToString("D3"), "", "", line.Trim(), voiceComboBox.Text, languageComboBox.Text, "Pending", sourceName);
                    id++; imported++;
                }
            }
        }
        
        lblSubtitleStatus.Text = $"Subtitles | Done: 0 | Processing: 0 | Total: {dgv.Rows.Count} | Failed: 0 | Elapsed: 00:00";
        WriteLog($"Imported {imported} segments from folder {Path.GetFileName(dialog.SelectedPath)}");
    }

    private void BtnOpenAudioOutput_Click(object? sender, EventArgs e)
    {
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        if (txtFolder != null && !string.IsNullOrWhiteSpace(txtFolder.Text) && Directory.Exists(txtFolder.Text))
        {
            Process.Start(new ProcessStartInfo { FileName = txtFolder.Text, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show(this, "Output folder is not set or does not exist.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async void BtnCreateSrt_Click(object? sender, EventArgs e)
    {
        await GenerateSrtTask();
    }
    
    private async Task<double> GetAudioDurationSecs(string path)
    {
        double durationSecs = 0;
        try {
            var psi = new ProcessStartInfo("ffprobe") {
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
                RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc != null) {
                var outStr = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                if (double.TryParse(outStr.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d)) {
                    durationSecs = d;
                }
            }
        } catch { }

        if (durationSecs <= 0) {
            try {
                var fi = new FileInfo(path);
                durationSecs = (fi.Length - 44) / 48000.0;
                if (durationSecs < 0) durationSecs = 0;
            } catch { }
        }
        return durationSecs;
    }
    
    private async Task GenerateSrtTask()
    {
        var dgv = Controls.Find("dgvJobs", true).FirstOrDefault() as DataGridView;
        var txtFolder = Controls.Find("txtBatchFolder", true).FirstOrDefault() as TextBox;
        if (dgv == null || txtFolder == null || string.IsNullOrWhiteSpace(txtFolder.Text)) return;
        
        var groups = new Dictionary<string, List<DataGridViewRow>>();
        for (int i = 0; i < dgv.Rows.Count; i++)
        {
            var row = dgv.Rows[i];
            if (row.Cells[6].Value?.ToString() != "Completed") continue;
            
            var sourceFile = row.Cells[7].Value?.ToString();
            if (string.IsNullOrWhiteSpace(sourceFile)) sourceFile = "000_Full_Audio";
            else sourceFile = "000_" + sourceFile;
            
            if (!groups.ContainsKey(sourceFile)) groups[sourceFile] = new List<DataGridViewRow>();
            groups[sourceFile].Add(row);
        }

        foreach (var group in groups)
        {
            var sourceName = group.Key;
            string srtPath = Path.Combine(txtFolder.Text, $"{sourceName}_subtitles.srt");
            var sb = new System.Text.StringBuilder();
            TimeSpan current = TimeSpan.Zero;
            int index = 1;

            foreach (var row in group.Value)
            {
                double durationSecs = 0;
                var timingValue = row.Cells[2].Value?.ToString();
                
                if (!string.IsNullOrEmpty(timingValue) && double.TryParse(timingValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double dur))
                {
                    durationSecs = dur;
                }
                else
                {
                    var filename = row.Cells[1].Value?.ToString();
                    if (string.IsNullOrEmpty(filename)) continue;
                    
                    var path = Path.Combine(txtFolder.Text, filename);
                    if (!File.Exists(path)) continue;
                    
                    durationSecs = await GetAudioDurationSecs(path);
                    row.Cells[2].Value = durationSecs.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                
                if (durationSecs <= 0) continue;

                var start = current;
                var end = current.Add(TimeSpan.FromSeconds(durationSecs));
                
                sb.AppendLine(index.ToString());
                sb.AppendLine($"{start:hh\\:mm\\:ss\\,fff} --> {end:hh\\:mm\\:ss\\,fff}");
                sb.AppendLine(row.Cells[3].Value?.ToString());
                sb.AppendLine();
                
                current = end;
                index++;
            }
            
            try {
                await File.WriteAllTextAsync(srtPath, sb.ToString(), lifetime.Token);
                WriteLog($"SRT created at {srtPath}");
            } catch (Exception ex) {
                WriteLog($"Failed to create SRT for {sourceName}: " + ex.Message);
            }
        }
    }
    #endregion


    // ==========================================
    // TAB: Video Editor
    // ==========================================
    #region TAB: Video Editor

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

        var parsedSubs = new List<(string id, string start, string end, TimeSpan tsStart, TimeSpan tsEnd, string content)>();

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
                    var times = timing.Split(new[] { "-->" }, StringSplitOptions.None);
                    string start = times.Length > 0 ? times[0].Trim() : "";
                    string end = times.Length > 1 ? times[1].Trim() : "";
                    
                    if (TimeSpan.TryParse(start.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture, out var tsStart) && 
                        TimeSpan.TryParse(end.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture, out var tsEnd))
                    {
                        parsedSubs.Add((id.ToString("D3"), start, end, tsStart, tsEnd, content.Trim()));
                    }
                }
            }
        }

        dgv.Rows.Clear();
        int imageIndex = 0;
        for (int i = 0; i < parsedSubs.Count; i++)
        {
            var sub = parsedSubs[i];
            TimeSpan durationTs = sub.tsEnd - sub.tsStart;
            
            // If there's a next subtitle, the duration of THIS image should span until the NEXT subtitle starts
            // This perfectly accounts for any silent gaps between subtitles.
            if (i < parsedSubs.Count - 1)
            {
                durationTs = parsedSubs[i + 1].tsStart - sub.tsStart;
            }

            string duration = durationTs.TotalSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s";
            string imgPath = images.Count > 0 ? Path.GetFileName(images[imageIndex % images.Count]) : "";
            imageIndex++;

            dgv.Rows.Add(sub.id, sub.start, sub.end, duration, imgPath, sub.content, "Pending");
        }

        WriteLog($"Analyzed timeline: {dgv.Rows.Count} segments matched with {images.Count} images.");
    }
    private async Task<string> MonitorFfmpegProgress(Process process)
    {
        double totalDuration = 0;
        var sb = new System.Text.StringBuilder();
        var lblStatus = Controls.Find("lblVideoRenderStatus", true).FirstOrDefault() as Label;
        var pBar = Controls.Find("progressVideoRender", true).FirstOrDefault() as ProgressBar;

        if (lblStatus != null) Invoke(() => lblStatus.Text = "Rendering... 0%");
        if (pBar != null) Invoke(() => { pBar.Style = ProgressBarStyle.Continuous; pBar.Value = 0; });

        using var reader = process.StandardError;
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line)) continue;
            sb.AppendLine(line);
            
            if (totalDuration <= 0 && line.Contains("Duration:"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(line, @"Duration:\s*(\d{2}):(\d{2}):(\d{2})\.(\d+)");
                if (match.Success)
                {
                    TimeSpan ts = new TimeSpan(0, int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value) * 10);
                    totalDuration = ts.TotalSeconds;
                }
            }

            if (line.StartsWith("frame=") || line.Contains("time="))
            {
                var timeMatch = System.Text.RegularExpressions.Regex.Match(line, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d+)");
                if (timeMatch.Success && totalDuration > 0)
                {
                    TimeSpan ts = new TimeSpan(0, int.Parse(timeMatch.Groups[1].Value), int.Parse(timeMatch.Groups[2].Value), int.Parse(timeMatch.Groups[3].Value), int.Parse(timeMatch.Groups[4].Value) * 10);
                    int pct = (int)((ts.TotalSeconds / totalDuration) * 100);
                    if (pct < 0) pct = 0;
                    if (pct > 100) pct = 100;
                    
                    if (pBar != null && lblStatus != null) Invoke(() => {
                        pBar.Value = pct;
                        lblStatus.Text = $"Rendering... {pct}%";
                    });
                }
            }
        }
        
        return sb.ToString();
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

        string outputMp4 = "";
        using (var sfd = new SaveFileDialog { Filter = "MP4 Video|*.mp4", DefaultExt = "mp4", FileName = "final_render.mp4" })
        {
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            outputMp4 = sfd.FileName;
        }

        SetBusy(true);
        try
        {
            var tempDir = Path.Combine(Path.GetDirectoryName(outputMp4) ?? "", "temp_frames");
            Directory.CreateDirectory(tempDir);

            // Read user settings
            int targetW = 1920;
            int targetH = 1080;
            var cmbRes = Controls.Find("cmbResolution", true).FirstOrDefault() as ComboBox;
            if (cmbRes?.SelectedItem != null)
            {
                var parts = cmbRes.SelectedItem.ToString()!.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0].Trim(), out int w) && int.TryParse(parts[1].Trim(), out int h))
                {
                    targetW = w;
                    targetH = h;
                }
            }

            int fps = 30;
            var cmbFpsCtrl = Controls.Find("cmbFps", true).FirstOrDefault() as ComboBox;
            if (cmbFpsCtrl?.SelectedItem != null && int.TryParse(cmbFpsCtrl.SelectedItem.ToString()!.Trim(), out int parsedFps))
            {
                fps = parsedFps;
            }

            var cmbEnc = Controls.Find("cmbEncoder", true).FirstOrDefault() as ComboBox;
            string encChoice = cmbEnc?.SelectedItem?.ToString() ?? "Auto";
            bool useGpu = false;
            if (encChoice == "NVIDIA NVENC") useGpu = true;
            else if (encChoice == "CPU x264" || encChoice == "CPU x265") useGpu = false;
            else
            {
                var system = await api.GetSystemAsync(lifetime.Token);
                useGpu = system?.CudaAvailable == true;
            }

            string vcodec = useGpu ? "h264_nvenc -preset p6 -cq 23" : (encChoice == "CPU x265" ? "libx265 -crf 26 -preset fast" : "libx264 -crf 23 -preset veryfast");

            WriteLog($"Starting video render: {targetW}x{targetH} @ {fps}fps, encoder: {(useGpu ? "NVENC" : "CPU")}...");

            // Pre-process and normalize all unique images to target resolution
            var imageCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int imgCounter = 0;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                var imgName = row.Cells[4].Value?.ToString();
                if (string.IsNullOrEmpty(imgName)) continue;
                
                var origPath = Path.Combine(txtImg.Text, imgName);
                if (!File.Exists(origPath)) continue;

                if (!imageCache.ContainsKey(origPath))
                {
                    string localImgName = $"frame_{imgCounter:D4}.png";
                    string localImgPath = Path.Combine(tempDir, localImgName);
                    
                    using (var src = Image.FromFile(origPath))
                    using (var bmp = new Bitmap(targetW, targetH))
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.Black);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        double ratio = Math.Min((double)targetW / src.Width, (double)targetH / src.Height);
                        int newW = Math.Max(1, (int)(src.Width * ratio));
                        int newH = Math.Max(1, (int)(src.Height * ratio));
                        int posX = (targetW - newW) / 2;
                        int posY = (targetH - newH) / 2;
                        g.DrawImage(src, posX, posY, newW, newH);
                        bmp.Save(localImgPath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    imageCache[origPath] = localImgName;
                    imgCounter++;
                }
            }

            // Generate ffconcat slides.txt
            var concatFile = Path.Combine(tempDir, "slides.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("ffconcat version 1.0");

            string lastLocalImg = "";
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var imgName = row.Cells[4].Value?.ToString();
                var durationStr = row.Cells[3].Value?.ToString()?.Replace("s", "")?.Replace(",", ".");
                
                if (!string.IsNullOrEmpty(imgName) && !string.IsNullOrEmpty(durationStr) && double.TryParse(durationStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double durVal))
                {
                    var origPath = Path.Combine(txtImg.Text, imgName);
                    if (imageCache.TryGetValue(origPath, out var localImgName))
                    {
                        sb.AppendLine($"file '{localImgName}'");
                        sb.AppendLine($"duration {durVal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");
                        lastLocalImg = localImgName;
                    }
                }
            }

            // Concat demuxer requirement: repeat the last file without duration directive
            if (!string.IsNullOrEmpty(lastLocalImg))
            {
                sb.AppendLine($"file '{lastLocalImg}'");
            }

            await File.WriteAllTextAsync(concatFile, sb.ToString(), lifetime.Token);

            // Copy SRT to temp folder to avoid Windows path colon escaping issues in FFmpeg filter
            string localSrtName = "subs.srt";
            string localSrtPath = Path.Combine(tempDir, localSrtName);
            File.Copy(txtSrt.Text, localSrtPath, true);

            bool burnSub = (Controls.Find("chkBurnSubtitle", true).FirstOrDefault() as CheckBox)?.Checked == true;
            string vfOpt = burnSub ? $"subtitles='{localSrtName}',fps={fps}" : $"fps={fps}";

            // Run FFmpeg with constant framerate and audio sync
            var psi = new ProcessStartInfo("ffmpeg")
            {
                WorkingDirectory = tempDir,
                Arguments = $"-y -f concat -safe 0 -i \"slides.txt\" -i \"{txtAudio.Text}\" -vf \"{vfOpt}\" -c:v {vcodec} -r {fps} -pix_fmt yuv420p -c:a aac -shortest \"{outputMp4}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };

            WriteLog($"Running: ffmpeg {psi.Arguments}");
            var process = Process.Start(psi);
            if (process != null)
            {
                string err = await MonitorFfmpegProgress(process);
                await process.WaitForExitAsync(lifetime.Token);
                
                var lblStatus = Controls.Find("lblVideoRenderStatus", true).FirstOrDefault() as Label;
                var pBar = Controls.Find("progressVideoRender", true).FirstOrDefault() as ProgressBar;

                if (process.ExitCode == 0)
                {
                    WriteLog($"Render completed successfully: {outputMp4}");
                    if (lblStatus != null) Invoke(() => lblStatus.Text = "Render completed!");
                    if (pBar != null) Invoke(() => pBar.Value = 100);
                    
                    Process.Start(new ProcessStartInfo(outputMp4) { UseShellExecute = true });
                    try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{outputMp4}\"") { UseShellExecute = true }); } catch { }
                }
                else
                {
                    if (useGpu)
                    {
                        WriteLog("GPU render failed, falling back to CPU (libx264)...");
                        psi.Arguments = $"-y -f concat -safe 0 -i \"slides.txt\" -i \"{txtAudio.Text}\" -vf \"{vfOpt}\" -c:v libx264 -crf 23 -preset veryfast -r {fps} -pix_fmt yuv420p -c:a aac -shortest \"{outputMp4}\"";
                        var fbProcess = Process.Start(psi);
                        if (fbProcess != null)
                        {
                            string fbErr = await MonitorFfmpegProgress(fbProcess);
                            await fbProcess.WaitForExitAsync(lifetime.Token);
                            if (fbProcess.ExitCode == 0)
                            {
                                WriteLog($"Render completed successfully: {outputMp4}");
                                if (lblStatus != null) Invoke(() => lblStatus.Text = "Render completed (CPU Fallback)!");
                                if (pBar != null) Invoke(() => pBar.Value = 100);

                                Process.Start(new ProcessStartInfo(outputMp4) { UseShellExecute = true });
                                try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{outputMp4}\"") { UseShellExecute = true }); } catch { }
                            }
                            else
                            {
                                WriteLog("FFmpeg fallback render failed: " + fbErr);
                                if (lblStatus != null) Invoke(() => lblStatus.Text = "Render Failed!");
                            }
                        }
                    }
                    else
                    {
                        WriteLog("FFmpeg render failed: " + err);
                        if (lblStatus != null) Invoke(() => lblStatus.Text = "Render Failed!");
                    }
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
    #endregion


    // ==========================================
    // TAB: Voice Library
    // ==========================================
    #region TAB: Voice Library

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
                var req = new GenerateRequest(txt.Text, v.Id, "en", engineComboBox.Text, new TtsGenerationSettings(1.0, 0.5, 0.8, 0.5, true, null, 0.0));
                var job = await api.GenerateAsync(req, lifetime.Token);
                while(true) {
                    await Task.Delay(500, lifetime.Token);
                    var status = await api.GetJobAsync(job.Id, lifetime.Token);
                    if (status?.Status == "failed") throw new Exception($"Preview failed: {status.Error}");
                    if (status?.Status == "completed") {
                        var audio = await new HttpClient().GetByteArrayAsync(new Uri(settings.BackendUri, $"api/jobs/{job.Id}/audio"));
                        var temp = Path.GetTempFileName() + ".wav";
                        await File.WriteAllBytesAsync(temp, audio, lifetime.Token);
                        try { await api.CancelJobAsync(job.Id, lifetime.Token); } catch { } // Clean up backend
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = temp, UseShellExecute = true });
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
            lblSelectedVoiceName.Text = v.Name;
            txtSelectedVoiceId.Text = v.Id;
            lblSelectedVoiceLanguage.Text = v.Language ?? "N/A";
            lblSelectedVoiceGender.Text = v.Gender ?? "N/A";
            txtSelectedVoiceDescription.Text = v.Description ?? "";
            
            // Check if reference exists
            txtReferenceAudio.Text = string.IsNullOrEmpty(v.ReferenceFile) ? "None" : "reference.wav";
        }
    }

    private async void BtnPlayReference_Click(object? sender, EventArgs e)
    {
        var dgv = Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() as DataGridView;
        if (dgv?.SelectedRows.Count > 0 && dgv.SelectedRows[0].Tag is VoiceDto v)
        {
            if (string.IsNullOrEmpty(v.ReferenceFile)) return;
            try {
                var audio = await new HttpClient().GetByteArrayAsync(new Uri(settings.BackendUri, $"api/voices/{v.Id}/reference"));
                var temp = Path.GetTempFileName() + ".wav";
                await File.WriteAllBytesAsync(temp, audio);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = temp, UseShellExecute = true });
            } catch (Exception ex) {
                WriteLog("Error playing reference: " + ex.Message);
            }
        }
    }

    private void BtnSearchVoice_Click(object? sender, EventArgs e)
    {
        var txtId = Controls.Find("txtVoiceName", true).FirstOrDefault() as TextBox;
        var cmbVoice = Controls.Find("voiceComboBox", true).FirstOrDefault() as ComboBox;
        var cmbLang = Controls.Find("languageComboBox", true).FirstOrDefault() as ComboBox;

        if (txtId == null || cmbVoice == null || cmbLang == null) return;

        string searchId = txtId.Text.Trim();
        if (string.IsNullOrWhiteSpace(searchId)) return;

        bool found = false;
        for (int i = 0; i < cmbVoice.Items.Count; i++)
        {
            if (cmbVoice.Items[i] is Models.VoiceDto vm && string.Equals(vm.Id, searchId, StringComparison.OrdinalIgnoreCase))
            {
                cmbVoice.SelectedIndex = i;
                found = true;
                
                // Map language Code to friendly language name
                if (!string.IsNullOrEmpty(vm.Language))
                {
                    var langStr = vm.Language.ToLower();
                    for (int j = 0; j < cmbLang.Items.Count; j++)
                    {
                        var itemStr = cmbLang.Items[j].ToString()?.ToLower();
                        if (itemStr != null && itemStr.Contains(langStr))
                        {
                            cmbLang.SelectedIndex = j;
                            break;
                        }
                    }
                }
                break;
            }
        }

        if (!found)
        {
            MessageBox.Show(this, "Voice ID not found in library.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void BtnBrowseVoiceFile_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Audio (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            if (Controls.Find("txtNewVoiceFile", true).FirstOrDefault() is TextBox txt) txt.Text = dialog.FileName;
        }
    }

    private void BtnPreviewNewVoice_Click(object? sender, EventArgs e)
    {
        var txt = Controls.Find("txtNewVoiceFile", true).FirstOrDefault() as TextBox;
        if (txt != null && File.Exists(txt.Text))
        {
            try
            {
                Process.Start(new ProcessStartInfo(txt.Text) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                WriteLog("Error playing preview: " + ex.Message);
            }
        }
    }

    private void BtnClearNewVoice_Click(object? sender, EventArgs e)
    {
        if (Controls.Find("txtNewVoiceName", true).FirstOrDefault() is TextBox txtName) txtName.Text = "";
        if (Controls.Find("txtNewVoiceId", true).FirstOrDefault() is TextBox txtId) txtId.Text = "";
        if (Controls.Find("cmbNewVoiceLanguage", true).FirstOrDefault() is ComboBox cmbLang) cmbLang.SelectedIndex = -1;
        if (Controls.Find("cmbNewVoiceGender", true).FirstOrDefault() is ComboBox cmbGender) cmbGender.SelectedIndex = -1;
        if (Controls.Find("txtNewVoiceDescription", true).FirstOrDefault() is TextBox txtDesc) txtDesc.Text = "";
        if (Controls.Find("txtNewVoiceFile", true).FirstOrDefault() is TextBox txtFile) txtFile.Text = "";
    }

    private async void BtnSaveNewVoice_Click(object? sender, EventArgs e)
    {
        var txtName = Controls.Find("txtNewVoiceName", true).FirstOrDefault() as TextBox;
        var txtVoiceId = Controls.Find("txtNewVoiceId", true).FirstOrDefault() as TextBox;
        var cmbLang = Controls.Find("cmbNewVoiceLanguage", true).FirstOrDefault() as ComboBox;
        var cmbGender = Controls.Find("cmbNewVoiceGender", true).FirstOrDefault() as ComboBox;
        var txtDesc = Controls.Find("txtNewVoiceDescription", true).FirstOrDefault() as TextBox;
        var txtFile = Controls.Find("txtNewVoiceFile", true).FirstOrDefault() as TextBox;

        var name = txtName?.Text?.Trim();
        var voiceId = txtVoiceId?.Text?.Trim();
        if (string.IsNullOrEmpty(voiceId)) voiceId = null;
        var lang = GetLangCode(cmbLang?.Text?.Trim() ?? "");
        var gender = cmbGender?.Text?.Trim();
        var desc = txtDesc?.Text?.Trim();
        var filePath = txtFile?.Text?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show(this, "Voice name is required.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            MessageBox.Show(this, "Please select a valid audio file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        try
        {
            var voice = await api.AddVoiceAsync(name, lang, gender, desc, voiceId, filePath, lifetime.Token);
            await RefreshVoices(null, lifetime.Token);
            
            BtnClearNewVoice_Click(null, EventArgs.Empty);

            if (voice is not null)
                voiceComboBox.SelectedValue = voice.Id;

            WriteLog($"Added new voice: {voice?.Name ?? name}");
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

    private void BtnLibraryAddVoice_Click(object? sender, EventArgs e)
    {
        if (Controls.Find("txtNewVoiceName", true).FirstOrDefault() is TextBox txtName)
        {
            txtName.Focus();
        }
    }
    #endregion

    // ==========================================
    // Other
    // ==========================================
    #region Other

    private void SetGenerating(bool active)
    {
        SetBusy(active);
        stopButton.Enabled = active;
        UpdateToolbarState();
    }
    #endregion

    private List<string> SmartSplit(string text, string splitChars, int maxChars = 250)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(splitChars) || string.IsNullOrWhiteSpace(text))
        {
            if (!string.IsNullOrWhiteSpace(text)) result.Add(text.Trim());
            return result;
        }

        var segments = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[" + System.Text.RegularExpressions.Regex.Escape(splitChars) + "])");
        var currentBuffer = new System.Text.StringBuilder();

        int WordCount(string s) => s.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        foreach (var segment in segments)
        {
            var seg = segment.Trim();
            if (string.IsNullOrWhiteSpace(seg)) continue;

            if (currentBuffer.Length > 0) currentBuffer.Append(" ");
            currentBuffer.Append(seg);

            // If the buffered sentence has more than 3 words, it's long enough to stand alone.
            // Or if it exceeds maxChars, we must flush it.
            if (WordCount(currentBuffer.ToString()) > 3 || currentBuffer.Length >= maxChars)
            {
                result.Add(currentBuffer.ToString());
                currentBuffer.Clear();
            }
        }

        if (currentBuffer.Length > 0)
        {
            if (result.Count > 0 && WordCount(currentBuffer.ToString()) <= 3)
            {
                // Merge with the last sentence if it's just 1-3 words
                result[result.Count - 1] += " " + currentBuffer.ToString();
            }
            else
            {
                result.Add(currentBuffer.ToString());
            }
        }

        return result;
    }
}

public class UserSettings
{
    public string OutputFolder { get; set; } = "";
    public int MaxChars { get; set; } = 150;
    public int Silence { get; set; } = 300;
    public string OutputFormat { get; set; } = "WAV";
    public string Mp3Bitrate { get; set; } = "192k";
}

