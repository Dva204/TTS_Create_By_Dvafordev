using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

public sealed partial class AiVideoStudioControl : UserControl
{
    private static readonly (string Id, string Label)[] EffectOptions = {
        ("whiteboard", "Tay vẽ Whiteboard"),
        ("layer", "Hiện lần lượt Layer"),
        ("map", "Bản đồ (sắp có)"),
        ("timeline", "Dòng thời gian (sắp có)"),
        ("image-motion", "Ảnh chuyển động Zoom/Pan"),
        ("infographic", "Sơ đồ/biểu đồ (sắp có)"),
        ("text", "Hiệu ứng chữ"),
        ("image-story", "Tranh AI kể chuyện")
    };

    private readonly TextBox titleInput = new() { Dock = DockStyle.Fill };
    private readonly TextBox audioInput = new() { Dock = DockStyle.Fill };
    private readonly TextBox subtitleInput = new() { Dock = DockStyle.Fill };
    private readonly TextBox imageFolderInput = new() { Dock = DockStyle.Fill };
    private readonly ComboBox styleInput = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown cueCount = new() { Minimum = 1, Maximum = 20, Value = 3, Width = 70 };
    private readonly ComboBox groupingInput = new() {
        DropDownStyle = ComboBoxStyle.DropDownList, Width = 165
    };
    private readonly DataGridView storyboardGrid = new() {
        Dock = DockStyle.Fill, ReadOnly = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoGenerateColumns = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
    };
    private readonly Label status = new() {
        AutoEllipsis = true, Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(7, 0, 0, 0),
        BackColor = Color.White, ForeColor = Color.FromArgb(45, 70, 94)
    };
    private AiVideoProject project = new();
    private BindingList<AiVideoScene> scenes = new();
    private bool dirty;

    public AiVideoStudioControl()
    {
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        Dock = DockStyle.Fill;
        var view = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(9, 7, 9, 6),
            RowCount = 5, ColumnCount = 1,
            BackColor = Color.FromArgb(244, 246, 249)
        };
        // Compact the inputs and make the storyboard / player area flexible.
        view.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        view.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        view.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        view.RowStyles.Add(new RowStyle(SizeType.Absolute, 205));
        view.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        Controls.Add(view);

