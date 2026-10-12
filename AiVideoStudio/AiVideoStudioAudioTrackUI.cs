using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>One narration file, accurately segmented by each scene's SRT timing.
/// This is a duration map, NOT a fabricated audio waveform.</summary>
public sealed partial class AiVideoStudioControl
{
    private readonly Panel capcutAudioLane = new() {
        Dock = DockStyle.Fill, BackColor = Color.FromArgb(238,248,243),
        Cursor = Cursors.Hand
    };

    private void ConfigureCapcutAudioLane()
    {
        capcutAudioLane.Paint += (_, e) => PaintCapcutAudioLane(e.Graphics);
        capcutAudioLane.MouseClick += (_, e) =>
        {
            if (scenes.Count == 0 || capcutAudioLane.Width <= 0) return;
            long total = Math.Max(1, scenes[^1].EndMs);
            long pos = Math.Clamp((long)((double)e.X / capcutAudioLane.Width * total), 0, total);
            int index = scenes.ToList().FindIndex(s => pos < s.EndMs);
            if (index < 0) index = scenes.Count - 1;
            if (index >= storyboardGrid.Rows.Count) return;
            storyboardGrid.ClearSelection();
            storyboardGrid.CurrentCell = storyboardGrid.Rows[index].Cells[0];
            storyboardGrid.Rows[index].Selected = true;
            HighlightSelectedScene();
            UpdateCapcutInspector();
            capcutAudioLane.Invalidate();
        };
    }

    private void PaintCapcutAudioLane(Graphics g)
    {
        var width = capcutAudioLane.ClientSize.Width;
        var height = capcutAudioLane.ClientSize.Height;
        if (width <= 0 || height <= 0) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Color.FromArgb(224, 241, 233));
        g.FillRectangle(background, 0, 0, width, height);
        if (scenes.Count == 0)
        {
            using var empty = new SolidBrush(Color.FromArgb(64, 111, 89));
            g.DrawString("TTS tổng · Chưa có phân cảnh", Font, empty, 9, 5);
            return;
        }

        long total = Math.Max(1, scenes[^1].EndMs);
        using var regular = new SolidBrush(Color.FromArgb(49, 119, 86));
        using var selected = new SolidBrush(Color.FromArgb(23, 91, 65));
        using var separators = new Pen(Color.White, 2);
        using var typeface = new Font("Segoe UI", 8f, FontStyle.Bold);
        for(int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            int left = (int)Math.Round(scene.StartMs * (double)width / total);
            int right = (int)Math.Round(scene.EndMs * (double)width / total);
            int segmentWidth = Math.Max(1, right-left);
            bool active = storyboardGrid.CurrentRow?.Index == i;
            g.FillRectangle(active ? selected : regular,
                left, 3, segmentWidth, height-6);
            g.DrawLine(separators, right, 3, right, height-3);
            if (segmentWidth > 54)
            {
                var label = $"SRT {scene.CueFrom}–{scene.CueTo}";
                g.DrawString(label, typeface, Brushes.White, left + 6, 5);
            }
        }
    }
}
