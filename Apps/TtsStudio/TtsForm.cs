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
        Text = DvaForDev.Shared.StudioBranding.TtsName;
        // Standalone TTS: no Video Editor, Whiteboard or AI Video Studio.
        mainTabControl.TabPages.Remove(tabVideoEditor);
        InitializeUpgradeTabs();
        InitializeNativeWinFormsUi();
        api = new(settings);
        backend.OutputReceived += WriteLog;
        FormClosing += Form1_FormClosing;

        // Dynamic Bindings
        if (Controls.Find("btnRunBatch", true).FirstOrDefault() is Button btnRun) btnRun.Click += BtnRunBatch_Click;
        if (Controls.Find("btnJoinMp3", true).FirstOrDefault() is Button btnJoin) btnJoin.Click += BtnJoinMp3_Click;
        if (Controls.Find("btnBrowseOutputFolder", true).FirstOrDefault() is Button btnOutFolder) btnOutFolder.Click += BtnBrowseOutputFolder_Click;
        if (Controls.Find("btnBrowseFolder", true).FirstOrDefault() is Button btnBrowseFold) btnBrowseFold.Click += BtnBrowseOutputFolder_Click;
        if (Controls.Find("btnImportFile", true).FirstOrDefault() is Button btnImport) btnImport.Click += BtnImportFile_Click;
        InitializeSubtitleBatchImport();
        if (Controls.Find("btnImportFolder", true).FirstOrDefault() is Button btnImpFolder) btnImpFolder.Click += BtnImportFolder_Click;
        if (Controls.Find("btnClearList", true).FirstOrDefault() is Button btnClr) btnClr.Click += (s, e) => {
            if (Controls.Find("dgvJobs", true).FirstOrDefault() is DataGridView dgv) dgv.Rows.Clear();
            if (_importedSrtInfo != null) _importedSrtInfo.Text = "TXT / SRT";
        };
        if (Controls.Find("btnImportVoice", true).FirstOrDefault() is Button btnImpVoice) btnImpVoice.Click += AddVoiceButton_Click;
        if (Controls.Find("btnOpenAudioOutput", true).FirstOrDefault() is Button btnOpenOut) btnOpenOut.Click += BtnOpenAudioOutput_Click;
        if (Controls.Find("btnCreateSrt", true).FirstOrDefault() is Button btnCreateSrt) btnCreateSrt.Click += BtnCreateSrt_Click;
        if (Controls.Find("btnSaveSettings", true).FirstOrDefault() is Button btnSave) btnSave.Click += (s, e) => SaveSettings();
        if (Controls.Find("btnDeleteVoice", true).FirstOrDefault() is Button btnDelVoice) btnDelVoice.Click += BtnDeleteVoice_Click;
        if (Controls.Find("btnReplaceReference", true).FirstOrDefault() is Button btnRepRef) btnRepRef.Click += BtnReplaceReference_Click;
        if (Controls.Find("btnGeneratePreview", true).FirstOrDefault() is Button btnGenPrev) btnGenPrev.Click += BtnGeneratePreview_Click;
        if (Controls.Find("btnUseSelectedVoice", true).FirstOrDefault() is Button btnUseVoice) btnUseVoice.Click += BtnUseSelectedVoice_Click;
        voiceComboBox.SelectedIndexChanged += (s, e) => ApplyVoiceLanguageAndEngine(voiceComboBox.SelectedItem as VoiceDto);
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
        if (Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() is ComboBox cmbLibLang) cmbLibLang.SelectedIndexChanged += CmbLibLang_SelectedIndexChanged;

        if (Controls.Find("dgvVoiceLibrary", true).FirstOrDefault() is DataGridView dgvVoice)
        {
            dgvVoice.SelectionChanged += DgvVoiceLibrary_SelectionChanged;
            dgvVoice.SelectionChanged += (_, _) => SyncVoiceWorkspace();
        }

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
        if (Controls.Find("cmbMp3Bitrate", true).FirstOrDefault() is ComboBox cmbBitrate)
        {
            var digits = System.Text.RegularExpressions.Regex.Match(userSettings.Mp3Bitrate ?? "", @"\d+").Value;
            int idx = -1;
            for (int k = 0; k < cmbBitrate.Items.Count; k++)
            {
                if (cmbBitrate.Items[k]?.ToString()?.Contains(digits) == true)
                {
                    idx = k;
                    break;
                }
            }
            if (idx >= 0) cmbBitrate.SelectedIndex = idx;
            else cmbBitrate.Text = userSettings.Mp3Bitrate;
        }
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
            UpdateEngineSettingsUI();
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

        UpdateEngineSettingsUI();
        SetStatus("Backend: Đang khởi động / kết nối...", "GPU: Đang kiểm tra");
        try
        {
            await backend.StartAsync(settings.PythonExecutable, lifetime.Token);
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            // Keep monitoring: externally started/slow backends can recover.
            WriteLog("Khởi động backend: " + ex.Message);
        }

        // Engine initialization can exceed 30 seconds on CPU. Keep watching
        // asynchronously until the API comes online instead of abandoning
        // the Voice Library after a fixed startup timeout.
        _ = Monitor(lifetime.Token);
    }

    private async Task Monitor(CancellationToken token)
    {
        bool connected = false;
        while (!token.IsCancellationRequested)
        {
            try
            {
                await RefreshStatus(token);
                if (!connected)
                {
                    // Also supports an externally started backend after the UI
                    // has already opened and initially showed an empty library.
                    await RefreshVoices(null, token);
                    if (lastVoiceFetchSucceeded)
                        WriteLog($"Backend kết nối thành công. Đang hiển thị {allVoices.Count} giọng.");
                }
                connected = lastVoiceFetchSucceeded;
                lastConnectionError = null;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                connected = false;
                SetStatus(backend.IsRunning
                    ? "Backend: Đang khởi động / chờ API..."
                    : "Backend: Chưa kết nối", "GPU: Chưa xác định");
                if (lastConnectionError != ex.Message)
                {
                    lastConnectionError = ex.Message;
                    WriteLog("Không kết nối được backend trên cổng 7865: " + ex.Message);
                }
            }

            try { await Task.Delay(2500, token); } catch (OperationCanceledException) { break; }
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
    private bool lastVoiceFetchSucceeded;
    private string? lastVoiceFetchError;

    private async Task RefreshVoices(string? q = null, CancellationToken token = default)
    {
        // Fetch from API only if it's a full refresh (q == null) or if we don't have voices yet
        if (q == null || allVoices.Count == 0)
        {
            lastVoiceFetchSucceeded = false;
            try
            {
                var voices = await api.GetVoicesAsync(null, token);
                if (voices != null)
                {
                    lastVoiceFetchSucceeded = true;
                    lastVoiceFetchError = null;
                    allVoices = voices.ToList();
                    var currentVoiceId = voiceComboBox.SelectedValue?.ToString();

                    voiceComboBox.DataSource = allVoices.ToList();
                    voiceComboBox.DisplayMember = nameof(VoiceDto.Name);
                    voiceComboBox.ValueMember = nameof(VoiceDto.Id);

                    if (currentVoiceId != null) voiceComboBox.SelectedValue = currentVoiceId;

                    // Ensure the language filter dropdown contains all standard languages + any extra languages from data
                    var cmbLibLangDrop = Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() as ComboBox;
                    if (cmbLibLangDrop != null)
                    {
                        var prevSel = cmbLibLangDrop.SelectedIndex > 0 ? cmbLibLangDrop.Text : null;
                        var standardLangs = new List<string>
                        {
                            "All Languages",
                            "English (en)",
                            "Tiếng Việt (vi)",
                            "Spanish (es)",
                            "French (fr)",
                            "German (de)",
                            "Italian (it)",
                            "Portuguese (pt)",
                            "Russian (ru)",
                            "Japanese (ja)",
                            "Korean (ko)",
                            "Chinese (zh)"
                        };

                        var extraLangs = allVoices
                            .Select(v => v.Language?.Trim())
                            .Where(l => !string.IsNullOrEmpty(l))
                            .Select(l => l!)
                            .Where(l => !standardLangs.Any(s => MatchesLanguage(l, s)))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(l => l)
                            .ToList();

                        cmbLibLangDrop.SelectedIndexChanged -= CmbLibLang_SelectedIndexChanged;
                        cmbLibLangDrop.Items.Clear();
                        foreach (var lang in standardLangs)
                            cmbLibLangDrop.Items.Add(lang);
                        foreach (var extra in extraLangs)
                            cmbLibLangDrop.Items.Add(extra);

                        // Restore previous language selection if still available
                        if (prevSel != null)
                        {
                            int restoreIdx = cmbLibLangDrop.FindStringExact(prevSel);
                            cmbLibLangDrop.SelectedIndex = restoreIdx >= 0 ? restoreIdx : 0;
                        }
                        else
                        {
                            cmbLibLangDrop.SelectedIndex = 0;
                        }
                        cmbLibLangDrop.SelectedIndexChanged += CmbLibLang_SelectedIndexChanged;
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // Keep the UI responsive but explain why the library has not
                // loaded. Avoid repeating the same error on every reconnect.
                if (lastVoiceFetchError != ex.Message)
                {
                    lastVoiceFetchError = ex.Message;
                    WriteLog("Không thể tải Voice Library từ backend: " + ex.Message);
                }
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

            // Voice Library V3: local favorite/tag metadata does not touch voice.json.
            if (_favoritesOnly?.Checked == true)
                filtered = filtered.Where(v => _voiceWorkspace.IsFavorite(v.Id));
            if (!string.IsNullOrWhiteSpace(q))
            {
                string lowerTags = q.Trim();
                // Include entries matching custom tags while preserving other filters.
                // Existing text matches remain supported.
                filtered = allVoices.Where(v =>
                    (v.Name?.Contains(lowerTags,StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.Id?.Contains(lowerTags,StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (v.Description?.Contains(lowerTags,StringComparison.OrdinalIgnoreCase) ?? false) ||
                    _voiceWorkspace.Get(v.Id??"").Tags.Contains(lowerTags,StringComparison.OrdinalIgnoreCase));
                if (_favoritesOnly?.Checked == true)
                    filtered = filtered.Where(v=>_voiceWorkspace.IsFavorite(v.Id));
            }

            // Language filter using comprehensive matching (supports "English (en)", "en", "English", etc.)
            var cmbLibLang = Controls.Find("cmbLibraryLanguage", true).FirstOrDefault() as ComboBox;
            if (cmbLibLang != null && cmbLibLang.SelectedIndex > 0)
            {
                var selectedLang = cmbLibLang.Text?.Trim();
                if (!string.IsNullOrEmpty(selectedLang))
                {
                    filtered = filtered.Where(v => MatchesLanguage(v.Language, selectedLang));
                }
            }

            foreach (var v in filtered)
            {
                int idx = dgv.Rows.Add(v.Name, v.Id, v.Language, v.Gender, v.CreatedAt);
                dgv.Rows[idx].Tag = v;
                if (dgv.Columns.Contains("voiceFavoriteMarker"))
                    dgv.Rows[idx].Cells["voiceFavoriteMarker"].Value = _voiceWorkspace.IsFavorite(v.Id) ? "★" : "☆";
            }

            // Update count label if present
            var lblCount = Controls.Find("lblLibraryCount", true).FirstOrDefault() as Label;
            if (lblCount != null)
                lblCount.Text = $"{dgv.Rows.Count} voices";
        }
    }

    private void CmbLibLang_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var txtSearch = Controls.Find("txtLibrarySearch", true).FirstOrDefault() as TextBox;
        _ = RefreshVoices(txtSearch?.Text, lifetime.Token);
    }

    private static readonly List<string[]> LanguageAliasGroups = new()
    {
        new[] { "en", "english" },
        new[] { "vi", "tiếng việt", "tieng viet", "vietnamese" },
        new[] { "es", "spanish", "español", "espanol" },
        new[] { "fr", "french", "français", "francais" },
        new[] { "de", "german", "deutsch" },
        new[] { "it", "italian", "italiano" },
        new[] { "pt", "portuguese", "português", "portugues" },
        new[] { "ru", "russian", "русский" },
        new[] { "ja", "japanese", "nihongo", "japan" },
        new[] { "ko", "korean", "hangul", "korea" },
        new[] { "zh", "chinese", "mandarin", "china" }
    };

    private static bool MatchesLanguage(string? voiceLang, string? selectedFilter)
    {
        if (string.IsNullOrWhiteSpace(selectedFilter) ||
            selectedFilter.Equals("All Languages", StringComparison.OrdinalIgnoreCase) ||
            selectedFilter.Equals("All", StringComparison.OrdinalIgnoreCase) ||
            selectedFilter.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(voiceLang)) return false;

        voiceLang = voiceLang.Trim();
        selectedFilter = selectedFilter.Trim();

        if (string.Equals(voiceLang, selectedFilter, StringComparison.OrdinalIgnoreCase))
            return true;

        static (string code, string name) ParseLang(string input)
        {
            string code = "";
            string name = input;
            int s = input.LastIndexOf('(');
            int e = input.LastIndexOf(')');
            if (s >= 0 && e > s)
            {
                code = input.Substring(s + 1, e - s - 1).Trim();
                name = input.Substring(0, s).Trim();
            }
            return (code, name);
        }

        var (fCode, fName) = ParseLang(selectedFilter);
        var (vCode, vName) = ParseLang(voiceLang);

        if (!string.IsNullOrEmpty(fCode) && string.Equals(voiceLang, fCode, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(fName) && string.Equals(voiceLang, fName, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!string.IsNullOrEmpty(vCode) && !string.IsNullOrEmpty(fCode) && string.Equals(vCode, fCode, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var group in LanguageAliasGroups)
        {
            bool filterInGroup = group.Any(g =>
                string.Equals(g, selectedFilter, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(fCode) && string.Equals(g, fCode, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(fName) && string.Equals(g, fName, StringComparison.OrdinalIgnoreCase))
            );

            if (filterInGroup)
            {
                bool voiceInGroup = group.Any(g =>
                    string.Equals(g, voiceLang, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(vCode) && string.Equals(g, vCode, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(vName) && string.Equals(g, vName, StringComparison.OrdinalIgnoreCase))
                );

                if (voiceInGroup) return true;
            }
        }

        return false;
    }

    // Route TTS according to the selected voice's declared language.
    // Do not guess for unknown languages: avoid silently using the wrong model.
    private void ApplyVoiceLanguageAndEngine(VoiceDto? voice)
    {
        if (voice == null || string.IsNullOrWhiteSpace(voice.Language)) return;

        string? selectedLanguage = null;
        foreach (var item in languageComboBox.Items)
        {
            string? display = item?.ToString();
            if (!string.IsNullOrWhiteSpace(display) && display != "Auto" &&
                MatchesLanguage(voice.Language, display))
            {
                selectedLanguage = display;
                break;
            }
        }
        if (selectedLanguage == null)
        {
            WriteLog($"No supported language entry for voice {voice.Name}: {voice.Language}");
            return;
        }

        bool isVietnamese = MatchesLanguage(voice.Language, "Vietnamese");
        string engine = isVietnamese ? "vieneu_v3_turbo" : "chatterbox_multilingual";
        if (engineComboBox.Items.Contains(engine))
            engineComboBox.SelectedItem = engine;
        languageComboBox.SelectedItem = selectedLanguage;
    }
    private readonly ToolTip engineSettingsTips = new() { AutoPopDelay = 12000 };

    private void UpdateEngineSettingsUI()
    {
        // VieNeu inference accepts temperature; playback speed is FFmpeg.
        // Its worker does not support CFG, exaggeration, denoise or speaker boost.
        bool isVieNeu = string.Equals(engineComboBox.SelectedItem?.ToString(),
            "vieneu_v3_turbo", StringComparison.OrdinalIgnoreCase);
        foreach (Control control in new Control[] {
            lblStability, numStability, lblStyle, numStyle,
            lblDenoise, numDenoise, chkSpeakerBoost })
        {
            control.Enabled = !isVieNeu;
            engineSettingsTips.SetToolTip(control, isVieNeu
                ? "Chỉ hỗ trợ Chatterbox, không áp dụng cho VieNeu." : "");
        }
        engineSettingsTips.SetToolTip(numSimilarity, isVieNeu
            ? "Temperature gửi trực tiếp cho VieNeu. Giữ nguyên 65 nếu muốn dùng cấu hình đã thử."
            : "Temperature gửi cho Chatterbox.");
        engineSettingsTips.SetToolTip(numSpeed,
            "Speed được FFmpeg áp dụng sau khi tạo giọng.");
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
        using var dialog = new OpenFileDialog { Filter = "Văn bản TXT (*.txt)|*.txt", Title = "Nhập văn bản TTS" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { ImportBatchText(dialog.FileName); }
        catch(Exception ex){ShowError(ex);}
    }

    private void ImportBatchText(string source)
    {
        var dgv = Controls.Find("dgvJobs", true).OfType<DataGridView>().First();
        bool autoSplit = (Controls.Find("chkAutoSplit", true).FirstOrDefault() as CheckBox)?.Checked == true;
        string splitChars = (Controls.Find("txtSplitChars", true).FirstOrDefault() as TextBox)?.Text ?? ".!?\n";
        var text = File.ReadAllText(source);
        var segments = autoSplit && !string.IsNullOrEmpty(splitChars)
            ? SmartSplit(text, splitChars, userSettings.MaxChars)
            : text.Split(new[] { "\r\n", "\n" },StringSplitOptions.RemoveEmptyEntries)
                  .Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
        dgv.Rows.Clear();
        int id=1;
        foreach(var line in segments)
            dgv.Rows.Add((id++).ToString("D3"),"","",line,
                voiceComboBox.Text,languageComboBox.Text,"Pending",
                BatchOutputNaming.FromSourcePath(source));
        if(_importedSrtInfo != null) _importedSrtInfo.Text=$"TXT: {id-1} câu";
        WriteLog($"Đã nhập {id-1} câu TXT: {Path.GetFileName(source)}");
    }

    private void ImportTimedSrt(string source)
    {
        var cues=SrtQualityService.Read(File.ReadAllText(source));
        if(cues.Count == 0)throw new InvalidDataException("SRT không có câu hợp lệ.");
        for(int i=0;i<cues.Count;i++)
        {
            if(cues[i].Start<TimeSpan.Zero || (i>0 && cues[i].Start<cues[i-1].End))
                throw new InvalidDataException($"SRT tại câu {i+1} bị trùng/lùi thời gian.");
        }
        var dgv = Controls.Find("dgvJobs", true).OfType<DataGridView>().First();
        dgv.Rows.Clear();
        string sourceName=Path.GetFileNameWithoutExtension(source);
        for(int i=0;i<cues.Count;i++)
        {
            var cue=cues[i];
            int id=dgv.Rows.Add((i+1).ToString("D3"),"","",cue.Text,
                voiceComboBox.Text,languageComboBox.Text,"Pending",sourceName);
            var row=dgv.Rows[id];
            row.Tag=cue;
            row.Cells["SrtStart"].Value=cue.Start.ToString(@"hh\:mm\:ss\,fff");
            row.Cells["SrtEnd"].Value=cue.End.ToString(@"hh\:mm\:ss\,fff");
        }
        double gaps=cues[0].Start.TotalSeconds;
        for(int i=1;i<cues.Count;i++)
            gaps+=(cues[i].Start-cues[i-1].End).TotalSeconds;
        if(_importedSrtInfo != null)
            _importedSrtInfo.Text=$"SRT: {cues.Count} câu | Nghỉ {gaps:F1}s";
        WriteLog($"Đã nhập SRT: {cues.Count} câu, {gaps:F2}s nghỉ. Mốc thời gian giữ nguyên.");
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
            TtsGenerationSettings? settingsReq = new TtsGenerationSettings(
                Speed: (double)numSpeed.Value,
                CfgWeight: (double)numStability.Value / 100.0,
                Temperature: (double)numSimilarity.Value / 100.0,
                Exaggeration: (double)numStyle.Value / 100.0,
                SpeakerBoost: chkSpeakerBoost.Checked,
                Seed: null,
                Denoise: (double)numDenoise.Value / 100.0
            );
            if ((Controls.Find("chkChangeVoiceSettings", true).FirstOrDefault() as CheckBox)?.Checked == false)
                settingsReq = null;
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
            string rawBitrate = userSettings.Mp3Bitrate;
            string cleanBitrate = System.Text.RegularExpressions.Regex.Match(rawBitrate ?? "", @"\d+").Value;
            if (string.IsNullOrEmpty(cleanBitrate)) cleanBitrate = "192";
            cleanBitrate += "k";

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                var statusStr = row.Cells[6].Value?.ToString();
                if (statusStr == "Completed") continue;
                if (statusStr == "Failed" && !retryFailed) continue;

                var safeVoiceName = string.Join("_", voiceComboBox.Text.Split(Path.GetInvalidFileNameChars()));
                var filename = $"{row.Cells[0].Value}_{safeVoiceName}.{ext}";
                var path = Path.Combine(txtFolder.Text, filename);

                if (skipExisting && File.Exists(path))
                {
                    double dur = await GetAudioDurationSecs(path);
                    if (dur > 0.05)
                    {
                        row.Cells[1].Value = filename;
                        row.Cells[2].Value = dur.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        row.Cells[6].Value = "Completed";
                        WriteLog($"Segment {row.Cells[0].Value} skipped (valid audio exists).");
                        Interlocked.Increment(ref done);
                        continue;
                    }
                    WriteLog($"Segment {row.Cells[0].Value}: existing audio is invalid; generating again.");
                }

                var content = row.Cells[3].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(content))
                {
                    row.Cells[6].Value = "Skipped";
                    Interlocked.Increment(ref done);
                    continue;
                }

                row.Cells[6].Value = "Pending";
                string rowLang = row.Cells[5].Value?.ToString() ?? "";
                string segLang = !string.IsNullOrWhiteSpace(rowLang) ? GetLangCode(rowLang) : lang;
                var req = new GenerateRequest(content, voiceId, segLang, engineTxt, settingsReq);
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
                                var audioBytes = await api.DownloadAudioAsync(jobId, token);
                                // Write and verify an output temp file first; never mark a
                                // truncated MP3/WAV as Completed and never leave one as
                                // a candidate for "Skip Existing" on the next run.
                                var tempWav = path + ".tmp.wav";
                                var tempOutput = path + (ext == "mp3" ? ".tmp.mp3" : ".tmp.wav");
                                try
                                {
                                    if (ext == "mp3")
                                    {
                                        await File.WriteAllBytesAsync(tempWav, audioBytes, token);
                                        var psi = new ProcessStartInfo("ffmpeg")
                                        {
                                            Arguments = $"-y -i \"{tempWav}\" -b:a {cleanBitrate} \"{tempOutput}\"",
                                            UseShellExecute = false,
                                            CreateNoWindow = true,
                                            RedirectStandardError = true
                                        };
                                        using var proc = Process.Start(psi)
                                            ?? throw new InvalidOperationException("Cannot start FFmpeg.");
                                        var err = await proc.StandardError.ReadToEndAsync(token);
                                        await proc.WaitForExitAsync(token);
                                        if (proc.ExitCode != 0 || !File.Exists(tempOutput))
                                            throw new InvalidOperationException($"FFmpeg conversion failed: {err}");
                                    }
                                    else
                                    {
                                        await File.WriteAllBytesAsync(tempOutput, audioBytes, token);
                                    }
                                    double dur = await GetAudioDurationSecs(tempOutput);
                                    if (dur <= 0.05)
                                        throw new InvalidDataException("Audio generated was empty or invalid.");
                                    File.Move(tempOutput, path, overwrite: true);
                                    try { await api.CancelJobAsync(jobId, lifetime.Token); } catch { }
                                    Invoke(() => {
                                        row.Cells[1].Value = filename;
                                        row.Cells[2].Value = dur.ToString(System.Globalization.CultureInfo.InvariantCulture);
                                        row.Cells[6].Value = "Completed";
                                    });
                                }
                                finally
                                {
                                    try { File.Delete(tempWav); } catch { }
                                    try { File.Delete(tempOutput); } catch { }
                                }
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
            WriteLog("Batch cancelled. A running AI inference may still need time to finish safely; do not force-close Python.");
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
            // SRT-imported rows carry original cue times in their Tag.
            // Do not use the ordinary seamless MP3 concatenation for them.
            if(dgv.Rows.Cast<DataGridViewRow>().Any(row=>row.Tag is SubtitleCue))
            {
                await JoinTimedSrtAudioAsync(txtFolder.Text,dgv,lifetime.Token);
                return;
            }
            var groups = new Dictionary<string, List<string>>();
            string ext = userSettings.OutputFormat.ToLower() == "mp3" ? "mp3" : "wav";

            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var row = dgv.Rows[i];
                var filename = row.Cells[1].Value?.ToString();
                if (string.IsNullOrWhiteSpace(filename)) continue;

                var path = Path.Combine(txtFolder.Text, filename);
                if (!File.Exists(path)) continue;

                string sourceFile=BatchOutputNaming.SafeStem(
                    row.Cells[7].Value?.ToString());

                if (!groups.ContainsKey(sourceFile)) groups[sourceFile] = new List<string>();
                groups[sourceFile].Add(path);
            }

            if (groups.Count == 0) return;

            foreach (var group in groups)
            {
                var sourceName = group.Key;
                var files = group.Value;
                if (files.Count == 0) continue;

                var listFile = Path.Combine(txtFolder.Text,
                    "file_list_" + Guid.NewGuid().ToString("N") + ".txt");
                var outputAudio = BatchOutputNaming.AvailablePath(
                    txtFolder.Text,sourceName,ext);
                var tempOutput = Path.Combine(txtFolder.Text,
                    "." + Guid.NewGuid().ToString("N") + ".tmp." + ext);
                try
                {
                    // Concat input entries may contain single quotes in voice names.
                    var lines=files.Select(f =>
                        "file '" + Path.GetFileName(f).Replace("'", "'\\''") + "'").ToArray();
                    await File.WriteAllLinesAsync(listFile,lines,lifetime.Token);
                    var psi = new ProcessStartInfo("ffmpeg")
                    {
                        WorkingDirectory=txtFolder.Text,
                        UseShellExecute=false,
                        CreateNoWindow=true,
                        RedirectStandardError=true
                    };
                    foreach(var arg in new[]{"-nostdin","-n","-f","concat","-safe","0",
                        "-i",listFile,"-c","copy",tempOutput})
                        psi.ArgumentList.Add(arg);
                    using var process = Process.Start(psi) ??
                        throw new InvalidOperationException("Không thể khởi chạy FFmpeg.");
                    var errTask=process.StandardError.ReadToEndAsync(lifetime.Token);
                    await process.WaitForExitAsync(lifetime.Token);
                    var err=await errTask;
                    if(process.ExitCode!=0 || !File.Exists(tempOutput) ||
                       new FileInfo(tempOutput).Length==0)
                        throw new InvalidDataException(
                            $"Ghép audio {sourceName} thất bại: {err}");
                    File.Move(tempOutput,outputAudio,overwrite:false);
                    WriteLog($"Đã ghép {files.Count} đoạn thành: {Path.GetFileName(outputAudio)}");
                }
                finally
                {
                    try{File.Delete(listFile);}catch{}
                    try{File.Delete(tempOutput);}catch{}
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
        if (!File.Exists(path) || new FileInfo(path).Length < 64) return 0;
        // An MP3 file cannot be validated by treating its byte length as PCM.
        // ffprobe is authoritative when available.
        try
        {
            var psi = new ProcessStartInfo("ffprobe")
            {
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var outputTask = proc.StandardOutput.ReadToEndAsync();
                var errorTask = proc.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try { await proc.WaitForExitAsync(timeout.Token); }
                catch (OperationCanceledException)
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    return 0;
                }
                var text = await outputTask;
                await errorTask;
                if (proc.ExitCode == 0 &&
                    double.TryParse(text.Trim(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double seconds) &&
                    double.IsFinite(seconds) && seconds > 0.05)
                    return seconds;
            }
        }
        catch (System.ComponentModel.Win32Exception) { /* ffprobe not installed */ }
        catch (IOException) { return 0; }

        // Without ffprobe, validate the RIFF/WAVE header and data chunk.
        // MP3 requires ffprobe; do NOT guess its duration from file size.
        if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return 0;
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(stream);
            if (new string(reader.ReadChars(4)) != "RIFF") return 0;
            reader.ReadUInt32();
            if (new string(reader.ReadChars(4)) != "WAVE") return 0;
            uint byteRate = 0;
            while (stream.Position + 8 <= stream.Length)
            {
                string id = new string(reader.ReadChars(4));
                uint length = reader.ReadUInt32();
                long next = stream.Position + length + (length % 2);
                if (next > stream.Length) return 0;
                if (id == "fmt " && length >= 16)
                {
                    ushort encoding = reader.ReadUInt16();
                    reader.ReadUInt16(); reader.ReadUInt32();
                    byteRate = reader.ReadUInt32();
                    if (encoding != 1) return 0; // Only accept uncompressed PCM fallback.
                }
                if (id == "data" && byteRate != 0)
                    return length / (double)byteRate;
                stream.Position = next;
            }
        }
        catch (IOException) { }
        return 0;
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

            string sourceFile=BatchOutputNaming.SafeStem(
                row.Cells[7].Value?.ToString());

            if (!groups.ContainsKey(sourceFile)) groups[sourceFile] = new List<DataGridViewRow>();
            groups[sourceFile].Add(row);
        }

        foreach (var group in groups)
        {
            var sourceName = group.Key;
            string srtPath = Path.Combine(txtFolder.Text, $"{sourceName}_subtitles.srt");
            var timedRows=group.Value.Where(x=>x.Tag is SubtitleCue).ToArray();
            if(timedRows.Length>0)
            {
                if(timedRows.Length!=group.Value.Count)
                {
                    WriteLog("Không thể trộn SRT có timeline và TXT trong cùng một nhóm.");
                    continue;
                }
                // Emit precisely the original cue times (including gaps) ONLY
                // when all cues completed and audio is present.
                var expected = dgv.Rows.Cast<DataGridViewRow>()
                    .Where(x=>(x.Cells[7].Value?.ToString()??"")==
                        sourceName)
                    .ToList();
                if(expected.Count!=group.Value.Count)
                {
                    WriteLog($"SRT {sourceName}: còn cue thiếu audio, chưa xuất file đồng bộ.");
                    continue;
                }
                var timed=timedRows
                    .Select(row=>(Row:row,Cue:(SubtitleCue)row.Tag!))
                    .OrderBy(x=>x.Cue.Start)
                    .Select(x=>new TimedAudioCue(x.Cue.Number,x.Cue.Start,x.Cue.End,x.Cue.Text,
                        Path.Combine(txtFolder.Text,x.Row.Cells[1].Value?.ToString()??"")))
                    .ToArray();
                try
                {
                    // Generating the original subtitle timeline does not need
                    // per-cue audio: Del Segments may already have removed it.
                    SrtBatchTimelineService.ValidateTimings(timed);
                    if(File.Exists(srtPath))
                        throw new IOException("SRT đầu ra đã tồn tại: "+srtPath);
                    await File.WriteAllTextAsync(srtPath,
                        SrtBatchTimelineService.BuildOriginalSrt(timed),lifetime.Token);
                    WriteLog($"Đã tạo phụ đề giữ nguyên khoảng nghỉ: {srtPath}");
                }
                catch(Exception ex){WriteLog("Không xuất SRT: "+ex.Message);}
                continue;
            }
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
                string vLang = string.IsNullOrWhiteSpace(v.Language) ? "en" : v.Language;
                var req = new GenerateRequest(txt.Text, v.Id, vLang, engineComboBox.Text, new TtsGenerationSettings(1.0, 0.5, 0.8, 0.5, true, null, 0.0));
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
            ApplyVoiceLanguageAndEngine(v);
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
                    for (int j = 0; j < cmbLang.Items.Count; j++)
                    {
                        var itemStr = cmbLang.Items[j]?.ToString();
                        if (itemStr != null && MatchesLanguage(vm.Language, itemStr))
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
