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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), AutoScroll = true, ColumnCount = 3, RowCount = 7 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        page.Controls.Add(layout);
        mainTabControl.TabPages.Add(page);
        // Whiteboard Batch is superseded by AI Video Studio. Keep its renderer code for compatibility.

        TextBox AddFileRow(int row, string title, string filter, bool save = false)
        {
            var lbl = new Label { Text = title, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft };
            var txt = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 8, 3, 3) };
            var browse = new Button { Text = "Browse...", Dock = DockStyle.Fill };
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

        var image = AddFileRow(0, "Image / line art", "Images|*.png;*.jpg;*.jpeg;*.webp;*.svg;*.bmp");
        var audio = AddFileRow(1, "TTS audio (optional)", "Audio|*.wav;*.mp3;*.m4a;*.flac");
        var output = AddFileRow(2, "Output MP4", "MP4 video|*.mp4", true);
        var engine = AddFileRow(3, "Renderer executable", "Executable|*.exe");
        engine.Text = @"D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe";
        layout.Controls.Add(new Label { Text = "Duration (seconds)", Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.MiddleLeft }, 0, 4);
        var duration = new NumericUpDown { Minimum = 3, Maximum = 120, Value = 12, DecimalPlaces = 1, Dock = DockStyle.Left, Width = 110, Margin = new Padding(3, 8, 3, 3) };
        layout.Controls.Add(duration, 1, 4);
        var start = new Button { Text = "Render preview", Dock = DockStyle.Fill };
        layout.Controls.Add(start, 2, 4);
        var status = new Label { Text = "Local-only renderer. TTS is optional.", Dock = DockStyle.Fill, AutoSize = true };
        layout.Controls.Add(status, 1, 5);
        var log = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        layout.Controls.Add(log, 0, 6);
        layout.SetColumnSpan(log, 3);

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
                MessageBox.Show(this, "Select a valid image and renderer executable.");
                return;
            }
            if (!string.IsNullOrWhiteSpace(audio.Text) && !File.Exists(audio.Text))
            {
                MessageBox.Show(this, "TTS audio file was not found.");
                return;
            }
            if (string.IsNullOrWhiteSpace(output.Text))
            {
                MessageBox.Show(this, "Choose output MP4 path.");
                return;
            }
            start.Enabled = false;
            log.Clear();
            status.Text = "Rendering...";
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
                int resultCode = await RunAsync(engine.Text, psi =>
                {
                    foreach (var arg in new[] { "render-image", image.Text, "-o", rawVideo, "--duration", seconds.ToString(CultureInfo.InvariantCulture), "--fps", "24", "--width", "1280", "--height", "720", "--hand", "asian" }) psi.ArgumentList.Add(arg);
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
                status.Text = "Finished: " + destination;
            }
            catch (Exception ex)
            {
                status.Text = "Error: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Whiteboard AI", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { start.Enabled = true; }
        };
    }
}
