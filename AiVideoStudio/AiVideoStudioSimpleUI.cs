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
                                  TableLayoutPanel inputs,
                                  FlowLayoutPanel fileButtons)
    {
        simpleStudioLayout = true;
        // Full source panel, one large editable grid, no embedded player.
        view.RowStyles[0].SizeType = SizeType.Absolute;
        view.RowStyles[0].Height = 68;
        view.RowStyles[1].SizeType = SizeType.Absolute;
        view.RowStyles[1].Height = 310;
        view.RowStyles[2].SizeType = SizeType.Percent;
        view.RowStyles[2].Height = 100;
        view.RowStyles[3].SizeType = SizeType.Absolute;
        view.RowStyles[3].Height = 0;
        view.RowStyles[4].SizeType = SizeType.Absolute;
        view.RowStyles[4].Height = 30;

        inputs.RowCount = 8;
        foreach (RowStyle row in inputs.RowStyles) {
            row.SizeType = SizeType.Absolute;
            row.Height = 38;
        }
        inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        inputs.SetRow(titleInput, 0);
        inputs.Controls.Add(new Label {
            Text = "Thư mục output", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 7);
        inputs.Controls.Add(outputFolderInput, 1, 7);
        // Keep both folder actions together; selecting the folder also
        // updates the default directory for the existing MP4 Save dialog.
        inputs.ColumnStyles[2].SizeType = SizeType.Absolute;
        inputs.ColumnStyles[2].Width = 206;
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
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        chooseOutput.Click += (_, _) => ChooseOutputFolder();
        outputActions.Controls.Add(chooseOutput, 0, 0);
        var openOutput = new Button {
            Name = "btnOpenVideoOutputFolder", Text = "Mở output",
            Dock = DockStyle.Fill, Margin = new Padding(2),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        openOutput.Click += (_, _) => OpenOutputFolder();
        outputActions.Controls.Add(openOutput, 1, 0);
        inputs.Controls.Add(outputActions, 2, 7);
        outputFolderInput.Text = ReadOutputDirectory();

        // Keep project/save controls at the top; import is the most-used
        // storyboard action. Render buttons are re-homed, not duplicated.
        var import = new Button {
            Name = "btnImportScenePlanBasic", Text = "Import Scene Plan",
            AutoSize = true, Height = 30,
            Margin = new Padding(4, 2, 6, 2),
            BackColor = Color.FromArgb(230, 241, 254)
        };
        import.Click += (_, _) => ImportScenePlan();
        fileButtons.Controls.Add(import);

        Button TakeRenderButton(string name)
        {
            var button = fileButtons.Controls.OfType<Button>()
                .Single(b => b.Name == name);
            fileButtons.Controls.Remove(button);
            button.AutoSize = true;
            button.Dock = DockStyle.None;
            button.Margin = new Padding(4, 7, 5, 4);
            return button;
        }
        var preview = TakeRenderButton("btnStudioPreview");
        var scenePreview = TakeRenderButton("btnStudioSelectedScenePreview");
        var stop = TakeRenderButton("btnStudioStopRender");
        var export = TakeRenderButton("btnStudioExportYouTube");

        // The source controls continue to own all existing project actions.
        var left = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.White, Padding = new Padding(9, 11, 4, 6)
        };
        left.Controls.Add(new Label {
            Text = "AI VIDEO STUDIO", Width = 174, Height = 37,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(25, 50, 82),
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        });
        left.Controls.Add(new Label {
            Text = "1. Chọn nguồn   →   2. Import   →   3. Kiểm tra   →   4. Xuất",
            AutoSize = true, Height = 34, Margin = new Padding(4, 11, 9, 0),
            ForeColor = Color.FromArgb(80, 100, 121)
        });
        preview.Text = "Xem thử MP4";
        scenePreview.Text = "Xem cảnh";
        stop.Text = "Dừng render";
        left.Controls.Add(preview);
        left.Controls.Add(scenePreview);
        left.Controls.Add(stop);

        var header = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2,
            BackColor = Color.White, Margin = new Padding(0)
        };
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 211));
        header.Controls.Add(left, 0, 0);
        var exportHost = new Panel {
            Dock = DockStyle.Fill, Padding = new Padding(9, 7, 10, 7),
            BackColor = Color.FromArgb(234, 246, 240)
        };
        export.AutoSize = false;
        export.Dock = DockStyle.Fill;
        export.Margin = new Padding(0);
        export.Text = "XUẤT VIDEO 1080p";
        export.Font = new Font("Segoe UI", 11.5f, FontStyle.Bold);
        export.FlatStyle = FlatStyle.Flat;
        export.FlatAppearance.BorderSize = 0;
        export.BackColor = Color.FromArgb(20, 145, 81);
        export.ForeColor = Color.White;
        export.Cursor = Cursors.Hand;
        export.AccessibleName = "Xuất video YouTube Full HD";
        exportHost.Controls.Add(export);
        header.Controls.Add(exportHost, 1, 0);
        view.Controls.Add(header, 0, 0);

        // The existing grid is still editable (effects, transitions, notes,
        // double-click image replacement and shifting SRT boundaries).
        workspaceSplit.Panel2Collapsed = true;
        workspaceSplit.BackColor = Color.White;
        var sceneTableHost = workspaceSplit.Panel1.Controls
            .OfType<TableLayoutPanel>().FirstOrDefault();
        if (sceneTableHost is not null)
        {
            sceneTableHost.Padding = new Padding(4, 2, 4, 0);
            sceneTableHost.RowStyles[0].Height = 30;
        }
    }
}
