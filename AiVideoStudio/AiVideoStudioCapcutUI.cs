using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// CapCut-inspired layout, driven by the existing scene BindingList.
/// No scene/audio/SRT model changes. Detail grid stays available on demand.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private TableLayoutPanel? capcutWorkspace;

    private TableLayoutPanel? capcutDetails;
    private TableLayoutPanel? capcutRoot;
    private TableLayoutPanel? capcutSources;
    private FlowLayoutPanel? capcutLibrary;
    private Panel? capcutLibraryPanel;
    private Panel? capcutInspectorPanel;
    private Label? capcutProjectTitle;
    private Label? capcutSelectedTitle;
    private Label? capcutImagePath;
    private Label? capcutTimeLabel;
    private ComboBox? capcutEffect;
    private ComboBox? capcutTransition;
    private TextBox? capcutNotes;
    private TextBox? capcutNarration;
    private bool capcutBinding;
    private bool capcutFocus;
    private bool capcutLibraryVisible = true;

    private static readonly Color EditorBackground = Color.FromArgb(244, 247, 251);
    private static readonly Color EditorInk = Color.FromArgb(30, 45, 65);

    private static Button StudioAction(string title, Action click, bool primary = false)
    {
        var button = new Button {
            Text = title, AutoSize = true, Height = 33,
            Margin = new Padding(3, 3, 5, 3),
            FlatStyle = FlatStyle.Flat
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(215, 225, 236);
        if (primary) {
            button.BackColor = Color.FromArgb(34, 116, 216);
            button.ForeColor = Color.White;
        }
        button.Click += (_, _) => click();
        return button;
    }

    private static Label EditorLabel(string label, bool bold = false)
    {
        return new Label {
            Text = label, AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = EditorInk,
            Font = new Font("Segoe UI", bold ? 9.5f : 9f,
                bold ? FontStyle.Bold : FontStyle.Regular)
        };
    }

    private void BuildCapcutShell(TableLayoutPanel root, TableLayoutPanel sourceControls)
    {
        capcutRoot = root;
        capcutSources = sourceControls;

        // 56px toolbar + on-demand project sources + editor + compact timeline.
        root.RowStyles[0].SizeType = SizeType.Absolute;
        root.RowStyles[0].Height = 88;
        root.RowStyles[1].SizeType = SizeType.Absolute;
        root.RowStyles[1].Height = 0;
        root.RowStyles[2].SizeType = SizeType.Percent;
        root.RowStyles[2].Height = 100;
        root.RowStyles[3].SizeType = SizeType.Absolute;
        root.RowStyles[3].Height = 205;
        root.RowStyles[4].SizeType = SizeType.Absolute;
        root.RowStyles[4].Height = 28;

        root.SetRow(sourceControls, 1);
        sourceControls.Visible = false;

        var toolbar = new FlowLayoutPanel {
            Dock = DockStyle.Fill, AutoScroll = true,
            WrapContents = false, FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White, Padding = new Padding(7, 5, 5, 2)
        };
        capcutProjectTitle = new Label {
            AutoSize = false, Width = 178, Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = EditorInk
        };
        toolbar.Controls.Add(capcutProjectTitle);
        toolbar.Controls.Add(StudioAction("Nguồn TTS / SRT / Ảnh", ToggleCapcutSources));
        toolbar.Controls.Add(StudioAction("Tạo storyboard", GenerateStoryboard));
        toolbar.Controls.Add(StudioAction("Mở dự án", OpenProject));
        toolbar.Controls.Add(StudioAction("Lưu", () => SaveProject()));
        toolbar.Controls.Add(StudioAction("Lưu thành...", () => SaveProject(asNew: true)));
        // Move the original render buttons, with their async handlers and
        // in-progress state, out of the legacy project settings row.
        // Never create a second Export button or duplicate render handlers.
        var sourceActions = sourceControls.GetControlFromPosition(1, 5)
            as FlowLayoutPanel ?? throw new InvalidOperationException(
                "Missing original video render toolbar");
        Button MoveRenderAction(string name, Control destination)
        {
            var button = sourceActions.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Text == name)
                ?? throw new InvalidOperationException("Missing render action: " + name);
            sourceActions.Controls.Remove(button);
            destination.Controls.Add(button);
            return button;
        }
        MoveRenderAction("Xem trước MP4", toolbar);
        MoveRenderAction("Xem cảnh đã chọn", toolbar);
        MoveRenderAction("Dừng render", toolbar);
        toolbar.Controls.Add(StudioAction("Bảng chi tiết", ToggleCapcutDetails));
        toolbar.Controls.Add(StudioAction("Nhạc / Phụ đề", EditComposerTracks));
        toolbar.Controls.Add(StudioAction("Whiteboard HD", EditWhiteboardQuality));
        // Export is the ONE primary call to action, persistently anchored to
        // the top-right. It stays visible when the other actions wrap or scroll.
        var header = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2,
            Margin = new Padding(0), BackColor = Color.White
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 226));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(toolbar, 0, 0);
        var exportHost = new Panel {
            Dock = DockStyle.Fill, Padding = new Padding(9, 10, 10, 10),
            BackColor = Color.FromArgb(236, 247, 242)
        };
        header.Controls.Add(exportHost, 1, 0);
        var export = MoveRenderAction("Xuất YouTube", exportHost);
        export.Name = "btnExportYouTubePrimary";
        export.Text = "XUẤT VIDEO\\nYouTube · 1080p".Replace("\\n", Environment.NewLine);
        export.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
        export.AutoSize = false;
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(0);
        export.FlatStyle = FlatStyle.Flat;
        export.FlatAppearance.BorderSize = 0;
        export.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 122, 68);
        export.BackColor = Color.FromArgb(20, 145, 81);
        export.ForeColor = Color.White;
        export.TextAlign = ContentAlignment.MiddleCenter;
        export.Cursor = Cursors.Hand;
        export.AccessibleName = "Xuất video YouTube Full HD";
        export.AccessibleDescription = "Xuất toàn bộ storyboard thành MP4 1920×1080, sử dụng TTS và SRT đã chọn.";
        var exportTip = new ToolTip();
        exportTip.SetToolTip(export, "Xuất YouTube · Full HD 1920×1080 · H.264 + AAC");
        export.Disposed += (_, _) => exportTip.Dispose();
        root.Controls.Add(header, 0, 0);

        capcutWorkspace = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 3,
            Margin = new Padding(0, 4, 0, 4),
            BackColor = EditorBackground
        };
        capcutWorkspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        capcutWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 242));
        capcutWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        capcutWorkspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        root.Controls.Add(capcutWorkspace, 0, 2);

        capcutLibraryPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(6) };
        capcutWorkspace.Controls.Add(capcutLibraryPanel, 0, 0);
        var libraryLayout = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1
        };
        libraryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        libraryLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        libraryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        capcutLibraryPanel.Controls.Add(libraryLayout);
        libraryLayout.Controls.Add(EditorLabel("THƯ VIỆN ẢNH", true), 0, 0);
        var assetsActions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        assetsActions.Controls.Add(StudioAction("Chọn thư mục", () => {
            using var picker = new FolderBrowserDialog();
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            imageFolderInput.Text = picker.SelectedPath;
            RefreshCapcutLibrary();
            dirty = true;
        }));
        libraryLayout.Controls.Add(assetsActions, 0, 1);
        capcutLibrary = new FlowLayoutPanel {
            Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true,
            BackColor = Color.White
        };
        libraryLayout.Controls.Add(capcutLibrary, 0, 2);

        var center = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
            BackColor = Color.FromArgb(27, 32, 42), Padding = new Padding(3)
        };
        center.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        center.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        center.Controls.Add(new Label {
            Text = "TRÌNH XEM TRƯỚC  ·  VIDEO / CẢNH ĐANG CHỌN",
            ForeColor = Color.White,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Padding = new Padding(12, 0, 0, 0)
        }, 0, 0);
        // Preserve the pre-existing playback host and WebP image renderer.
        previewOuter.Parent?.Controls.Remove(previewOuter);
        center.Controls.Add(previewOuter, 0, 1);
        transcriptCollapsed = true;
        LayoutPreviewPanels(true);
        capcutWorkspace.Controls.Add(center, 1, 0);

        capcutInspectorPanel = new Panel {
            Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10)
        };
        capcutWorkspace.Controls.Add(capcutInspectorPanel, 2, 0);
        ConfigureCapcutInspector(capcutInspectorPanel);

        capcutDetails = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Visible = false, Padding = new Padding(9)
        };
        capcutDetails.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        capcutDetails.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var detailActions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        detailActions.Controls.Add(StudioAction("← Quay lại trình dựng", ToggleCapcutDetails));
        detailActions.Controls.Add(EditorLabel("BẢNG CHI TIẾT TẤT CẢ CẢNH"));
        capcutDetails.Controls.Add(detailActions, 0, 0);
        storyboardGrid.Parent?.Controls.Remove(storyboardGrid);
        capcutDetails.Controls.Add(storyboardGrid, 0, 1);
        root.Controls.Add(capcutDetails, 0, 2);

        // Retire the old split shell only after all live controls were moved.
        root.Controls.Remove(workspaceSplit);
        workspaceSplit.Dispose();
        capcutWorkspace.Resize += (_, _) => AdjustCapcutColumns();
        titleInput.TextChanged += (_, _) => UpdateCapcutProjectTitle();
        imageFolderInput.TextChanged += (_, _) => RefreshCapcutLibrary();
        AdjustCapcutColumns();
        UpdateCapcutProjectTitle();
        RefreshCapcutLibrary();
        UpdateCapcutInspector();
    }

    private void UpdateCapcutProjectTitle()
    {
        if (capcutProjectTitle is not null)
            capcutProjectTitle.Text = string.IsNullOrWhiteSpace(titleInput.Text)
                ? "AI VIDEO STUDIO" : titleInput.Text.Trim();
    }

    private void ToggleCapcutSources()
    {
        if (capcutRoot is null || capcutSources is null) return;
        bool show = !capcutSources.Visible;
        capcutSources.Visible = show;
        capcutRoot.RowStyles[1].Height = show ? 236 : 0;
        capcutSources.BringToFront();
    }

    private void ToggleCapcutDetails()
    {
        if (capcutDetails is null || capcutWorkspace is null) return;
        storyboardGrid.EndEdit();
        bool show = !capcutDetails.Visible;
        capcutWorkspace.Visible = !show;
        capcutDetails.Visible = show;
        if (!show) UpdateCapcutInspector();
    }

    private void ToggleCapcutVideoFocus()
    {
        capcutFocus = !capcutFocus;
        AdjustCapcutColumns();
    }

    private void AdjustCapcutColumns()
    {
        if (capcutWorkspace is null || capcutWorkspace.Width <= 0) return;
        // Keep a useful preview even on laptop-size restored windows.
        bool narrow = capcutWorkspace.Width < 1080;
        int left = capcutFocus ? 0 : (narrow ? 0 : capcutLibraryVisible ? 225 : 0);
        int right = capcutFocus ? 0 : (narrow ? 240 : 300);
        capcutWorkspace.ColumnStyles[0].Width = left;
        capcutWorkspace.ColumnStyles[2].Width = right;
        if (capcutLibraryPanel is not null) capcutLibraryPanel.Visible = left > 0;
        if (capcutInspectorPanel is not null) capcutInspectorPanel.Visible = right > 0;
        UpdatePreviewViewport();
    }

    private void ConfigureCapcutInspector(Control host)
    {
        var inspector = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 11,
            AutoScroll = true, BackColor = Color.White
        };
        int[] sizes = {36,35,25,39,25,39,25,39,37,70,26};
        for(int i=0;i<sizes.Length;i++)
            inspector.RowStyles.Add(new RowStyle(SizeType.Absolute,sizes[i]));
        host.Controls.Add(inspector);
        capcutSelectedTitle = EditorLabel("CHỌN MỘT CẢNH", true);
        inspector.Controls.Add(capcutSelectedTitle, 0, 0);
        capcutTimeLabel = EditorLabel("Thời lượng và mốc SRT");
        inspector.Controls.Add(capcutTimeLabel, 0, 1);
        inspector.Controls.Add(EditorLabel("Hiệu ứng"), 0, 2);
        capcutEffect = new ComboBox {
            Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList
        };
        capcutEffect.DataSource = EffectOptions.Select(x => new StyleOption(x.Id,x.Label)).ToList();
        capcutEffect.DisplayMember = nameof(StyleOption.Label);
        capcutEffect.ValueMember = nameof(StyleOption.Id);
        capcutEffect.SelectedValueChanged += (_,_) => {
            if (capcutBinding || capcutEffect.SelectedValue is not string id) return;
            if (CurrentCapcutScene() is not { } scene || scene.StyleId == id) return;
            scene.StyleId = id; CommitCapcutSceneEdit();
        };
        inspector.Controls.Add(capcutEffect, 0, 3);
        inspector.Controls.Add(EditorLabel("Chuyển cảnh"), 0, 4);
        capcutTransition = new ComboBox {
            Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList
        };
        capcutTransition.Items.AddRange(new object[] { "fade", "cut" });
        capcutTransition.SelectedIndexChanged += (_,_) => {
            if (capcutBinding || CurrentCapcutScene() is not { } scene ||
                capcutTransition.SelectedItem is not string value || scene.Transition == value) return;
            scene.Transition = value; CommitCapcutSceneEdit();
        };
        inspector.Controls.Add(capcutTransition, 0, 5);
        inspector.Controls.Add(EditorLabel("Ảnh của cảnh"), 0, 6);
        capcutImagePath = EditorLabel("Chưa chọn");
        capcutImagePath.AutoEllipsis = true;
        inspector.Controls.Add(capcutImagePath, 0, 7);
        var actions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false
        };
        actions.Controls.Add(StudioAction("Thay ảnh", () => {
            if (CurrentCapcutScene() is not { } scene) return;
            using var choose = new OpenFileDialog {
                Filter="Ảnh|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.svg",
                InitialDirectory = Directory.Exists(imageFolderInput.Text)
                    ? imageFolderInput.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (choose.ShowDialog(this) != DialogResult.OK) return;
            scene.ImagePath=choose.FileName; CommitCapcutSceneEdit();
        }));
        actions.Controls.Add(StudioAction("+1 câu", () => ShiftSceneBoundary(+1)));
        actions.Controls.Add(StudioAction("-1 câu", () => ShiftSceneBoundary(-1)));
        inspector.Controls.Add(actions, 0, 8);
        capcutNarration = new TextBox {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            ScrollBars = ScrollBars.Vertical, BackColor = Color.FromArgb(246,248,251)
        };
        inspector.Controls.Add(capcutNarration,0,9);
        inspector.Controls.Add(EditorLabel("Ghi chú / kịch bản cảnh"), 0,10);

        // Make notes grow into remaining available inspector space.
        inspector.RowCount = 12;
        inspector.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        capcutNotes = new TextBox {
            Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical
        };
        capcutNotes.TextChanged += (_,_) => {
            if (capcutBinding || CurrentCapcutScene() is not { } scene) return;
            if(scene.Notes == capcutNotes.Text) return;
            scene.Notes = capcutNotes.Text; dirty=true; storyboardGrid.Refresh();
        };
        inspector.Controls.Add(capcutNotes, 0,11);
    }

    private AiVideoScene? CurrentCapcutScene() =>
        storyboardGrid.CurrentRow?.DataBoundItem as AiVideoScene;

    private void CommitCapcutSceneEdit()
    {
        dirty=true;
        storyboardGrid.Refresh();
        RebuildSceneTimeline();
        ShowSelectedSceneImage();
        UpdateCapcutInspector();
    }

    private void UpdateCapcutInspector()
    {
        if (capcutSelectedTitle is null) return;
        capcutBinding=true;
        try {
            var scene=CurrentCapcutScene();
            capcutSelectedTitle.Text = scene is null ? "CHỌN MỘT CẢNH"
                : $"CẢNH {scene.Index:00} · THUỘC TÍNH";
            capcutTimeLabel!.Text = scene is null ? "Chưa tạo storyboard"
                : $"{scene.StartMs/1000.0:0.0}s – {scene.EndMs/1000.0:0.0}s" +
                  $"  ·  SRT {scene.CueFrom}–{scene.CueTo}";
            capcutImagePath!.Text = scene is null ? "Chưa có ảnh"
                : Path.GetFileName(scene.ImagePath);
            if (scene is not null)
            {
                capcutEffect!.SelectedValue = scene.StyleId;
                capcutTransition!.SelectedItem = scene.Transition;
            }
            capcutNarration!.Text = scene?.Narration ?? "";
            capcutNotes!.Text = scene?.Notes ?? "";
            capcutEffect!.Enabled = scene is not null;
            capcutTransition!.Enabled = scene is not null;
            capcutNotes.Enabled = scene is not null;
        }
        finally { capcutBinding=false; }
    }

    private void RefreshCapcutLibrary()
    {
        if (capcutLibrary is null || capcutLibrary.IsDisposed) return;
        var folder = imageFolderInput.Text.Trim();
        var paths = Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder).Where(p => new[] {
                    ".png",".jpg",".jpeg",".webp",".bmp",".svg"
                }.Contains(Path.GetExtension(p).ToLowerInvariant()))
                .OrderBy(p => Path.GetFileName(p), StringComparer.OrdinalIgnoreCase)
                .Take(120).ToArray()
            : scenes.Select(s => s.ImagePath).Where(File.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase).Take(120).ToArray();
        WarmPreviewImages(paths);
        capcutLibrary.SuspendLayout();
        try {
            foreach (Control control in capcutLibrary.Controls.Cast<Control>().ToArray())
                control.Dispose();
            capcutLibrary.Controls.Clear();
            foreach (string file in paths)
            {
                var item = new Panel {
                    Width = 100, Height = 108, BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(3), BackColor = Color.FromArgb(250,252,255)
                };
                var picture = new PictureBox {
                    Dock = DockStyle.Top, Height = 77,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Image = GetThumbnail(file,98,76), BackColor = Color.White
                };
                var caption = new Label {
                    Dock=DockStyle.Fill,AutoEllipsis=true,
                    Text=Path.GetFileName(file),TextAlign=ContentAlignment.MiddleCenter,
                    Font=new Font("Segoe UI",7.5f)
                };
                item.Controls.Add(caption);
                item.Controls.Add(picture);
                item.Disposed += (_,_) => {
                    picture.Image?.Dispose(); picture.Image=null;
                };
                void Assign()
                {
                    if (CurrentCapcutScene() is not { } scene)
                    {
                        status.Text="Chọn cảnh trên timeline trước khi gán ảnh.";
                        return;
                    }
                    scene.ImagePath=file; CommitCapcutSceneEdit();
                }
                picture.Click += (_,_) => Assign();
                caption.Click += (_,_) => Assign();
                item.Click += (_,_) => Assign();
                var hint=new ToolTip();
                hint.SetToolTip(picture, "Nhấn để dùng ảnh cho cảnh đang chọn: "+file);
                item.Disposed += (_,_) => hint.Dispose();
                capcutLibrary.Controls.Add(item);
            }
        }
        finally { capcutLibrary.ResumeLayout(true); }
    }
}
