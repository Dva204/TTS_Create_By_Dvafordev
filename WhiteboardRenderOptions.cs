using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;

namespace TTS_Create_By_DvaForDev;

public sealed class WhiteboardRenderOptions
{
    public int LineThickness { get; set; } = 2;
    public string Hand { get; set; } = "asian";
    public decimal HandScale { get; set; } = 1.0m;
    public string LineReveal { get; set; } = "stroke";
    public string ColorFill { get; set; } = "fade";
    public string BlockFillStyle { get; set; } = "crayon";
    public string ColorFillScope { get; set; } = "block";
    public int MaxDrawBlocks { get; set; } = 6;
    public string BlockOrder { get; set; } = "reading";
    public decimal BlockOverlap { get; set; } = 0.08m;
    public int LineartSnapThreshold { get; set; } = 96;
    public bool LineartSnap { get; set; } = true;
    public string PreprocessMode { get; set; } = "edges";

    // Shared on this machine, independently of individual AI Video projects.
    public static string SharedPresetPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DvaForDev", "whiteboard-shared-preset.json");

    public void SaveSharedPreset()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SharedPresetPath)!);
        File.WriteAllText(SharedPresetPath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public static WhiteboardRenderOptions? LoadSharedPreset()
    {
        if (!File.Exists(SharedPresetPath)) return null;
        return JsonSerializer.Deserialize<WhiteboardRenderOptions>(
            File.ReadAllText(SharedPresetPath));
    }

    public IEnumerable<string> Arguments()
    {
        yield return "--line-thickness"; yield return LineThickness.ToString();
        yield return "--hand"; yield return Hand;
        yield return "--hand-scale"; yield return HandScale.ToString(System.Globalization.CultureInfo.InvariantCulture);
        yield return "--line-reveal"; yield return LineReveal;
        yield return "--color-fill"; yield return ColorFill;
        yield return "--block-fill-style"; yield return BlockFillStyle;
        yield return "--color-fill-scope"; yield return ColorFillScope;
        yield return "--max-draw-blocks"; yield return MaxDrawBlocks.ToString();
        yield return "--block-order"; yield return BlockOrder;
        yield return "--block-overlap"; yield return BlockOverlap.ToString(System.Globalization.CultureInfo.InvariantCulture);
        yield return "--lineart-snap-threshold"; yield return LineartSnapThreshold.ToString();
        if (!LineartSnap) yield return "--no-lineart-snap";
    }

    private sealed record RenderChoice(string Value, string Label)
    {
        public override string ToString() => Label;
    }

    public bool Edit(IWin32Window owner)
    {
        using var window = new Form { Text = "Whiteboard - Thiết lập kết xuất nâng cao",
            StartPosition = FormStartPosition.CenterParent, Size = new Size(660, 650),
            MinimumSize = new Size(500, 520) };
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2,
            AutoScroll = true, Padding = new Padding(12), RowCount = 0 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        window.Controls.Add(grid);
        void Add(string caption, Control control)
        {
            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            grid.Controls.Add(new Label { Text = caption, TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill }, 0, row);
            control.Dock = DockStyle.Fill;
            grid.Controls.Add(control, 1, row);
        }
        ComboBox Pick(string[] values, string[] labels, string current)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
            for (int i = 0; i < values.Length; i++)
                c.Items.Add(new RenderChoice(values[i], labels[i]));
            int found = Array.IndexOf(values, current);
            c.SelectedIndex = found < 0 ? 0 : found;
            return c;
        }
        NumericUpDown Number(decimal value, decimal min, decimal max, int decimals = 0)
            => new() { Minimum = min, Maximum = max, Value = Math.Clamp(value,min,max),
                       DecimalPlaces = decimals, Increment = decimals > 0 ? .05m : 1m };
        var thickness = Number(LineThickness, 0, 8); Add("Độ dày nét (0 = tự động)", thickness);
        var hand = Pick(new[] {"asian","black","children","white","procedural","none"}, new[] {"Tay châu Á","Tay da đen","Tay trẻ em","Tay da trắng","Tay mô phỏng","Không hiện tay"}, Hand); Add("Kiểu bàn tay", hand);
        var handScale = Number(HandScale, .1m, 3, 2); Add("Tỷ lệ kích thước tay", handScale);
        var reveal = Pick(new[] {"stroke","detail-wipe"}, new[] {"Vẽ theo từng nét","Hiện dần chi tiết"}, LineReveal); Add("Cách xuất hiện nét", reveal);
        var color = Pick(new[] {"fade","contour-wipe","brush-scan","top-down-blocks","left-to-right-gradient"}, new[] {"Mờ dần","Lộ màu theo đường viền","Quét cọ","Tô từng vùng từ trên xuống","Lộ màu từ trái sang phải"}, ColorFill); Add("Hiệu ứng hiện màu", color);
        var fill = Pick(new[] {"crayon","clean","soft-wash","dry-brush"}, new[] {"Màu sáp","Màu phẳng","Màu loang nhẹ","Cọ khô"}, BlockFillStyle); Add("Chất liệu tô", fill);
        var scope = Pick(new[] {"block","scene"}, new[] {"Theo từng đối tượng","Toàn cảnh"}, ColorFillScope); Add("Phạm vi tô màu", scope);
        var blocks = Number(MaxDrawBlocks, 1, 40); Add("Số nhóm nét tối đa", blocks);
        var order = Pick(new[] {"reading","source"}, new[] {"Thứ tự đọc","Thứ tự ảnh gốc"}, BlockOrder); Add("Thứ tự nhóm nét", order);
        var overlap = Number(BlockOverlap, 0, .65m, 2); Add("Độ chồng lấn nhóm", overlap);
        var threshold = Number(LineartSnapThreshold, 0, 255); Add("Ngưỡng bám nét gốc", threshold);
        var snap = new CheckBox { Checked = LineartSnap, Text = "Bám đường nét ảnh gốc" }; Add("Bám nét gốc", snap);
        var preprocessing = Pick(new[] {"edges","detail","ink"}, new[] {"Lấy đường viền","Giữ nhiều chi tiết","Giữ nét mực đậm"}, PreprocessMode); Add("Xử lý ảnh đầu vào", preprocessing);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = "Áp dụng", Width = 98 };
        var cancel = new Button { Text = "Hủy", Width = 98, DialogResult = DialogResult.Cancel };
        void ReadForm()
        {
            LineThickness = (int)thickness.Value;
            Hand = ((RenderChoice)hand.SelectedItem!).Value;
            HandScale = handScale.Value;
            LineReveal = ((RenderChoice)reveal.SelectedItem!).Value;
            ColorFill = ((RenderChoice)color.SelectedItem!).Value;
            BlockFillStyle = ((RenderChoice)fill.SelectedItem!).Value;
            ColorFillScope = ((RenderChoice)scope.SelectedItem!).Value;
            MaxDrawBlocks = (int)blocks.Value;
            BlockOrder = ((RenderChoice)order.SelectedItem!).Value;
            BlockOverlap = overlap.Value;
            LineartSnapThreshold = (int)threshold.Value;
            LineartSnap = snap.Checked;
            PreprocessMode = ((RenderChoice)preprocessing.SelectedItem!).Value;
        }
        buttons.Controls.Add(ok); buttons.Controls.Add(cancel);
        Add("Thao tác", buttons);
        ok.Click += (_, _) => { window.DialogResult = DialogResult.OK; };
        if (window.ShowDialog(owner) != DialogResult.OK) return false;
        ReadForm();
        return true;
    }
}
