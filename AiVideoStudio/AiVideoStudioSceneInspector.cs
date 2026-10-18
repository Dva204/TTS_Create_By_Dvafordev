using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>Editable per-scene inspector; reuses the existing storyboard binding.</summary>
public sealed partial class AiVideoStudioControl
{
    private void InstallSceneInspector(Control previewPanel)
    {
        var holder = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Color.FromArgb(241, 245, 249), Padding = new Padding(4, 3, 4, 3)
        };
        holder.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        holder.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
        var preview = previewOuter;
        previewPanel.Controls.Remove(preview);
        holder.Controls.Add(preview, 0, 0);
        var inspector = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 4,
            Padding = new Padding(9, 5, 9, 4), BackColor = Color.White
        };
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        inspector.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        inspector.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        inspector.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inspector.RowStyles.Add(new RowStyle(SizeType.Absolute, 33));
        holder.Controls.Add(inspector, 0, 1);
        previewPanel.Controls.Add(holder);
        var header = new Label {
            Text = "THUỘC TÍNH PHÂN CẢNH", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var current = new Label {
            Text = "Chọn một cảnh trên storyboard", Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight, ForeColor = Color.DimGray
        };
        inspector.Controls.Add(header, 0, 0);
        inspector.SetColumnSpan(header, 2);
        var topRight = new Panel { Dock = DockStyle.Fill };
        current.Dock = DockStyle.Fill;
        current.AutoEllipsis = true;
        var toggle = new Button {
            Text = "Mở chỉnh sửa", Dock = DockStyle.Right, Width = 118,
            FlatStyle = FlatStyle.Flat
        };
        toggle.FlatAppearance.BorderColor = Color.FromArgb(214, 225, 235);
        topRight.Controls.Add(current);
        topRight.Controls.Add(toggle);
        inspector.Controls.Add(topRight, 2, 0);
        inspector.SetColumnSpan(topRight, 2);
        bool manuallyCollapsed = false;
        bool hasShownScene = false;
        void SetInspectorExpanded(bool expanded)
        {
            holder.RowStyles[1].Height = expanded ? 188 : 34;
            inspector.RowStyles[1].Height = expanded ? 35 : 0;
            inspector.RowStyles[2].SizeType =
                expanded ? SizeType.Percent : SizeType.Absolute;
            inspector.RowStyles[2].Height = expanded ? 100 : 0;
            inspector.RowStyles[3].Height = expanded ? 33 : 0;
            foreach (Control child in inspector.Controls)
                if (child != header && child != topRight) child.Visible = expanded;
        }
        SetInspectorExpanded(false);
        toggle.Click += (_, _) =>
        {
            manuallyCollapsed = !manuallyCollapsed;
            SetInspectorExpanded(!manuallyCollapsed);
            toggle.Text = manuallyCollapsed ? "Mở chỉnh sửa" : "Thu gọn";
        };
        var style = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        style.Items.AddRange(new object[] { "whiteboard", "image-story", "image-motion", "layer", "text" });
        var transition = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        transition.Items.AddRange(new object[] { "fade", "cut" });
        var narration = new TextBox {
            Dock = DockStyle.Fill, ReadOnly = true, BackColor = Color.FromArgb(248, 250, 252),
            Multiline = true, ScrollBars = ScrollBars.Vertical
        };
        var notes = new TextBox { Dock = DockStyle.Fill, Multiline = true,
            ScrollBars = ScrollBars.Vertical };
        Label Caption(string title) => new() {
            Text = title, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        inspector.Controls.Add(Caption("Kiểu cảnh:"), 0, 1);
        inspector.Controls.Add(style, 1, 1);
        inspector.Controls.Add(Caption("Chuyển cảnh:"), 2, 1);
        inspector.Controls.Add(transition, 3, 1);
        inspector.Controls.Add(narration, 0, 2);
        inspector.SetColumnSpan(narration, 2);
        inspector.Controls.Add(notes, 2, 2);
        inspector.SetColumnSpan(notes, 2);
        var info = new Label {
            Dock = DockStyle.Fill, Text = "Bên trái: lời đọc  |  Bên phải: ghi chú",
            ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft
        };
        inspector.Controls.Add(info, 0, 3);
        inspector.SetColumnSpan(info, 2);
        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false
        };
        var save = new Button { Text = "Áp dụng", Width = 90, Height = 28 };
        var pickImage = new Button { Text = "Đổi ảnh", Width = 85, Height = 28 };
        buttons.Controls.Add(save); buttons.Controls.Add(pickImage);
        inspector.Controls.Add(buttons, 2, 3);
        inspector.SetColumnSpan(buttons, 2);
        void Refresh()
        {
            var selected = storyboardGrid.CurrentRow?.Index ?? -1;
            bool valid = selected >= 0 && selected < scenes.Count;
            inspector.Enabled = true;
            if (!valid)
            {
                current.Text = "Chưa chọn cảnh";
                SetInspectorExpanded(false);
                hasShownScene = false;
                toggle.Text = "Mở chỉnh sửa";
                toggle.Enabled = false;
                return;
            }
            toggle.Enabled = true;
            if (!hasShownScene) { manuallyCollapsed = false; hasShownScene = true; }
            SetInspectorExpanded(!manuallyCollapsed);
            toggle.Text = manuallyCollapsed ? "Mở chỉnh sửa" : "Thu gọn";
            var scene = scenes[selected];
            current.Text = $"#{scene.Index:000} • SRT {scene.CueFrom}–{scene.CueTo} • " +
                           $"{TimeSpan.FromMilliseconds(scene.StartMs):mm\\:ss} → " +
                           $"{TimeSpan.FromMilliseconds(scene.EndMs):mm\\:ss}";
            style.SelectedItem = scene.StyleId;
            transition.SelectedItem = scene.Transition;
            narration.Text = scene.Narration;
            notes.Text = scene.Notes;
        }
        save.Click += (_, _) =>
        {
            int index = storyboardGrid.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= scenes.Count) return;
            var scene = scenes[index];
            scene.StyleId = style.SelectedItem?.ToString() ?? scene.StyleId;
            scene.Transition = transition.SelectedItem?.ToString() ?? scene.Transition;
            scene.Notes = notes.Text;
            storyboardGrid.Refresh();
            dirty = true;
            status.Text = $"Đã cập nhật Scene {scene.Index:000}. Nhấn Lưu dự án để ghi lại.";
        };
        pickImage.Click += (_, _) =>
        {
            int index = storyboardGrid.CurrentRow?.Index ?? -1;
            if (index < 0 || index >= scenes.Count) return;
            using var picker = new OpenFileDialog {
                Title = "Đổi ảnh phân cảnh",
                Filter = "Ảnh (*.png;*.jpg;*.webp;*.bmp;*.svg)|*.png;*.jpg;*.jpeg;*.webp;*.bmp;*.svg"
            };
            if (picker.ShowDialog(this) != DialogResult.OK) return;
            scenes[index].ImagePath = picker.FileName;
            storyboardGrid.Refresh();
            RebuildSceneTimeline();
            ShowSelectedSceneImage();
            dirty = true;
        };
        storyboardGrid.SelectionChanged += (_, _) => Refresh();
        Refresh();
    }
}
