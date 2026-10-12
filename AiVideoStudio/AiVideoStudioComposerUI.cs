using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>Optional layers over the one master TTS audio track.</summary>
public sealed partial class AiVideoStudioControl
{
    private void ConfigureComposerButtons(FlowLayoutPanel bar)
    {
        var settings = new Button {
            Text = "Nhạc nền / Phụ đề", AutoSize = true, Height = 29
        };
        bar.Controls.Add(settings);
        var validate = new Button {
            Text = "Kiểm tra dự án", AutoSize = true, Height = 29
        };
        bar.Controls.Add(validate);
        settings.Click += (_, _) => EditComposerTracks();
        validate.Click += async (_, _) => await ValidateProjectAsync(validate);
    }

    private async Task ValidateProjectAsync(Button button)
    {
        if (renderProcess is { HasExited: false })
        {
            MessageBox.Show(this, "Hãy đợi quá trình render hiện tại kết thúc.");
            return;
        }
        var script = RenderScriptPath();
        var python = Path.Combine(Path.GetDirectoryName(rendererExecutable)!, "python.exe");
        if (script == null || !File.Exists(python))
        {
            MessageBox.Show(this, "Không tìm thấy Python/renderer. Hãy kiểm tra môi trường Whiteboard.");
            return;
        }
        var staging = Path.Combine(Path.GetTempPath(), "DvaForDevStudio",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        var original = project.FilePath;
        button.Enabled = false;
        try
        {
            UpdateFromForm();
            var snapshot = Path.Combine(staging, "validate.ai-video.json");
            AiVideoProjectService.Save(project, snapshot);
            var psi = new ProcessStartInfo(python) {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var item in new[] {
                script, "--project", snapshot, "--output",
                Path.Combine(staging, "validate.mp4"), "--validate-only"
            }) psi.ArgumentList.Add(item);
            using var proc = Process.Start(psi)
                ?? throw new IOException("Không thể chạy lệnh kiểm tra.");
            var stdout = proc.StandardOutput.ReadToEndAsync();
            var stderr = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            var details = (await stdout).Trim() + "\n" + (await stderr).Trim();
            status.Text = proc.ExitCode == 0 ? "Kiểm tra dự án đạt." : "Dự án chưa hợp lệ.";
            MessageBox.Show(this, details,
                proc.ExitCode == 0 ? "Dự án sẵn sàng render" : "Cần sửa dữ liệu dự án",
                MessageBoxButtons.OK, proc.ExitCode == 0
                    ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Lỗi kiểm tra dự án",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            project.FilePath = original;
            button.Enabled = true;
            try { Directory.Delete(staging, true); } catch (IOException) { }
        }
    }

    private void EditComposerTracks()
    {
        using var dialog = new Form {
            Text = "Video Studio V2 — Các track đầu ra",
            Width = 690, Height = 335, MinimumSize = new Size(600, 320),
            StartPosition = FormStartPosition.CenterParent
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(12),
            RowCount = 6, ColumnCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        dialog.Controls.Add(layout);

        var musicFile = new TextBox { Text = project.MusicPath, Dock = DockStyle.Fill };
        var browse = new Button { Text = "Chọn...", Dock = DockStyle.Fill };
        browse.Click += (_, _) => {
            using var choose = new OpenFileDialog {
                Title = "Chọn nhạc nền hợp lệ mà bạn có quyền sử dụng",
                Filter = "Nhạc nền|*.wav;*.mp3;*.m4a;*.flac;*.aac;*.ogg"
            };
            if (choose.ShowDialog(dialog) == DialogResult.OK)
                musicFile.Text = choose.FileName;
        };
        var volume = new NumericUpDown {
            Minimum = 0, Maximum = 50, DecimalPlaces = 0,
            Value = (decimal)Math.Round(project.MusicVolume * 100),
            Width = 75
        };
        var audioHelp = new Label {
            Text = "Nhạc nền sẽ tự lặp và trộn vào TTS tổng. Thử 8–15% để không át lời đọc.",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
        };
        var captions = new CheckBox {
            Text = "Ghi phụ đề trực tiếp vào hình ảnh video (burn-in)",
            Checked = project.BurnSubtitles, Dock = DockStyle.Fill
        };
        var notes = new Label {
            Dock = DockStyle.Fill, AutoSize = false,
            Text = "TTS tổng luôn là âm thanh chính. Nhạc nền chỉ là track phụ; " +
                   "file gốc không bị chỉnh sửa.\n" +
                   "Phụ đề lấy từ file SRT đã chọn; không tự dịch hay nhận diện lời nói.\n" +
                   "Bật phụ đề sẽ cần mã hóa lại video preview, nên thời gian lâu hơn."
        };
        void Label(string value, int row) => layout.Controls.Add(new Label {
            Text = value, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);

        Label("Nhạc nền", 0);
        layout.Controls.Add(musicFile, 1, 0);
        layout.Controls.Add(browse, 2, 0);
        Label("Âm lượng (%)", 1);
        layout.Controls.Add(volume, 1, 1);
        layout.Controls.Add(audioHelp, 2, 1);
        Label("Phụ đề", 2);
        layout.Controls.Add(captions, 1, 2);
        layout.SetColumnSpan(captions, 2);
        layout.Controls.Add(notes, 0, 3);
        layout.SetColumnSpan(notes, 3);

        var actions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft
        };
        var apply = new Button { Text = "Lưu thiết lập", Width = 135 };
        var cancel = new Button { Text = "Hủy", Width = 95 };
        actions.Controls.Add(apply);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 4);
        layout.SetColumnSpan(actions, 3);
        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        apply.Click += (_, _) => {
            var path = musicFile.Text.Trim();
            if (path.Length > 0 && !File.Exists(path))
            {
                MessageBox.Show(dialog, "File nhạc nền không tồn tại.", "Thiết lập video");
                return;
            }
            if (captions.Checked && !File.Exists(subtitleInput.Text))
            {
                MessageBox.Show(dialog,
                    "Hãy chọn một file SRT hợp lệ trước khi bật phụ đề.",
                    "Thiết lập video");
                return;
            }
            project.MusicPath = path;
            project.MusicVolume = (double)volume.Value / 100.0;
            project.BurnSubtitles = captions.Checked;
            dirty = true;
            UpdateTrackOverview();
            status.Text = "Đã lưu thiết lập track. Nhấn Lưu project để giữ lại trong JSON.";
            dialog.DialogResult = DialogResult.OK;
        };
        dialog.ShowDialog(this);
    }
}
