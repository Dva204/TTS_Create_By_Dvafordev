using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>Lightweight visual scene strip over a single narration track.</summary>
public sealed partial class AiVideoStudioControl
{
    private readonly FlowLayoutPanel sceneStrip = new() {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
        WrapContents = false, AutoScroll = true, Padding = new Padding(4, 3, 4, 2),
        BackColor = Color.White
    };
    private readonly Label timelineSummary = new() {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private readonly Label trackOverview = new() {
        Dock = DockStyle.Fill, AutoEllipsis = true,
        TextAlign = ContentAlignment.MiddleLeft
    };
    private bool isRebuildingStrip;

    private static string Timestamp(long ms) =>
        TimeSpan.FromMilliseconds(Math.Max(0, ms)).ToString(@"mm\:ss\.f", CultureInfo.InvariantCulture);

    private void ConfigureSceneTimeline(TableLayoutPanel view)
    {
        var wrapper = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3
        };
        wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        wrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        wrapper.BackColor = Color.White;
        timelineSummary.Font = new Font(timelineSummary.Font, FontStyle.Bold);
        timelineSummary.ForeColor = Color.FromArgb(35, 54, 81);
        timelineSummary.Padding = new Padding(6, 0, 0, 0);
        trackOverview.ForeColor = Color.FromArgb(75, 95, 118);
        trackOverview.Padding = new Padding(6, 0, 0, 0);
        wrapper.Controls.Add(timelineSummary, 0, 0);
        wrapper.Controls.Add(sceneStrip, 0, 1);
        wrapper.Controls.Add(trackOverview, 0, 2);
        view.Controls.Add(wrapper, 0, 1);
    }

    private void ConfigureSceneButtons(FlowLayoutPanel bar)
    {
        var increase = new Button { Text = "Cảnh +1 câu", AutoSize = true, Height = 29 };
        var decrease = new Button { Text = "Cảnh -1 câu", AutoSize = true, Height = 29 };
        var explanation = new Label {
            AutoSize = true, Margin = new Padding(9, 8, 0, 0),
            Text = "Một audio TTS tổng • chỉnh ranh giới theo SRT • xem trước từng cảnh"
        };
        bar.Controls.Add(increase);
        bar.Controls.Add(decrease);
        ConfigureComposerButtons(bar);
        ConfigureWhiteboardQualityButton(bar);
        bar.Controls.Add(explanation);
        increase.Click += (_, _) => ShiftSceneBoundary(+1);
        decrease.Click += (_, _) => ShiftSceneBoundary(-1);
    }

