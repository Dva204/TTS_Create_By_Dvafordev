using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private void InitializeWhiteboardBatchTab()
    {
        var tab = new TabPage("Whiteboard Batch");
        mainTabControl.TabPages.Add(tab);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(15),
            ColumnCount = 3, RowCount = 13
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 182));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        for (int i = 0; i < 12; ++i) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);

        TextBox MakePath(int row, string title, bool folder, bool save, string filter)
        {
            layout.Controls.Add(new Label
            {
                Text = title, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            var input = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 8, 3, 3) };
            layout.Controls.Add(input, 1, row);
            var browse = new Button { Text = "Chọn...", Dock = DockStyle.Fill };
            browse.Click += (_, _) =>
            {
                if (folder)
                {
                    using var dialog = new FolderBrowserDialog();
                    if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.SelectedPath;
                }
                else if (save)
                {
                    using var dialog = new SaveFileDialog { Filter = filter, DefaultExt = "mp4", FileName = "whiteboard_youtube.mp4" };
                    if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.FileName;
                }
                else
                {
                    using var dialog = new OpenFileDialog { Filter = filter };
                    if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.FileName;
                }
            };
            layout.Controls.Add(browse, 2, row);
            return input;
        }

        var images = MakePath(0, "Thư mục ảnh (001, 002...)", true, false, "");
        var audio = MakePath(1, "TTS tổng (MP3/WAV)", false, false, "Audio|*.wav;*.mp3;*.m4a;*.flac");
        var srt = MakePath(2, "Phụ đề SRT", false, false, "Subtitles|*.srt");
        var output = MakePath(3, "Xuất video MP4", false, true, "MP4 Video|*.mp4");
        var engine = MakePath(4, "Whiteboard renderer", false, false, "Executable|*.exe");
        engine.Text = @"D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe";

        layout.Controls.Add(new Label
        {
            Text = "Độ phân giải / FPS", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 5);
        var quality = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        quality.Items.AddRange(new object[] {
            "Xem trước 640x360 / 20 FPS",
            "HD 1280x720 / 24 FPS",
            "YouTube nhanh: vẽ 720p → xuất 1080p",
            "YouTube gốc: vẽ trực tiếp 1080p (chậm)"
        });
        quality.SelectedIndex = 2;
        layout.Controls.Add(quality, 1, 5);
        var fromEditor = new Button { Text = "Từ Video Editor", Dock = DockStyle.Fill };
        layout.Controls.Add(fromEditor, 2, 5);
        fromEditor.Click += (_, _) =>
        {
            var a = Controls.Find("txtVideoAudio", true).OfType<TextBox>().FirstOrDefault();
            var b = Controls.Find("txtVideoSrt", true).OfType<TextBox>().FirstOrDefault();
            var c = Controls.Find("txtImageFolder", true).OfType<TextBox>().FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(a?.Text)) audio.Text = a.Text;
            if (!string.IsNullOrWhiteSpace(b?.Text)) srt.Text = b.Text;
            if (!string.IsNullOrWhiteSpace(c?.Text)) images.Text = c.Text;
        };

        layout.Controls.Add(new Label {
            Text = "Chất lượng tách nét", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
        var lineartMode = new ComboBox {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        lineartMode.Items.AddRange(new object[] {
            "Cân bằng - nét vẽ rõ (edges)",
            "Nét mảnh, nhiều chi tiết (detail)",
            "Chỉ nét mực đậm (ink)" });
        lineartMode.SelectedIndex = 0;
        layout.Controls.Add(lineartMode, 1, 6);
        var renderStyle = new ComboBox {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        renderStyle.Items.AddRange(new object[] {
            "Toàn cảnh", "Theo nhóm" });
        renderStyle.SelectedIndex = 0;
        layout.Controls.Add(renderStyle, 2, 6);

        layout.Controls.Add(new Label {
            Text = "Số nhóm nét / cảnh", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft }, 0, 7);
        var blockCount = new NumericUpDown {
            Minimum = 1, Maximum = 12, Value = 4, Dock = DockStyle.Left,
            Width = 90, Margin = new Padding(3, 8, 3, 3) };
        layout.Controls.Add(blockCount, 1, 7);
        var blockOrder = new ComboBox {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        blockOrder.Items.AddRange(new object[] { "reading", "source" });
        blockOrder.SelectedIndex = 0;
        layout.Controls.Add(blockOrder, 2, 7);

        var scenePlan = MakePath(8, "Kế hoạch cảnh JSON", false, false, "JSON|*.json");
        var message = new Label {
            Text = "Tạo JSON rồi sửa ảnh, số câu SRT và thứ tự nét.",
            Dock = DockStyle.Fill, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(message, 1, 9);
        var createPlan = new Button { Text = "Tạo JSON mẫu", Dock = DockStyle.Fill };
        layout.Controls.Add(createPlan, 2, 9);
        var start = new Button { Text = "Render Batch", Dock = DockStyle.Fill };
        layout.Controls.Add(start, 2, 10);
        var cancel = new Button { Text = "Dừng", Dock = DockStyle.Fill, Enabled = false };
        layout.Controls.Add(cancel, 2, 11);
        var status = new Label {
            Text = "Sẵn sàng. Xử lý local, không dùng API tạo video.",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        layout.Controls.Add(status, 1, 11);
        var log = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ReadOnly = true,
            WordWrap = false, ScrollBars = ScrollBars.Both,
            Font = new Font(FontFamily.GenericMonospace, 9f)
        };
        layout.Controls.Add(log, 0, 12);
        layout.SetColumnSpan(log, 3);

        Process? running = null;
        void Append(string line)
        {
            if (log.IsDisposed) return;
            try
            {
                if (log.InvokeRequired)
                    log.BeginInvoke(new Action(() => Append(line)));
                else
                {
                    log.AppendText(line + Environment.NewLine);
                    if (line.StartsWith("[SCENE") || line.StartsWith("[CACHE") || line.StartsWith("[DONE"))
                        status.Text = line;
                }
            }
            catch (InvalidOperationException) { }
        }

        cancel.Click += (_, _) =>
        {
            try
            {
                if (running is { HasExited: false }) running.Kill(entireProcessTree: true);
                status.Text = "Đã yêu cầu dừng render.";
            }
            catch (Exception ex) { Append("[WARN] " + ex.Message); }
        };

        string? ScriptPath()
        {
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[] {
                Path.Combine(baseDir, "whiteboard_tools", "whiteboard_batch.py"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "whiteboard_tools", "whiteboard_batch.py"))
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        createPlan.Click += async (_, _) =>
        {
            if (!Directory.Exists(images.Text) || !File.Exists(audio.Text) ||
                !File.Exists(srt.Text) || !File.Exists(engine.Text))
            {
                MessageBox.Show(this, "Hãy chọn thư mục ảnh, TTS, SRT và renderer trước.");
                return;
            }
            var script = ScriptPath();
            var python = Path.Combine(Path.GetDirectoryName(engine.Text)!, "python.exe");
            if (script is null || !File.Exists(python))
            {
                MessageBox.Show(this, "Không tìm thấy Python hoặc script Whiteboard.");
                return;
            }
            using var picker = new SaveFileDialog {
                Filter = "Scene Plan JSON|*.json", DefaultExt = "json",
                FileName = "whiteboard_scene_plan.json" };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            createPlan.Enabled = false;
            try
            {
                var psi = new ProcessStartInfo(python) {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
                string placeholderOutput = Path.Combine(Path.GetDirectoryName(picker.FileName)!, "whiteboard.mp4");
                foreach (var arg in new[] {
                    script, "--images", images.Text, "--audio", audio.Text,
                    "--srt", srt.Text, "--output", placeholderOutput,
                    "--engine", engine.Text, "--write-template", picker.FileName
                }) psi.ArgumentList.Add(arg);
                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Không chạy được script.");
                var stdout = proc.StandardOutput.ReadToEndAsync();
                var stderr = proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
                Append(await stdout);
                Append(await stderr);
                if (proc.ExitCode != 0) throw new InvalidOperationException("Tạo kế hoạch lỗi. Xem log.");
                scenePlan.Text = picker.FileName;
                status.Text = "Đã tạo JSON. Hãy mở sửa trước khi render.";
                Process.Start(new ProcessStartInfo(picker.FileName) { UseShellExecute = true });
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message); }
            finally { createPlan.Enabled = true; }
        };

        start.Click += async (_, _) =>
        {
            if (!Directory.Exists(images.Text) || !File.Exists(audio.Text) ||
                !File.Exists(srt.Text) || !File.Exists(engine.Text) ||
                string.IsNullOrWhiteSpace(output.Text))
            {
                MessageBox.Show(this, "Hãy chọn đủ thư mục ảnh, TTS, SRT, renderer và nơi xuất video.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(scenePlan.Text) && !File.Exists(scenePlan.Text))
            {
                MessageBox.Show(this, "Không tìm thấy kế hoạch JSON.");
                return;
            }
            var script = ScriptPath();
            if (script is null)
            {
                MessageBox.Show(this, "Không tìm thấy whiteboard_tools/whiteboard_batch.py.");
                return;
            }
            var python = Path.Combine(Path.GetDirectoryName(engine.Text)!, "python.exe");
            if (!File.Exists(python))
            {
                MessageBox.Show(this, "Không tìm thấy python.exe trong môi trường renderer.");
                return;
            }
            var selections = new[] { (640, 360, 20, 640, 360),
                                     (1280, 720, 24, 1280, 720),
                                     (1280, 720, 24, 1920, 1080),
                                     (1920, 1080, 30, 1920, 1080) };
            var config = selections[quality.SelectedIndex];
            start.Enabled = false;
            cancel.Enabled = true;
            log.Clear();
            status.Text = "Đang dựng video...";
            try
            {
                var psi = new ProcessStartInfo(python)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                foreach (var arg in new[] {
                    script, "--images", images.Text, "--audio", audio.Text,
                    "--srt", srt.Text, "--output", Path.GetFullPath(output.Text),
                    "--engine", engine.Text, "--width", config.Item1.ToString(),
                    "--height", config.Item2.ToString(), "--fps", config.Item3.ToString(),
                    "--final-width", config.Item4.ToString(), "--final-height", config.Item5.ToString(),
                    "--lineart-mode", new[] { "edges", "detail", "ink" }[lineartMode.SelectedIndex],
                    "--draw-blocks", blockCount.Value.ToString(),
                    "--block-order", blockOrder.SelectedItem!.ToString()!,
                    "--preset", renderStyle.SelectedIndex == 0 ? "classic" : "block-speedpaint"
                }) psi.ArgumentList.Add(arg);
                if (!string.IsNullOrWhiteSpace(scenePlan.Text))
                {
                    psi.ArgumentList.Add("--scene-plan");
                    psi.ArgumentList.Add(scenePlan.Text);
                }
                using var proc = new Process { StartInfo = psi };
                running = proc;
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) Append(e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (e.Data != null) Append(e.Data); };
                if (!proc.Start()) throw new InvalidOperationException("Không khởi động được Python.");
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                await proc.WaitForExitAsync();
                status.Text = proc.ExitCode == 0
                    ? "Đã hoàn thành: " + output.Text
                    : "Render lỗi hoặc đã dừng. Xem log bên dưới.";
            }
            catch (Exception ex)
            {
                status.Text = "Lỗi: " + ex.Message;
                Append("[ERROR] " + ex);
            }
            finally
            {
                running = null;
                start.Enabled = true;
                cancel.Enabled = false;
            }
        };
    }
}
