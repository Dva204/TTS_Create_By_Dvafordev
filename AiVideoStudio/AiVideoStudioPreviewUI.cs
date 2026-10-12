using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Windows Media Player ActiveX: native in many Windows installations.
/// No external NuGet dependencies. If WMP is absent, preview opens externally.
/// </summary>
internal sealed class NativeVideoPlayer : AxHost
{
    internal NativeVideoPlayer()
        : base("6BF52A52-394A-11D3-B153-00C04F79FAA6") { }

    internal void PlayFile(string path)
    {
        // Must have a created host handle before calling GetOcx.
        if (!IsHandleCreated) CreateControl();
        dynamic player = GetOcx();
        player.uiMode = "full";
        player.stretchToFit = true;
        player.settings.autoStart = false;
        player.URL = path;
        player.controls.play();
    }

    internal void StopPlayback()
    {
        if (!IsHandleCreated) return;
        try
        {
            dynamic player = GetOcx();
            player.controls.stop();
            player.URL = "";
        }
        catch (Exception) { /* COM host may have been torn down */ }
    }
}

public sealed partial class AiVideoStudioControl
{
    private readonly Panel previewHost = new() { Dock = DockStyle.Fill, BackColor = Color.Black };
    private readonly PictureBox previewStill = new() {
        Dock = DockStyle.Fill, BackColor = Color.White, SizeMode = PictureBoxSizeMode.Zoom
    };
    private readonly Label previewHeader = new() {
        Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly TextBox previewNarration = new() {
        Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical
    };
    private readonly Label previewHint = new() {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true
    };
    private readonly Dictionary<string, (long Version, Image Picture)> thumbnailCache =
        new(StringComparer.OrdinalIgnoreCase);

    private Image GetThumbnail(string? path, int width, int height)
    {
        string key = (path ?? "") + "|" + width + "|" + height;
        long version = 0;
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                var info = new FileInfo(path);
                version = info.LastWriteTimeUtc.Ticks ^ info.Length;
            }
        }
        catch (IOException) { }
        if (!thumbnailCache.TryGetValue(key, out var entry) || entry.Version != version)
        {
            if (entry.Picture != null) entry.Picture.Dispose();
            var thumbnail = CreatePreviewBitmap(ResolveDisplayImage(path), width, height);
            thumbnailCache[key] = (version, thumbnail);
            entry = (version, thumbnail);
        }
        // Every timeline PictureBox gets a private clone, so rebuilding cannot
        // dispose another tile's shared cached image.
        return new Bitmap(entry.Picture);
    }

    private NativeVideoPlayer? nativePlayer;
    private string? lastPreviewVideo;
    private int lastSelectedSceneIndex = -2;

    private void ResetSelectedScenePreviewTracking()
    {
        lastSelectedSceneIndex = -2;
        UpdateSelectedScenePreview();
    }

    private void UpdateSelectedScenePreview()
    {
        int selection = storyboardGrid.CurrentRow?.Index ?? -1;
        if (selection == lastSelectedSceneIndex) return;
        lastSelectedSceneIndex = selection;
        if (selection < 0) ClearInlinePreview();
        else ShowSelectedSceneImage();
    }

    private void ConfigureInlinePreview(Control workspace)
    {
        var outer = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(5)
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        workspace.Controls.Add(outer);

        var left = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Margin = new Padding(0), BackColor = Color.Black
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.Controls.Add(left, 0, 0);

        var previewToolbar = new FlowLayoutPanel {
            Dock = DockStyle.Fill, WrapContents = false,
            BackColor = SystemColors.Control, Padding = new Padding(3, 3, 0, 0)
        };
        previewToolbar.Controls.Add(new Label {
            Text = "Khung hình", AutoSize = true, Margin = new Padding(2, 7, 6, 0)
        });
        previewToolbar.Controls.Add(previewRatioPicker);
        var detach = new Button { Text = "Xem riêng / Phóng to", AutoSize = true };
        detach.Click += (_, _) => OpenDetachedPreview();
        previewToolbar.Controls.Add(detach);
        var focusVideo = new Button {
            Text = "Ưu tiên video", AutoSize = true
        };
        focusVideo.Click += (_, _) => {
            if (workspace is SplitterPanel splitterPanel &&
                splitterPanel.Parent is SplitContainer split)
            {
                split.Panel1Collapsed = !split.Panel1Collapsed;
                focusVideo.Text = split.Panel1Collapsed ? "Hiện storyboard" : "Ưu tiên video";
                UpdatePreviewViewport();
            }
        };
        previewToolbar.Controls.Add(focusVideo);
        previewToolbar.Controls.Add(new Label {
            Text = "Tỷ lệ chỉ áp dụng cho khung xem, không đổi file MP4",
            AutoSize = true, ForeColor = Color.DimGray, Margin = new Padding(9, 7, 0, 0)
        });
        left.Controls.Add(previewToolbar, 0, 0);
        left.Controls.Add(previewHost, 0, 1);
        previewHost.Controls.Add(previewHint);
        previewHint.BackColor = Color.FromArgb(245, 245, 245);
        previewHint.ForeColor = Color.DimGray;
        previewHint.Height = 22;
        previewHint.Dock = DockStyle.Bottom;
        ConfigurePreviewRatioPicker();

        var info = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 39));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        outer.Controls.Add(info, 1, 0);
        info.Controls.Add(previewHeader, 0, 0);
        info.Controls.Add(previewNarration, 0, 1);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
        info.Controls.Add(actions, 0, 2);
        var showImage = new Button { Text = "Xem ảnh gốc", AutoSize = true };
        var browseVideo = new Button { Text = "Mở MP4", AutoSize = true };
        actions.Controls.Add(showImage);
        actions.Controls.Add(browseVideo);
        showImage.Click += (_, _) => ShowSelectedSceneImage();
        browseVideo.Click += (_, _) => {
            using var dialog = new OpenFileDialog { Filter = "Video MP4|*.mp4" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                OpenVideoInStudio(dialog.FileName);
        };
        previewNarration.Text = "Chọn một scene trong timeline để xem ảnh.";
        previewHeader.Text = "TRÌNH XEM TRƯỚC / TTS TỔNG";
        previewHint.Text = "Ảnh gốc (không phải clip đã render)";
    }

    private static Image CreatePreviewBitmap(string? file, int width, int height)
    {
        var canvas = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(canvas);
        graphics.Clear(Color.White);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        var caption = string.IsNullOrWhiteSpace(file) ? "Chưa chọn ảnh"
            : Path.GetExtension(file).Equals(".svg", StringComparison.OrdinalIgnoreCase)
                ? "Ảnh SVG — xem hiệu ứng qua render" : "Không tải được ảnh";
        if (file is not null && File.Exists(file) &&
            !file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
                using var source = Image.FromStream(stream);
                float scale = Math.Min((float)width / source.Width, (float)height / source.Height);
                int w = Math.Max(1, (int)(source.Width * scale));
                int h = Math.Max(1, (int)(source.Height * scale));
                graphics.DrawImage(source, (width - w) / 2, (height - h) / 2, w, h);
                return canvas;
            }
            catch (Exception) { /* Missing/unsupported image formats get a placeholder. */ }
        }
        using var brush = new SolidBrush(Color.DimGray);
        using var font = new Font("Segoe UI", Math.Max(9, width / 26f));
        using var format = new StringFormat {
            Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center
        };
        graphics.DrawString(caption, font, brush, new RectangleF(0, 0, width, height), format);
        return canvas;
    }

    private void ShowSelectedSceneImage()
    {
        if (storyboardGrid.CurrentRow?.DataBoundItem is not AiVideoScene scene)
        {
            ClearInlinePreview();
            return;
        }
        StopInlinePlayback();
        var old = previewStill.Image;
        previewStill.Image = CreatePreviewBitmap(ResolveDisplayImage(scene.ImagePath), 600, 338);
        old?.Dispose();
        previewStill.Visible = true;
        previewStill.BringToFront();
        previewHint.Text = "Ảnh nguồn — bấm 'Xem cảnh đã chọn' để xem chuyển động + TTS";
        previewHint.BringToFront();
        previewHeader.Text = $"CẢNH {scene.Index:00} · {scene.StyleId} · " +
            $"{(scene.EndMs - scene.StartMs) / 1000.0:0.0}s · {scene.Transition}";
        previewNarration.Text = scene.Narration;
    }

    private void ClearInlinePreview()
    {
        StopInlinePlayback();
        var old = previewStill.Image;
        previewStill.Image = null;
        old?.Dispose();
        previewNarration.Clear();
        previewHeader.Text = "TRÌNH XEM TRƯỚC / TTS TỔNG";
        previewHint.Text = "Chưa có scene";
    }

    private void StopInlinePlayback()
    {
        try { nativePlayer?.StopPlayback(); }
        catch (Exception) { }
        if (nativePlayer is not null) nativePlayer.Visible = false;
        previewStill.Visible = true;
    }

    private void OpenVideoInStudio(string video)
    {
        if (!File.Exists(video)) return;
        lastPreviewVideo = video;
        videoFrameSize = ProbeVideoFrameSize(video);
        UpdatePreviewViewport();
        try
        {
            if (nativePlayer == null)
            {
                nativePlayer = new NativeVideoPlayer { Dock = DockStyle.Fill };
                previewViewport.Controls.Add(nativePlayer);
                nativePlayer.CreateControl();
            }
            previewStill.Visible = false;
            nativePlayer.Visible = true;
            nativePlayer.BringToFront();
            previewHint.BringToFront();
            nativePlayer.PlayFile(video);
            previewHint.Text = "Video đã render · điều khiển Play / Pause / Seek ngay bên dưới";
            previewHeader.Text = "MP4: " + Path.GetFileName(video);
        }
        catch (Exception error)
        {
            StopInlinePlayback();
            previewHint.Text = "Không thể nhúng trình phát; sẽ mở bằng ứng dụng mặc định";
            try
            {
                Process.Start(new ProcessStartInfo(video) { UseShellExecute = true });
            }
            catch (Exception other)
            {
                MessageBox.Show(this, error.Message + "\n" + other.Message,
                    "Lỗi mở video", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void ReleaseInlinePreview()
    {
        StopInlinePlayback();
        previewStill.Image?.Dispose();
        previewStill.Image = null;
        foreach (var entry in thumbnailCache.Values) entry.Picture.Dispose();
        thumbnailCache.Clear();
        if (nativePlayer is not null)
        {
            nativePlayer.Dispose();
            nativePlayer = null;
        }
    }
}