        var inputs = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7,
            AutoScroll = true, Padding = new Padding(6, 1, 6, 0),
            BackColor = Color.White
        };
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 142));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 133));
        for (var i = 0; i < 7; i++)
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        view.Controls.Add(inputs, 0, 1);

        void AddLabel(string name, int row) =>
            inputs.Controls.Add(new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);

        AddLabel("Tên dự án", 0);
        inputs.Controls.Add(titleInput, 1, 0);
        var newProject = new Button { Text = "Dự án mới", Dock = DockStyle.Fill };
        inputs.Controls.Add(newProject, 2, 0);
        newProject.Click += (_, _) => { if (ConfirmDiscard()) ResetProject(); };

        void AddFileRow(int row, string label, TextBox input, string filter, bool folder = false)
        {
            AddLabel(label, row);
            inputs.Controls.Add(input, 1, row);
            var select = new Button { Text = "Chọn...", Dock = DockStyle.Fill };
            inputs.Controls.Add(select, 2, row);
            select.Click += (_, _) => {
                if (folder)
                {
                    using var picker = new FolderBrowserDialog();
                    if (picker.ShowDialog(this) == DialogResult.OK)
                        input.Text = picker.SelectedPath;
                }
                else
                {
                    using var picker = new OpenFileDialog { Filter = filter };
                    if (picker.ShowDialog(this) == DialogResult.OK)
                        input.Text = picker.FileName;
                }
                dirty = true;
            };
        }

        AddFileRow(1, "Audio TTS (tùy chọn)", audioInput, "Audio|*.mp3;*.wav;*.m4a;*.flac");
        AddFileRow(2, "Phụ đề SRT", subtitleInput, "SubRip|*.srt");
        AddFileRow(3, "Thư mục ảnh", imageFolderInput, "", folder: true);

        AddLabel("Thiết lập storyboard", 4);
        var options = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, FlowDirection = FlowDirection.LeftToRight };
        styleInput.DataSource = EffectOptions.Select(a => new StyleOption(a.Id, a.Label)).ToList();
        styleInput.DisplayMember = nameof(StyleOption.Label);
        styleInput.ValueMember = nameof(StyleOption.Id);
        styleInput.SelectedValue = "image-motion";
        groupingInput.Items.AddRange(new object[] {
            "Theo số ảnh (khuyên dùng)", "Theo số câu SRT"
        });
        groupingInput.SelectedIndex = 1;
        groupingInput.SelectedIndexChanged += (_, _) => {
            cueCount.Enabled = groupingInput.SelectedIndex == 1;
        };
        options.Controls.Add(groupingInput);
        options.Controls.Add(new Label { Text = "Câu/cảnh", AutoSize = true, Margin = new Padding(8, 9, 6, 0) });
        options.Controls.Add(cueCount);
        cueCount.Enabled = true;
        options.Controls.Add(new Label { Text = "Kiểu mặc định", AutoSize = true, Margin = new Padding(10, 9, 6, 0) });
        options.Controls.Add(styleInput);
        inputs.Controls.Add(options, 1, 4);
        var build = new Button { Text = "Tạo storyboard", Dock = DockStyle.Fill };
        build.Click += (_, _) => GenerateStoryboard();
        inputs.Controls.Add(build, 2, 4);

        AddLabel("Quản lý project", 5);
        var fileButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        Button AddButton(string name, Action action)
        {
            var btn = new Button { Text = name, AutoSize = true, Height = 30, Margin = new Padding(2, 2, 8, 2) };
            btn.Click += (_, _) => action();
            fileButtons.Controls.Add(btn);
            return btn;
        }
        AddButton("Mở project JSON", OpenProject);
        AddButton("Lưu project", () => SaveProject());
        AddButton("Lưu thành...", () => SaveProject(asNew: true));
        ConfigureRenderButtons(fileButtons);
        inputs.Controls.Add(fileButtons, 1, 5);
        var openAsset = new Button { Text = "Xem ảnh đã chọn", Dock = DockStyle.Fill };
        openAsset.Click += (_, _) => OpenSelectedImage();
        inputs.Controls.Add(openAsset, 2, 5);

        AddLabel("Quản lý phân cảnh", 6);
        var sceneButtons = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        ConfigureSceneButtons(sceneButtons);
        inputs.Controls.Add(sceneButtons, 1, 6);
        inputs.SetColumnSpan(sceneButtons, 2);

        ConfigureGrid();
        ConfigureSceneTimeline(view);
        // The previous 58% grid / 42% player split made a 16:9 video tiny
        // (typically ~180px wide) because its preview region was too shallow.
        // Drag the splitter to allocate as much area as needed to the movie.
        workspaceSplit = new SplitContainer {
            Dock = DockStyle.Fill, Orientation = Orientation.Vertical,
            SplitterWidth = 7, Margin = new Padding(0),
            BackColor = Color.FromArgb(210, 217, 227),
            Panel1MinSize = 0, Panel2MinSize = 0
        };
        var editor = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            BackColor = Color.White, Padding = new Padding(4)
        };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        var editorTitle = new Label {
            Text = "BẢNG PHÂN CẢNH · CHỈNH SỬA STORYBOARD",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = Color.FromArgb(34, 55, 84)
        };
        var editorHint = new Label {
            Text = "Chọn cảnh để xem ảnh / lời thoại · Nhấp đôi cột ảnh để thay hình",
            Dock = DockStyle.Fill, AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(83, 101, 121)
        };
        editor.Controls.Add(editorTitle, 0, 0);
        editor.Controls.Add(storyboardGrid, 0, 1);
        editor.Controls.Add(editorHint, 0, 2);
        workspaceSplit.Panel1.Controls.Add(editor);
        ConfigureInlinePreview(workspaceSplit.Panel2);
        view.Controls.Add(workspaceSplit, 0, 2);
        // Keep usability on restored, narrow windows as well as maximized displays.
        workspaceSplit.SizeChanged += (_, _) => UpdateStudioLayout();
        workspaceSplit.HandleCreated += (_, _) => UpdateStudioLayout();
        view.Controls.Add(status, 0, 4);
        titleInput.TextChanged += (_, _) => dirty = true;
        audioInput.TextChanged += (_, _) => dirty = true;
        subtitleInput.TextChanged += (_, _) => dirty = true;
        imageFolderInput.TextChanged += (_, _) => dirty = true;
        storyboardGrid.CellValueChanged += (_, _) => {
            dirty = true;
            RebuildSceneTimeline();
            ShowSelectedSceneImage();
        };
        storyboardGrid.SelectionChanged += (_, _) => {
            HighlightSelectedScene();
            UpdateSelectedScenePreview();
            UpdateCapcutInspector();
            capcutAudioLane.Invalidate();
        };
        storyboardGrid.CurrentCellDirtyStateChanged += (_, _) => {
            if (storyboardGrid.IsCurrentCellDirty)
                storyboardGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        storyboardGrid.CellDoubleClick += (_, args) => {
            if (args.RowIndex < 0 || args.ColumnIndex < 0 ||
                storyboardGrid.Columns[args.ColumnIndex].DataPropertyName != nameof(AiVideoScene.ImagePath)) return;
            using var picker = new OpenFileDialog {
                Filter = "Ảnh|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.svg",
                InitialDirectory = Directory.Exists(imageFolderInput.Text)
                    ? imageFolderInput.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                scenes[args.RowIndex].ImagePath = picker.FileName;
                storyboardGrid.Refresh();
                RebuildSceneTimeline();
                ShowSelectedSceneImage();
                dirty = true;
            }
        };
        BuildSimpleShell(view, inputs, fileButtons);
        ResetProject();
        Disposed += (_, _) => ReleaseInlinePreview();
    }

    private sealed record StyleOption(string Id, string Label);

    private void ConfigureGrid()
    {
        void TextColumn(string heading, string field, int width, bool readOnly = false)
        {
            storyboardGrid.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = heading, DataPropertyName = field, Width = width,
                ReadOnly = readOnly, SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        TextColumn("#", nameof(AiVideoScene.Index), 40, true);
        TextColumn("SRT từ", nameof(AiVideoScene.CueFrom), 52, true);
        TextColumn("Đến", nameof(AiVideoScene.CueTo), 47, true);
        TextColumn("Bắt đầu", nameof(AiVideoScene.StartMs), 82, true);
        TextColumn("Kết thúc", nameof(AiVideoScene.EndMs), 82, true);
        var effects = new DataGridViewComboBoxColumn {
            HeaderText = "Hiệu ứng", DataPropertyName = nameof(AiVideoScene.StyleId),
            Width = 159, DataSource = EffectOptions.Select(x => new StyleOption(x.Id, x.Label)).ToList(),
            DisplayMember = nameof(StyleOption.Label), ValueMember = nameof(StyleOption.Id),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat
        };
        storyboardGrid.Columns.Add(effects);
        storyboardGrid.Columns.Add(new DataGridViewComboBoxColumn {
            HeaderText = "Chuyển cảnh",
            DataPropertyName = nameof(AiVideoScene.Transition),
            Width = 92, DataSource = new[] { "fade", "cut" },
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        });
        TextColumn("Ảnh (nhấp đôi để chọn)", nameof(AiVideoScene.ImagePath), 195);
        TextColumn("Director", nameof(AiVideoScene.DirectorSummary), 220, true);
        TextColumn("Lời thuyết minh", nameof(AiVideoScene.Narration), 300, true);
        TextColumn("Ghi chú", nameof(AiVideoScene.Notes), 240);
        storyboardGrid.EnableHeadersVisualStyles = false;
        storyboardGrid.BackgroundColor = Color.White;
        storyboardGrid.BorderStyle = BorderStyle.None;
        storyboardGrid.ColumnHeadersHeight = 32;
        storyboardGrid.RowTemplate.Height = 31;
        storyboardGrid.GridColor = Color.FromArgb(224, 230, 236);
        storyboardGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 237, 246);
        storyboardGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 47, 69);
        storyboardGrid.ColumnHeadersDefaultCellStyle.Font = new Font(
            storyboardGrid.Font, FontStyle.Bold);
        storyboardGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 250, 255);
        storyboardGrid.DataError += (_, args) => {
            status.Text = "Cảnh có kiểu hiệu ứng không hợp lệ hoặc dữ liệu chưa được lưu.";
            args.ThrowException = false;
        };
    }

    private bool ConfirmDiscard() =>
        !dirty || MessageBox.Show(this, "Có thay đổi chưa lưu. Tiếp tục và bỏ các thay đổi?",
            "AI Video Studio", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private void ResetProject()
    {
        project = new AiVideoProject();
        scenes = new BindingList<AiVideoScene>();
        storyboardGrid.DataSource = scenes;
        RebuildSceneTimeline();
        ResetSelectedScenePreviewTracking();
        titleInput.Text = project.Title;
        audioInput.Clear(); subtitleInput.Clear(); imageFolderInput.Clear();
        status.Text = "Dự án mới. Import TTS/SRT rồi bấm Tạo storyboard.";
        dirty = false;
    }

    private void GenerateStoryboard()
    {
        try
        {
            if (scenes.Count > 0 &&
                MessageBox.Show(this, "Tạo lại storyboard sẽ thay toàn bộ scene hiện tại. Đồng ý?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            var srt = AiVideoProjectService.ReadSrt(subtitleInput.Text);
            var style = styleInput.SelectedValue?.ToString() ?? "whiteboard";
            var items = groupingInput.SelectedIndex == 0
                ? AiVideoProjectService.GenerateByImageCount(srt, imageFolderInput.Text, style)
                : AiVideoProjectService.GenerateStoryboard(srt, (int)cueCount.Value, imageFolderInput.Text, style);
            scenes = new BindingList<AiVideoScene>(items);
            storyboardGrid.DataSource = scenes;
            RebuildSceneTimeline();
            ResetSelectedScenePreviewTracking();
            dirty = true;
            status.Text = $"Đã tạo {scenes.Count} phân cảnh từ {srt.Count} mục SRT. Chỉnh hiệu ứng, ảnh và ghi chú trước khi lưu.";
        }
        catch (Exception error)
        {
            MessageBox.Show(this, error.Message, "Lỗi tạo storyboard", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateFromForm()
    {
        project.Title = titleInput.Text.Trim();
        project.AudioPath = audioInput.Text.Trim();
        project.SubtitlePath = subtitleInput.Text.Trim();
        project.ImageFolder = imageFolderInput.Text.Trim();
        storyboardGrid.EndEdit();
        project.Scenes = scenes.ToList();
    }

    private void SaveProject(bool asNew = false)
    {
        try
        {
            UpdateFromForm();
            string file = project.FilePath;
            if (asNew || string.IsNullOrWhiteSpace(file))
            {
                using var dialog = new SaveFileDialog {
                    FileName = "project.ai-video.json", DefaultExt = "json",
                    Filter = "AI Video Project|*.json" };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                file = dialog.FileName;
                if (File.Exists(file) &&
                    MessageBox.Show(this, "Ghi đè project này?", "Xác nhận",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }
            AiVideoProjectService.Save(project, file);
            dirty = false;
            status.Text = "Đã lưu project: " + file;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lỗi lưu project", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenProject()
    {
        if (!ConfirmDiscard()) return;
        using var picker = new OpenFileDialog { Filter = "AI Video Project|*.json" };
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            project = AiVideoProjectService.Load(picker.FileName);
            scenes = new BindingList<AiVideoScene>(project.Scenes);
            storyboardGrid.DataSource = scenes;
            RebuildSceneTimeline();
            ResetSelectedScenePreviewTracking();
            titleInput.Text = project.Title;
            audioInput.Text = project.AudioPath;
            subtitleInput.Text = project.SubtitlePath;
            imageFolderInput.Text = project.ImageFolder;
            dirty = false;
            status.Text = $"Đã mở {Path.GetFileName(picker.FileName)} ({scenes.Count} cảnh).";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lỗi mở project", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenSelectedImage()
    {
        if (storyboardGrid.CurrentRow?.DataBoundItem is not AiVideoScene scene || !File.Exists(scene.ImagePath))
        {
            MessageBox.Show(this, "Hãy chọn một scene có ảnh hợp lệ.");
            return;
        }
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(scene.ImagePath) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message); }
    }
}

