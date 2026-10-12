using System;
using System.Drawing;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

public sealed partial class AiVideoStudioControl
{
    private void ConfigureWhiteboardQualityButton(FlowLayoutPanel controls)
    {
        var quality = new Button {
            Text = "Độ nét Whiteboard", AutoSize = true, Height = 29
        };
        quality.Click += (_, _) => EditWhiteboardQuality();
        controls.Controls.Add(quality);
    }

    private void EditWhiteboardQuality()
    {
        using var popup = new Form {
            Text = "Whiteboard V2.1-B — Chất lượng và giữ màu ảnh gốc",
            Width = 650, Height = 390, MinimumSize = new Size(610, 365),
            StartPosition = FormStartPosition.CenterParent
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, Padding = new Padding(14),
            ColumnCount = 2, RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 43));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        popup.Controls.Add(layout);

        var preset = new ComboBox {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList
        };
        preset.Items.AddRange(new object[] {
            "Nhanh (1.0x)", "Nét cao (1.5x) — khuyên dùng",
            "Siêu nét (2.0x) — chậm hơn"
        });
        var initial = project.WhiteboardRenderScale;
        preset.SelectedIndex = initial < 1.25 ? 0 : initial < 1.75 ? 1 : 2;
        var sharpen = new NumericUpDown {
            Dock = DockStyle.Left, Width = 85,
            Minimum = 0, Maximum = 50,
            DecimalPlaces = 0, Increment = 5,
            Value = (decimal)Math.Round(project.WhiteboardSharpenAmount * 100)
        };
        void Label(string title, int row) => layout.Controls.Add(new Label {
            Text = title, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        Label("Chất lượng xuất", 0);
        layout.Controls.Add(preset, 1, 0);
        Label("Tăng nét (%)", 1);
        layout.Controls.Add(sharpen, 1, 1);
        var reveal = new ComboBox {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList
        };
        reveal.Items.AddRange(new object[] {
            "Cổ điển — tô màu mờ dần",
            "Giữ màu ảnh gốc — reveal theo đường viền"
        });
        reveal.SelectedIndex = project.WhiteboardRevealMode == "source-mask" ? 1 : 0;
        Label("Cách hiện màu", 2);
        layout.Controls.Add(reveal, 1, 2);

        // Preset values are conservative to avoid halos on high-contrast ink.
        preset.SelectedIndexChanged += (_, _) =>
            sharpen.Value = preset.SelectedIndex switch {
                0 => 0, 1 => 15, _ => 10
            };

        var info = new Label {
            Dock = DockStyle.Fill,
            Text = "Giữ màu ảnh gốc: tay vẽ nét trước, sau đó ảnh gốc hiện dần " +
                   "qua mask bám đường viền (không tô lại bằng AI).\n" +
                   "Preview thể hiện kiểu reveal ở độ phân giải thấp; mức 1.5x/2.0x " +
                   "chỉ áp dụng khi Xuất YouTube.\n" +
                   "1.5x: dựng nét vẽ Full HD thực sự. 2.0x: dựng 1440p rồi thu về 1080p.\n" +
                   "Nếu cần giống ảnh gốc nhất, chọn 1.5x và Giữ màu ảnh gốc."
        };
        layout.Controls.Add(info, 0, 3);
        layout.SetColumnSpan(info, 2);
        var actions = new FlowLayoutPanel {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };
        var accept = new Button { Text = "Áp dụng", Width = 105 };
        var cancel = new Button { Text = "Hủy", Width = 80 };
        actions.Controls.Add(accept);
        actions.Controls.Add(cancel);
        layout.Controls.Add(actions, 0, 4);
        layout.SetColumnSpan(actions, 2);
        cancel.Click += (_, _) => popup.DialogResult = DialogResult.Cancel;
        accept.Click += (_, _) => {
            double scale = preset.SelectedIndex switch {
                1 => 1.5, 2 => 2.0, _ => 1.0
            };
            if (scale == 2.0 && MessageBox.Show(popup,
                    "Render 2.0x sử dụng nhiều RAM và thời gian hơn trên RTX 3050 4GB.\n" +
                    "Bạn muốn tiếp tục?",
                    "Chất lượng siêu nét",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            project.WhiteboardRenderScale = scale;
            project.WhiteboardSharpenAmount = (double)sharpen.Value / 100.0;
            project.WhiteboardRevealMode = reveal.SelectedIndex == 1
                ? "source-mask" : "classic";
            dirty = true;
            status.Text = $"Whiteboard: {scale:0.0}x, tăng nét {sharpen.Value}%, " +
                          (reveal.SelectedIndex == 1 ? "giữ màu ảnh gốc" : "màu cổ điển") +
                          ". Nhấn Lưu project.";
            popup.DialogResult = DialogResult.OK;
        };
        popup.ShowDialog(this);
    }
}
