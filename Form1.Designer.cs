#nullable enable

namespace TTS_Create_By_DvaForDev;

partial class Form1
{
    private System.ComponentModel.IContainer? components = null;

    // =========================================================
    // MAIN
    // =========================================================
    private TabControl mainTabControl = null!;
    private TabPage tabTtsBatch = null!;
    private TabPage tabVoiceLibrary = null!;
    private TabPage tabSettings = null!;

    // =========================================================
    // TTS / BATCH
    // =========================================================
    private TableLayoutPanel ttsRootLayout = null!;
    private TableLayoutPanel ttsTopLayout = null!;
    private TableLayoutPanel batchOptionsLayout = null!;
    private TableLayoutPanel statusLayout = null!;

    private GroupBox grpVoice = null!;
    private Label lblVoiceName = null!;
    private TextBox txtVoiceName = null!;
    private Button btnSearchVoice = null!;
    private Label lblVoice = null!;
    private ComboBox voiceComboBox = null!;
    private Label lblModel = null!;
    private ComboBox engineComboBox = null!;
    private Label lblLanguage = null!;
    private ComboBox languageComboBox = null!;
    private Button btnAddLibrary = null!;
    private Button addVoiceButton = null!;
    private Button btnOpenVoiceLibrary = null!;

    private GroupBox grpVoiceSettings = null!;
    private CheckBox chkVoiceSettings = null!;
    private Label lblSpeed = null!;
    private NumericUpDown numSpeed = null!;
    private Label lblStability = null!;
    private NumericUpDown numStability = null!;
    private Label lblSimilarity = null!;
    private NumericUpDown numSimilarity = null!;
    private Label lblStyle = null!;
    private NumericUpDown numStyle = null!;
    private CheckBox chkSpeakerBoost = null!;
    private Button btnResetVoiceSettings = null!;

    private GroupBox grpSystem = null!;
    private Label lblDevice = null!;
    private ComboBox cmbDevice = null!;
    private Label backendStatusLabel = null!;
    private Label gpuStatusLabel = null!;
    private Label lblCudaStatus = null!;
    private Label lblVramStatus = null!;

    private GroupBox grpBatchJob = null!;
    private Label lblFolder = null!;
    private TextBox txtBatchFolder = null!;
    private Button btnBrowseFolder = null!;
    private CheckBox chkAutoSrt = null!;
    private Button btnRunBatch = null!;
    private TextBox txtBatchInfo = null!;

    private GroupBox grpOptions = null!;
    private CheckBox chkRetryFailed = null!;
    private CheckBox chkAutoSplit = null!;
    private CheckBox chkSkipExisting = null!;
    private CheckBox chkAutoJoin = null!;
    private Label lblSplitChars = null!;
    private TextBox txtSplitChars = null!;
    private Button btnAdvancedSettings = null!;

    private FlowLayoutPanel pnlToolbar = null!;
    private Button generateButton = null!;
    private Button stopButton = null!;
    private Button btnImportFile = null!;
    private Button btnImportFolder = null!;
    private Button btnImportVoice = null!;
    private Button btnOpenAudioOutput = null!;
    private Button btnCreateSrt = null!;
    private Button btnJoinMp3 = null!;
    private Button playButton = null!;
    private Button saveWavButton = null!;

    private Label lblSubtitleStatus = null!;
    private Label lblCurrentJob = null!;
    private ProgressBar progressTotal = null!;
    private DataGridView dgvJobs = null!;
    private TextBox logsTextBox = null!;

    private DataGridViewTextBoxColumn colId = null!;
    private DataGridViewTextBoxColumn colOutput = null!;
    private DataGridViewTextBoxColumn colTiming = null!;
    private DataGridViewTextBoxColumn colContent = null!;
    private DataGridViewTextBoxColumn colVoice = null!;
    private DataGridViewTextBoxColumn colLanguage = null!;
    private DataGridViewTextBoxColumn colStatus = null!;

    // =========================================================
    // VOICE LIBRARY
    // =========================================================
    private TableLayoutPanel libraryRootLayout = null!;
    private TableLayoutPanel libraryHeaderLayout = null!;
    private Label lblLibraryTitle = null!;
    private TextBox txtLibrarySearch = null!;
    private Button btnLibrarySearch = null!;
    private Button btnLibraryRefresh = null!;
    private Button btnLibraryAddVoice = null!;

    private SplitContainer splitVoiceLibrary = null!;
    private DataGridView dgvVoiceLibrary = null!;

    private DataGridViewTextBoxColumn voiceColName = null!;
    private DataGridViewTextBoxColumn voiceColId = null!;
    private DataGridViewTextBoxColumn voiceColLanguage = null!;
    private DataGridViewTextBoxColumn voiceColGender = null!;
    private DataGridViewTextBoxColumn voiceColCreated = null!;

    private TableLayoutPanel libraryRightLayout = null!;

    private GroupBox grpSelectedVoice = null!;
    private TableLayoutPanel selectedVoiceLayout = null!;
    private Label lblSelectedVoiceNameText = null!;
    private Label lblSelectedVoiceName = null!;
    private Label lblSelectedVoiceIdText = null!;
    private TextBox txtSelectedVoiceId = null!;
    private Label lblSelectedVoiceLanguageText = null!;
    private Label lblSelectedVoiceLanguage = null!;
    private Label lblSelectedVoiceGenderText = null!;
    private Label lblSelectedVoiceGender = null!;
    private Label lblSelectedVoiceDescriptionText = null!;
    private TextBox txtSelectedVoiceDescription = null!;
    private Label lblReferenceAudio = null!;
    private TextBox txtReferenceAudio = null!;
    private Button btnPlayReference = null!;
    private Label lblPreviewSentence = null!;
    private TextBox txtPreviewSentence = null!;
    private FlowLayoutPanel pnlVoiceActions = null!;
    private Button btnGeneratePreview = null!;
    private Button btnUseSelectedVoice = null!;
    private Button btnCopyVoiceId = null!;
    private Button btnReplaceReference = null!;
    private Button btnDeleteVoice = null!;

    private GroupBox grpAddVoice = null!;
    private TableLayoutPanel addVoiceLayout = null!;
    private Label lblNewVoiceName = null!;
    private TextBox txtNewVoiceName = null!;
    private Label lblNewVoiceId = null!;
    private TextBox txtNewVoiceId = null!;
    private Label lblNewVoiceLanguage = null!;
    private ComboBox cmbNewVoiceLanguage = null!;
    private Label lblNewVoiceGender = null!;
    private ComboBox cmbNewVoiceGender = null!;
    private Label lblNewVoiceDescription = null!;
    private TextBox txtNewVoiceDescription = null!;
    private Label lblNewVoiceFile = null!;
    private TextBox txtNewVoiceFile = null!;
    private Button btnBrowseVoiceFile = null!;
    private FlowLayoutPanel pnlAddVoiceActions = null!;
    private Button btnPreviewNewVoice = null!;
    private Button btnSaveNewVoice = null!;
    private Button btnClearNewVoice = null!;

    // =========================================================
    // SETTINGS
    // =========================================================
    private GroupBox grpAppSettings = null!;
    private TableLayoutPanel settingsLayout = null!;
    private Label lblOutputFolder = null!;
    private TextBox txtOutputFolder = null!;
    private Button btnBrowseOutputFolder = null!;
    private Label lblMaxChars = null!;
    private NumericUpDown numMaxChars = null!;
    private Label lblSilence = null!;
    private NumericUpDown numSilence = null!;
    private Label lblOutputFormat = null!;
    private ComboBox cmbOutputFormat = null!;
    private Label lblMp3Bitrate = null!;
    private ComboBox cmbMp3Bitrate = null!;
    private Button btnSaveSettings = null!;

    // =========================================================
    // RENDERVIDEO
    // =========================================================
    private TabPage tabVideoEditor = null!;

    // Video editor root
    private TableLayoutPanel videoRootLayout = null!;
    private GroupBox grpVideoSource = null!;
    private GroupBox grpVideoOptions = null!;
    private GroupBox grpVideoPreview = null!;

    // Source
    private Label lblVideoAudio = null!;
    private TextBox txtVideoAudio = null!;
    private Button btnBrowseVideoAudio = null!;

    private Label lblVideoSrt = null!;
    private TextBox txtVideoSrt = null!;
    private Button btnBrowseVideoSrt = null!;

    private Label lblImageFolder = null!;
    private TextBox txtImageFolder = null!;
    private Button btnBrowseImageFolder = null!;

    // Timeline / mapping
    private Label lblTimelineMode = null!;
    private ComboBox cmbTimelineMode = null!;

    private Label lblImageMapping = null!;
    private ComboBox cmbImageMapping = null!;

    // Render options
    private Label lblResolution = null!;
    private ComboBox cmbResolution = null!;

    private Label lblFps = null!;
    private ComboBox cmbFps = null!;

    private Label lblAspectRatio = null!;
    private ComboBox cmbAspectRatio = null!;

    private Label lblEncoder = null!;
    private ComboBox cmbEncoder = null!;

    private Label lblVideoQuality = null!;
    private ComboBox cmbVideoQuality = null!;

    // Effects
    private CheckBox chkKenBurns = null!;
    private CheckBox chkCrossfade = null!;
    private CheckBox chkBurnSubtitle = null!;
    private CheckBox chkHardwareEncoding = null!;

    private Label lblTransitionDuration = null!;
    private NumericUpDown numTransitionDuration = null!;

    // Buttons
    private Button btnAnalyzeTimeline = null!;
    private Button btnPreviewVideo = null!;
    private Button btnRenderVideo = null!;
    private Button btnOpenVideoOutput = null!;

    // Timeline grid
    private DataGridView dgvVideoTimeline = null!;

    private DataGridViewTextBoxColumn videoColId = null!;
    private DataGridViewTextBoxColumn videoColStart = null!;
    private DataGridViewTextBoxColumn videoColEnd = null!;
    private DataGridViewTextBoxColumn videoColDuration = null!;
    private DataGridViewTextBoxColumn videoColImage = null!;
    private DataGridViewTextBoxColumn videoColSubtitle = null!;
    private DataGridViewTextBoxColumn videoColStatus = null!;

