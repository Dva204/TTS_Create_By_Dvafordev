using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Scales the PLAYER VIEWPORT, not source video pixels. Original MP4 is untouched.
/// Auto uses ffprobe frame dimensions; other entries preview typical YouTube/Shorts ratios.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private static readonly string[] PreviewRatios = {
        "Tự động", "16:9 · Ngang", "9:16 · Dọc", "1:1 · Vuông",
        "4:5 · Dọc", "4:3 · Cổ điển"
    };
    private readonly Panel previewViewport = new() { BackColor = Color.Black };
    private readonly ComboBox previewRatioPicker = new() {
        DropDownStyle = ComboBoxStyle.DropDownList, Width = 140
    };
    private Size videoFrameSize = new(16, 9);

    internal static Rectangle FitPreviewViewport(Size surface, Size aspect)
    {
        if (surface.Width <= 0 || surface.Height <= 0 ||
            aspect.Width <= 0 || aspect.Height <= 0) return Rectangle.Empty;

        // Integer-safe containment. Never crop/stretch; keep the preview centered.
        double factor = Math.Min((double)surface.Width / aspect.Width,
                                 (double)surface.Height / aspect.Height);
        var w = Math.Clamp((int)Math.Round(aspect.Width * factor), 1, surface.Width);
        var h = Math.Clamp((int)Math.Round(aspect.Height * factor), 1, surface.Height);
        return new Rectangle((surface.Width - w) / 2,
                             (surface.Height - h) / 2, w, h);
    }

    private static Size ProbeVideoFrameSize(string video)
    {
        try
        {
            var processInfo = new ProcessStartInfo("ffprobe") {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = false
            };
            foreach (string item in new[] {
                "-v", "error", "-select_streams", "v:0",
                "-show_entries", "stream=width,height", "-of", "csv=p=0:s=x",
                video
            }) processInfo.ArgumentList.Add(item);
            using var process = Process.Start(processInfo);
            if (process is null) return new Size(16, 9);
            string output = process.StandardOutput.ReadToEnd().Trim();
            if (!process.WaitForExit(3500) || process.ExitCode != 0)
            {
                if (!process.HasExited) process.Kill();
                return new Size(16, 9);
            }
            var parts = output.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) &&
                int.TryParse(parts[1], out int height) && width > 0 && height > 0)
                return new Size(width, height);
        }
        catch (Exception) { /* ffprobe missing: default to 16:9. */ }
        return new Size(16, 9);
    }

    private static Size AspectForSelection(int selection, Size source) => selection switch {
        1 => new Size(16, 9),
        2 => new Size(9, 16),
        3 => new Size(1, 1),
        4 => new Size(4, 5),
        5 => new Size(4, 3),
        _ => source.Width > 0 && source.Height > 0 ? source : new Size(16, 9)
    };

    private void ConfigurePreviewRatioPicker()
    {
        previewRatioPicker.Items.AddRange(PreviewRatios);
        previewRatioPicker.SelectedIndexChanged += (_, _) => UpdatePreviewViewport();
        previewRatioPicker.SelectedIndex = 0;
        previewHost.Resize += (_, _) => UpdatePreviewViewport();
        previewViewport.Controls.Add(previewStill);
        previewHost.Controls.Add(previewViewport);
        previewViewport.BringToFront();
        previewHint.BringToFront();
    }

    private void UpdatePreviewViewport()
    {
        int frameHeight = Math.Max(0, previewHost.ClientSize.Height - previewHint.Height);
        Size ratio = AspectForSelection(previewRatioPicker.SelectedIndex, videoFrameSize);
        var rectangle = FitPreviewViewport(
            new Size(previewHost.ClientSize.Width, frameHeight), ratio);
        if (!rectangle.IsEmpty) previewViewport.Bounds = rectangle;
    }

    private void ChoosePreviewRatio(int index)
    {
        if (index < 0 || index >= PreviewRatios.Length) return;
        previewRatioPicker.SelectedIndex = index;
        UpdatePreviewViewport();
    }

    private void OpenDetachedPreview()
    {
        if (string.IsNullOrWhiteSpace(lastPreviewVideo) ||
            !File.Exists(lastPreviewVideo))
        {
            MessageBox.Show(this,
                "Hãy xuất video hoặc chọn Mở MP4 trước khi xem ở cửa sổ riêng.",
                "Xem video", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var video = lastPreviewVideo;
        StopInlinePlayback(); // Prevent playing two copies of the narration at once.
        using var dialog = new Form {
            Text = "Xem video đã render — " + Path.GetFileName(video),
            Width = 980, Height = 650, MinimumSize = new Size(480, 370),
            StartPosition = FormStartPosition.CenterParent,
            KeyPreview = true
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Color.FromArgb(23, 27, 34)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dialog.Controls.Add(layout);

        var controls = new FlowLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 4),
            BackColor = SystemColors.Control
        };
        controls.Controls.Add(new Label {
            Text = "Khung hình", AutoSize = true,
            Margin = new Padding(2, 7, 8, 0)
        });
        var ratio = new ComboBox {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = 158
        };
        ratio.Items.AddRange(PreviewRatios);
        ratio.SelectedIndex = previewRatioPicker.SelectedIndex;
        controls.Controls.Add(ratio);
        var actualSize = new Label {
            Text = $"Video: {videoFrameSize.Width} × {videoFrameSize.Height}",
            AutoSize = true, Margin = new Padding(15, 7, 5, 0)
        };
        controls.Controls.Add(actualSize);
        layout.Controls.Add(controls, 0, 0);

        var backdrop = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
        layout.Controls.Add(backdrop, 0, 1);
        var viewport = new Panel { BackColor = Color.Black };
        backdrop.Controls.Add(viewport);
        NativeVideoPlayer? popPlayer = null;

        void Fit()
        {
            var wanted = AspectForSelection(ratio.SelectedIndex, videoFrameSize);
            var bounds = FitPreviewViewport(backdrop.ClientSize, wanted);
            if (!bounds.IsEmpty) viewport.Bounds = bounds;
        }
        backdrop.Resize += (_, _) => Fit();
        ratio.SelectedIndexChanged += (_, _) => Fit();
        dialog.Shown += (_, _) => {
            Fit();
            try
            {
                popPlayer = new NativeVideoPlayer { Dock = DockStyle.Fill };
                viewport.Controls.Add(popPlayer);
                popPlayer.CreateControl();
                popPlayer.PlayFile(video);
            }
            catch (Exception exception)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(video) { UseShellExecute = true });
                    dialog.Close();
                }
                catch (Exception fallback)
                {
                    MessageBox.Show(dialog,
                        exception.Message + Environment.NewLine + fallback.Message,
                        "Không thể phát MP4", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };
        dialog.FormClosed += (_, _) => {
            popPlayer?.StopPlayback();
            popPlayer?.Dispose();
        };
        dialog.ShowDialog(this);
    }
}
