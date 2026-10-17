using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private void InitializeWhiteboardTab()
    {
        var page = new TabPage("Whiteboard AI");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true, ColumnCount = 3, RowCount = 8 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);
        mainTabControl.TabPages.Add(page);
        // Whiteboard Batch is superseded by AI Video Studio. Keep its renderer code for compatibility.

        TextBox AddFileRow(int row, string title, string filter, bool save = false)
        {
            var lbl = new Label { Text = title, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var txt = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 8, 3, 3) };
            var browse = new Button { Text = "Chọn...", Dock = DockStyle.Fill };
            layout.Controls.Add(lbl, 0, row);
            layout.Controls.Add(txt, 1, row);
            layout.Controls.Add(browse, 2, row);
            browse.Click += (_, _) =>
            {
                if (save)
                {
                    using var dialog = new SaveFileDialog { Filter = filter, DefaultExt = "mp4" };
                    if (dialog.ShowDialog(this) == DialogResult.OK) txt.Text = dialog.FileName;
                }
                else
                {
                    using var dialog = new OpenFileDialog { Filter = filter };
                    if (dialog.ShowDialog(this) == DialogResult.OK) txt.Text = dialog.FileName;
                }
            };
            return txt;
        }

        var image = AddFileRow(0, "Ảnh / nét vẽ", "Hình ảnh|*.png;*.jpg;*.jpeg;*.webp;*.svg;*.bmp");
        var audio = AddFileRow(1, "Âm thanh TTS (không bắt buộc)", "Âm thanh|*.wav;*.mp3;*.m4a;*.flac");
        var output = AddFileRow(2, "Video MP4 đầu ra", "MP4 video|*.mp4", true);
        var engine = AddFileRow(3, "Chương trình vẽ", "Tệp chương trình|*.exe");
        engine.Text = @"D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe";
        layout.Controls.Add(new Label { Text = "Thời lượng (giây)", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 4);
        var duration = new NumericUpDown { Minimum = 3, Maximum = 120, Value = 12, DecimalPlaces = 1, Dock = DockStyle.Left, Width = 110, Margin = new Padding(3, 8, 3, 3) };
        layout.Controls.Add(duration, 1, 4);
        var start = new Button { Text = "Tạo video xem thử", Dock = DockStyle.Fill };
        layout.Controls.Add(start, 2, 4);
        var whiteboardSettings = new GroupBox {
            Text = "WHITEBOARD - KIỂU VẼ", Dock = DockStyle.Fill,
            Padding = new Padding(10, 12, 6, 2)
        };
        var settingActions = new FlowLayoutPanel {
            Dock = DockStyle.Fill, AutoScroll = true, WrapContents = false
        };
        var strokes = new ComboBox { Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
        strokes.Items.AddRange(new object[] { "Ít nét - đơn giản", "Nhiều nét - chi tiết", "Tối đa nét vẽ" });
        strokes.SelectedIndex = 1;
        var animations = new ComboBox { Width = 216, DropDownStyle = ComboBoxStyle.DropDownList };
        animations.Items.AddRange(new object[] { "Vẽ toàn cảnh", "Vẽ từng đối tượng" });
        animations.SelectedIndex = 0;
        settingActions.Controls.Add(new Label { Text = "Độ chi tiết:", AutoSize = true, Margin = new Padding(3, 7, 5, 0) });
        settingActions.Controls.Add(strokes);
        settingActions.Controls.Add(new Label { Text = "Hoạt ảnh:", AutoSize = true, Margin = new Padding(14, 7, 5, 0) });
        settingActions.Controls.Add(animations);
        whiteboardSettings.Controls.Add(settingActions);
        layout.Controls.Add(whiteboardSettings, 0, 5);
        layout.SetColumnSpan(whiteboardSettings, 3);
        var renderOptions = new WhiteboardRenderOptions();
        var advancedRender = new Button { Text = "Thiết lập nâng cao...", AutoSize = true };
        advancedRender.Click += (_, _) => renderOptions.Edit(this);
        settingActions.Controls.Add(advancedRender);
        var status = new Label { Text = "Kết xuất trên máy, không bắt buộc có TTS.", Dock = DockStyle.Fill, AutoSize = true };
        layout.Controls.Add(status, 1, 6);
        var clearList = new Button { Text = "Xóa danh sách / Việc mới", AutoSize = true, Dock = DockStyle.Right, Margin = new Padding(3, 4, 3, 4) };
        layout.Controls.Add(clearList, 2, 6);
        var log = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        layout.Controls.Add(log, 0, 7);
        layout.SetColumnSpan(log, 3);

        clearList.Click += (_, _) =>
        {
            if (!start.Enabled) return;
            if (string.IsNullOrWhiteSpace(image.Text) && string.IsNullOrWhiteSpace(audio.Text) &&
                string.IsNullOrWhiteSpace(output.Text) && log.TextLength == 0) return;
            if (MessageBox.Show(this, "Xóa dữ liệu công việc hiện tại để bắt đầu lượt mới? Video đã xuất không bị xóa.",
                    "Whiteboard AI", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            image.Clear();
            audio.Clear();
            output.Clear();
            duration.Value = 12;
            strokes.SelectedIndex = 1;
            animations.SelectedIndex = 0;
            log.Clear();
            status.Text = "Sẵn sàng cho công việc mới. File cũ vẫn được giữ.";
        };

        async Task<int> RunAsync(string executable, Action<ProcessStartInfo> configure)
        {
            var psi = new ProcessStartInfo(executable) { CreateNoWindow = true, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };
            configure(psi);
            using var process = new Process { StartInfo = psi };
            process.Start();
            var stdOut = process.StandardOutput.ReadToEndAsync();
            var stdErr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            var a = await stdOut;
            var b = await stdErr;
            log.AppendText(a + Environment.NewLine + b + Environment.NewLine);
            return process.ExitCode;
        }

        start.Click += async (_, _) =>
        {
            if (!File.Exists(image.Text) || !File.Exists(engine.Text))
            {
                MessageBox.Show(this, "Hãy chọn ảnh và chương trình vẽ hợp lệ.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(audio.Text) && !File.Exists(audio.Text))
            {
                MessageBox.Show(this, "Không tìm thấy tệp âm thanh TTS.");
                return;
            }
            if (string.IsNullOrWhiteSpace(output.Text))
            {
                MessageBox.Show(this, "Hãy chọn đường dẫn lưu video MP4.");
                return;
            }
            start.Enabled = false;
            clearList.Enabled = false;
            log.Clear();
            status.Text = "Đang tạo video...";
            try
            {
                var destination = Path.GetFullPath(output.Text);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                var rawVideo = string.IsNullOrWhiteSpace(audio.Text) ? destination : Path.Combine(Path.GetDirectoryName(destination)!, Path.GetFileNameWithoutExtension(destination) + ".whiteboard.tmp.mp4");
                double seconds = (double)duration.Value;
                if (!string.IsNullOrWhiteSpace(audio.Text))
                {
                    var probe = new ProcessStartInfo("ffprobe") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                    foreach (var arg in new[] { "-v", "error", "-show_entries", "format=duration", "-of", "default=noprint_wrappers=1:nokey=1", audio.Text }) probe.ArgumentList.Add(arg);
                    using var p = Process.Start(probe) ?? throw new InvalidOperationException("Could not launch ffprobe.");
                    var result = await p.StandardOutput.ReadToEndAsync();
                    await p.StandardError.ReadToEndAsync();
                    await p.WaitForExitAsync();
                    if (p.ExitCode != 0 || !double.TryParse(result.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var audioSecs))
                        throw new InvalidOperationException("Unable to read audio duration.");
                    seconds = Math.Max(seconds, audioSecs);
                }
                // Never feed a full-color JPG directly into render-image: dark
                // regions get interpreted as thick strokes. Match the video
                // studio's line-art extraction + original color plate pipeline.
                string drawInput = image.Text;
                string? sourcePlate = null;
                if (!string.Equals(Path.GetExtension(image.Text), ".svg",
                                    StringComparison.OrdinalIgnoreCase))
                {
                    var workDir = Path.Combine(Path.GetDirectoryName(destination)!,
                        Path.GetFileNameWithoutExtension(destination) + ".whiteboard_prepare");
                    var py = Path.Combine(Path.GetDirectoryName(engine.Text)!, "python.exe");
                    var prepareScript = Path.Combine(AppContext.BaseDirectory,
                        "whiteboard_tools", "prepare_single_image.py");
                    if (!File.Exists(prepareScript))
                        prepareScript = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
                            "..", "..", "..", "whiteboard_tools", "prepare_single_image.py"));
                    if (!File.Exists(py) || !File.Exists(prepareScript))
                        throw new FileNotFoundException("Missing local Whiteboard preprocessing tools.");
                    int prepared = await RunAsync(py, psi =>
                    {
                        foreach (var arg in new[] { prepareScript, image.Text, workDir,
                            "--width", "1280", "--height", "720", "--mode", renderOptions.PreprocessMode }) psi.ArgumentList.Add(arg);
                    });
                    if (prepared != 0)
                        throw new InvalidOperationException("Line-art extraction failed. See log.");
                    drawInput = Path.Combine(workDir, "lineart.png");
                    sourcePlate = Path.Combine(workDir, "source.png");
                    if (!File.Exists(drawInput) || !File.Exists(sourcePlate))
                        throw new FileNotFoundException("Preprocessing did not produce lineart and source.");
                }
                int resultCode = await RunAsync(engine.Text, psi =>
                {
                    foreach (var arg in new[] { "render-image", drawInput, "-o", rawVideo,
                        "--duration", seconds.ToString(CultureInfo.InvariantCulture),
                        "--fps", "24", "--width", "1280", "--height", "720",
                        "--stroke-detail", strokes.SelectedIndex switch { 0 => "balanced", 2 => "max", _ => "rich" },
                        "--animation-preset", animations.SelectedIndex == 1 ? "block-speedpaint" : "classic" })
                        psi.ArgumentList.Add(arg);
                    foreach (var arg in renderOptions.Arguments()) psi.ArgumentList.Add(arg);
                    if (sourcePlate != null)
                    {
                        foreach (var arg in new[] { "--source-image", sourcePlate,
                            "--source-fit", "exact" })
                            psi.ArgumentList.Add(arg);
                    }
                });
                if (resultCode != 0) throw new InvalidOperationException("Whiteboard renderer failed. See log.");
                if (!string.IsNullOrWhiteSpace(audio.Text))
                {
                    int muxCode = await RunAsync("ffmpeg", psi =>
                    {
                        foreach (var arg in new[] { "-y", "-i", rawVideo, "-i", audio.Text, "-c:v", "copy", "-c:a", "aac", "-shortest", destination }) psi.ArgumentList.Add(arg);
                    });
                    if (muxCode != 0) throw new InvalidOperationException("Audio mux failed. See log.");
                    File.Delete(rawVideo);
                }
                status.Text = "Hoàn thành: " + destination;
            }
            catch (Exception ex)
            {
                status.Text = "Lỗi: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Whiteboard AI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { start.Enabled = true; clearList.Enabled = true; }
        };
    }
}
