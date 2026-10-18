using System.Drawing;
using System.Windows.Forms;
using DvaForDev.Shared;
using TTS_Create_By_DvaForDev.AiVideoStudio;

namespace TTS_Create_By_DvaForDev;

public partial class WhiteboardStudioForm : Form
{
    private readonly TabControl mainTabControl = new() {
        Dock = DockStyle.Fill, Alignment = TabAlignment.Top,
        Font = new Font("Segoe UI", 10f)
    };

    public WhiteboardStudioForm()
    {
        Text = StudioBranding.WhiteboardName;
        MinimumSize = new Size(1080, 700);
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(245, 247, 250);
        var shell = new TableLayoutPanel {
            Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1,
            Padding = Padding.Empty
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        var heading = new TableLayoutPanel {
            Dock = DockStyle.Fill, BackColor = Color.White,
            ColumnCount = 2, RowCount = 1,
            Padding = new Padding(18, 9, 20, 9)
        };
        heading.ColumnCount = 3;
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
        heading.Controls.Add(new Label {
            Text = StudioBranding.WhiteboardName,
            Font = new Font("Segoe UI Semibold", 17, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 113, 82),
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        var toggle = new Button {
            Text = "Chuyển sang AI Video Studio",
            Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(226, 242, 235),
            ForeColor = Color.FromArgb(24, 109, 76),
            Cursor = Cursors.Hand
        };
        toggle.FlatAppearance.BorderColor = Color.FromArgb(201, 228, 213);
        toggle.Click += (_, _) => mainTabControl.SelectedIndex =
            mainTabControl.SelectedIndex == 0 ? 1 : 0;
        heading.Controls.Add(toggle, 2, 0);
        var engineButton = new Button {
            Text = "Chọn Whiteboard Engine", Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            BackColor = Color.White
        };
        engineButton.FlatAppearance.BorderColor = Color.FromArgb(207, 218, 228);
        heading.Controls.Add(engineButton, 1, 0);
        shell.Controls.Add(heading, 0, 0);
        shell.Controls.Add(mainTabControl, 0, 1);
        Controls.Add(shell);

        InitializeWhiteboardTab();
        var videoTab = new TabPage("AI Video Studio / Biên tập video") {
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(4)
        };
        var studio = new AiVideoStudioControl();
        videoTab.Controls.Add(studio);
        mainTabControl.TabPages.Add(videoTab);

        string engineConfig = Path.Combine(
            StudioBranding.UserDataFolder("WhiteboardStudio"), "renderer-path.txt");
        void ApplyEngine(string executable)
        {
            studio.SetWhiteboardRenderer(executable);
            var sourceEngine = mainTabControl.Controls.Find(
                "txtWhiteboardRenderer", true).OfType<TextBox>().FirstOrDefault();
            if (sourceEngine is not null) sourceEngine.Text = executable;
        }
        engineButton.Click += (_, _) =>
        {
            using var select = new OpenFileDialog {
                Title = "Chọn whiteboard.exe trong thư mục .venv\\Scripts",
                Filter = "Whiteboard Engine|whiteboard.exe|Executable|*.exe",
                CheckFileExists = true
            };
            if (select.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                ApplyEngine(select.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(engineConfig)!);
                File.WriteAllText(engineConfig, select.FileName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Whiteboard Engine",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        if (File.Exists(engineConfig))
        {
            try { ApplyEngine(File.ReadAllText(engineConfig).Trim()); }
            catch (Exception) { /* old engine path may no longer exist */ }
        }
        mainTabControl.SelectedIndexChanged += (_, _) =>
            toggle.Text = mainTabControl.SelectedIndex == 0
                ? "Chuyển sang AI Video Studio" : "Chuyển sang Whiteboard AI";
    }
}