    // Bottom status
    private ProgressBar progressVideoRender = null!;
    private Label lblVideoRenderStatus = null!;



    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        mainTabControl = new TabControl();
        tabTtsBatch = new TabPage();
        ttsRootLayout = new TableLayoutPanel();
        ttsTopLayout = new TableLayoutPanel();
        grpVoice = new GroupBox();
        lblVoiceName = new Label();
        txtVoiceName = new TextBox();
        btnSearchVoice = new Button();
        lblVoice = new Label();
        voiceComboBox = new ComboBox();
        lblModel = new Label();
        engineComboBox = new ComboBox();
        lblLanguage = new Label();
        languageComboBox = new ComboBox();
        btnAddLibrary = new Button();
        addVoiceButton = new Button();
        btnOpenVoiceLibrary = new Button();
        grpVoiceSettings = new GroupBox();
        chkVoiceSettings = new CheckBox();
        lblSpeed = new Label();
        numSpeed = new NumericUpDown();
        lblStability = new Label();
        numStability = new NumericUpDown();
        lblSimilarity = new Label();
        numSimilarity = new NumericUpDown();
        lblStyle = new Label();
        numStyle = new NumericUpDown();
        chkSpeakerBoost = new CheckBox();
        btnResetVoiceSettings = new Button();
        grpSystem = new GroupBox();
        lblDevice = new Label();
        cmbDevice = new ComboBox();
        backendStatusLabel = new Label();
        gpuStatusLabel = new Label();
        lblCudaStatus = new Label();
        lblVramStatus = new Label();
        batchOptionsLayout = new TableLayoutPanel();
        grpBatchJob = new GroupBox();
        lblFolder = new Label();
        txtBatchFolder = new TextBox();
        btnBrowseFolder = new Button();
        chkAutoSrt = new CheckBox();
        btnRunBatch = new Button();
        txtBatchInfo = new TextBox();
        grpOptions = new GroupBox();
        chkRetryFailed = new CheckBox();
        chkAutoSplit = new CheckBox();
        chkSkipExisting = new CheckBox();
        chkAutoJoin = new CheckBox();
        lblSplitChars = new Label();
        txtSplitChars = new TextBox();
        btnAdvancedSettings = new Button();
        pnlToolbar = new FlowLayoutPanel();
        generateButton = new Button();
        stopButton = new Button();
        btnImportFile = new Button();
        btnImportFolder = new Button();
        btnImportVoice = new Button();
        btnOpenAudioOutput = new Button();
        btnCreateSrt = new Button();
        btnJoinMp3 = new Button();
        playButton = new Button();
        saveWavButton = new Button();
        statusLayout = new TableLayoutPanel();
        lblSubtitleStatus = new Label();
        lblCurrentJob = new Label();
        progressTotal = new ProgressBar();
        dgvJobs = new DataGridView();
        colId = new DataGridViewTextBoxColumn();
        colOutput = new DataGridViewTextBoxColumn();
        colTiming = new DataGridViewTextBoxColumn();
        colContent = new DataGridViewTextBoxColumn();
        colVoice = new DataGridViewTextBoxColumn();
        colLanguage = new DataGridViewTextBoxColumn();
        colStatus = new DataGridViewTextBoxColumn();
        logsTextBox = new TextBox();
        tabVoiceLibrary = new TabPage();
        libraryRootLayout = new TableLayoutPanel();
        libraryHeaderLayout = new TableLayoutPanel();
        lblLibraryTitle = new Label();
        txtLibrarySearch = new TextBox();
        btnLibrarySearch = new Button();
        btnLibraryRefresh = new Button();
        btnLibraryAddVoice = new Button();
        splitVoiceLibrary = new SplitContainer();
        dgvVoiceLibrary = new DataGridView();
        voiceColName = new DataGridViewTextBoxColumn();
        voiceColId = new DataGridViewTextBoxColumn();
        voiceColLanguage = new DataGridViewTextBoxColumn();
        voiceColGender = new DataGridViewTextBoxColumn();
        voiceColCreated = new DataGridViewTextBoxColumn();
        libraryRightLayout = new TableLayoutPanel();
        grpSelectedVoice = new GroupBox();
        selectedVoiceLayout = new TableLayoutPanel();
        lblSelectedVoiceNameText = new Label();
        lblSelectedVoiceName = new Label();
        lblSelectedVoiceIdText = new Label();
        txtSelectedVoiceId = new TextBox();
        lblSelectedVoiceLanguageText = new Label();
        lblSelectedVoiceLanguage = new Label();
        lblSelectedVoiceGenderText = new Label();
        lblSelectedVoiceGender = new Label();
        lblSelectedVoiceDescriptionText = new Label();
        txtSelectedVoiceDescription = new TextBox();
        lblReferenceAudio = new Label();
        txtReferenceAudio = new TextBox();
        btnPlayReference = new Button();
        lblPreviewSentence = new Label();
        txtPreviewSentence = new TextBox();
        pnlVoiceActions = new FlowLayoutPanel();
        btnGeneratePreview = new Button();
        btnUseSelectedVoice = new Button();
        btnCopyVoiceId = new Button();
        btnReplaceReference = new Button();
        btnDeleteVoice = new Button();
        grpAddVoice = new GroupBox();
        addVoiceLayout = new TableLayoutPanel();
        lblNewVoiceName = new Label();
        txtNewVoiceName = new TextBox();
        lblNewVoiceId = new Label();
        txtNewVoiceId = new TextBox();
        lblNewVoiceLanguage = new Label();
        cmbNewVoiceLanguage = new ComboBox();
        lblNewVoiceGender = new Label();
        cmbNewVoiceGender = new ComboBox();
        lblNewVoiceDescription = new Label();
        txtNewVoiceDescription = new TextBox();
        lblNewVoiceFile = new Label();
        txtNewVoiceFile = new TextBox();
        btnBrowseVoiceFile = new Button();
        pnlAddVoiceActions = new FlowLayoutPanel();
        btnPreviewNewVoice = new Button();
        btnSaveNewVoice = new Button();
        btnClearNewVoice = new Button();
        tabVideoEditor = new TabPage();
        videoRootLayout = new TableLayoutPanel();
        grpVideoSource = new GroupBox();
        lblVideoAudio = new Label();
        txtVideoAudio = new TextBox();
        btnBrowseVideoAudio = new Button();
        lblVideoSrt = new Label();
        txtVideoSrt = new TextBox();
        btnBrowseVideoSrt = new Button();
        lblImageFolder = new Label();
        txtImageFolder = new TextBox();
        btnBrowseImageFolder = new Button();
        lblTimelineMode = new Label();
        cmbTimelineMode = new ComboBox();
        lblImageMapping = new Label();
        cmbImageMapping = new ComboBox();
        btnAnalyzeTimeline = new Button();
        grpVideoOptions = new GroupBox();
        lblResolution = new Label();
        cmbResolution = new ComboBox();
        lblFps = new Label();
        cmbFps = new ComboBox();
        lblAspectRatio = new Label();
        cmbAspectRatio = new ComboBox();
        lblEncoder = new Label();
        cmbEncoder = new ComboBox();
        lblVideoQuality = new Label();
        cmbVideoQuality = new ComboBox();
        chkKenBurns = new CheckBox();
        chkCrossfade = new CheckBox();
        chkBurnSubtitle = new CheckBox();
        chkHardwareEncoding = new CheckBox();
        lblTransitionDuration = new Label();
        numTransitionDuration = new NumericUpDown();
        lblTransitionSeconds = new Label();
        grpVideoPreview = new GroupBox();
        dgvVideoTimeline = new DataGridView();
        videoColId = new DataGridViewTextBoxColumn();
        videoColStart = new DataGridViewTextBoxColumn();
        videoColEnd = new DataGridViewTextBoxColumn();
        videoColDuration = new DataGridViewTextBoxColumn();
        videoColImage = new DataGridViewTextBoxColumn();
        videoColSubtitle = new DataGridViewTextBoxColumn();
        videoColStatus = new DataGridViewTextBoxColumn();
        pnlVideoBottom = new TableLayoutPanel();
        lblVideoRenderStatus = new Label();
        btnPreviewVideo = new Button();
        btnRenderVideo = new Button();
        btnOpenVideoOutput = new Button();
        progressVideoRender = new ProgressBar();
        tabSettings = new TabPage();
        grpAppSettings = new GroupBox();
        settingsLayout = new TableLayoutPanel();
        lblOutputFolder = new Label();
        txtOutputFolder = new TextBox();
        btnBrowseOutputFolder = new Button();
        lblMaxChars = new Label();
        numMaxChars = new NumericUpDown();
        lblSilence = new Label();
        numSilence = new NumericUpDown();
        lblOutputFormat = new Label();
        cmbOutputFormat = new ComboBox();
        lblMp3Bitrate = new Label();
        cmbMp3Bitrate = new ComboBox();
        btnSaveSettings = new Button();
        mainTabControl.SuspendLayout();
        tabTtsBatch.SuspendLayout();
        ttsRootLayout.SuspendLayout();
        ttsTopLayout.SuspendLayout();
        grpVoice.SuspendLayout();
        grpVoiceSettings.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numSpeed).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStability).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numSimilarity).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numStyle).BeginInit();
        grpSystem.SuspendLayout();
        batchOptionsLayout.SuspendLayout();
        grpBatchJob.SuspendLayout();
        grpOptions.SuspendLayout();
        pnlToolbar.SuspendLayout();
        statusLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvJobs).BeginInit();
        tabVoiceLibrary.SuspendLayout();
        libraryRootLayout.SuspendLayout();
        libraryHeaderLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitVoiceLibrary).BeginInit();
        splitVoiceLibrary.Panel1.SuspendLayout();
        splitVoiceLibrary.Panel2.SuspendLayout();
        splitVoiceLibrary.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvVoiceLibrary).BeginInit();
        libraryRightLayout.SuspendLayout();
        grpSelectedVoice.SuspendLayout();
        selectedVoiceLayout.SuspendLayout();
        pnlVoiceActions.SuspendLayout();
        grpAddVoice.SuspendLayout();
        addVoiceLayout.SuspendLayout();
        pnlAddVoiceActions.SuspendLayout();
        tabVideoEditor.SuspendLayout();
        videoRootLayout.SuspendLayout();
        grpVideoSource.SuspendLayout();
        grpVideoOptions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numTransitionDuration).BeginInit();
        grpVideoPreview.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dgvVideoTimeline).BeginInit();
        pnlVideoBottom.SuspendLayout();
        tabSettings.SuspendLayout();
        grpAppSettings.SuspendLayout();
        settingsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numMaxChars).BeginInit();
        ((System.ComponentModel.ISupportInitialize)numSilence).BeginInit();
        SuspendLayout();
        // 
        // mainTabControl
        // 
        mainTabControl.Controls.Add(tabTtsBatch);
        mainTabControl.Controls.Add(tabVoiceLibrary);
        mainTabControl.Controls.Add(tabVideoEditor);
        mainTabControl.Controls.Add(tabSettings);
        mainTabControl.Dock = DockStyle.Fill;
        mainTabControl.Location = new Point(0, 0);
        mainTabControl.Name = "mainTabControl";
        mainTabControl.Padding = new Point(18, 6);
        mainTabControl.SelectedIndex = 0;
        mainTabControl.Size = new Size(1280, 800);
        mainTabControl.TabIndex = 0;
        // 
        // tabTtsBatch
        // 
        tabTtsBatch.BackColor = Color.FromArgb(245, 247, 250);
        tabTtsBatch.Controls.Add(ttsRootLayout);
        tabTtsBatch.Location = new Point(4, 30);
        tabTtsBatch.Name = "tabTtsBatch";
        tabTtsBatch.Padding = new Padding(8);
        tabTtsBatch.Size = new Size(1272, 766);
        tabTtsBatch.TabIndex = 0;
        tabTtsBatch.Text = "TTS / Batch";
        // 
        // ttsRootLayout
        // 
        ttsRootLayout.ColumnCount = 1;
        ttsRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        ttsRootLayout.Controls.Add(ttsTopLayout, 0, 0);
        ttsRootLayout.Controls.Add(batchOptionsLayout, 0, 1);
        ttsRootLayout.Controls.Add(pnlToolbar, 0, 2);
        ttsRootLayout.Controls.Add(statusLayout, 0, 3);
        ttsRootLayout.Controls.Add(dgvJobs, 0, 4);
        ttsRootLayout.Controls.Add(logsTextBox, 0, 5);
        ttsRootLayout.Dock = DockStyle.Fill;
        ttsRootLayout.Location = new Point(8, 8);
        ttsRootLayout.Name = "ttsRootLayout";
        ttsRootLayout.RowCount = 6;
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 165F));
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F));
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ttsRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
        ttsRootLayout.Size = new Size(1256, 750);
        ttsRootLayout.TabIndex = 0;
        // 
        // ttsTopLayout
        // 
        ttsTopLayout.ColumnCount = 3;
        ttsTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46F));
        ttsTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        ttsTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        ttsTopLayout.Controls.Add(grpVoice, 0, 0);
        ttsTopLayout.Controls.Add(grpVoiceSettings, 1, 0);
        ttsTopLayout.Controls.Add(grpSystem, 2, 0);
        ttsTopLayout.Dock = DockStyle.Fill;
        ttsTopLayout.Location = new Point(3, 3);
        ttsTopLayout.Name = "ttsTopLayout";
        ttsTopLayout.RowCount = 1;
        ttsTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        ttsTopLayout.Size = new Size(1250, 159);
        ttsTopLayout.TabIndex = 0;
        // 
        // grpVoice
        // 
        grpVoice.BackColor = Color.White;
        grpVoice.Controls.Add(lblVoiceName);
        grpVoice.Controls.Add(txtVoiceName);
        grpVoice.Controls.Add(btnSearchVoice);
        grpVoice.Controls.Add(lblVoice);
        grpVoice.Controls.Add(voiceComboBox);
        grpVoice.Controls.Add(lblModel);
        grpVoice.Controls.Add(engineComboBox);
        grpVoice.Controls.Add(lblLanguage);
        grpVoice.Controls.Add(languageComboBox);
        grpVoice.Controls.Add(btnAddLibrary);
        grpVoice.Controls.Add(addVoiceButton);
        grpVoice.Controls.Add(btnOpenVoiceLibrary);
        grpVoice.Dock = DockStyle.Fill;
        grpVoice.Location = new Point(0, 0);
        grpVoice.Margin = new Padding(0, 0, 6, 4);
        grpVoice.Name = "grpVoice";
        grpVoice.Size = new Size(569, 155);
        grpVoice.TabIndex = 0;
        grpVoice.TabStop = false;
        grpVoice.Text = "Voice";
        // 
        // lblVoiceName
        // 
        lblVoiceName.AutoSize = true;
        lblVoiceName.Location = new Point(14, 30);
        lblVoiceName.Name = "lblVoiceName";
        lblVoiceName.Size = new Size(42, 15);
        lblVoiceName.TabIndex = 0;
        lblVoiceName.Text = "Name:";
        // 
        // txtVoiceName
        // 
        txtVoiceName.Location = new Point(64, 26);
        txtVoiceName.Name = "txtVoiceName";
        txtVoiceName.Size = new Size(200, 23);
        txtVoiceName.TabIndex = 1;
        // 
        // btnSearchVoice
        // 
        btnSearchVoice.Location = new Point(270, 25);
        btnSearchVoice.Name = "btnSearchVoice";
        btnSearchVoice.Size = new Size(75, 25);
        btnSearchVoice.TabIndex = 2;
        btnSearchVoice.Text = "Search";
        btnSearchVoice.UseVisualStyleBackColor = true;
        // 
        // lblVoice
        // 
        lblVoice.AutoSize = true;
        lblVoice.Location = new Point(357, 30);
        lblVoice.Name = "lblVoice";
        lblVoice.Size = new Size(38, 15);
        lblVoice.TabIndex = 3;
        lblVoice.Text = "Voice:";
        // 
        // voiceComboBox
        // 
        voiceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        voiceComboBox.FormattingEnabled = true;
        voiceComboBox.Location = new Point(402, 26);
        voiceComboBox.Name = "voiceComboBox";
        voiceComboBox.Size = new Size(155, 23);
        voiceComboBox.TabIndex = 4;
        // 
        // lblModel
        // 
        lblModel.AutoSize = true;
        lblModel.Location = new Point(357, 63);
        lblModel.Name = "lblModel";
        lblModel.Size = new Size(44, 15);
        lblModel.TabIndex = 5;
        lblModel.Text = "Model:";
        // 
        // engineComboBox
        // 
        engineComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        engineComboBox.FormattingEnabled = true;
        engineComboBox.Items.AddRange(new object[] { "chatterbox_multilingual", "qwen3_tts", "kokoro" });
        engineComboBox.Location = new Point(402, 59);
        engineComboBox.Name = "engineComboBox";
        engineComboBox.Size = new Size(155, 23);
        engineComboBox.TabIndex = 6;
        // 
        // lblLanguage
        // 
        lblLanguage.AutoSize = true;
        lblLanguage.Location = new Point(333, 96);
        lblLanguage.Name = "lblLanguage";
        lblLanguage.Size = new Size(62, 15);
        lblLanguage.TabIndex = 7;
        lblLanguage.Text = "Language:";
        // 
        // languageComboBox
        // 
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.FormattingEnabled = true;
        languageComboBox.Items.AddRange(new object[] { "Auto", "en", "vi", "es", "fr", "de", "it", "pt", "ru", "ja", "ko", "zh" });
        languageComboBox.Location = new Point(402, 92);
        languageComboBox.Name = "languageComboBox";
        languageComboBox.Size = new Size(155, 23);
        languageComboBox.TabIndex = 8;
        // 
        // btnAddLibrary
        // 
        btnAddLibrary.BackColor = Color.White;
        btnAddLibrary.FlatStyle = FlatStyle.Flat;
        btnAddLibrary.Location = new Point(14, 92);
        btnAddLibrary.Name = "btnAddLibrary";
        btnAddLibrary.Size = new Size(105, 30);
        btnAddLibrary.TabIndex = 9;
        btnAddLibrary.Text = "+ Add to Library";
        btnAddLibrary.UseVisualStyleBackColor = false;
        // 
        // addVoiceButton
        // 
        addVoiceButton.BackColor = Color.FromArgb(246, 235, 255);
        addVoiceButton.FlatStyle = FlatStyle.Flat;
        addVoiceButton.ForeColor = Color.FromArgb(128, 54, 172);
        addVoiceButton.Location = new Point(125, 92);
        addVoiceButton.Name = "addVoiceButton";
        addVoiceButton.Size = new Size(105, 30);
        addVoiceButton.TabIndex = 10;
        addVoiceButton.Text = "Import Voice";
        addVoiceButton.UseVisualStyleBackColor = false;
        addVoiceButton.Click += AddVoiceButton_Click;
        // 
        // btnOpenVoiceLibrary
        // 
        btnOpenVoiceLibrary.Location = new Point(236, 92);
        btnOpenVoiceLibrary.Name = "btnOpenVoiceLibrary";
        btnOpenVoiceLibrary.Size = new Size(95, 30);
        btnOpenVoiceLibrary.TabIndex = 11;
        btnOpenVoiceLibrary.Text = "Voice Library";
        btnOpenVoiceLibrary.UseVisualStyleBackColor = true;
        // 
        // grpVoiceSettings
        // 
        grpVoiceSettings.BackColor = Color.White;
        grpVoiceSettings.Controls.Add(chkVoiceSettings);
        grpVoiceSettings.Controls.Add(lblSpeed);
        grpVoiceSettings.Controls.Add(numSpeed);
        grpVoiceSettings.Controls.Add(lblStability);
        grpVoiceSettings.Controls.Add(numStability);
        grpVoiceSettings.Controls.Add(lblSimilarity);
        grpVoiceSettings.Controls.Add(numSimilarity);
        grpVoiceSettings.Controls.Add(lblStyle);
        grpVoiceSettings.Controls.Add(numStyle);
        grpVoiceSettings.Controls.Add(chkSpeakerBoost);
        grpVoiceSettings.Controls.Add(btnResetVoiceSettings);
        grpVoiceSettings.Dock = DockStyle.Fill;
        grpVoiceSettings.Location = new Point(575, 0);
        grpVoiceSettings.Margin = new Padding(0, 0, 6, 4);
        grpVoiceSettings.Name = "grpVoiceSettings";
        grpVoiceSettings.Size = new Size(419, 155);
        grpVoiceSettings.TabIndex = 1;
        grpVoiceSettings.TabStop = false;
        // 
        // chkVoiceSettings
        // 
        chkVoiceSettings.AutoSize = true;
        chkVoiceSettings.Checked = true;
        chkVoiceSettings.CheckState = CheckState.Checked;
        chkVoiceSettings.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        chkVoiceSettings.Location = new Point(14, 21);
        chkVoiceSettings.Name = "chkVoiceSettings";
        chkVoiceSettings.Size = new Size(147, 19);
        chkVoiceSettings.TabIndex = 0;
        chkVoiceSettings.Text = "Change voice settings";
        chkVoiceSettings.UseVisualStyleBackColor = true;
        // 
        // lblSpeed
        // 
        lblSpeed.AutoSize = true;
        lblSpeed.Location = new Point(18, 54);
        lblSpeed.Name = "lblSpeed";
        lblSpeed.Size = new Size(42, 15);
        lblSpeed.TabIndex = 1;
        lblSpeed.Text = "Speed:";
        // 
        // numSpeed
        // 
        numSpeed.DecimalPlaces = 2;
        numSpeed.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        numSpeed.Location = new Point(100, 50);
        numSpeed.Maximum = new decimal(new int[] { 200, 0, 0, 131072 });
        numSpeed.Minimum = new decimal(new int[] { 50, 0, 0, 131072 });
        numSpeed.Name = "numSpeed";
        numSpeed.Size = new Size(75, 23);
        numSpeed.TabIndex = 2;
        numSpeed.Value = new decimal(new int[] { 100, 0, 0, 131072 });
        // 
        // lblStability
        // 
        lblStability.AutoSize = true;
        lblStability.Location = new Point(18, 85);
        lblStability.Name = "lblStability";
        lblStability.Size = new Size(73, 15);
        lblStability.TabIndex = 3;
        lblStability.Text = "CFG Weight:";
        // 
        // numStability
        // 
        numStability.Location = new Point(100, 81);
        numStability.Name = "numStability";
        numStability.Size = new Size(75, 23);
        numStability.TabIndex = 4;
        numStability.Value = new decimal(new int[] { 50, 0, 0, 0 });
        // 
        // lblSimilarity
        // 
        lblSimilarity.AutoSize = true;
        lblSimilarity.Location = new Point(18, 116);
        lblSimilarity.Name = "lblSimilarity";
        lblSimilarity.Size = new Size(77, 15);
        lblSimilarity.TabIndex = 5;
        lblSimilarity.Text = "Temperature:";
        // 
        // numSimilarity
        // 
        numSimilarity.Location = new Point(100, 112);
        numSimilarity.Name = "numSimilarity";
        numSimilarity.Size = new Size(75, 23);
        numSimilarity.TabIndex = 6;
        numSimilarity.Value = new decimal(new int[] { 80, 0, 0, 0 });
        // 
        // lblStyle
        // 
        lblStyle.AutoSize = true;
        lblStyle.Location = new Point(205, 54);
        lblStyle.Name = "lblStyle";
        lblStyle.Size = new Size(78, 15);
        lblStyle.TabIndex = 7;
        lblStyle.Text = "Exaggeration:";
        // 
        // numStyle
        // 
        numStyle.Location = new Point(297, 50);
        numStyle.Name = "numStyle";
        numStyle.Size = new Size(75, 23);
        numStyle.TabIndex = 8;
        numStyle.Value = new decimal(new int[] { 50, 0, 0, 0 });
        // 
        // chkSpeakerBoost
        // 
        chkSpeakerBoost.AutoSize = true;
        chkSpeakerBoost.Checked = true;
        chkSpeakerBoost.CheckState = CheckState.Checked;
        chkSpeakerBoost.Location = new Point(205, 84);
        chkSpeakerBoost.Name = "chkSpeakerBoost";
        chkSpeakerBoost.Size = new Size(100, 19);
        chkSpeakerBoost.TabIndex = 9;
        chkSpeakerBoost.Text = "Speaker Boost";
        chkSpeakerBoost.UseVisualStyleBackColor = true;
        // 
        // btnResetVoiceSettings
        // 
        btnResetVoiceSettings.Location = new Point(297, 109);
        btnResetVoiceSettings.Name = "btnResetVoiceSettings";
        btnResetVoiceSettings.Size = new Size(75, 27);
        btnResetVoiceSettings.TabIndex = 10;
        btnResetVoiceSettings.Text = "Reset";
        btnResetVoiceSettings.UseVisualStyleBackColor = true;
        // 
        // grpSystem
        // 
        grpSystem.BackColor = Color.White;
        grpSystem.Controls.Add(lblDevice);
        grpSystem.Controls.Add(cmbDevice);
        grpSystem.Controls.Add(backendStatusLabel);
        grpSystem.Controls.Add(gpuStatusLabel);
        grpSystem.Controls.Add(lblCudaStatus);
        grpSystem.Controls.Add(lblVramStatus);
        grpSystem.Dock = DockStyle.Fill;
        grpSystem.Location = new Point(1003, 3);
        grpSystem.Name = "grpSystem";
        grpSystem.Size = new Size(244, 153);
        grpSystem.TabIndex = 2;
        grpSystem.TabStop = false;
        grpSystem.Text = "System";
        // 
        // lblDevice
        // 
        lblDevice.AutoSize = true;
        lblDevice.Location = new Point(12, 29);
        lblDevice.Name = "lblDevice";
        lblDevice.Size = new Size(45, 15);
        lblDevice.TabIndex = 0;
        lblDevice.Text = "Device:";
        // 
        // cmbDevice
        // 
        cmbDevice.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbDevice.FormattingEnabled = true;
        cmbDevice.Items.AddRange(new object[] { "Auto", "CUDA", "CPU" });
        cmbDevice.Location = new Point(68, 25);
        cmbDevice.Name = "cmbDevice";
        cmbDevice.Size = new Size(120, 23);
        cmbDevice.TabIndex = 1;
        // 
        // backendStatusLabel
        // 
        backendStatusLabel.AutoSize = true;
        backendStatusLabel.ForeColor = Color.Firebrick;
        backendStatusLabel.Location = new Point(12, 61);
        backendStatusLabel.Name = "backendStatusLabel";
        backendStatusLabel.Size = new Size(94, 15);
        backendStatusLabel.TabIndex = 2;
        backendStatusLabel.Text = "Backend: Offline";
        // 
        // gpuStatusLabel
        // 
        gpuStatusLabel.AutoSize = true;
        gpuStatusLabel.Location = new Point(12, 84);
        gpuStatusLabel.Name = "gpuStatusLabel";
        gpuStatusLabel.Size = new Size(87, 15);
        gpuStatusLabel.TabIndex = 3;
        gpuStatusLabel.Text = "GPU: Unknown";
        // 
        // lblCudaStatus
        // 
        lblCudaStatus.AutoSize = true;
        lblCudaStatus.Location = new Point(12, 107);
        lblCudaStatus.Name = "lblCudaStatus";
        lblCudaStatus.Size = new Size(96, 15);
        lblCudaStatus.TabIndex = 4;
        lblCudaStatus.Text = "CUDA: Unknown";
        // 
        // lblVramStatus
        // 
        lblVramStatus.AutoSize = true;
        lblVramStatus.Location = new Point(12, 130);
        lblVramStatus.Name = "lblVramStatus";
        lblVramStatus.Size = new Size(77, 15);
        lblVramStatus.TabIndex = 5;
        lblVramStatus.Text = "VRAM: -- / --";
        // 
        // batchOptionsLayout
        // 
        batchOptionsLayout.ColumnCount = 2;
        batchOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
        batchOptionsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
        batchOptionsLayout.Controls.Add(grpBatchJob, 0, 0);
        batchOptionsLayout.Controls.Add(grpOptions, 1, 0);
        batchOptionsLayout.Dock = DockStyle.Fill;
        batchOptionsLayout.Location = new Point(3, 168);
        batchOptionsLayout.Name = "batchOptionsLayout";
        batchOptionsLayout.RowCount = 1;
        batchOptionsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        batchOptionsLayout.Size = new Size(1250, 119);
        batchOptionsLayout.TabIndex = 1;
        // 
        // grpBatchJob
        // 
        grpBatchJob.BackColor = Color.White;
        grpBatchJob.Controls.Add(lblFolder);
        grpBatchJob.Controls.Add(txtBatchFolder);
        grpBatchJob.Controls.Add(btnBrowseFolder);
        grpBatchJob.Controls.Add(chkAutoSrt);
        grpBatchJob.Controls.Add(btnRunBatch);
        grpBatchJob.Controls.Add(txtBatchInfo);
        grpBatchJob.Dock = DockStyle.Fill;
        grpBatchJob.Location = new Point(0, 0);
        grpBatchJob.Margin = new Padding(0, 0, 6, 4);
        grpBatchJob.Name = "grpBatchJob";
        grpBatchJob.Size = new Size(894, 115);
        grpBatchJob.TabIndex = 0;
        grpBatchJob.TabStop = false;
        grpBatchJob.Text = "Batch Job";
        // 
        // lblFolder
        // 
        lblFolder.AutoSize = true;
        lblFolder.Location = new Point(14, 31);
        lblFolder.Name = "lblFolder";
        lblFolder.Size = new Size(43, 15);
        lblFolder.TabIndex = 0;
        lblFolder.Text = "Folder:";
        // 
        // txtBatchFolder
        // 
        txtBatchFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtBatchFolder.Location = new Point(64, 27);
        txtBatchFolder.Name = "txtBatchFolder";
        txtBatchFolder.Size = new Size(1084, 23);
        txtBatchFolder.TabIndex = 1;
        // 
        // btnBrowseFolder
        // 
        btnBrowseFolder.Location = new Point(462, 26);
        btnBrowseFolder.Name = "btnBrowseFolder";
        btnBrowseFolder.Size = new Size(38, 25);
        btnBrowseFolder.TabIndex = 2;
        btnBrowseFolder.Text = "...";
        btnBrowseFolder.UseVisualStyleBackColor = true;
        // 
        // chkAutoSrt
        // 
        chkAutoSrt.AutoSize = true;
        chkAutoSrt.Location = new Point(64, 72);
        chkAutoSrt.Name = "chkAutoSrt";
        chkAutoSrt.Size = new Size(109, 19);
        chkAutoSrt.TabIndex = 3;
        chkAutoSrt.Text = "Auto create SRT";
        chkAutoSrt.UseVisualStyleBackColor = true;
        // 
        // btnRunBatch
        // 
        btnRunBatch.Location = new Point(190, 66);
        btnRunBatch.Name = "btnRunBatch";
        btnRunBatch.Size = new Size(110, 30);
        btnRunBatch.TabIndex = 4;
        btnRunBatch.Text = "Run Batch";
        btnRunBatch.UseVisualStyleBackColor = true;
        // 
        // txtBatchInfo
        // 
        txtBatchInfo.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        txtBatchInfo.BackColor = Color.White;
        txtBatchInfo.Location = new Point(1214, 22);
        txtBatchInfo.Multiline = true;
        txtBatchInfo.Name = "txtBatchInfo";
        txtBatchInfo.ReadOnly = true;
        txtBatchInfo.ScrollBars = ScrollBars.Vertical;
        txtBatchInfo.Size = new Size(350, 93);
        txtBatchInfo.TabIndex = 5;
        // 
        // grpOptions
        // 
        grpOptions.BackColor = Color.White;
        grpOptions.Controls.Add(chkRetryFailed);
        grpOptions.Controls.Add(chkAutoSplit);
        grpOptions.Controls.Add(chkSkipExisting);
        grpOptions.Controls.Add(chkAutoJoin);
        grpOptions.Controls.Add(lblSplitChars);
        grpOptions.Controls.Add(txtSplitChars);
        grpOptions.Controls.Add(btnAdvancedSettings);
        grpOptions.Dock = DockStyle.Fill;
        grpOptions.Location = new Point(903, 3);
        grpOptions.Name = "grpOptions";
        grpOptions.Size = new Size(344, 113);
        grpOptions.TabIndex = 1;
        grpOptions.TabStop = false;
        grpOptions.Text = "Options";
        // 
        // chkRetryFailed
        // 
        chkRetryFailed.AutoSize = true;
        chkRetryFailed.Checked = true;
        chkRetryFailed.CheckState = CheckState.Checked;
        chkRetryFailed.Location = new Point(15, 29);
        chkRetryFailed.Name = "chkRetryFailed";
        chkRetryFailed.Size = new Size(87, 19);
        chkRetryFailed.TabIndex = 0;
        chkRetryFailed.Text = "Retry Failed";
        chkRetryFailed.UseVisualStyleBackColor = true;
        // 
        // chkAutoSplit
        // 
        chkAutoSplit.AutoSize = true;
        chkAutoSplit.Checked = true;
        chkAutoSplit.CheckState = CheckState.Checked;
        chkAutoSplit.Location = new Point(118, 29);
        chkAutoSplit.Name = "chkAutoSplit";
        chkAutoSplit.Size = new Size(78, 19);
        chkAutoSplit.TabIndex = 1;
        chkAutoSplit.Text = "Auto Split";
        chkAutoSplit.UseVisualStyleBackColor = true;
        // 
        // chkSkipExisting
        // 
        chkSkipExisting.AutoSize = true;
        chkSkipExisting.Location = new Point(15, 58);
        chkSkipExisting.Name = "chkSkipExisting";
        chkSkipExisting.Size = new Size(91, 19);
        chkSkipExisting.TabIndex = 2;
        chkSkipExisting.Text = "Skip Existing";
        chkSkipExisting.UseVisualStyleBackColor = true;
        // 
        // chkAutoJoin
        // 
        chkAutoJoin.AutoSize = true;
        chkAutoJoin.Location = new Point(118, 58);
        chkAutoJoin.Name = "chkAutoJoin";
        chkAutoJoin.Size = new Size(76, 19);
        chkAutoJoin.TabIndex = 3;
        chkAutoJoin.Text = "Auto Join";
        chkAutoJoin.UseVisualStyleBackColor = true;
        // 
        // lblSplitChars
        // 
        lblSplitChars.AutoSize = true;
        lblSplitChars.Location = new Point(212, 31);
        lblSplitChars.Name = "lblSplitChars";
        lblSplitChars.Size = new Size(33, 15);
        lblSplitChars.TabIndex = 4;
        lblSplitChars.Text = "Split:";
        // 
        // txtSplitChars
        // 
        txtSplitChars.Location = new Point(250, 27);
        txtSplitChars.Name = "txtSplitChars";
        txtSplitChars.Size = new Size(64, 23);
        txtSplitChars.TabIndex = 5;
        txtSplitChars.Text = ".,!?";
        // 
        // btnAdvancedSettings
        // 
        btnAdvancedSettings.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        btnAdvancedSettings.Location = new Point(15, 79);
        btnAdvancedSettings.Name = "btnAdvancedSettings";
        btnAdvancedSettings.Size = new Size(311, 28);
        btnAdvancedSettings.TabIndex = 6;
        btnAdvancedSettings.Text = "Advanced Settings";
        btnAdvancedSettings.UseVisualStyleBackColor = true;
        // 
        // pnlToolbar
        // 
        pnlToolbar.AutoScroll = true;
        pnlToolbar.BackColor = Color.White;
        pnlToolbar.Controls.Add(generateButton);
        pnlToolbar.Controls.Add(stopButton);
        pnlToolbar.Controls.Add(btnImportFile);
        pnlToolbar.Controls.Add(btnImportFolder);
        pnlToolbar.Controls.Add(btnImportVoice);
        pnlToolbar.Controls.Add(btnOpenAudioOutput);
        pnlToolbar.Controls.Add(btnCreateSrt);
        pnlToolbar.Controls.Add(btnJoinMp3);
        pnlToolbar.Controls.Add(playButton);
        pnlToolbar.Controls.Add(saveWavButton);
        pnlToolbar.Dock = DockStyle.Fill;
        pnlToolbar.Location = new Point(3, 293);
        pnlToolbar.Name = "pnlToolbar";
        pnlToolbar.Padding = new Padding(5, 6, 5, 4);
        pnlToolbar.Size = new Size(1250, 40);
        pnlToolbar.TabIndex = 2;
        pnlToolbar.WrapContents = false;
        // 
        // generateButton
        // 
        generateButton.BackColor = Color.FromArgb(232, 248, 235);
        generateButton.FlatStyle = FlatStyle.Flat;
        generateButton.ForeColor = Color.FromArgb(23, 120, 50);
        generateButton.Location = new Point(5, 6);
        generateButton.Margin = new Padding(0, 0, 7, 0);
        generateButton.Name = "generateButton";
        generateButton.Size = new Size(70, 29);
        generateButton.TabIndex = 0;
        generateButton.Text = "Start";
        generateButton.UseVisualStyleBackColor = false;
        generateButton.Click += GenerateButton_Click;
        // 
        // stopButton
        // 
        stopButton.BackColor = Color.FromArgb(255, 235, 235);
        stopButton.FlatStyle = FlatStyle.Flat;
        stopButton.ForeColor = Color.Firebrick;
        stopButton.Location = new Point(82, 6);
        stopButton.Margin = new Padding(0, 0, 7, 0);
        stopButton.Name = "stopButton";
        stopButton.Size = new Size(70, 29);
        stopButton.TabIndex = 1;
        stopButton.Text = "Stop";
        stopButton.UseVisualStyleBackColor = false;
        stopButton.Click += StopButton_Click;
        // 
        // btnImportFile
        // 
        btnImportFile.Location = new Point(159, 6);
        btnImportFile.Margin = new Padding(0, 0, 7, 0);
        btnImportFile.Name = "btnImportFile";
        btnImportFile.Size = new Size(90, 29);
        btnImportFile.TabIndex = 2;
        btnImportFile.Text = "Import File";
        btnImportFile.UseVisualStyleBackColor = true;
        // 
        // btnImportFolder
        // 
        btnImportFolder.Location = new Point(256, 6);
        btnImportFolder.Margin = new Padding(0, 0, 7, 0);
        btnImportFolder.Name = "btnImportFolder";
        btnImportFolder.Size = new Size(100, 29);
        btnImportFolder.TabIndex = 3;
        btnImportFolder.Text = "Import Folder";
        btnImportFolder.UseVisualStyleBackColor = true;
        // 
        // btnImportVoice
        // 
        btnImportVoice.Location = new Point(363, 6);
        btnImportVoice.Margin = new Padding(0, 0, 7, 0);
        btnImportVoice.Name = "btnImportVoice";
        btnImportVoice.Size = new Size(100, 29);
        btnImportVoice.TabIndex = 4;
        btnImportVoice.Text = "Import Voice";
        btnImportVoice.UseVisualStyleBackColor = true;
        // 
        // btnOpenAudioOutput
        // 
        btnOpenAudioOutput.Location = new Point(470, 6);
        btnOpenAudioOutput.Margin = new Padding(0, 0, 7, 0);
        btnOpenAudioOutput.Name = "btnOpenAudioOutput";
        btnOpenAudioOutput.Size = new Size(130, 29);
        btnOpenAudioOutput.TabIndex = 5;
        btnOpenAudioOutput.Text = "Open Audio Output";
        btnOpenAudioOutput.UseVisualStyleBackColor = true;
        // 
        // btnCreateSrt
        // 
        btnCreateSrt.Location = new Point(607, 6);
        btnCreateSrt.Margin = new Padding(0, 0, 7, 0);
        btnCreateSrt.Name = "btnCreateSrt";
        btnCreateSrt.Size = new Size(90, 29);
        btnCreateSrt.TabIndex = 6;
        btnCreateSrt.Text = "Create SRT";
        btnCreateSrt.UseVisualStyleBackColor = true;
        // 
        // btnJoinMp3
        // 
        btnJoinMp3.Location = new Point(704, 6);
        btnJoinMp3.Margin = new Padding(0, 0, 7, 0);
        btnJoinMp3.Name = "btnJoinMp3";
        btnJoinMp3.Size = new Size(85, 29);
        btnJoinMp3.TabIndex = 7;
        btnJoinMp3.Text = "Join MP3";
        btnJoinMp3.UseVisualStyleBackColor = true;
        // 
        // playButton
        // 
        playButton.Location = new Point(796, 6);
        playButton.Margin = new Padding(0, 0, 7, 0);
        playButton.Name = "playButton";
        playButton.Size = new Size(70, 29);
        playButton.TabIndex = 8;
        playButton.Text = "Play";
        playButton.UseVisualStyleBackColor = true;
        playButton.Click += PlayButton_Click;
        // 
        // saveWavButton
        // 
        saveWavButton.Location = new Point(873, 6);
        saveWavButton.Margin = new Padding(0, 0, 7, 0);
        saveWavButton.Name = "saveWavButton";
        saveWavButton.Size = new Size(90, 29);
        saveWavButton.TabIndex = 9;
        saveWavButton.Text = "Save WAV";
        saveWavButton.UseVisualStyleBackColor = true;
        saveWavButton.Click += SaveWavButton_Click;
        // 
        // statusLayout
        // 
        statusLayout.ColumnCount = 3;
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        statusLayout.Controls.Add(lblSubtitleStatus, 0, 0);
        statusLayout.Controls.Add(lblCurrentJob, 1, 0);
        statusLayout.Controls.Add(progressTotal, 2, 0);
        statusLayout.Dock = DockStyle.Fill;
        statusLayout.Location = new Point(3, 339);
        statusLayout.Name = "statusLayout";
        statusLayout.RowCount = 1;
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        statusLayout.Size = new Size(1250, 28);
        statusLayout.TabIndex = 3;
        // 
        // lblSubtitleStatus
        // 
        lblSubtitleStatus.Dock = DockStyle.Fill;
        lblSubtitleStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSubtitleStatus.Location = new Point(3, 0);
        lblSubtitleStatus.Name = "lblSubtitleStatus";
        lblSubtitleStatus.Size = new Size(619, 28);
        lblSubtitleStatus.TabIndex = 0;
        lblSubtitleStatus.Text = "Subtitles | Done: 0 | Processing: 0 | Total: 0 | Failed: 0 | Elapsed: 00:00";
        lblSubtitleStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblCurrentJob
        // 
        lblCurrentJob.Dock = DockStyle.Fill;
        lblCurrentJob.Location = new Point(628, 0);
        lblCurrentJob.Name = "lblCurrentJob";
        lblCurrentJob.Size = new Size(306, 28);
        lblCurrentJob.TabIndex = 1;
        lblCurrentJob.Text = "Current: Idle";
        lblCurrentJob.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // progressTotal
        // 
        progressTotal.Dock = DockStyle.Fill;
        progressTotal.Location = new Point(942, 8);
        progressTotal.Margin = new Padding(5, 8, 5, 8);
        progressTotal.Name = "progressTotal";
        progressTotal.Size = new Size(303, 12);
        progressTotal.TabIndex = 2;
        // 
        // dgvJobs
        // 
        dgvJobs.AllowUserToAddRows = false;
        dgvJobs.AllowUserToDeleteRows = false;
        dgvJobs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvJobs.BackgroundColor = Color.White;
        dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(238, 240, 244);
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
        dgvJobs.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        dgvJobs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvJobs.Columns.AddRange(new DataGridViewColumn[] { colId, colOutput, colTiming, colContent, colVoice, colLanguage, colStatus });
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Window;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = Color.FromArgb(40, 45, 55);
        dataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(220, 235, 252);
        dataGridViewCellStyle2.SelectionForeColor = Color.Black;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.False;
        dgvJobs.DefaultCellStyle = dataGridViewCellStyle2;
        dgvJobs.Dock = DockStyle.Fill;
        dgvJobs.EnableHeadersVisualStyles = false;
        dgvJobs.Location = new Point(3, 373);
        dgvJobs.MultiSelect = false;
        dgvJobs.Name = "dgvJobs";
        dgvJobs.ReadOnly = true;
        dgvJobs.RowHeadersVisible = false;
        dgvJobs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvJobs.Size = new Size(1250, 292);
        dgvJobs.TabIndex = 4;
        // 
        // colId
        // 
        colId.FillWeight = 35F;
        colId.HeaderText = "Id";
        colId.Name = "colId";
        colId.ReadOnly = true;
        // 
        // colOutput
        // 
        colOutput.FillWeight = 80F;
        colOutput.HeaderText = "Output";
        colOutput.Name = "colOutput";
        colOutput.ReadOnly = true;
        // 
        // colTiming
        // 
        colTiming.FillWeight = 65F;
        colTiming.HeaderText = "Timing";
        colTiming.Name = "colTiming";
        colTiming.ReadOnly = true;
        // 
        // colContent
        // 
        colContent.FillWeight = 400F;
        colContent.HeaderText = "Content";
        colContent.Name = "colContent";
        colContent.ReadOnly = true;
        // 
        // colVoice
        // 
        colVoice.HeaderText = "Voice";
        colVoice.Name = "colVoice";
        colVoice.ReadOnly = true;
        // 
        // colLanguage
        // 
        colLanguage.FillWeight = 75F;
        colLanguage.HeaderText = "Language";
        colLanguage.Name = "colLanguage";
        colLanguage.ReadOnly = true;
        // 
        // colStatus
        // 
        colStatus.FillWeight = 90F;
        colStatus.HeaderText = "Status";
        colStatus.Name = "colStatus";
        colStatus.ReadOnly = true;
        // 
        // logsTextBox
        // 
        logsTextBox.BackColor = Color.White;
        logsTextBox.Dock = DockStyle.Fill;
        logsTextBox.Font = new Font("Consolas", 8.5F);
        logsTextBox.ForeColor = Color.FromArgb(65, 70, 80);
        logsTextBox.Location = new Point(3, 671);
        logsTextBox.Multiline = true;
        logsTextBox.Name = "logsTextBox";
        logsTextBox.ReadOnly = true;
        logsTextBox.ScrollBars = ScrollBars.Vertical;
        logsTextBox.Size = new Size(1250, 76);
        logsTextBox.TabIndex = 5;
        // 
        // tabVoiceLibrary
        // 
        tabVoiceLibrary.BackColor = Color.FromArgb(245, 247, 250);
        tabVoiceLibrary.Controls.Add(libraryRootLayout);
        tabVoiceLibrary.Location = new Point(4, 30);
        tabVoiceLibrary.Name = "tabVoiceLibrary";
        tabVoiceLibrary.Padding = new Padding(8);
        tabVoiceLibrary.Size = new Size(1272, 766);
        tabVoiceLibrary.TabIndex = 1;
        tabVoiceLibrary.Text = "Voice Library";
        // 
        // libraryRootLayout
        // 
        libraryRootLayout.ColumnCount = 1;
        libraryRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        libraryRootLayout.Controls.Add(libraryHeaderLayout, 0, 0);
        libraryRootLayout.Controls.Add(splitVoiceLibrary, 0, 1);
        libraryRootLayout.Dock = DockStyle.Fill;
        libraryRootLayout.Location = new Point(8, 8);
        libraryRootLayout.Name = "libraryRootLayout";
        libraryRootLayout.RowCount = 2;
        libraryRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
        libraryRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        libraryRootLayout.Size = new Size(1256, 750);
        libraryRootLayout.TabIndex = 0;
        // 
        // libraryHeaderLayout
        // 
        libraryHeaderLayout.BackColor = Color.White;
        libraryHeaderLayout.ColumnCount = 6;
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165F));
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
        libraryHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        libraryHeaderLayout.Controls.Add(lblLibraryTitle, 0, 0);
        libraryHeaderLayout.Controls.Add(txtLibrarySearch, 1, 0);
        libraryHeaderLayout.Controls.Add(btnLibrarySearch, 2, 0);
        libraryHeaderLayout.Controls.Add(btnLibraryRefresh, 3, 0);
        libraryHeaderLayout.Controls.Add(btnLibraryAddVoice, 5, 0);
        libraryHeaderLayout.Dock = DockStyle.Fill;
        libraryHeaderLayout.Location = new Point(3, 3);
        libraryHeaderLayout.Name = "libraryHeaderLayout";
        libraryHeaderLayout.RowCount = 1;
        libraryHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        libraryHeaderLayout.Size = new Size(1250, 56);
        libraryHeaderLayout.TabIndex = 0;
        // 
        // lblLibraryTitle
        // 
        lblLibraryTitle.Dock = DockStyle.Fill;
        lblLibraryTitle.Font = new Font("Segoe UI Semibold", 15F);
        lblLibraryTitle.Location = new Point(3, 0);
        lblLibraryTitle.Name = "lblLibraryTitle";
        lblLibraryTitle.Size = new Size(159, 56);
        lblLibraryTitle.TabIndex = 0;
        lblLibraryTitle.Text = "Voice Library";
        lblLibraryTitle.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLibrarySearch
        // 
        txtLibrarySearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        txtLibrarySearch.Location = new Point(168, 16);
        txtLibrarySearch.Name = "txtLibrarySearch";
        txtLibrarySearch.PlaceholderText = "Search voice...";
        txtLibrarySearch.Size = new Size(745, 23);
        txtLibrarySearch.TabIndex = 1;
        // 
        // btnLibrarySearch
        // 
        btnLibrarySearch.Anchor = AnchorStyles.None;
        btnLibrarySearch.Location = new Point(919, 14);
        btnLibrarySearch.Name = "btnLibrarySearch";
        btnLibrarySearch.Size = new Size(75, 28);
        btnLibrarySearch.TabIndex = 2;
        btnLibrarySearch.Text = "Search";
        btnLibrarySearch.UseVisualStyleBackColor = true;
        // 
        // btnLibraryRefresh
        // 
        btnLibraryRefresh.Anchor = AnchorStyles.None;
        btnLibraryRefresh.Location = new Point(1001, 14);
        btnLibraryRefresh.Name = "btnLibraryRefresh";
        btnLibraryRefresh.Size = new Size(75, 28);
        btnLibraryRefresh.TabIndex = 3;
        btnLibraryRefresh.Text = "Refresh";
        btnLibraryRefresh.UseVisualStyleBackColor = true;
        // 
        // btnLibraryAddVoice
        // 
        btnLibraryAddVoice.Anchor = AnchorStyles.None;
        btnLibraryAddVoice.BackColor = Color.FromArgb(37, 99, 235);
        btnLibraryAddVoice.FlatStyle = FlatStyle.Flat;
        btnLibraryAddVoice.ForeColor = Color.White;
        btnLibraryAddVoice.Location = new Point(1107, 13);
        btnLibraryAddVoice.Name = "btnLibraryAddVoice";
        btnLibraryAddVoice.Size = new Size(135, 30);
        btnLibraryAddVoice.TabIndex = 4;
        btnLibraryAddVoice.Text = "+ Add New Voice";
        btnLibraryAddVoice.UseVisualStyleBackColor = false;
        // 
        // splitVoiceLibrary
        // 
        splitVoiceLibrary.Dock = DockStyle.Fill;
        splitVoiceLibrary.FixedPanel = FixedPanel.Panel2;
        splitVoiceLibrary.Location = new Point(3, 65);
        splitVoiceLibrary.Name = "splitVoiceLibrary";
        // 
        // splitVoiceLibrary.Panel1
        // 
        splitVoiceLibrary.Panel1.Controls.Add(dgvVoiceLibrary);
        splitVoiceLibrary.Panel1.Padding = new Padding(0, 0, 8, 0);
        // 
        // splitVoiceLibrary.Panel2
        // 
        splitVoiceLibrary.Panel2.Controls.Add(libraryRightLayout);
        splitVoiceLibrary.Panel2MinSize = 390;
        splitVoiceLibrary.Size = new Size(1250, 682);
        splitVoiceLibrary.SplitterDistance = 842;
        splitVoiceLibrary.TabIndex = 1;
        // 
        // dgvVoiceLibrary
        // 
        dgvVoiceLibrary.AllowUserToAddRows = false;
        dgvVoiceLibrary.AllowUserToDeleteRows = false;
        dgvVoiceLibrary.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvVoiceLibrary.BackgroundColor = Color.White;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.FromArgb(238, 240, 244);
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.True;
        dgvVoiceLibrary.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle3;
        dgvVoiceLibrary.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dgvVoiceLibrary.Columns.AddRange(new DataGridViewColumn[] { voiceColName, voiceColId, voiceColLanguage, voiceColGender, voiceColCreated });
        dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle4.BackColor = SystemColors.Window;
        dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle4.ForeColor = Color.FromArgb(40, 45, 55);
        dataGridViewCellStyle4.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle4.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
        dgvVoiceLibrary.DefaultCellStyle = dataGridViewCellStyle4;
        dgvVoiceLibrary.Dock = DockStyle.Fill;
        dgvVoiceLibrary.EnableHeadersVisualStyles = false;
        dgvVoiceLibrary.Location = new Point(0, 0);
        dgvVoiceLibrary.MultiSelect = false;
        dgvVoiceLibrary.Name = "dgvVoiceLibrary";
        dgvVoiceLibrary.ReadOnly = true;
        dgvVoiceLibrary.RowHeadersVisible = false;
        dgvVoiceLibrary.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvVoiceLibrary.Size = new Size(834, 682);
        dgvVoiceLibrary.TabIndex = 0;
        // 
        // voiceColName
        // 
        voiceColName.FillWeight = 160F;
        voiceColName.HeaderText = "Voice Name";
        voiceColName.Name = "voiceColName";
        voiceColName.ReadOnly = true;
        // 
        // voiceColId
        // 
        voiceColId.FillWeight = 170F;
        voiceColId.HeaderText = "Voice ID";
        voiceColId.Name = "voiceColId";
        voiceColId.ReadOnly = true;
        // 
        // voiceColLanguage
        // 
        voiceColLanguage.FillWeight = 80F;
        voiceColLanguage.HeaderText = "Language";
        voiceColLanguage.Name = "voiceColLanguage";
        voiceColLanguage.ReadOnly = true;
        // 
        // voiceColGender
        // 
        voiceColGender.FillWeight = 75F;
        voiceColGender.HeaderText = "Gender";
        voiceColGender.Name = "voiceColGender";
        voiceColGender.ReadOnly = true;
        // 
        // voiceColCreated
        // 
        voiceColCreated.FillWeight = 105F;
        voiceColCreated.HeaderText = "Created";
        voiceColCreated.Name = "voiceColCreated";
        voiceColCreated.ReadOnly = true;
        // 
        // libraryRightLayout
        // 
        libraryRightLayout.ColumnCount = 1;
        libraryRightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        libraryRightLayout.Controls.Add(grpSelectedVoice, 0, 0);
        libraryRightLayout.Controls.Add(grpAddVoice, 0, 1);
        libraryRightLayout.Dock = DockStyle.Fill;
        libraryRightLayout.Location = new Point(0, 0);
        libraryRightLayout.Name = "libraryRightLayout";
        libraryRightLayout.RowCount = 2;
        libraryRightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
        libraryRightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
        libraryRightLayout.Size = new Size(404, 682);
        libraryRightLayout.TabIndex = 0;
        // 
        // grpSelectedVoice
        // 
        grpSelectedVoice.BackColor = Color.White;
        grpSelectedVoice.Controls.Add(selectedVoiceLayout);
        grpSelectedVoice.Dock = DockStyle.Fill;
        grpSelectedVoice.Location = new Point(0, 0);
        grpSelectedVoice.Margin = new Padding(0, 0, 0, 6);
        grpSelectedVoice.Name = "grpSelectedVoice";
        grpSelectedVoice.Padding = new Padding(10);
        grpSelectedVoice.Size = new Size(404, 369);
        grpSelectedVoice.TabIndex = 0;
        grpSelectedVoice.TabStop = false;
        grpSelectedVoice.Text = "Selected Voice";
        // 
        // selectedVoiceLayout
        // 
        selectedVoiceLayout.ColumnCount = 3;
        selectedVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        selectedVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        selectedVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceNameText, 0, 0);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceName, 1, 0);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceIdText, 0, 1);
        selectedVoiceLayout.Controls.Add(txtSelectedVoiceId, 1, 1);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceLanguageText, 0, 2);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceLanguage, 1, 2);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceGenderText, 2, 2);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceGender, 2, 3);
        selectedVoiceLayout.Controls.Add(lblSelectedVoiceDescriptionText, 0, 3);
        selectedVoiceLayout.Controls.Add(txtSelectedVoiceDescription, 1, 3);
        selectedVoiceLayout.Controls.Add(lblReferenceAudio, 0, 4);
        selectedVoiceLayout.Controls.Add(txtReferenceAudio, 1, 4);
        selectedVoiceLayout.Controls.Add(btnPlayReference, 2, 4);
        selectedVoiceLayout.Controls.Add(lblPreviewSentence, 0, 5);
        selectedVoiceLayout.Controls.Add(txtPreviewSentence, 1, 5);
        selectedVoiceLayout.Controls.Add(pnlVoiceActions, 0, 6);
        selectedVoiceLayout.Dock = DockStyle.Fill;
        selectedVoiceLayout.Location = new Point(10, 26);
        selectedVoiceLayout.Name = "selectedVoiceLayout";
        selectedVoiceLayout.RowCount = 7;
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        selectedVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        selectedVoiceLayout.Size = new Size(384, 333);
        selectedVoiceLayout.TabIndex = 0;
        // 
        // lblSelectedVoiceNameText
        // 
        lblSelectedVoiceNameText.Dock = DockStyle.Fill;
        lblSelectedVoiceNameText.Location = new Point(3, 0);
        lblSelectedVoiceNameText.Name = "lblSelectedVoiceNameText";
        lblSelectedVoiceNameText.Size = new Size(80, 31);
        lblSelectedVoiceNameText.TabIndex = 0;
        lblSelectedVoiceNameText.Text = "Name:";
        lblSelectedVoiceNameText.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceName
        // 
        selectedVoiceLayout.SetColumnSpan(lblSelectedVoiceName, 2);
        lblSelectedVoiceName.Dock = DockStyle.Fill;
        lblSelectedVoiceName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        lblSelectedVoiceName.Location = new Point(89, 0);
        lblSelectedVoiceName.Name = "lblSelectedVoiceName";
        lblSelectedVoiceName.Size = new Size(292, 31);
        lblSelectedVoiceName.TabIndex = 1;
        lblSelectedVoiceName.Text = "-";
        lblSelectedVoiceName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceIdText
        // 
        lblSelectedVoiceIdText.Dock = DockStyle.Fill;
        lblSelectedVoiceIdText.Location = new Point(3, 31);
        lblSelectedVoiceIdText.Name = "lblSelectedVoiceIdText";
        lblSelectedVoiceIdText.Size = new Size(80, 31);
        lblSelectedVoiceIdText.TabIndex = 2;
        lblSelectedVoiceIdText.Text = "Voice ID:";
        lblSelectedVoiceIdText.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtSelectedVoiceId
        // 
        selectedVoiceLayout.SetColumnSpan(txtSelectedVoiceId, 2);
        txtSelectedVoiceId.Dock = DockStyle.Fill;
        txtSelectedVoiceId.Location = new Point(89, 35);
        txtSelectedVoiceId.Margin = new Padding(3, 4, 3, 4);
        txtSelectedVoiceId.Name = "txtSelectedVoiceId";
        txtSelectedVoiceId.ReadOnly = true;
        txtSelectedVoiceId.Size = new Size(292, 23);
        txtSelectedVoiceId.TabIndex = 3;
        // 
        // lblSelectedVoiceLanguageText
        // 
        lblSelectedVoiceLanguageText.Dock = DockStyle.Fill;
        lblSelectedVoiceLanguageText.Location = new Point(3, 62);
        lblSelectedVoiceLanguageText.Name = "lblSelectedVoiceLanguageText";
        lblSelectedVoiceLanguageText.Size = new Size(80, 31);
        lblSelectedVoiceLanguageText.TabIndex = 4;
        lblSelectedVoiceLanguageText.Text = "Language:";
        lblSelectedVoiceLanguageText.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceLanguage
        // 
        lblSelectedVoiceLanguage.Dock = DockStyle.Fill;
        lblSelectedVoiceLanguage.Location = new Point(89, 62);
        lblSelectedVoiceLanguage.Name = "lblSelectedVoiceLanguage";
        lblSelectedVoiceLanguage.Size = new Size(200, 31);
        lblSelectedVoiceLanguage.TabIndex = 5;
        lblSelectedVoiceLanguage.Text = "-";
        lblSelectedVoiceLanguage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceGenderText
        // 
        lblSelectedVoiceGenderText.Dock = DockStyle.Fill;
        lblSelectedVoiceGenderText.Location = new Point(295, 62);
        lblSelectedVoiceGenderText.Name = "lblSelectedVoiceGenderText";
        lblSelectedVoiceGenderText.Size = new Size(86, 31);
        lblSelectedVoiceGenderText.TabIndex = 6;
        lblSelectedVoiceGenderText.Text = "Gender:";
        lblSelectedVoiceGenderText.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceGender
        // 
        lblSelectedVoiceGender.Dock = DockStyle.Fill;
        lblSelectedVoiceGender.Location = new Point(295, 93);
        lblSelectedVoiceGender.Name = "lblSelectedVoiceGender";
        lblSelectedVoiceGender.Size = new Size(86, 58);
        lblSelectedVoiceGender.TabIndex = 7;
        lblSelectedVoiceGender.Text = "-";
        lblSelectedVoiceGender.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // lblSelectedVoiceDescriptionText
        // 
        lblSelectedVoiceDescriptionText.Dock = DockStyle.Fill;
        lblSelectedVoiceDescriptionText.Location = new Point(3, 93);
        lblSelectedVoiceDescriptionText.Name = "lblSelectedVoiceDescriptionText";
        lblSelectedVoiceDescriptionText.Size = new Size(80, 58);
        lblSelectedVoiceDescriptionText.TabIndex = 8;
        lblSelectedVoiceDescriptionText.Text = "Description:";
        lblSelectedVoiceDescriptionText.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtSelectedVoiceDescription
        // 
        txtSelectedVoiceDescription.Dock = DockStyle.Fill;
        txtSelectedVoiceDescription.Location = new Point(89, 96);
        txtSelectedVoiceDescription.Multiline = true;
        txtSelectedVoiceDescription.Name = "txtSelectedVoiceDescription";
        txtSelectedVoiceDescription.ReadOnly = true;
        txtSelectedVoiceDescription.ScrollBars = ScrollBars.Vertical;
        txtSelectedVoiceDescription.Size = new Size(200, 52);
        txtSelectedVoiceDescription.TabIndex = 9;
        // 
        // lblReferenceAudio
        // 
        lblReferenceAudio.Dock = DockStyle.Fill;
        lblReferenceAudio.Location = new Point(3, 151);
        lblReferenceAudio.Name = "lblReferenceAudio";
        lblReferenceAudio.Size = new Size(80, 34);
        lblReferenceAudio.TabIndex = 10;
        lblReferenceAudio.Text = "Reference:";
        lblReferenceAudio.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtReferenceAudio
        // 
        txtReferenceAudio.Dock = DockStyle.Fill;
        txtReferenceAudio.Location = new Point(89, 156);
        txtReferenceAudio.Margin = new Padding(3, 5, 3, 4);
        txtReferenceAudio.Name = "txtReferenceAudio";
        txtReferenceAudio.ReadOnly = true;
        txtReferenceAudio.Size = new Size(200, 23);
        txtReferenceAudio.TabIndex = 11;
        // 
        // btnPlayReference
        // 
        btnPlayReference.Dock = DockStyle.Fill;
        btnPlayReference.Location = new Point(295, 154);
        btnPlayReference.Name = "btnPlayReference";
        btnPlayReference.Size = new Size(86, 28);
        btnPlayReference.TabIndex = 12;
        btnPlayReference.Text = "▶ Play";
        btnPlayReference.UseVisualStyleBackColor = true;
        // 
        // lblPreviewSentence
        // 
        lblPreviewSentence.Dock = DockStyle.Fill;
        lblPreviewSentence.Location = new Point(3, 185);
        lblPreviewSentence.Name = "lblPreviewSentence";
        lblPreviewSentence.Size = new Size(80, 34);
        lblPreviewSentence.TabIndex = 13;
        lblPreviewSentence.Text = "Preview:";
        lblPreviewSentence.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtPreviewSentence
        // 
        selectedVoiceLayout.SetColumnSpan(txtPreviewSentence, 2);
        txtPreviewSentence.Dock = DockStyle.Fill;
        txtPreviewSentence.Location = new Point(89, 190);
        txtPreviewSentence.Margin = new Padding(3, 5, 3, 4);
        txtPreviewSentence.Name = "txtPreviewSentence";
        txtPreviewSentence.PlaceholderText = "Enter a sentence to preview this voice...";
        txtPreviewSentence.Size = new Size(292, 23);
        txtPreviewSentence.TabIndex = 14;
        // 
        // pnlVoiceActions
        // 
        pnlVoiceActions.AutoScroll = true;
        selectedVoiceLayout.SetColumnSpan(pnlVoiceActions, 3);
        pnlVoiceActions.Controls.Add(btnGeneratePreview);
        pnlVoiceActions.Controls.Add(btnUseSelectedVoice);
        pnlVoiceActions.Controls.Add(btnCopyVoiceId);
        pnlVoiceActions.Controls.Add(btnReplaceReference);
        pnlVoiceActions.Controls.Add(btnDeleteVoice);
        pnlVoiceActions.Dock = DockStyle.Fill;
        pnlVoiceActions.Location = new Point(3, 222);
        pnlVoiceActions.Name = "pnlVoiceActions";
        pnlVoiceActions.Padding = new Padding(0, 6, 0, 0);
        pnlVoiceActions.Size = new Size(378, 108);
        pnlVoiceActions.TabIndex = 15;
        // 
        // btnGeneratePreview
        // 
        btnGeneratePreview.Location = new Point(0, 6);
        btnGeneratePreview.Margin = new Padding(0, 0, 6, 5);
        btnGeneratePreview.Name = "btnGeneratePreview";
        btnGeneratePreview.Size = new Size(120, 29);
        btnGeneratePreview.TabIndex = 0;
        btnGeneratePreview.Text = "Generate Preview";
        btnGeneratePreview.UseVisualStyleBackColor = true;
        // 
        // btnUseSelectedVoice
        // 
        btnUseSelectedVoice.BackColor = Color.FromArgb(37, 99, 235);
        btnUseSelectedVoice.FlatStyle = FlatStyle.Flat;
        btnUseSelectedVoice.ForeColor = Color.White;
        btnUseSelectedVoice.Location = new Point(126, 6);
        btnUseSelectedVoice.Margin = new Padding(0, 0, 6, 5);
        btnUseSelectedVoice.Name = "btnUseSelectedVoice";
        btnUseSelectedVoice.Size = new Size(108, 29);
        btnUseSelectedVoice.TabIndex = 1;
        btnUseSelectedVoice.Text = "Use This Voice";
        btnUseSelectedVoice.UseVisualStyleBackColor = false;
        // 
        // btnCopyVoiceId
        // 
        btnCopyVoiceId.Location = new Point(240, 6);
        btnCopyVoiceId.Margin = new Padding(0, 0, 6, 5);
        btnCopyVoiceId.Name = "btnCopyVoiceId";
        btnCopyVoiceId.Size = new Size(72, 29);
        btnCopyVoiceId.TabIndex = 2;
        btnCopyVoiceId.Text = "Copy ID";
        btnCopyVoiceId.UseVisualStyleBackColor = true;
        // 
        // btnReplaceReference
        // 
        btnReplaceReference.Location = new Point(0, 40);
        btnReplaceReference.Margin = new Padding(0, 0, 6, 5);
        btnReplaceReference.Name = "btnReplaceReference";
        btnReplaceReference.Size = new Size(75, 29);
        btnReplaceReference.TabIndex = 3;
        btnReplaceReference.Text = "Replace";
        btnReplaceReference.UseVisualStyleBackColor = true;
        // 
        // btnDeleteVoice
        // 
        btnDeleteVoice.ForeColor = Color.Firebrick;
        btnDeleteVoice.Location = new Point(81, 40);
        btnDeleteVoice.Margin = new Padding(0, 0, 6, 5);
        btnDeleteVoice.Name = "btnDeleteVoice";
        btnDeleteVoice.Size = new Size(70, 29);
        btnDeleteVoice.TabIndex = 4;
        btnDeleteVoice.Text = "Delete";
        btnDeleteVoice.UseVisualStyleBackColor = true;
        // 
        // grpAddVoice
        // 
        grpAddVoice.BackColor = Color.White;
        grpAddVoice.Controls.Add(addVoiceLayout);
        grpAddVoice.Dock = DockStyle.Fill;
        grpAddVoice.Location = new Point(0, 375);
        grpAddVoice.Margin = new Padding(0);
        grpAddVoice.Name = "grpAddVoice";
        grpAddVoice.Padding = new Padding(10);
        grpAddVoice.Size = new Size(404, 307);
        grpAddVoice.TabIndex = 1;
        grpAddVoice.TabStop = false;
        grpAddVoice.Text = "Add New Voice";
        // 
        // addVoiceLayout
        // 
        addVoiceLayout.ColumnCount = 3;
        addVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
        addVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        addVoiceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44F));
        addVoiceLayout.Controls.Add(lblNewVoiceName, 0, 0);
        addVoiceLayout.Controls.Add(txtNewVoiceName, 1, 0);
        addVoiceLayout.Controls.Add(lblNewVoiceId, 0, 1);
        addVoiceLayout.Controls.Add(txtNewVoiceId, 1, 1);
        addVoiceLayout.Controls.Add(lblNewVoiceLanguage, 0, 2);
        addVoiceLayout.Controls.Add(cmbNewVoiceLanguage, 1, 2);
        addVoiceLayout.Controls.Add(lblNewVoiceGender, 0, 3);
        addVoiceLayout.Controls.Add(cmbNewVoiceGender, 1, 3);
        addVoiceLayout.Controls.Add(lblNewVoiceDescription, 0, 4);
        addVoiceLayout.Controls.Add(txtNewVoiceDescription, 1, 4);
        addVoiceLayout.Controls.Add(lblNewVoiceFile, 0, 5);
        addVoiceLayout.Controls.Add(txtNewVoiceFile, 1, 5);
        addVoiceLayout.Controls.Add(btnBrowseVoiceFile, 2, 5);
        addVoiceLayout.Controls.Add(pnlAddVoiceActions, 0, 6);
        addVoiceLayout.Dock = DockStyle.Fill;
        addVoiceLayout.Location = new Point(10, 26);
        addVoiceLayout.Name = "addVoiceLayout";
        addVoiceLayout.RowCount = 7;
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 31F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        addVoiceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        addVoiceLayout.Size = new Size(384, 271);
        addVoiceLayout.TabIndex = 0;
        // 
        // lblNewVoiceName
        // 
        lblNewVoiceName.Dock = DockStyle.Fill;
        lblNewVoiceName.Location = new Point(3, 0);
        lblNewVoiceName.Name = "lblNewVoiceName";
        lblNewVoiceName.Size = new Size(80, 31);
        lblNewVoiceName.TabIndex = 0;
        lblNewVoiceName.Text = "Name:";
        lblNewVoiceName.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtNewVoiceName
        // 
        addVoiceLayout.SetColumnSpan(txtNewVoiceName, 2);
        txtNewVoiceName.Dock = DockStyle.Fill;
        txtNewVoiceName.Location = new Point(89, 4);
        txtNewVoiceName.Margin = new Padding(3, 4, 3, 4);
        txtNewVoiceName.Name = "txtNewVoiceName";
        txtNewVoiceName.Size = new Size(292, 23);
        txtNewVoiceName.TabIndex = 1;
        // 
        // lblNewVoiceId
        // 
        lblNewVoiceId.Dock = DockStyle.Fill;
        lblNewVoiceId.Location = new Point(3, 31);
        lblNewVoiceId.Name = "lblNewVoiceId";
        lblNewVoiceId.Size = new Size(80, 31);
        lblNewVoiceId.TabIndex = 20;
        lblNewVoiceId.Text = "Voice ID:";
        lblNewVoiceId.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtNewVoiceId
        // 
        addVoiceLayout.SetColumnSpan(txtNewVoiceId, 2);
        txtNewVoiceId.Dock = DockStyle.Fill;
        txtNewVoiceId.Location = new Point(89, 35);
        txtNewVoiceId.Margin = new Padding(3, 4, 3, 4);
        txtNewVoiceId.Name = "txtNewVoiceId";
        txtNewVoiceId.Size = new Size(292, 23);
        txtNewVoiceId.TabIndex = 21;
        // 
        // lblNewVoiceLanguage
        // 
        lblNewVoiceLanguage.Dock = DockStyle.Fill;
        lblNewVoiceLanguage.Location = new Point(3, 62);
        lblNewVoiceLanguage.Name = "lblNewVoiceLanguage";
        lblNewVoiceLanguage.Size = new Size(80, 31);
        lblNewVoiceLanguage.TabIndex = 2;
        lblNewVoiceLanguage.Text = "Language:";
        lblNewVoiceLanguage.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbNewVoiceLanguage
        // 
        cmbNewVoiceLanguage.Dock = DockStyle.Fill;
        cmbNewVoiceLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNewVoiceLanguage.FormattingEnabled = true;
        cmbNewVoiceLanguage.Items.AddRange(new object[] { "en", "vi", "es", "fr", "de", "it", "pt", "ru", "ja", "ko", "zh" });
        cmbNewVoiceLanguage.Location = new Point(89, 66);
        cmbNewVoiceLanguage.Margin = new Padding(3, 4, 3, 4);
        cmbNewVoiceLanguage.Name = "cmbNewVoiceLanguage";
        cmbNewVoiceLanguage.Size = new Size(248, 23);
        cmbNewVoiceLanguage.TabIndex = 3;
        // 
        // lblNewVoiceGender
        // 
        lblNewVoiceGender.Dock = DockStyle.Fill;
        lblNewVoiceGender.Location = new Point(3, 93);
        lblNewVoiceGender.Name = "lblNewVoiceGender";
        lblNewVoiceGender.Size = new Size(80, 31);
        lblNewVoiceGender.TabIndex = 4;
        lblNewVoiceGender.Text = "Gender:";
        lblNewVoiceGender.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbNewVoiceGender
        // 
        cmbNewVoiceGender.Dock = DockStyle.Fill;
        cmbNewVoiceGender.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNewVoiceGender.FormattingEnabled = true;
        cmbNewVoiceGender.Items.AddRange(new object[] { "Unknown", "Male", "Female" });
        cmbNewVoiceGender.Location = new Point(89, 97);
        cmbNewVoiceGender.Margin = new Padding(3, 4, 3, 4);
        cmbNewVoiceGender.Name = "cmbNewVoiceGender";
        cmbNewVoiceGender.Size = new Size(248, 23);
        cmbNewVoiceGender.TabIndex = 5;
        // 
        // lblNewVoiceDescription
        // 
        lblNewVoiceDescription.Dock = DockStyle.Fill;
        lblNewVoiceDescription.Location = new Point(3, 124);
        lblNewVoiceDescription.Name = "lblNewVoiceDescription";
        lblNewVoiceDescription.Size = new Size(80, 54);
        lblNewVoiceDescription.TabIndex = 6;
        lblNewVoiceDescription.Text = "Description:";
        lblNewVoiceDescription.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtNewVoiceDescription
        // 
        addVoiceLayout.SetColumnSpan(txtNewVoiceDescription, 2);
        txtNewVoiceDescription.Dock = DockStyle.Fill;
        txtNewVoiceDescription.Location = new Point(89, 127);
        txtNewVoiceDescription.Multiline = true;
        txtNewVoiceDescription.Name = "txtNewVoiceDescription";
        txtNewVoiceDescription.ScrollBars = ScrollBars.Vertical;
        txtNewVoiceDescription.Size = new Size(292, 48);
        txtNewVoiceDescription.TabIndex = 7;
        // 
        // lblNewVoiceFile
        // 
        lblNewVoiceFile.Dock = DockStyle.Fill;
        lblNewVoiceFile.Location = new Point(3, 178);
        lblNewVoiceFile.Name = "lblNewVoiceFile";
        lblNewVoiceFile.Size = new Size(80, 34);
        lblNewVoiceFile.TabIndex = 8;
        lblNewVoiceFile.Text = "Reference:";
        lblNewVoiceFile.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtNewVoiceFile
        // 
        txtNewVoiceFile.Dock = DockStyle.Fill;
        txtNewVoiceFile.Location = new Point(89, 183);
        txtNewVoiceFile.Margin = new Padding(3, 5, 3, 4);
        txtNewVoiceFile.Name = "txtNewVoiceFile";
        txtNewVoiceFile.ReadOnly = true;
        txtNewVoiceFile.Size = new Size(248, 23);
        txtNewVoiceFile.TabIndex = 9;
        // 
        // btnBrowseVoiceFile
        // 
        btnBrowseVoiceFile.Dock = DockStyle.Fill;
        btnBrowseVoiceFile.Location = new Point(343, 181);
        btnBrowseVoiceFile.Name = "btnBrowseVoiceFile";
        btnBrowseVoiceFile.Size = new Size(38, 28);
        btnBrowseVoiceFile.TabIndex = 10;
        btnBrowseVoiceFile.Text = "...";
        btnBrowseVoiceFile.UseVisualStyleBackColor = true;
        // 
        // pnlAddVoiceActions
        // 
        addVoiceLayout.SetColumnSpan(pnlAddVoiceActions, 3);
        pnlAddVoiceActions.Controls.Add(btnPreviewNewVoice);
        pnlAddVoiceActions.Controls.Add(btnSaveNewVoice);
        pnlAddVoiceActions.Controls.Add(btnClearNewVoice);
        pnlAddVoiceActions.Dock = DockStyle.Fill;
        pnlAddVoiceActions.Location = new Point(3, 215);
        pnlAddVoiceActions.Name = "pnlAddVoiceActions";
        pnlAddVoiceActions.Padding = new Padding(0, 7, 0, 0);
        pnlAddVoiceActions.Size = new Size(378, 53);
        pnlAddVoiceActions.TabIndex = 11;
        pnlAddVoiceActions.WrapContents = false;
        // 
        // btnPreviewNewVoice
        // 
        btnPreviewNewVoice.Location = new Point(0, 7);
        btnPreviewNewVoice.Margin = new Padding(0, 0, 6, 0);
        btnPreviewNewVoice.Name = "btnPreviewNewVoice";
        btnPreviewNewVoice.Size = new Size(92, 29);
        btnPreviewNewVoice.TabIndex = 0;
        btnPreviewNewVoice.Text = "▶ Preview";
        btnPreviewNewVoice.UseVisualStyleBackColor = true;
        // 
        // btnSaveNewVoice
        // 
        btnSaveNewVoice.BackColor = Color.FromArgb(37, 99, 235);
        btnSaveNewVoice.FlatStyle = FlatStyle.Flat;
        btnSaveNewVoice.ForeColor = Color.White;
        btnSaveNewVoice.Location = new Point(98, 7);
        btnSaveNewVoice.Margin = new Padding(0, 0, 6, 0);
        btnSaveNewVoice.Name = "btnSaveNewVoice";
        btnSaveNewVoice.Size = new Size(100, 29);
        btnSaveNewVoice.TabIndex = 1;
        btnSaveNewVoice.Text = "Add Voice";
        btnSaveNewVoice.UseVisualStyleBackColor = false;
        // 
        // btnClearNewVoice
        // 
        btnClearNewVoice.Location = new Point(204, 7);
        btnClearNewVoice.Margin = new Padding(0);
        btnClearNewVoice.Name = "btnClearNewVoice";
        btnClearNewVoice.Size = new Size(72, 29);
        btnClearNewVoice.TabIndex = 2;
        btnClearNewVoice.Text = "Clear";
        btnClearNewVoice.UseVisualStyleBackColor = true;
        // 
        // tabVideoEditor
        // 
        tabVideoEditor.BackColor = Color.FromArgb(245, 247, 250);
        tabVideoEditor.Controls.Add(videoRootLayout);
        tabVideoEditor.Location = new Point(4, 30);
        tabVideoEditor.Name = "tabVideoEditor";
        tabVideoEditor.Padding = new Padding(8);
        tabVideoEditor.Size = new Size(1272, 766);
        tabVideoEditor.TabIndex = 2;
        tabVideoEditor.Text = "Video Editor";
        // 
        // videoRootLayout
        // 
        videoRootLayout.ColumnCount = 2;
        videoRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        videoRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        videoRootLayout.Controls.Add(grpVideoSource, 0, 0);
        videoRootLayout.Controls.Add(grpVideoOptions, 1, 0);
        videoRootLayout.Controls.Add(grpVideoPreview, 0, 1);
        videoRootLayout.Controls.Add(pnlVideoBottom, 0, 2);
        videoRootLayout.Dock = DockStyle.Fill;
        videoRootLayout.Location = new Point(8, 8);
        videoRootLayout.Name = "videoRootLayout";
        videoRootLayout.RowCount = 3;
        videoRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 240F));
        videoRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        videoRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        videoRootLayout.Size = new Size(1256, 750);
        videoRootLayout.TabIndex = 0;
        // 
        // grpVideoSource
        // 
        grpVideoSource.BackColor = Color.White;
        grpVideoSource.Controls.Add(lblVideoAudio);
        grpVideoSource.Controls.Add(txtVideoAudio);
        grpVideoSource.Controls.Add(btnBrowseVideoAudio);
        grpVideoSource.Controls.Add(lblVideoSrt);
        grpVideoSource.Controls.Add(txtVideoSrt);
        grpVideoSource.Controls.Add(btnBrowseVideoSrt);
        grpVideoSource.Controls.Add(lblImageFolder);
        grpVideoSource.Controls.Add(txtImageFolder);
        grpVideoSource.Controls.Add(btnBrowseImageFolder);
        grpVideoSource.Controls.Add(lblTimelineMode);
        grpVideoSource.Controls.Add(cmbTimelineMode);
        grpVideoSource.Controls.Add(lblImageMapping);
        grpVideoSource.Controls.Add(cmbImageMapping);
        grpVideoSource.Controls.Add(btnAnalyzeTimeline);
        grpVideoSource.Dock = DockStyle.Fill;
        grpVideoSource.Location = new Point(0, 0);
        grpVideoSource.Margin = new Padding(0, 0, 6, 6);
        grpVideoSource.Name = "grpVideoSource";
        grpVideoSource.Size = new Size(810, 234);
        grpVideoSource.TabIndex = 0;
        grpVideoSource.TabStop = false;
        grpVideoSource.Text = "Video Source";
        // 
        // lblVideoAudio
        // 
        lblVideoAudio.AutoSize = true;
        lblVideoAudio.Location = new Point(15, 35);
        lblVideoAudio.Name = "lblVideoAudio";
        lblVideoAudio.Size = new Size(42, 15);
        lblVideoAudio.TabIndex = 0;
        lblVideoAudio.Text = "Audio:";
        // 
        // txtVideoAudio
        // 
        txtVideoAudio.Location = new Point(90, 31);
        txtVideoAudio.Name = "txtVideoAudio";
        txtVideoAudio.Size = new Size(560, 23);
        txtVideoAudio.TabIndex = 1;
        // 
        // btnBrowseVideoAudio
        // 
        btnBrowseVideoAudio.Location = new Point(658, 30);
        btnBrowseVideoAudio.Name = "btnBrowseVideoAudio";
        btnBrowseVideoAudio.Size = new Size(40, 25);
        btnBrowseVideoAudio.TabIndex = 2;
        btnBrowseVideoAudio.Text = "...";
        // 
        // lblVideoSrt
        // 
        lblVideoSrt.AutoSize = true;
        lblVideoSrt.Location = new Point(15, 75);
        lblVideoSrt.Name = "lblVideoSrt";
        lblVideoSrt.Size = new Size(50, 15);
        lblVideoSrt.TabIndex = 3;
        lblVideoSrt.Text = "Subtitle:";
        // 
        // txtVideoSrt
        // 
        txtVideoSrt.Location = new Point(90, 71);
        txtVideoSrt.Name = "txtVideoSrt";
        txtVideoSrt.Size = new Size(560, 23);
        txtVideoSrt.TabIndex = 4;
        // 
        // btnBrowseVideoSrt
        // 
        btnBrowseVideoSrt.Location = new Point(658, 70);
        btnBrowseVideoSrt.Name = "btnBrowseVideoSrt";
        btnBrowseVideoSrt.Size = new Size(40, 25);
        btnBrowseVideoSrt.TabIndex = 5;
        btnBrowseVideoSrt.Text = "...";
        // 
        // lblImageFolder
        // 
        lblImageFolder.AutoSize = true;
        lblImageFolder.Location = new Point(15, 115);
        lblImageFolder.Name = "lblImageFolder";
        lblImageFolder.Size = new Size(48, 15);
        lblImageFolder.TabIndex = 6;
        lblImageFolder.Text = "Images:";
        // 
        // txtImageFolder
        // 
        txtImageFolder.Location = new Point(90, 111);
        txtImageFolder.Name = "txtImageFolder";
        txtImageFolder.Size = new Size(560, 23);
        txtImageFolder.TabIndex = 7;
        // 
        // btnBrowseImageFolder
        // 
        btnBrowseImageFolder.Location = new Point(658, 110);
        btnBrowseImageFolder.Name = "btnBrowseImageFolder";
        btnBrowseImageFolder.Size = new Size(40, 25);
        btnBrowseImageFolder.TabIndex = 8;
        btnBrowseImageFolder.Text = "...";
        // 
        // lblTimelineMode
        // 
        lblTimelineMode.AutoSize = true;
        lblTimelineMode.Location = new Point(15, 160);
        lblTimelineMode.Name = "lblTimelineMode";
        lblTimelineMode.Size = new Size(56, 15);
        lblTimelineMode.TabIndex = 9;
        lblTimelineMode.Text = "Timeline:";
        // 
        // cmbTimelineMode
        // 
        cmbTimelineMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbTimelineMode.Items.AddRange(new object[] { "From SRT", "From Image Filename", "From File Order" });
        cmbTimelineMode.Location = new Point(90, 156);
        cmbTimelineMode.Name = "cmbTimelineMode";
        cmbTimelineMode.Size = new Size(190, 23);
        cmbTimelineMode.TabIndex = 10;
        // 
        // lblImageMapping
        // 
        lblImageMapping.AutoSize = true;
        lblImageMapping.Location = new Point(310, 160);
        lblImageMapping.Name = "lblImageMapping";
        lblImageMapping.Size = new Size(58, 15);
        lblImageMapping.TabIndex = 11;
        lblImageMapping.Text = "Mapping:";
        // 
        // cmbImageMapping
        // 
        cmbImageMapping.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbImageMapping.Items.AddRange(new object[] { "Timestamp Filename", "SRT Number", "File Order" });
        cmbImageMapping.Location = new Point(375, 156);
        cmbImageMapping.Name = "cmbImageMapping";
        cmbImageMapping.Size = new Size(190, 23);
        cmbImageMapping.TabIndex = 12;
        // 
        // btnAnalyzeTimeline
        // 
        btnAnalyzeTimeline.Location = new Point(590, 153);
        btnAnalyzeTimeline.Name = "btnAnalyzeTimeline";
        btnAnalyzeTimeline.Size = new Size(130, 30);
        btnAnalyzeTimeline.TabIndex = 13;
        btnAnalyzeTimeline.Text = "Analyze Timeline";
        // 
        // grpVideoOptions
        // 
        grpVideoOptions.BackColor = Color.White;
        grpVideoOptions.Controls.Add(lblResolution);
        grpVideoOptions.Controls.Add(cmbResolution);
        grpVideoOptions.Controls.Add(lblFps);
        grpVideoOptions.Controls.Add(cmbFps);
        grpVideoOptions.Controls.Add(lblAspectRatio);
        grpVideoOptions.Controls.Add(cmbAspectRatio);
        grpVideoOptions.Controls.Add(lblEncoder);
        grpVideoOptions.Controls.Add(cmbEncoder);
        grpVideoOptions.Controls.Add(lblVideoQuality);
        grpVideoOptions.Controls.Add(cmbVideoQuality);
        grpVideoOptions.Controls.Add(chkKenBurns);
        grpVideoOptions.Controls.Add(chkCrossfade);
        grpVideoOptions.Controls.Add(chkBurnSubtitle);
        grpVideoOptions.Controls.Add(chkHardwareEncoding);
        grpVideoOptions.Controls.Add(lblTransitionDuration);
        grpVideoOptions.Controls.Add(numTransitionDuration);
        grpVideoOptions.Controls.Add(lblTransitionSeconds);
        grpVideoOptions.Dock = DockStyle.Fill;
        grpVideoOptions.Location = new Point(816, 0);
        grpVideoOptions.Margin = new Padding(0, 0, 0, 6);
        grpVideoOptions.Name = "grpVideoOptions";
        grpVideoOptions.Size = new Size(440, 234);
        grpVideoOptions.TabIndex = 1;
        grpVideoOptions.TabStop = false;
        grpVideoOptions.Text = "Render Options";
        // 
        // lblResolution
        // 
        lblResolution.AutoSize = true;
        lblResolution.Location = new Point(15, 32);
        lblResolution.Name = "lblResolution";
        lblResolution.Size = new Size(66, 15);
        lblResolution.TabIndex = 0;
        lblResolution.Text = "Resolution:";
        // 
        // cmbResolution
        // 
        cmbResolution.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbResolution.Items.AddRange(new object[] { "1920x1080", "2560x1440", "3840x2160" });
        cmbResolution.Location = new Point(105, 28);
        cmbResolution.Name = "cmbResolution";
        cmbResolution.Size = new Size(150, 23);
        cmbResolution.TabIndex = 1;
        // 
        // lblFps
        // 
        lblFps.AutoSize = true;
        lblFps.Location = new Point(280, 32);
        lblFps.Name = "lblFps";
        lblFps.Size = new Size(29, 15);
        lblFps.TabIndex = 2;
        lblFps.Text = "FPS:";
        // 
        // cmbFps
        // 
        cmbFps.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbFps.Items.AddRange(new object[] { "24", "30", "60" });
        cmbFps.Location = new Point(320, 28);
        cmbFps.Name = "cmbFps";
        cmbFps.Size = new Size(70, 23);
        cmbFps.TabIndex = 3;
        // 
        // lblAspectRatio
        // 
        lblAspectRatio.AutoSize = true;
        lblAspectRatio.Location = new Point(15, 70);
        lblAspectRatio.Name = "lblAspectRatio";
        lblAspectRatio.Size = new Size(46, 15);
        lblAspectRatio.TabIndex = 4;
        lblAspectRatio.Text = "Aspect:";
        // 
        // cmbAspectRatio
        // 
        cmbAspectRatio.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAspectRatio.Items.AddRange(new object[] { "16:9", "9:16", "1:1" });
        cmbAspectRatio.Location = new Point(105, 66);
        cmbAspectRatio.Name = "cmbAspectRatio";
        cmbAspectRatio.Size = new Size(150, 23);
        cmbAspectRatio.TabIndex = 5;
        // 
        // lblEncoder
        // 
        lblEncoder.AutoSize = true;
        lblEncoder.Location = new Point(15, 108);
        lblEncoder.Name = "lblEncoder";
        lblEncoder.Size = new Size(53, 15);
        lblEncoder.TabIndex = 6;
        lblEncoder.Text = "Encoder:";
        // 
        // cmbEncoder
        // 
        cmbEncoder.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEncoder.Items.AddRange(new object[] { "Auto", "NVIDIA NVENC", "CPU x264", "CPU x265" });
        cmbEncoder.Location = new Point(105, 104);
        cmbEncoder.Name = "cmbEncoder";
        cmbEncoder.Size = new Size(150, 23);
        cmbEncoder.TabIndex = 7;
        // 
        // lblVideoQuality
        // 
        lblVideoQuality.AutoSize = true;
        lblVideoQuality.Location = new Point(280, 108);
        lblVideoQuality.Name = "lblVideoQuality";
        lblVideoQuality.Size = new Size(48, 15);
        lblVideoQuality.TabIndex = 8;
        lblVideoQuality.Text = "Quality:";
        // 
        // cmbVideoQuality
        // 
        cmbVideoQuality.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVideoQuality.Items.AddRange(new object[] { "Fast", "Balanced", "Maximum Quality" });
        cmbVideoQuality.Location = new Point(335, 104);
        cmbVideoQuality.Name = "cmbVideoQuality";
        cmbVideoQuality.Size = new Size(150, 23);
        cmbVideoQuality.TabIndex = 9;
        // 
        // chkKenBurns
        // 
        chkKenBurns.AutoSize = true;
        chkKenBurns.Checked = true;
        chkKenBurns.CheckState = CheckState.Checked;
        chkKenBurns.Location = new Point(15, 145);
        chkKenBurns.Name = "chkKenBurns";
        chkKenBurns.Size = new Size(79, 19);
        chkKenBurns.TabIndex = 10;
        chkKenBurns.Text = "Ken Burns";
        // 
        // chkCrossfade
        // 
        chkCrossfade.AutoSize = true;
        chkCrossfade.Checked = true;
        chkCrossfade.CheckState = CheckState.Checked;
        chkCrossfade.Location = new Point(115, 145);
        chkCrossfade.Name = "chkCrossfade";
        chkCrossfade.Size = new Size(78, 19);
        chkCrossfade.TabIndex = 11;
        chkCrossfade.Text = "Crossfade";
        // 
        // chkBurnSubtitle
        // 
        chkBurnSubtitle.AutoSize = true;
        chkBurnSubtitle.Location = new Point(225, 145);
        chkBurnSubtitle.Name = "chkBurnSubtitle";
        chkBurnSubtitle.Size = new Size(94, 19);
        chkBurnSubtitle.TabIndex = 12;
        chkBurnSubtitle.Text = "Burn Subtitle";
        // 
        // chkHardwareEncoding
        // 
        chkHardwareEncoding.AutoSize = true;
        chkHardwareEncoding.Checked = true;
        chkHardwareEncoding.CheckState = CheckState.Checked;
        chkHardwareEncoding.Location = new Point(345, 145);
        chkHardwareEncoding.Name = "chkHardwareEncoding";
        chkHardwareEncoding.Size = new Size(119, 19);
        chkHardwareEncoding.TabIndex = 13;
        chkHardwareEncoding.Text = "Hardware Encode";
        // 
        // lblTransitionDuration
        // 
        lblTransitionDuration.AutoSize = true;
        lblTransitionDuration.Location = new Point(15, 185);
        lblTransitionDuration.Name = "lblTransitionDuration";
        lblTransitionDuration.Size = new Size(62, 15);
        lblTransitionDuration.TabIndex = 14;
        lblTransitionDuration.Text = "Transition:";
        // 
        // numTransitionDuration
        // 
        numTransitionDuration.DecimalPlaces = 2;
        numTransitionDuration.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
        numTransitionDuration.Location = new Point(105, 181);
        numTransitionDuration.Maximum = new decimal(new int[] { 300, 0, 0, 131072 });
        numTransitionDuration.Name = "numTransitionDuration";
        numTransitionDuration.Size = new Size(80, 23);
        numTransitionDuration.TabIndex = 15;
        numTransitionDuration.Value = new decimal(new int[] { 35, 0, 0, 131072 });
        // 
        // lblTransitionSeconds
        // 
        lblTransitionSeconds.AutoSize = true;
        lblTransitionSeconds.Location = new Point(192, 185);
        lblTransitionSeconds.Name = "lblTransitionSeconds";
        lblTransitionSeconds.Size = new Size(50, 15);
        lblTransitionSeconds.TabIndex = 16;
        lblTransitionSeconds.Text = "seconds";
        // 
        // grpVideoPreview
        // 
        grpVideoPreview.BackColor = Color.White;
        videoRootLayout.SetColumnSpan(grpVideoPreview, 2);
        grpVideoPreview.Controls.Add(dgvVideoTimeline);
        grpVideoPreview.Dock = DockStyle.Fill;
        grpVideoPreview.Location = new Point(0, 240);
        grpVideoPreview.Margin = new Padding(0, 0, 0, 6);
        grpVideoPreview.Name = "grpVideoPreview";
        grpVideoPreview.Size = new Size(1256, 458);
        grpVideoPreview.TabIndex = 2;
        grpVideoPreview.TabStop = false;
        grpVideoPreview.Text = "Timeline";
        // 
        // dgvVideoTimeline
        // 
        dgvVideoTimeline.AllowUserToAddRows = false;
        dgvVideoTimeline.AllowUserToDeleteRows = false;
        dgvVideoTimeline.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvVideoTimeline.BackgroundColor = Color.White;
        dgvVideoTimeline.Columns.AddRange(new DataGridViewColumn[] { videoColId, videoColStart, videoColEnd, videoColDuration, videoColImage, videoColSubtitle, videoColStatus });
        dgvVideoTimeline.Dock = DockStyle.Fill;
        dgvVideoTimeline.Location = new Point(3, 19);
        dgvVideoTimeline.MultiSelect = false;
        dgvVideoTimeline.Name = "dgvVideoTimeline";
        dgvVideoTimeline.ReadOnly = true;
        dgvVideoTimeline.RowHeadersVisible = false;
        dgvVideoTimeline.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvVideoTimeline.Size = new Size(1250, 436);
        dgvVideoTimeline.TabIndex = 0;
        // 
        // videoColId
        // 
        videoColId.FillWeight = 35F;
        videoColId.HeaderText = "Id";
        videoColId.Name = "videoColId";
        videoColId.ReadOnly = true;
        // 
        // videoColStart
        // 
        videoColStart.FillWeight = 70F;
        videoColStart.HeaderText = "Start";
        videoColStart.Name = "videoColStart";
        videoColStart.ReadOnly = true;
        // 
        // videoColEnd
        // 
        videoColEnd.FillWeight = 70F;
        videoColEnd.HeaderText = "End";
        videoColEnd.Name = "videoColEnd";
        videoColEnd.ReadOnly = true;
        // 
        // videoColDuration
        // 
        videoColDuration.FillWeight = 60F;
        videoColDuration.HeaderText = "Duration";
        videoColDuration.Name = "videoColDuration";
        videoColDuration.ReadOnly = true;
        // 
        // videoColImage
        // 
        videoColImage.FillWeight = 140F;
        videoColImage.HeaderText = "Image";
        videoColImage.Name = "videoColImage";
        videoColImage.ReadOnly = true;
        // 
        // videoColSubtitle
        // 
        videoColSubtitle.FillWeight = 320F;
        videoColSubtitle.HeaderText = "Subtitle";
        videoColSubtitle.Name = "videoColSubtitle";
        videoColSubtitle.ReadOnly = true;
        // 
        // videoColStatus
        // 
        videoColStatus.FillWeight = 75F;
        videoColStatus.HeaderText = "Status";
        videoColStatus.Name = "videoColStatus";
        videoColStatus.ReadOnly = true;
        // 
        // pnlVideoBottom
        // 
        pnlVideoBottom.ColumnCount = 5;
        videoRootLayout.SetColumnSpan(pnlVideoBottom, 2);
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        pnlVideoBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
        pnlVideoBottom.Controls.Add(lblVideoRenderStatus, 0, 0);
        pnlVideoBottom.Controls.Add(btnPreviewVideo, 1, 0);
        pnlVideoBottom.Controls.Add(btnRenderVideo, 2, 0);
        pnlVideoBottom.Controls.Add(btnOpenVideoOutput, 3, 0);
        pnlVideoBottom.Controls.Add(progressVideoRender, 4, 0);
        pnlVideoBottom.Dock = DockStyle.Fill;
        pnlVideoBottom.Location = new Point(3, 707);
        pnlVideoBottom.Name = "pnlVideoBottom";
        pnlVideoBottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
        pnlVideoBottom.Size = new Size(1250, 40);
        pnlVideoBottom.TabIndex = 3;
        // 
        // lblVideoRenderStatus
        // 
        lblVideoRenderStatus.Dock = DockStyle.Fill;
        lblVideoRenderStatus.Location = new Point(3, 0);
        lblVideoRenderStatus.Name = "lblVideoRenderStatus";
        lblVideoRenderStatus.Size = new Size(749, 40);
        lblVideoRenderStatus.TabIndex = 0;
        lblVideoRenderStatus.Text = "Ready";
        lblVideoRenderStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // btnPreviewVideo
        // 
        btnPreviewVideo.Dock = DockStyle.Fill;
        btnPreviewVideo.Location = new Point(759, 7);
        btnPreviewVideo.Margin = new Padding(4, 7, 4, 7);
        btnPreviewVideo.Name = "btnPreviewVideo";
        btnPreviewVideo.Size = new Size(122, 26);
        btnPreviewVideo.TabIndex = 1;
        btnPreviewVideo.Text = "Preview";
        // 
        // btnRenderVideo
        // 
        btnRenderVideo.BackColor = Color.FromArgb(37, 99, 235);
        btnRenderVideo.Dock = DockStyle.Fill;
        btnRenderVideo.FlatStyle = FlatStyle.Flat;
        btnRenderVideo.ForeColor = Color.White;
        btnRenderVideo.Location = new Point(889, 7);
        btnRenderVideo.Margin = new Padding(4, 7, 4, 7);
        btnRenderVideo.Name = "btnRenderVideo";
        btnRenderVideo.Size = new Size(107, 26);
        btnRenderVideo.TabIndex = 2;
        btnRenderVideo.Text = "Render Video";
        btnRenderVideo.UseVisualStyleBackColor = false;
        // 
        // btnOpenVideoOutput
        // 
        btnOpenVideoOutput.Dock = DockStyle.Fill;
        btnOpenVideoOutput.Location = new Point(1004, 7);
        btnOpenVideoOutput.Margin = new Padding(4, 7, 4, 7);
        btnOpenVideoOutput.Name = "btnOpenVideoOutput";
        btnOpenVideoOutput.Size = new Size(112, 26);
        btnOpenVideoOutput.TabIndex = 3;
        btnOpenVideoOutput.Text = "Open Output";
        // 
        // progressVideoRender
        // 
        progressVideoRender.Dock = DockStyle.Fill;
        progressVideoRender.Location = new Point(1128, 12);
        progressVideoRender.Margin = new Padding(8, 12, 8, 12);
        progressVideoRender.Name = "progressVideoRender";
        progressVideoRender.Size = new Size(114, 16);
        progressVideoRender.TabIndex = 4;
        // 
        // tabSettings
        // 
        tabSettings.BackColor = Color.FromArgb(245, 247, 250);
        tabSettings.Controls.Add(grpAppSettings);
        tabSettings.Location = new Point(4, 30);
        tabSettings.Name = "tabSettings";
        tabSettings.Padding = new Padding(16);
        tabSettings.Size = new Size(1272, 766);
        tabSettings.TabIndex = 2;
        tabSettings.Text = "Settings";
        // 
        // grpAppSettings
        // 
        grpAppSettings.BackColor = Color.White;
        grpAppSettings.Controls.Add(settingsLayout);
        grpAppSettings.Dock = DockStyle.Fill;
        grpAppSettings.Location = new Point(16, 16);
        grpAppSettings.Name = "grpAppSettings";
        grpAppSettings.Padding = new Padding(12);
        grpAppSettings.Size = new Size(1240, 734);
        grpAppSettings.TabIndex = 0;
        grpAppSettings.TabStop = false;
        grpAppSettings.Text = "Application Settings";
        // 
        // settingsLayout
        // 
        settingsLayout.ColumnCount = 3;
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
        settingsLayout.Controls.Add(lblOutputFolder, 0, 0);
        settingsLayout.Controls.Add(txtOutputFolder, 1, 0);
        settingsLayout.Controls.Add(btnBrowseOutputFolder, 2, 0);
        settingsLayout.Controls.Add(lblMaxChars, 0, 1);
        settingsLayout.Controls.Add(numMaxChars, 1, 1);
        settingsLayout.Controls.Add(lblSilence, 0, 2);
        settingsLayout.Controls.Add(numSilence, 1, 2);
        settingsLayout.Controls.Add(lblOutputFormat, 0, 3);
        settingsLayout.Controls.Add(cmbOutputFormat, 1, 3);
        settingsLayout.Controls.Add(lblMp3Bitrate, 0, 4);
        settingsLayout.Controls.Add(cmbMp3Bitrate, 1, 4);
        settingsLayout.Controls.Add(btnSaveSettings, 1, 5);
        settingsLayout.Dock = DockStyle.Fill;
        settingsLayout.Location = new Point(12, 28);
        settingsLayout.Name = "settingsLayout";
        settingsLayout.RowCount = 6;
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        settingsLayout.Size = new Size(1216, 694);
        settingsLayout.TabIndex = 0;
        // 
        // lblOutputFolder
        // 
        lblOutputFolder.Dock = DockStyle.Fill;
        lblOutputFolder.Location = new Point(3, 0);
        lblOutputFolder.Name = "lblOutputFolder";
        lblOutputFolder.Size = new Size(119, 42);
        lblOutputFolder.TabIndex = 0;
        lblOutputFolder.Text = "Output folder:";
        lblOutputFolder.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtOutputFolder
        // 
        txtOutputFolder.Dock = DockStyle.Fill;
        txtOutputFolder.Location = new Point(128, 8);
        txtOutputFolder.Margin = new Padding(3, 8, 3, 8);
        txtOutputFolder.Name = "txtOutputFolder";
        txtOutputFolder.Size = new Size(1037, 23);
        txtOutputFolder.TabIndex = 1;
        // 
        // btnBrowseOutputFolder
        // 
        btnBrowseOutputFolder.Dock = DockStyle.Fill;
        btnBrowseOutputFolder.Location = new Point(1171, 7);
        btnBrowseOutputFolder.Margin = new Padding(3, 7, 3, 7);
        btnBrowseOutputFolder.Name = "btnBrowseOutputFolder";
        btnBrowseOutputFolder.Size = new Size(42, 28);
        btnBrowseOutputFolder.TabIndex = 2;
        btnBrowseOutputFolder.Text = "...";
        btnBrowseOutputFolder.UseVisualStyleBackColor = true;
        // 
        // lblMaxChars
        // 
        lblMaxChars.Dock = DockStyle.Fill;
        lblMaxChars.Location = new Point(3, 42);
        lblMaxChars.Name = "lblMaxChars";
        lblMaxChars.Size = new Size(119, 42);
        lblMaxChars.TabIndex = 3;
        lblMaxChars.Text = "Max characters:";
        lblMaxChars.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // numMaxChars
        // 
        numMaxChars.Location = new Point(128, 45);
        numMaxChars.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        numMaxChars.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
        numMaxChars.Name = "numMaxChars";
        numMaxChars.Size = new Size(120, 23);
        numMaxChars.TabIndex = 4;
        numMaxChars.Value = new decimal(new int[] { 300, 0, 0, 0 });
        // 
        // lblSilence
        // 
        lblSilence.Dock = DockStyle.Fill;
        lblSilence.Location = new Point(3, 84);
        lblSilence.Name = "lblSilence";
        lblSilence.Size = new Size(119, 42);
        lblSilence.TabIndex = 5;
        lblSilence.Text = "Silence (ms):";
        lblSilence.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // numSilence
        // 
        numSilence.Increment = new decimal(new int[] { 50, 0, 0, 0 });
        numSilence.Location = new Point(128, 87);
        numSilence.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
        numSilence.Name = "numSilence";
        numSilence.Size = new Size(120, 23);
        numSilence.TabIndex = 6;
        numSilence.Value = new decimal(new int[] { 250, 0, 0, 0 });
        // 
        // lblOutputFormat
        // 
        lblOutputFormat.Dock = DockStyle.Fill;
        lblOutputFormat.Location = new Point(3, 126);
        lblOutputFormat.Name = "lblOutputFormat";
        lblOutputFormat.Size = new Size(119, 42);
        lblOutputFormat.TabIndex = 7;
        lblOutputFormat.Text = "Output format:";
        lblOutputFormat.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbOutputFormat
        // 
        cmbOutputFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbOutputFormat.FormattingEnabled = true;
        cmbOutputFormat.Items.AddRange(new object[] { "WAV", "MP3" });
        cmbOutputFormat.Location = new Point(128, 129);
        cmbOutputFormat.Name = "cmbOutputFormat";
        cmbOutputFormat.Size = new Size(140, 23);
        cmbOutputFormat.TabIndex = 8;
        // 
        // lblMp3Bitrate
        // 
        lblMp3Bitrate.Dock = DockStyle.Fill;
        lblMp3Bitrate.Location = new Point(3, 168);
        lblMp3Bitrate.Name = "lblMp3Bitrate";
        lblMp3Bitrate.Size = new Size(119, 42);
        lblMp3Bitrate.TabIndex = 9;
        lblMp3Bitrate.Text = "MP3 bitrate:";
        lblMp3Bitrate.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cmbMp3Bitrate
        // 
        cmbMp3Bitrate.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbMp3Bitrate.FormattingEnabled = true;
        cmbMp3Bitrate.Items.AddRange(new object[] { "128 kbps", "192 kbps", "256 kbps", "320 kbps" });
        cmbMp3Bitrate.Location = new Point(128, 171);
        cmbMp3Bitrate.Name = "cmbMp3Bitrate";
        cmbMp3Bitrate.Size = new Size(140, 23);
        cmbMp3Bitrate.TabIndex = 10;
        // 
        // btnSaveSettings
        // 
        btnSaveSettings.BackColor = Color.FromArgb(37, 99, 235);
        btnSaveSettings.FlatStyle = FlatStyle.Flat;
        btnSaveSettings.ForeColor = Color.White;
        btnSaveSettings.Location = new Point(128, 213);
        btnSaveSettings.Name = "btnSaveSettings";
        btnSaveSettings.Size = new Size(130, 32);
        btnSaveSettings.TabIndex = 11;
        btnSaveSettings.Text = "Save Settings";
        btnSaveSettings.UseVisualStyleBackColor = false;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(245, 247, 250);
        ClientSize = new Size(1280, 800);
        Controls.Add(mainTabControl);
        Font = new Font("Segoe UI", 9F);
        ForeColor = Color.FromArgb(40, 45, 55);
        MinimumSize = new Size(1100, 700);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "DvaForDev AI TTS Studio";
        Load += Form1_Load;
        mainTabControl.ResumeLayout(false);
        tabTtsBatch.ResumeLayout(false);
        ttsRootLayout.ResumeLayout(false);
        ttsRootLayout.PerformLayout();
        ttsTopLayout.ResumeLayout(false);
        grpVoice.ResumeLayout(false);
        grpVoice.PerformLayout();
        grpVoiceSettings.ResumeLayout(false);
        grpVoiceSettings.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numSpeed).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStability).EndInit();
        ((System.ComponentModel.ISupportInitialize)numSimilarity).EndInit();
        ((System.ComponentModel.ISupportInitialize)numStyle).EndInit();
        grpSystem.ResumeLayout(false);
        grpSystem.PerformLayout();
        batchOptionsLayout.ResumeLayout(false);
        grpBatchJob.ResumeLayout(false);
        grpBatchJob.PerformLayout();
        grpOptions.ResumeLayout(false);
        grpOptions.PerformLayout();
        pnlToolbar.ResumeLayout(false);
        statusLayout.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvJobs).EndInit();
        tabVoiceLibrary.ResumeLayout(false);
        libraryRootLayout.ResumeLayout(false);
        libraryHeaderLayout.ResumeLayout(false);
        libraryHeaderLayout.PerformLayout();
        splitVoiceLibrary.Panel1.ResumeLayout(false);
        splitVoiceLibrary.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitVoiceLibrary).EndInit();
        splitVoiceLibrary.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvVoiceLibrary).EndInit();
        libraryRightLayout.ResumeLayout(false);
        grpSelectedVoice.ResumeLayout(false);
        selectedVoiceLayout.ResumeLayout(false);
        selectedVoiceLayout.PerformLayout();
        pnlVoiceActions.ResumeLayout(false);
        grpAddVoice.ResumeLayout(false);
        addVoiceLayout.ResumeLayout(false);
        addVoiceLayout.PerformLayout();
        pnlAddVoiceActions.ResumeLayout(false);
        tabVideoEditor.ResumeLayout(false);
        videoRootLayout.ResumeLayout(false);
        grpVideoSource.ResumeLayout(false);
        grpVideoSource.PerformLayout();
        grpVideoOptions.ResumeLayout(false);
        grpVideoOptions.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numTransitionDuration).EndInit();
        grpVideoPreview.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dgvVideoTimeline).EndInit();
        pnlVideoBottom.ResumeLayout(false);
        tabSettings.ResumeLayout(false);
        grpAppSettings.ResumeLayout(false);
        settingsLayout.ResumeLayout(false);
        settingsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numMaxChars).EndInit();
        ((System.ComponentModel.ISupportInitialize)numSilence).EndInit();
        ResumeLayout(false);
    }
    private Label lblTransitionSeconds;
    private TableLayoutPanel pnlVideoBottom;
}
