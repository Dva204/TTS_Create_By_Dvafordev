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
        WrapContents = false, AutoScroll = true, Padding = new Padding(4, 3, 4, 2)
    };
    private readonly Label timelineSummary = new() {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        AutoEllipsis = true
    };
    private bool isRebuildingStrip;

    private static string Timestamp(long ms) =>
        TimeSpan.FromMilliseconds(Math.Max(0, ms)).ToString(@"mm\:ss\.f", CultureInfo.InvariantCulture);

    private void ConfigureSceneTimeline(TableLayoutPanel view)
    {
        var wrapper = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2
        };
        wrapper.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        wrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wrapper.Controls.Add(timelineSummary, 0, 0);
        wrapper.Controls.Add(sceneStrip, 0, 1);
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

    private void RebuildSceneTimeline()
    {
        if (isRebuildingStrip) return;
        isRebuildingStrip = true;
        try
        {
            sceneStrip.SuspendLayout();
            foreach (Control existing in sceneStrip.Controls.Cast<Control>().ToArray())
                existing.Dispose();
            sceneStrip.Controls.Clear();
            long total = scenes.Count > 0 ? scenes[^1].EndMs : 0;
            timelineSummary.Text = scenes.Count == 0
                ? "Timeline: chưa có cảnh — một TTS tổng, chia thời gian bằng SRT"
                : $"Timeline hình ảnh: {scenes.Count} cảnh · {Timestamp(total)} · " +
                  $"Audio: {(string.IsNullOrWhiteSpace(audioInput.Text) ? "chưa chọn" : Path.GetFileName(audioInput.Text))}";
            foreach (var scene in scenes)
            {
                int number = scene.Index - 1;
                long duration = scene.EndMs - scene.StartMs;
                int width = Math.Clamp(82 + (int)(duration / 800), 95, 205);
                var button = new Button {
                    Tag = number, Width = width, Height = 35,
                    Text = $"{scene.Index:00} · {duration / 1000.0:0.0}s",
                    Margin = new Padding(2, 1, 2, 1),
                    FlatStyle = FlatStyle.Flat
                };
                button.FlatAppearance.BorderSize = 1;
                var hint = $"{Timestamp(scene.StartMs)} → {Timestamp(scene.EndMs)}\n" +
                           $"{scene.StyleId} | {Path.GetFileName(scene.ImagePath)}\n{scene.Narration}";
                var tip = new ToolTip();
                tip.SetToolTip(button, hint);
                button.Disposed += (_, _) => tip.Dispose();
                button.Click += (_, _) =>
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
                sceneStrip.Controls.Add(button);
            }
            sceneStrip.ResumeLayout();
            HighlightSelectedScene();
        }
        finally { isRebuildingStrip = false; }
    }

    private void HighlightSelectedScene()
    {
        if (sceneStrip.IsDisposed || isRebuildingStrip) return;
        int selected = storyboardGrid.CurrentRow?.Index ?? -1;
        foreach (Button button in sceneStrip.Controls.OfType<Button>())
        {
            bool active = button.Tag is int number && number == selected;
            button.BackColor = active ? Color.FromArgb(207, 231, 250) : SystemColors.Control;
            button.FlatAppearance.BorderColor = active ? Color.DodgerBlue : SystemColors.ControlDark;
            button.ForeColor = active ? Color.MidnightBlue : SystemColors.ControlText;
        }
    }
}
