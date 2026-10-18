using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Basic editor shell: sources -> scene grid -> export.
/// Reuses all existing TTS/SRT/Whiteboard/AI Director/render handlers.
/// No CapCut workspace or embedded video preview is shown.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private bool simpleStudioLayout;
    private readonly TextBox outputFolderInput = new() {
        Dock = DockStyle.Fill, ReadOnly = true, TabStop = false,
        BackColor = Color.FromArgb(248, 250, 253)
    };

    private static string RememberedOutputFile =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DvaForDev", "TTSStudio", "last_video_output_dir.txt");

    private static string DefaultOutputFolder()
    {
        var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        if (string.IsNullOrWhiteSpace(videos))
            videos = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(videos, "DvaForDev AI Videos");
    }

    private static string ReadOutputDirectory()
    {
        try
        {
            if (File.Exists(RememberedOutputFile))
            {
                var value = File.ReadAllText(RememberedOutputFile).Trim();
                if (Path.IsPathFullyQualified(value)) return value;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        catch (ArgumentException) { }
        return DefaultOutputFolder();
    }

    private void UpdateOutputDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) return;
        outputFolderInput.Text = Path.GetFullPath(directory);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RememberedOutputFile)!);
            File.WriteAllText(RememberedOutputFile, outputFolderInput.Text);
        }
        catch (IOException) { /* folder can still be opened for this session */ }
        catch (UnauthorizedAccessException) { }
    }

    private void ChooseOutputFolder()
    {
        using var picker = new FolderBrowserDialog {
            Description = "Chọn thư mục lưu video MP4",
            ShowNewFolderButton = true
        };
        if (Directory.Exists(outputFolderInput.Text))
            picker.SelectedPath = outputFolderInput.Text;
        else if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)))
            picker.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

        if (picker.ShowDialog(this) != DialogResult.OK) return;
        UpdateOutputDirectory(picker.SelectedPath);
        status.Text = "Đã chọn thư mục xuất video: " + outputFolderInput.Text;
    }

    private void OpenOutputFolder()
    {
        try
        {
            string folder = string.IsNullOrWhiteSpace(outputFolderInput.Text)
                ? DefaultOutputFolder() : Path.GetFullPath(outputFolderInput.Text);
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo {
                FileName = folder, UseShellExecute = true
            });
            status.Text = "Đã mở thư mục output: " + folder;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   System.ComponentModel.Win32Exception or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "Không thể mở thư mục output",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BuildSimpleShell(TableLayoutPanel view,
                                  TableLayoutPanel oldInputs,
                                  FlowLayoutPanel fileButtons)
    {
        simpleStudioLayout = true;

        // Keep every existing handler/engine: move controls, don't recreate
        // project, render, scene or composer logic.
        Button Existing(int col, int row) =>
            oldInputs.GetControlFromPosition(col, row) as Button ??
            throw new InvalidOperationException($"Missing source action at {col},{row}");
        var createProject = Existing(2, 0);
        var browseAudio = Existing(2, 1);
        var browseSrt = Existing(2, 2);
        var browseImages = Existing(2, 3);
        var generate = Existing(2, 4);
        var openImage = Existing(2, 5);
        var options = oldInputs.GetControlFromPosition(1, 4) as FlowLayoutPanel
            ?? throw new InvalidOperationException("Missing storyboard options.");
        var sceneActions = oldInputs.GetControlFromPosition(1, 6) as FlowLayoutPanel
            ?? throw new InvalidOperationException("Missing scene actions.");
        var projectActions = fileButtons.Controls.OfType<Button>().ToArray();
        if (projectActions.Length != 7)
            throw new InvalidOperationException("Unexpected project action layout.");
        var openProject = projectActions[0];
        var saveProject = projectActions[1];
        var saveAs = projectActions[2];
        Button RenderButton(string name) => projectActions.Single(x => x.Name == name);
        var preview = RenderButton("btnStudioPreview");
        var previewScene = RenderButton("btnStudioSelectedScenePreview");
        var export = RenderButton("btnStudioExportYouTube");
        var stop = RenderButton("btnStudioStopRender");

        // Preserve the existing scene editor and live video preview.
        // Reusing the existing preview controls retains playback handlers.
        // Modern workspace: scene table and embedded player side by side.
        workspaceSplit.Panel2Collapsed = false;
        workspaceSplit.Orientation = Orientation.Vertical;
        InstallSceneInspector(workspaceSplit.Panel2);
        workspaceSplit.BackColor = Color.FromArgb(226, 233, 240);
        workspaceSplit.SplitterWidth = 6;
        void ResizeWorkspace()
        {
            if (workspaceSplit.IsDisposed || workspaceSplit.Width < 420) return;
            var usable = workspaceSplit.Width - workspaceSplit.SplitterWidth;
            var target = (int)(usable * (workspaceSplit.Width >= 1000 ? 0.43 : 0.46));
            if (workspaceSplit.SplitterDistance != target)
                workspaceSplit.SplitterDistance = target;
        }
        workspaceSplit.HandleCreated += (_, _) => BeginInvoke((Action)ResizeWorkspace);
        // The divider remains draggable; initial sizing happens once.

        var gridHost = workspaceSplit.Panel1.Controls.OfType<TableLayoutPanel>().FirstOrDefault();
        if (gridHost is not null)
        {
            gridHost.Padding = new Padding(8, 5, 8, 5);
            gridHost.BackColor = Color.White;
            gridHost.RowStyles[0].Height = 34;
        }

        view.RowStyles[0].SizeType = SizeType.Absolute;
        view.RowStyles[0].Height = 63;
        view.RowStyles[1].SizeType = SizeType.Absolute;
        view.RowStyles[1].Height = 359;
        view.RowStyles[2].SizeType = SizeType.Percent;
        view.RowStyles[2].Height = 100;
        view.RowStyles[3].SizeType = SizeType.Absolute;
        view.RowStyles[3].Height = 0;
        view.RowStyles[4].SizeType = SizeType.Absolute;
        view.RowStyles[4].Height = 30;

        // Simple header: only the primary export CTA and a cancel button while
        // rendering. Preview renders and other functions are in Advanced.
        var header = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = Color.White, Margin = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 212));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        TableLayoutPanel? collapsibleSources = null;
        var headerLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var sourceToggle = new Button {
            Text = "Thu gọn nguồn dữ liệu", Width = 165, Height = 34,
            Dock = DockStyle.Right, Margin = new Padding(3),
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        sourceToggle.FlatAppearance.BorderColor = Color.FromArgb(213, 225, 233);
        sourceToggle.Click += (_, _) =>
        {
            if (collapsibleSources is null) return;
            bool expand = !collapsibleSources.Visible;
            collapsibleSources.Visible = expand;
            view.RowStyles[1].Height = expand ? 359 : 0;
            sourceToggle.Text = expand ? "Thu gọn nguồn dữ liệu" : "Mở nguồn dữ liệu";
        };
        headerLeft.Controls.Add(new Label {
            Text = "AI VIDEO STUDIO",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(13, 0, 0, 0),
            ForeColor = Color.FromArgb(25, 51, 82),
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        });
        headerLeft.Controls.Add(sourceToggle);
        header.Controls.Add(headerLeft, 0, 0);
        stop.Text = "Dừng render";
        stop.Dock = DockStyle.Fill;
        stop.AutoSize = false;
        stop.Margin = new Padding(3, 10, 3, 10);
        stop.Visible = false;
        header.Controls.Add(stop, 1, 0);

        export.Text = "XUẤT VIDEO";
        export.AutoSize = false;
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(4, 8, 10, 8);
        export.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        export.FlatStyle = FlatStyle.Flat;
        export.FlatAppearance.BorderSize = 0;
        export.BackColor = Color.FromArgb(20, 145, 81);
        export.ForeColor = Color.White;
        export.Cursor = Cursors.Hand;
        export.AccessibleName = "Xuất video YouTube 1080p";
        header.Controls.Add(export, 2, 0);
        view.Controls.Add(header, 0, 0);

        var sources = new TableLayoutPanel {
            Name = "minimalSourcePanel", Dock = DockStyle.Fill,
            ColumnCount = 3, RowCount = 6,
            BackColor = Color.White, Padding = new Padding(10, 8, 10, 3),
            Margin = new Padding(0, 3, 0, 3), AutoScroll = true
        };
        collapsibleSources = sources;
        sources.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        sources.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sources.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
        foreach (int height in new[] {39, 39, 39, 39, 193, 0})
            sources.RowStyles.Add(new RowStyle(SizeType.Absolute, height));

        void SourceRow(int row, string label, TextBox text, Button browse)
        {
            sources.Controls.Add(new Label {
                Text = label, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(52, 69, 91)
            }, 0, row);
            text.Dock = DockStyle.Fill;
            text.Margin = new Padding(2, 4, 5, 4);
            sources.Controls.Add(text, 1, row);
            browse.Text = "Chọn...";
            browse.Dock = DockStyle.Right;
            browse.Width = 110;
            browse.Margin = new Padding(2, 3, 2, 3);
            sources.Controls.Add(browse, 2, row);
        }
        SourceRow(0, "Âm thanh TTS", audioInput, browseAudio);
        SourceRow(1, "Phụ đề SRT", subtitleInput, browseSrt);
        SourceRow(2, "Thư mục ảnh", imageFolderInput, browseImages);

        sources.Controls.Add(new Label {
            Text = "Thư mục output", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(52, 69, 91)
        }, 0, 3);
        outputFolderInput.Margin = new Padding(2, 4, 5, 4);
        sources.Controls.Add(outputFolderInput, 1, 3);
        var outputActions = new TableLayoutPanel {
            Name = "outputFolderActions", Dock = DockStyle.Fill,
            ColumnCount = 2, RowCount = 1, Margin = new Padding(0)
        };
        outputActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        outputActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        outputActions.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var chooseOutput = new Button {
            Name = "btnChooseVideoOutputFolder", Text = "Chọn output",
            Dock = DockStyle.Fill, Margin = new Padding(2),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        chooseOutput.Click += (_, _) => ChooseOutputFolder();
        outputActions.Controls.Add(chooseOutput, 0, 0);
        var openOutput = new Button {
            Name = "btnOpenVideoOutputFolder", Text = "Mở output",
            Dock = DockStyle.Fill, Margin = new Padding(2),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        openOutput.Click += (_, _) => OpenOutputFolder();
        outputActions.Controls.Add(openOutput, 1, 0);
        sources.Controls.Add(outputActions, 2, 3);
        outputFolderInput.Text = ReadOutputDirectory();

        // Four decisions in the main workflow. Every other button moves
        // into a collapsed Advanced section (no loss of functionality).
        var essentials = new FlowLayoutPanel {
            Name = "essentialStudioActions", Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            AutoScroll = true, Margin = new Padding(0)
        };
        var import = new Button {
            Name = "btnImportScenePlanBasic", Text = "Nhập kế hoạch cảnh",
            Width = 173, Height = 33, Margin = new Padding(0, 5, 7, 4),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = Color.FromArgb(226, 238, 251)
        };
        import.Click += (_, _) => ImportScenePlan();
        essentials.Controls.Add(import);
        var inspectPlan = new Button {
            Text = "Kiểm tra SRT / Scene", AutoSize = true, Height = 34
        };
        inspectPlan.Click += (_, _) =>
        {
            if (!File.Exists(subtitleInput.Text))
            {
                MessageBox.Show(this, "Hãy chọn SRT trước khi kiểm tra Scene Plan.");
                return;
            }
            using var picker = new OpenFileDialog {
                Title = "Chọn Scene Plan để kiểm tra",
                Filter = "Scene Plan (*.json)|*.json", CheckFileExists = true
            };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                var report = ScenePlanDiagnostics.Check(
                    subtitleInput.Text, picker.FileName, imageFolderInput.Text);
                using var result = new Form {
                    Text = "Kiểm tra SRT & Scene Plan",
                    StartPosition = FormStartPosition.CenterParent,
                    Size = new Size(760, 580), MinimumSize = new Size(520, 380)
                };
                var field = new TextBox {
                    Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Both,
                    ReadOnly = true, WordWrap = true, Font = new Font("Consolas", 10),
                    Text = report.DisplayText()
                };
                result.Controls.Add(field);
                result.ShowDialog(this);
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or
                System.Text.Json.JsonException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Không thể kiểm tra Scene Plan",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        essentials.Controls.Add(inspectPlan);

        openProject.Text = "Mở dự án";
        saveProject.Text = "Lưu dự án";
        foreach (var button in new[] { openProject, saveProject })
        {
            button.AutoSize = false;
            button.Width = 110;
            button.Height = 33;
            button.Margin = new Padding(0, 5, 7, 4);
            essentials.Controls.Add(button);
        }
        var clearList = new Button {
            Name = "btnStudioClearList", Text = "Xóa danh sách / Dự án mới",
            Width = 174, Height = 33, Margin = new Padding(0, 5, 7, 4),
            BackColor = Color.FromArgb(255, 244, 231)
        };
        clearList.Click += (_, _) =>
        {
            if (renderProcess is { HasExited: false })
            {
                MessageBox.Show(this, "Hãy dừng kết xuất trước khi xóa danh sách cảnh.",
                    "AI Video Studio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (scenes.Count == 0 && string.IsNullOrWhiteSpace(audioInput.Text) &&
                string.IsNullOrWhiteSpace(subtitleInput.Text) && string.IsNullOrWhiteSpace(imageFolderInput.Text)) return;
            if (MessageBox.Show(this,
                "Xóa danh sách cảnh, lựa chọn âm thanh và SRT để tạo video mới? Dự án đã lưu và video xuất không bị xóa.",
                "Tạo video mới", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            ResetProject();
            status.Text = "Đã làm mới dự án. Các tệp đã lưu vẫn được giữ.";
        };
        essentials.Controls.Add(clearList);
        var advancedButton = new Button {
            Name = "btnMinimalAdvanced", Text = "Tùy chọn ▾",
            Width = 113, Height = 33, Margin = new Padding(0, 5, 4, 4)
        };
        essentials.Controls.Add(advancedButton);
        // Group commonly used actions and Whiteboard rendering settings separately.
        // Reparent the existing buttons so their original event handlers remain intact.
        var actionArea = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Margin = new Padding(0), Padding = new Padding(0)
        };
        actionArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        actionArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        actionArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var projectPanel = new GroupBox {
            Text = "DỰ ÁN & SCENE", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 10, 4, 1)
        };
        essentials.WrapContents = true;
        essentials.AutoScroll = true;
        essentials.Padding = new Padding(0, 3, 0, 0);
        projectPanel.Controls.Add(essentials);
        actionArea.Controls.Add(projectPanel, 0, 0);
        var whiteboardPanel = new GroupBox {
            Text = "WHITEBOARD - KIỂU VẼ", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 11, 4, 1)
        };
        var whiteboardActions = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
            Padding = new Padding(3, 2, 3, 0)
        };
        whiteboardActions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 85));
        whiteboardActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        whiteboardActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        whiteboardActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        whiteboardActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
        whiteboardActions.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        var strokePicker = new ComboBox {
            Name = "cmbStudioStrokeDetail", Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        strokePicker.Items.AddRange(new object[] {
            "Ít nét - đơn giản", "Nhiều nét - chi tiết", "Tối đa nét vẽ"
        });
        var animationPicker = new ComboBox {
            Name = "cmbStudioAnimation", Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9f)
        };
        animationPicker.Items.AddRange(new object[] {
            "Vẽ toàn cảnh", "Vẽ từng đối tượng"
        });
        void LoadWhiteboardOptions()
        {
            strokePicker.SelectedIndex = project.WhiteboardStrokeDetail switch {
                "balanced" => 0, "max" => 2, _ => 1
            };
            animationPicker.SelectedIndex =
                project.WhiteboardAnimationPreset == "block-speedpaint" ? 1 : 0;
        }
        void ApplyWhiteboardOptions()
        {
            if (strokePicker.SelectedIndex < 0 || animationPicker.SelectedIndex < 0) return;
            project.WhiteboardStrokeDetail = strokePicker.SelectedIndex switch {
                0 => "balanced", 2 => "max", _ => "rich"
            };
            project.WhiteboardAnimationPreset =
                animationPicker.SelectedIndex == 1 ? "block-speedpaint" : "classic";
            dirty = true;
        }
        strokePicker.SelectionChangeCommitted += (_, _) => ApplyWhiteboardOptions();
        animationPicker.SelectionChangeCommitted += (_, _) => ApplyWhiteboardOptions();
        whiteboardActions.Controls.Add(new Label {
            Text = "Nét vẽ", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        whiteboardActions.Controls.Add(strokePicker, 1, 0);
        whiteboardActions.Controls.Add(new Label {
            Text = "Hoạt ảnh", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        whiteboardActions.Controls.Add(animationPicker, 1, 1);
        var whiteboardMore = new Button {
            Text = "Thiết lập nâng cao...", Dock = DockStyle.Left, Width = 156,
            Height = 27
        };
        whiteboardMore.Click += (_, _) => { if (project.WhiteboardOptions.Edit(this)) dirty = true; };
        whiteboardActions.Controls.Add(whiteboardMore, 1, 2);
        var receiveSettings = new Button {
            Text = "Nhận cấu hình Whiteboard", AutoSize = true, Height = 27
        };
        receiveSettings.Click += (_, _) =>
        {
            try
            {
                var incoming = WhiteboardRenderOptions.LoadSharedPreset();
                if (incoming is null)
                {
                    MessageBox.Show(this, "Chưa có cấu hình dùng chung. Hãy lưu từ tab Whiteboard AI trước.",
                        "Chưa có cấu hình", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(this,
                    "Áp dụng cấu hình Whiteboard đã lưu vào dự án video hiện tại?",
                    "Áp dụng cấu hình", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes) return;
                project.WhiteboardOptions = incoming;
                dirty = true;
                status.Text = "Đã nhận cấu hình Whiteboard. Nhấn Lưu dự án để giữ thiết lập.";
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Lỗi nạp cấu hình"); }
        };
        whiteboardActions.Controls.Add(receiveSettings, 0, 3);
        whiteboardActions.SetColumnSpan(receiveSettings, 2);

        whiteboardPanel.Controls.Add(whiteboardActions);
        actionArea.Controls.Add(whiteboardPanel, 1, 0);
        // A resizable, airy workspace: keep input paths compact and use
        // two separate functional cards instead of one overflowing toolbar.
        sources.Padding = new Padding(16, 12, 16, 10);
        sources.BackColor = Color.FromArgb(250, 252, 254);
        sources.ColumnStyles[0].Width = 126;
        sources.ColumnStyles[2].Width = 205;
        foreach (Control item in sources.Controls)
        {
            if (item is TextBox box)
            {
                box.Font = new Font("Segoe UI", 10f);
                box.BorderStyle = BorderStyle.FixedSingle;
                box.BackColor = Color.White;
            }
        }
        projectPanel.BackColor = Color.White;
        whiteboardPanel.BackColor = Color.White;
        projectPanel.ForeColor = Color.FromArgb(36, 55, 72);
        whiteboardPanel.ForeColor = Color.FromArgb(36, 55, 72);
        essentials.Padding = new Padding(6, 4, 4, 4);
        essentials.Margin = Padding.Empty;
        foreach (Control action in essentials.Controls)
        {
            if (action is not Button b) continue;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(215, 225, 233);
            b.FlatAppearance.BorderSize = 1;
            b.Height = 38;
            b.Margin = new Padding(3, 4, 5, 4);
            b.Cursor = Cursors.Hand;
        }
        import.BackColor = Color.FromArgb(222, 239, 250);
        import.ForeColor = Color.FromArgb(28, 77, 125);
        clearList.BackColor = Color.FromArgb(255, 245, 231);
        whiteboardActions.Padding = new Padding(10, 5, 10, 3);
        whiteboardActions.RowStyles[0].Height = 35;
        whiteboardActions.RowStyles[1].Height = 35;
        whiteboardActions.RowStyles[2].Height = 35;
        whiteboardActions.RowStyles[3].Height = 35;
        whiteboardMore.FlatStyle = FlatStyle.Flat;
        receiveSettings.FlatStyle = FlatStyle.Flat;
        whiteboardMore.FlatAppearance.BorderColor = Color.FromArgb(212, 222, 230);
        receiveSettings.FlatAppearance.BorderColor = Color.FromArgb(212, 222, 230);
        receiveSettings.BackColor = Color.FromArgb(233, 245, 238);
        receiveSettings.ForeColor = Color.FromArgb(29, 114, 82);
        sources.Controls.Add(actionArea, 1, 4);
        sources.SetColumnSpan(actionArea, 2);
        sources.Controls.Add(new Label {
            Text = "Công cụ", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(52, 69, 91)
        }, 0, 4);
        // Refresh controls after loading, clearing or opening a project.
        refreshWhiteboardQuickOptions = LoadWhiteboardOptions;
        this.VisibleChanged += (_, _) => LoadWhiteboardOptions();
        LoadWhiteboardOptions();

        // Reuse existing manual grouping, quality, subtitle, and preview
        // controls, but hide by default to simplify everyday Claude workflows.
        var advanced = new TableLayoutPanel {
            Name = "studioAdvancedPanel", Dock = DockStyle.Fill,
            ColumnCount = 1, RowCount = 3,
            BackColor = Color.FromArgb(249, 251, 253),
            Margin = new Padding(0), Padding = new Padding(3, 1, 3, 1),
            Visible = false
        };
        advanced.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        advanced.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        advanced.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var projectExtras = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true
        };
        projectExtras.Controls.Add(new Label {
            Text = "Tên dự án", AutoSize = true,
            Margin = new Padding(3, 7, 5, 0)
        });
        titleInput.Dock = DockStyle.None;
        titleInput.Width = 240;
        titleInput.Margin = new Padding(0, 3, 10, 3);
        projectExtras.Controls.Add(titleInput);
        foreach (var button in new[] { createProject, saveAs, openImage })
        {
            button.Dock = DockStyle.None;
            button.AutoSize = true;
            button.Margin = new Padding(3, 2, 7, 2);
            projectExtras.Controls.Add(button);
        }
        createProject.Text = "Dự án mới";
        saveAs.Text = "Lưu thành...";
        openImage.Text = "Xem ảnh đã chọn";
        advanced.Controls.Add(projectExtras, 0, 0);

        var manual = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true
        };
        options.Dock = DockStyle.None;
        options.Width = 605;
        options.Height = 33;
        options.Margin = new Padding(0);
        manual.Controls.Add(options);
        generate.Text = "Tạo storyboard tự động";
        generate.Dock = DockStyle.None;
        generate.AutoSize = true;
        generate.Margin = new Padding(3, 2, 5, 2);
        manual.Controls.Add(generate);
        advanced.Controls.Add(manual, 0, 1);

        var moreActions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true
        };
        foreach (var button in new[] { preview, previewScene })
        {
            button.Dock = DockStyle.None;
            button.AutoSize = true;
            button.Height = 29;
            button.Margin = new Padding(3, 5, 6, 3);
            moreActions.Controls.Add(button);
        }
        preview.Text = "Xem thử MP4";
        previewScene.Text = "Xem cảnh";
        foreach (var child in sceneActions.Controls.OfType<Button>().ToArray())
        {
            child.Dock = DockStyle.None;
            child.AutoSize = true;
            child.Margin = new Padding(3, 5, 6, 3);
            moreActions.Controls.Add(child);
        }
        advanced.Controls.Add(moreActions, 0, 2);
        sources.Controls.Add(advanced, 0, 5);
        sources.SetColumnSpan(advanced, 3);

        advancedButton.Click += (_, _) => {
            bool show = !advanced.Visible;
            sources.SuspendLayout();
            try
            {
                advanced.Visible = show;
                sources.RowStyles[5].Height = show ? 118 : 0;
                view.RowStyles[1].Height = show ? 485 : 359;
                advancedButton.Text = show ? "Ẩn tùy chọn ▴" : "Tùy chọn ▾";
                storyboardGrid.Columns.Cast<DataGridViewColumn>()
                    .First(c => c.DataPropertyName == nameof(AiVideoScene.StartMs)).Visible = show;
                storyboardGrid.Columns.Cast<DataGridViewColumn>()
                    .First(c => c.DataPropertyName == nameof(AiVideoScene.EndMs)).Visible = show;
                storyboardGrid.Columns.Cast<DataGridViewColumn>()
                    .First(c => c.DataPropertyName == nameof(AiVideoScene.Transition)).Visible = show;
                storyboardGrid.Columns.Cast<DataGridViewColumn>()
                    .First(c => c.DataPropertyName == nameof(AiVideoScene.Notes)).Visible = show;
            }
            finally { sources.ResumeLayout(true); }
        };

        view.Controls.Remove(oldInputs);
        view.Controls.Add(sources, 0, 1);
        oldInputs.Dispose();
        ConfigureSceneProgressUi(view);

        // Show only meaningful columns by default. All scene state and
        // editing remain in the full model and the advanced table view.
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.DirectorSummary)).Visible = false;
        foreach (var field in new[] {
            nameof(AiVideoScene.StartMs), nameof(AiVideoScene.EndMs),
            nameof(AiVideoScene.Transition), nameof(AiVideoScene.Notes)
        })
            storyboardGrid.Columns.Cast<DataGridViewColumn>()
                .First(c => c.DataPropertyName == field).Visible = false;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.Index)).Width = 42;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.CueFrom)).Width = 67;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.CueTo)).Width = 65;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.StyleId)).Width = 190;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.ImagePath)).Width = 270;
        storyboardGrid.Columns.Cast<DataGridViewColumn>()
            .First(c => c.DataPropertyName == nameof(AiVideoScene.Narration)).AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;
        foreach (var (field, heading) in new[] {
            (nameof(AiVideoScene.Index), "#"),
            (nameof(AiVideoScene.CueFrom), "SRT từ"),
            (nameof(AiVideoScene.CueTo), "SRT đến"),
            (nameof(AiVideoScene.StyleId), "Hiệu ứng"),
            (nameof(AiVideoScene.ImagePath), "Ảnh (nháy đúp để thay)"),
            (nameof(AiVideoScene.Narration), "Lời đọc"),
            (nameof(AiVideoScene.StartMs), "Bắt đầu (ms)"),
            (nameof(AiVideoScene.EndMs), "Kết thúc (ms)"),
            (nameof(AiVideoScene.Transition), "Chuyển cảnh"),
            (nameof(AiVideoScene.Notes), "Ghi chú")
        })
            storyboardGrid.Columns.Cast<DataGridViewColumn>()
                .First(c => c.DataPropertyName == field).HeaderText = heading;
    }
}