    private void ShiftSceneBoundary(int direction)
    {
        storyboardGrid.EndEdit();
        if (scenes.Count < 2 || storyboardGrid.CurrentRow?.DataBoundItem is not AiVideoScene chosen)
        {
            MessageBox.Show(this, "Chọn cảnh có cảnh kế tiếp trong bảng storyboard.");
            return;
        }
        try
        {
            var cues = AiVideoProjectService.ReadSrt(subtitleInput.Text);
            AiVideoProjectService.ShiftCueBoundary(scenes.ToList(), cues, chosen.Index - 1, direction);
            storyboardGrid.Refresh();
            dirty = true;
            RebuildSceneTimeline();
            status.Text = $"Đã cập nhật ranh giới cảnh {chosen.Index} / {chosen.Index + 1} theo SRT. " +
                          "Các hiệu ứng Director của hai cảnh được xóa do mốc thời gian thay đổi.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Không thể đổi ranh giới cảnh",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void UpdateTrackOverview()
    {
        string tts = string.IsNullOrEmpty(audioInput.Text)
            ? "TTS: chưa chọn" : "TTS: " + Path.GetFileName(audioInput.Text);
        string subtitles = string.IsNullOrEmpty(subtitleInput.Text)
            ? "SRT: chưa chọn" : "SRT: " +
              (project.BurnSubtitles ? "CHÈN VÀO VIDEO" : "không chèn");
        string music = string.IsNullOrEmpty(project.MusicPath)
            ? "Nhạc nền: tắt" : $"Nhạc nền: {project.MusicVolume:P0}";
        trackOverview.Text = "TRACK 1 · " + tts + "     TRACK 2 · " +
                             subtitles + "     TRACK 3 · " + music;
    }

    private void RebuildSceneTimeline()
    {
        if (isRebuildingStrip) return;
        isRebuildingStrip = true;
        try
        {
            // Convert all WebP images in one Pillow invocation rather than a new
            // Python process for every thumbnail. Original image paths stay intact.
            WarmPreviewImages(scenes.Select(scene => scene.ImagePath));
            sceneStrip.SuspendLayout();
            foreach (Control existing in sceneStrip.Controls.Cast<Control>().ToArray())
                existing.Dispose();
            sceneStrip.Controls.Clear();
            long total = scenes.Count > 0 ? scenes[^1].EndMs : 0;
            timelineSummary.Text = scenes.Count == 0
                ? "Timeline: chưa có cảnh — một TTS tổng, chia thời gian bằng SRT"
                : $"Timeline hình ảnh: {scenes.Count} cảnh · {Timestamp(total)} · " +
                  $"Audio: {(string.IsNullOrWhiteSpace(audioInput.Text) ? "chưa chọn" : Path.GetFileName(audioInput.Text))}";
            UpdateTrackOverview();
            foreach (var scene in scenes)
            {
                int number = scene.Index - 1;
                long duration = scene.EndMs - scene.StartMs;
                var tile = new Panel {
                    Tag = number, Width = 158, Height = 111,
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = SystemColors.Control,
                    AllowDrop = true,
                    Margin = new Padding(3, 1, 3, 1)
                };
                var picture = new PictureBox {
                    Dock = DockStyle.Top, Height = 82,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.White,
                    Image = GetThumbnail(scene.ImagePath, 155, 82),
                    AllowDrop = true
                };
                var caption = new Label {
                    Dock = DockStyle.Fill,
                    Text = $"{scene.Index:00} · {duration / 1000.0:0.0}s · {scene.StyleId}",
                    AutoEllipsis = true, TextAlign = ContentAlignment.MiddleCenter,
                    AllowDrop = true
                };
                tile.Controls.Add(caption);
                tile.Controls.Add(picture);
                tile.Disposed += (_, _) => {
                    var old = picture.Image;
                    picture.Image = null;
                    old?.Dispose();
                };
                var hint = $"{Timestamp(scene.StartMs)} → {Timestamp(scene.EndMs)}\n" +
                           $"{scene.StyleId} | {Path.GetFileName(scene.ImagePath)}\n{scene.Narration}";
                var tip = new ToolTip();
                tip.SetToolTip(tile, hint);
                tip.SetToolTip(picture, hint);
                tip.SetToolTip(caption, hint);
                tile.Disposed += (_, _) => tip.Dispose();
                EventHandler selectScene = (_, _) =>
                {
                    if (number < storyboardGrid.Rows.Count)
                    {
                        storyboardGrid.ClearSelection();
                        storyboardGrid.Rows[number].Selected = true;
                        storyboardGrid.CurrentCell = storyboardGrid.Rows[number].Cells[0];
                        storyboardGrid.FirstDisplayedScrollingRowIndex = number;
                        HighlightSelectedScene();
                    }
                };
                tile.Click += selectScene;
                picture.Click += selectScene;
                caption.Click += selectScene;
                Point? dragStart = null;
                picture.MouseDown += (_, e) => {
                    dragStart = e.Button == MouseButtons.Left ? e.Location : null;
                };
                picture.MouseMove += (_, e) => {
                    if (dragStart is not Point point || e.Button != MouseButtons.Left ||
                        isRebuildingStrip) return;
                    Size trigger = SystemInformation.DragSize;
                    if (Math.Abs(e.X - point.X) >= trigger.Width / 2 ||
                        Math.Abs(e.Y - point.Y) >= trigger.Height / 2)
                    {
                        dragStart = null;
                        picture.DoDragDrop(number, DragDropEffects.Move);
                    }
                };
                picture.MouseUp += (_, _) => dragStart = null;
                DragEventHandler allowDrop = (_, e) =>
                    e.Effect = e.Data?.GetDataPresent(typeof(int)) == true
                        ? DragDropEffects.Move : DragDropEffects.None;
                DragEventHandler receiveDrop = (_, e) => {
                    if (e.Data?.GetDataPresent(typeof(int)) == true &&
                        e.Data.GetData(typeof(int)) is int source)
                        SwapSceneImages(source, number);
                };
                foreach (Control target in new Control[] { tile, picture, caption })
                {
                    target.DragEnter += allowDrop;
                    target.DragDrop += receiveDrop;
                }
                sceneStrip.Controls.Add(tile);
            }
            sceneStrip.ResumeLayout();
            HighlightSelectedScene();
        }
        finally { isRebuildingStrip = false; }
    }

    /// <summary>
    /// Drag-and-drop changes only illustrations; chronological narration and
    /// SRT cue boundaries remain untouched.
    /// </summary>
    private void SwapSceneImages(int source, int destination)
    {
        if (source == destination || source < 0 || destination < 0 ||
            source >= scenes.Count || destination >= scenes.Count) return;
        var old = scenes[source].ImagePath;
        scenes[source].ImagePath = scenes[destination].ImagePath;
        scenes[destination].ImagePath = old;
        storyboardGrid.Refresh();
        RebuildSceneTimeline();
        ShowSelectedSceneImage();
        dirty = true;
        status.Text = $"Đã đổi ảnh của cảnh {source + 1} và {destination + 1}. " +
                      "Thời gian SRT/TTS giữ nguyên.";
    }

    private void HighlightSelectedScene()
    {
        if (sceneStrip.IsDisposed || isRebuildingStrip) return;
        int selected = storyboardGrid.CurrentRow?.Index ?? -1;
        foreach (Panel tile in sceneStrip.Controls.OfType<Panel>())
        {
            bool active = tile.Tag is int number && number == selected;
            tile.BackColor = active ? Color.FromArgb(207, 231, 250) : SystemColors.Control;
            foreach (Label label in tile.Controls.OfType<Label>())
                label.ForeColor = active ? Color.MidnightBlue : SystemColors.ControlText;
        }
    }
}
