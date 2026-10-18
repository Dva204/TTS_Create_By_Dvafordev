using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

// All inference is opt-in. This UI never saves API secrets in project files.
public sealed partial class AiVideoStudioControl
{
    private Process? level2Process;

    private void AddLevel2Button(FlowLayoutPanel bar)
    {
        var launch = new Button { Text = "AI Director Mức 2", AutoSize = true, Height = 31 };
        var stop = new Button { Text = "Dừng AI", AutoSize = true, Height = 31, Enabled = false };
        bar.Controls.Add(launch);
        bar.Controls.Add(stop);
        stop.Click += (_, _) =>
        {
            try
            {
                if (level2Process is { HasExited: false })
                    level2Process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) { status.Text = "Dừng AI lỗi: " + ex.Message; }
        };
        launch.Click += async (_, _) => await ProposeLevel2Async(launch, stop);
    }

    private sealed record Level2Settings(string Provider, string Model, string Url,
                                          bool WithImages, bool ReplaceExisting);

    private Level2Settings? ChooseLevel2Settings()
    {
        using var dialog = new Form {
            Text = "AI Video Director Mức 2 — Kết nối mô hình",
            Size = new Size(680, 400), MinimumSize = new Size(630, 375),
            StartPosition = FormStartPosition.CenterParent
        };
        var layout = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8, Padding = new Padding(12)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 6; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        dialog.Controls.Add(layout);

        var provider = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        provider.Items.AddRange(new object[] {
            "Ollama (trên PC, miễn phí)", "API tương thích OpenAI (tùy chọn)"
        });
        provider.SelectedIndex = 0;
        var model = new TextBox { Dock = DockStyle.Fill, Text = "gemma3:4b" };
        var endpoint = new TextBox { Dock = DockStyle.Fill, Text = "http://127.0.0.1:11434" };
        var withImages = new CheckBox {
            Text = "Cho phép AI xem ảnh từng cảnh (model cần hỗ trợ Vision)",
            Dock = DockStyle.Fill, Checked = true
        };
        var overwrite = new CheckBox {
            Text = "Ghi đè các gợi ý Director đã có (mặc định: giữ nguyên)",
            Dock = DockStyle.Fill, Checked = false
        };
        void Add(string text, Control input, int row)
        {
            layout.Controls.Add(new Label {
                Text = text, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            layout.Controls.Add(input, 1, row);
        }
        Add("Nhà cung cấp", provider, 0);
        Add("Tên model", model, 1);
        Add("Địa chỉ máy chủ", endpoint, 2);
        Add("Hình ảnh", withImages, 3);
        Add("Quy tắc ghi đè", overwrite, 4);
        provider.SelectedIndexChanged += (_, _) =>
        {
            if (provider.SelectedIndex == 0)
            {
                endpoint.Text = "http://127.0.0.1:11434";
                model.Text = "gemma3:4b";
            }
            else
            {
                endpoint.Text = "https://api.openai.com/v1";
                model.Text = "gpt-4.1-mini";
            }
        };
        var help = new Label {
            Dock = DockStyle.Fill, AutoSize = false,
            Text = "Ollama: cần cài model Vision trước (gemma3:4b chỉ là ví dụ).\n" +
                   "API từ xa: đặt OPENAI_API_KEY trong môi trường Windows; không lưu key trong project.\n" +
                   "AI có thể chọn sai vật thể/vị trí zoom; luôn xem preview trước khi xuất.",
            ForeColor = SystemColors.GrayText
        };
        layout.Controls.Add(help, 0, 6);
        layout.SetColumnSpan(help, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft };
        var accept = new Button { Text = "Bắt đầu phân tích", AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Hủy", AutoSize = true, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(accept);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 7);
        layout.SetColumnSpan(buttons, 2);
        dialog.AcceptButton = accept;
        dialog.CancelButton = cancel;
        if (dialog.ShowDialog(this) != DialogResult.OK) return null;
        return new Level2Settings(provider.SelectedIndex == 0 ? "ollama" : "openai",
            model.Text.Trim(), endpoint.Text.Trim(), withImages.Checked, overwrite.Checked);
    }


    private async Task ProposeLevel2Async(Button trigger, Button stop)
    {
        if (scenes.Count == 0 || !File.Exists(subtitleInput.Text))
        {
            MessageBox.Show(this, "Hãy tạo storyboard và chọn file SRT hợp lệ trước.");
            return;
        }
        var settings = ChooseLevel2Settings();
        if (settings is null) return;
        if (string.IsNullOrWhiteSpace(settings.Model) || string.IsNullOrWhiteSpace(settings.Url))
        {
            MessageBox.Show(this, "Tên model và địa chỉ máy chủ không được để trống.");
            return;
        }
        if (settings.Provider == "openai")
        {
            string payload = settings.WithImages
                ? "nội dung SRT và ảnh minh họa đã thu nhỏ"
                : "nội dung SRT (không gửi ảnh)";
            if (MessageBox.Show(this,
                "Bạn sắp gửi " + payload + " đến:\n" + settings.Url +
                "\n\nDịch vụ bên ngoài có thể tính phí và xử lý dữ liệu theo chính sách của họ." +
                "\nBạn đồng ý tiếp tục?",
                "Xác nhận gửi dữ liệu đến dịch vụ ngoài",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }

        var script = RenderScriptPath();
        var modelScript = script is null ? null :
            Path.Combine(Path.GetDirectoryName(script)!, "ai_video_director_llm.py");
        var python = Path.Combine(Path.GetDirectoryName(rendererExecutable)!, "python.exe");
        if (modelScript is null || !File.Exists(modelScript) || !File.Exists(python))
        {
            MessageBox.Show(this, "Không tìm thấy script AI Mức 2 hoặc môi trường Python renderer.");
            return;
        }

        var previousFile = project.FilePath;
        var scratch = Path.Combine(Path.GetTempPath(), "DvaForDevStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var input = Path.Combine(scratch, "input.ai-video.json");
        var output = Path.Combine(scratch, "review.ai-video.json");
        trigger.Enabled = false;
        stop.Enabled = true;
        try
        {
            UpdateFromForm();
            AiVideoProjectService.Save(project, input);
            var psi = new ProcessStartInfo(python) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in new[] {
                modelScript, "--project", input, "--output", output,
                "--provider", settings.Provider, "--model", settings.Model,
                "--base-url", settings.Url
            }) psi.ArgumentList.Add(arg);
            if (settings.WithImages) psi.ArgumentList.Add("--with-images");
            if (settings.ReplaceExisting) psi.ArgumentList.Add("--replace-existing");

            var log = new StringBuilder();
            void ShowLine(string? line)
            {
                if (string.IsNullOrWhiteSpace(line) || IsDisposed) return;
                lock (log) { log.AppendLine(line); }
                SetStatusSafe(line);
            }
            using var process = new Process { StartInfo = psi };
            level2Process = process;
            process.OutputDataReceived += (_, args) => ShowLine(args.Data);
            process.ErrorDataReceived += (_, args) => ShowLine(args.Data);
            if (!process.Start()) throw new InvalidOperationException("Không thể chạy Python AI Director.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0 || !File.Exists(output))
            {
                string details;
                lock (log) details = log.ToString();
                if (details.Length > 2500) details = details[^2500..];
                throw new InvalidDataException("AI Director dừng hoặc lỗi:\n" + details);
            }

            var reviewed = AiVideoProjectService.Load(output);
            reviewed.FilePath = previousFile;
            project = reviewed;
            scenes = new BindingList<AiVideoScene>(reviewed.Scenes);
            storyboardGrid.DataSource = scenes;
            dirty = true;
            status.Text = "AI Mức 2 hoàn thành. Kiểm tra từng cảnh và lưu project.";
            MessageBox.Show(this,
                "Đã nhận gợi ý AI. Xem cột Director và dùng 'Chỉnh Director của cảnh' " +
                "để kiểm tra chữ, thời điểm và vị trí zoom trước khi render.\n" +
                "Điểm zoom do mô hình ước lượng nên có thể sai.",
                "AI Director Mức 2", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "AI Mức 2 lỗi: " + ex.Message;
            MessageBox.Show(this, ex.Message, "Lỗi AI Video Director",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            project.FilePath = previousFile;
            level2Process = null;
            trigger.Enabled = true;
            stop.Enabled = false;
            try { Directory.Delete(scratch, recursive: true); }
            catch (IOException) { }
        }
    }
}
