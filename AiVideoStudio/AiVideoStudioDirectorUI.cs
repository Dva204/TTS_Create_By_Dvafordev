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

public sealed partial class AiVideoStudioControl
{
    private void ConfigureDirectorButtons(FlowLayoutPanel container)
    {
        var generate = new Button { Text = "Gợi ý hiệu ứng (local)", AutoSize = true, Height = 31 };
        var edit = new Button { Text = "Chỉnh Director của cảnh", AutoSize = true, Height = 31 };
        var note = new Label {
            Text = "Gợi ý theo SRT; zoom giữa ảnh (chưa nhận diện vật thể).",
            AutoSize = true, Margin = new Padding(8, 9, 2, 2)
        };
        container.Controls.Add(generate);
        container.Controls.Add(edit);
        AddLevel2Button(container);
        container.Controls.Add(note);
        generate.Click += async (_, _) => await SuggestDirectorAsync(generate);
        edit.Click += (_, _) => EditDirectorForSelectedScene();
    }

    private async Task SuggestDirectorAsync(Button button)
    {
        if (scenes.Count == 0)
        {
            MessageBox.Show(this, "Hãy tạo storyboard từ SRT trước.");
            return;
        }
        if (!File.Exists(subtitleInput.Text))
        {
            MessageBox.Show(this, "File phụ đề SRT không tồn tại.");
            return;
        }
        var script = RenderScriptPath();
        if (script is null)
        {
            MessageBox.Show(this, "Chưa cài module ai_video_director.py.");
            return;
        }
        var directorScript = Path.Combine(Path.GetDirectoryName(script)!, "ai_video_director.py");
        var python = Path.Combine(Path.GetDirectoryName(rendererExecutable)!, "python.exe");
        if (!File.Exists(directorScript) || !File.Exists(python))
        {
            MessageBox.Show(this, "Không tìm thấy script Director hoặc môi trường Python renderer.");
            return;
        }

        var prior = project.FilePath;
        var folder = Path.Combine(Path.GetTempPath(), "DvaForDevStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var source = Path.Combine(folder, "source.ai-video.json");
        var suggested = Path.Combine(folder, "suggestions.ai-video.json");
        button.Enabled = false;
        try
        {
            UpdateFromForm();
            AiVideoProjectService.Save(project, source);
            var psi = new ProcessStartInfo(python) {
                CreateNoWindow = true, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (var arg in new[] {
                directorScript, "--project", source, "--output", suggested
            }) psi.ArgumentList.Add(arg);
            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Không khởi chạy được Director.");
            var stdout = proc.StandardOutput.ReadToEndAsync();
            var stderr = proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            var result = await stdout;
            var errors = await stderr;
            if (proc.ExitCode != 0)
                throw new InvalidDataException("Director không thể lập kế hoạch:\n" + errors + result);
            var loaded = AiVideoProjectService.Load(suggested);
            loaded.FilePath = prior;
            project = loaded;
            scenes = new BindingList<AiVideoScene>(loaded.Scenes);
            storyboardGrid.DataSource = scenes;
            dirty = true;
            status.Text = result.Trim().Replace("\n", " | ");
            MessageBox.Show(this,
                "Đã lập kế hoạch Director.\n\nBấm 'Chỉnh Director của cảnh' để xem và sửa từng hiệu ứng.\n" +
                "Các hiệu ứng sẽ được lưu trong project JSON khi bạn chọn Lưu project.\n\n" +
                "Đây là gợi ý theo quy tắc, không phải AI nhận biết vị trí nhân vật trong ảnh.",
                "AI Video Director", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            status.Text = "Director lỗi: " + ex.Message;
            MessageBox.Show(this, ex.Message, "AI Video Director",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            project.FilePath = prior;
            button.Enabled = true;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    private void EditDirectorForSelectedScene()
    {
        storyboardGrid.EndEdit();
        if (storyboardGrid.CurrentRow?.DataBoundItem is not AiVideoScene scene)
        {
            MessageBox.Show(this, "Hãy chọn một cảnh trong storyboard.");
            return;
        }
        var events = new BindingList<AiVideoDirectionEvent>(
            (scene.DirectionEvents ?? new()).Select(e => new AiVideoDirectionEvent {
                Kind = e.Kind, StartMs = e.StartMs, DurationMs = e.DurationMs,
                Text = e.Text, Zoom = e.Zoom, FocusX = e.FocusX, FocusY = e.FocusY, Reason = e.Reason, Source = e.Source
            }).ToList());

        using var dialog = new Form {
            Text = $"Director — Cảnh {scene.Index} ({scene.EndMs - scene.StartMs} ms)",
            Size = new Size(1050, 460), StartPosition = FormStartPosition.CenterParent,
            MinimumSize = new Size(800, 380)
        };
        var panel = new TableLayoutPanel {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(9)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        dialog.Controls.Add(panel);
        panel.Controls.Add(new Label {
            Dock = DockStyle.Fill, AutoSize = false,
            Text = "Thời điểm tính từ đầu cảnh (mili giây). Text = chữ hiện trên video; Zoom = phóng hình.\n" +
                   "FocusX / FocusY từ 0 đến 1; 0.5 là giữa ảnh. Zoom nên từ 1.05–1.15.",
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        var grid = new DataGridView {
            Dock = DockStyle.Fill, AutoGenerateColumns = false,
            DataSource = events, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false
        };
        var kinds = new DataGridViewComboBoxColumn {
            HeaderText = "Loại", DataPropertyName = nameof(AiVideoDirectionEvent.Kind),
            DataSource = new[] { "text", "zoom" }, Width = 80
        };
        grid.Columns.Add(kinds);
        void AddCol(string label, string field, int width)
            => grid.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = label, DataPropertyName = field, Width = width,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        AddCol("Bắt đầu ms", nameof(AiVideoDirectionEvent.StartMs), 95);
        AddCol("Kéo dài ms", nameof(AiVideoDirectionEvent.DurationMs), 95);
        AddCol("Chữ cần hiện", nameof(AiVideoDirectionEvent.Text), 270);
        AddCol("Zoom", nameof(AiVideoDirectionEvent.Zoom), 65);
        AddCol("Focus X", nameof(AiVideoDirectionEvent.FocusX), 70);
        AddCol("Focus Y", nameof(AiVideoDirectionEvent.FocusY), 70);
        AddCol("Lý do AI", nameof(AiVideoDirectionEvent.Reason), 190);
        AddCol("Nguồn", nameof(AiVideoDirectionEvent.Source), 100);
        grid.DataError += (_, e) => {
            e.ThrowException = false;
            MessageBox.Show(dialog, "Dữ liệu thời gian hoặc zoom chưa đúng định dạng.", "Lỗi nhập");
        };
        grid.CurrentCellDirtyStateChanged += (_, _) => {
            if (grid.IsCurrentCellDirty)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        panel.Controls.Add(grid, 0, 1);
        var buttons = new FlowLayoutPanel {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft
        };
        var accept = new Button { Text = "Lưu hiệu ứng", Width = 125 };
        var cancel = new Button { Text = "Hủy", Width = 85 };
        var plusText = new Button { Text = "+ Chèn chữ", Width = 110 };
        var plusZoom = new Button { Text = "+ Zoom", Width = 100 };
        var remove = new Button { Text = "Xóa hiệu ứng", Width = 110 };
        buttons.Controls.AddRange(new Control[] { accept, cancel, remove, plusZoom, plusText });
        panel.Controls.Add(buttons, 0, 2);
        cancel.Click += (_, _) => dialog.DialogResult = DialogResult.Cancel;
        plusText.Click += (_, _) => {
            if (events.Count < 4) events.Add(new AiVideoDirectionEvent {
                Kind = "text", StartMs = 0,
                DurationMs = (int)Math.Min(1500L, scene.EndMs - scene.StartMs),
                Text = "IMPORTANT"
            });
        };
        plusZoom.Click += (_, _) => {
            if (events.Count < 4) events.Add(new AiVideoDirectionEvent {
                Kind = "zoom", StartMs = 0,
                DurationMs = (int)Math.Min(1600L, scene.EndMs - scene.StartMs)
            });
        };
        remove.Click += (_, _) => {
            if (grid.CurrentRow?.DataBoundItem is AiVideoDirectionEvent item)
                events.Remove(item);
        };
        accept.Click += (_, _) => {
            grid.EndEdit();
            var original = scene.DirectionEvents;
            scene.DirectionEvents = events.ToList();
            try
            {
                AiVideoProjectService.ValidateScenes(scenes.ToList());
                // Renderer supports one camera movement per scene in V1.2.
                if (scene.DirectionEvents.Count(e => e.Kind == "zoom") > 1)
                    throw new InvalidDataException("Hiện chỉ hỗ trợ một zoom mỗi cảnh.");
                dialog.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                scene.DirectionEvents = original ?? new();
                MessageBox.Show(dialog, ex.Message, "Hiệu ứng không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            storyboardGrid.Refresh();
            dirty = true;
            status.Text = $"Cảnh {scene.Index}: có {(scene.DirectionEvents?.Count ?? 0)} hiệu ứng Director.";
        }
    }
}
