using System;
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
    private Process? renderProcess;
    private string rendererExecutable =
        @"D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe";

    private void ConfigureRenderButtons(FlowLayoutPanel panel)
    {
        var preview = new Button { Name = "btnStudioPreview", Text = "Xem trước MP4", AutoSize = true, Height = 30 };
        var youtube = new Button { Name = "btnStudioExportYouTube", Text = "Xuất YouTube", AutoSize = true, Height = 30 };
        var scenePreview = new Button { Name = "btnStudioSelectedScenePreview", Text = "Xem cảnh đã chọn", AutoSize = true, Height = 30 };
        var stop = new Button { Name = "btnStudioStopRender", Text = "Dừng render", AutoSize = true, Height = 30, Enabled = false };
        panel.Controls.Add(preview);
        panel.Controls.Add(scenePreview);
        panel.Controls.Add(youtube);
        panel.Controls.Add(stop);

        stop.Click += (_, _) =>
        {
            try
            {
                if (renderProcess is { HasExited: false })
                {
                    renderProcess.Kill(entireProcessTree: true);
                    status.Text = "Đang dừng render...";
                }
            }
            catch (Exception ex) { status.Text = "Không thể dừng: " + ex.Message; }
        };
        preview.Click += async (_, _) => await RunProjectRenderAsync("preview", preview, youtube, stop);
        youtube.Click += async (_, _) => await RunProjectRenderAsync("youtube", preview, youtube, stop, 0, scenePreview);
        scenePreview.Click += async (_, _) =>
        {
            int selected = storyboardGrid.CurrentRow?.Index ?? -1;
            if (selected < 0 || selected >= scenes.Count)
            {
                MessageBox.Show(this, "Hãy chọn một cảnh trong storyboard trước.");
                return;
            }
            await RunProjectRenderAsync("preview", preview, youtube, stop, selected + 1, scenePreview);
        };
    }

    private static string? RenderScriptPath()
    {
        string dir = AppContext.BaseDirectory;
        var paths = new[] {
            Path.Combine(dir, "whiteboard_tools", "ai_video_pipeline.py"),
            Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "whiteboard_tools", "ai_video_pipeline.py"))
        };
        return paths.FirstOrDefault(File.Exists);
    }

    private void SetStatusSafe(string text)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            if (InvokeRequired)
                BeginInvoke(new Action(() => { if (!IsDisposed) status.Text = text; }));
            else
                status.Text = text;
        }
        catch (InvalidOperationException) { }
    }

    private async Task RunProjectRenderAsync(string quality, Button preview, Button youtube,
                                               Button stop, int onlyScene = 0, Button? scenePreview = null)
    {
        if (renderProcess is { HasExited: false }) return;
        try
        {
            storyboardGrid.EndEdit();
            UpdateFromForm();
            if (project.Scenes.Count == 0)
                throw new InvalidOperationException("Hãy tạo storyboard trước khi render.");
            if (!File.Exists(project.AudioPath))
                throw new FileNotFoundException("Chưa chọn audio TTS hợp lệ.", project.AudioPath);
            AiVideoProjectService.ValidateScenes(project.Scenes);
            var unavailable = project.Scenes
                .Where(s => onlyScene == 0 || s.Index == onlyScene)
                .Where(s => s.StyleId is "map" or "timeline" or "infographic")
                .Select(s => $"Cảnh {s.Index}: {s.StyleId}").ToArray();
            if (unavailable.Length != 0)
                throw new NotSupportedException("Các engine chưa được triển khai:\n" +
                    string.Join("\n", unavailable) +
                    "\nHãy đổi sang Whiteboard, Layer, Zoom/Pan, Text hoặc Image Story.");

            var script = RenderScriptPath()
                         ?? throw new FileNotFoundException("Không tìm thấy ai_video_pipeline.py.");
            if (!File.Exists(rendererExecutable))
            {
                using var picker = new OpenFileDialog {
                    Filter = "Whiteboard renderer|whiteboard.exe|Executable|*.exe",
                    Title = "Chọn whiteboard.exe trong thư mục renderer .venv\\Scripts" };
                if (picker.ShowDialog(this) != DialogResult.OK) return;
                rendererExecutable = picker.FileName;
            }
            var python = Path.Combine(Path.GetDirectoryName(rendererExecutable)!, "python.exe");
            if (!File.Exists(python))
                throw new FileNotFoundException("Không tìm thấy python.exe đi kèm whiteboard.exe.", python);

            using var saveDialog = new SaveFileDialog {
                Filter = "MP4 Video|*.mp4", DefaultExt = "mp4",
                FileName = onlyScene > 0 ? $"scene_{onlyScene:000}_preview.mp4" :
                    quality == "preview" ? "ai_video_preview.mp4" : "ai_video_youtube.mp4",
                Title = "Chọn vị trí xuất video AI"
            };
            saveDialog.InitialDirectory = Directory.Exists(outputFolderInput.Text)
                ? outputFolderInput.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (saveDialog.ShowDialog(this) != DialogResult.OK) return;
            var destination = Path.GetFullPath(saveDialog.FileName);
            UpdateOutputDirectory(Path.GetDirectoryName(destination)!);
            string projectCopy = Path.Combine(Path.GetDirectoryName(destination)!,
                Path.GetFileNameWithoutExtension(destination) + ".ai-video.json");
            if (File.Exists(projectCopy) &&
                MessageBox.Show(this, "File JSON đi kèm đã tồn tại. Ghi đè bản này?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes) return;

            // Persist an export snapshot, but never overwrite the user's open project.
            string oldProjectFile = project.FilePath;
            try { AiVideoProjectService.Save(project, projectCopy); }
            finally { project.FilePath = oldProjectFile; }

            var psi = new ProcessStartInfo(python) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            foreach (string argument in new[] {
                script, "--project", projectCopy,
                "--output", destination, "--quality", quality,
                "--engine", rendererExecutable
            }) psi.ArgumentList.Add(argument);
            if (onlyScene > 0)
            {
                psi.ArgumentList.Add("--scene-index");
                psi.ArgumentList.Add(onlyScene.ToString());
            }

            using var process = new Process { StartInfo = psi };
            var log = new StringBuilder();
            void Capture(string? line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                lock (log)
                {
                    log.AppendLine(line);
                    if (log.Length > 8000) log.Remove(0, log.Length - 8000);
                }
                SetStatusSafe(line);
            }
            renderProcess = process;
            preview.Enabled = false;
            youtube.Enabled = false;
            if (scenePreview != null) scenePreview.Enabled = false;
            stop.Enabled = true;
            if (simpleStudioLayout) stop.Visible = true;
            status.Text = "Đang chuẩn bị render " + quality + "...";
            process.OutputDataReceived += (_, e) =>
            {
                Capture(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                Capture(e.Data);
            };
            if (!process.Start()) throw new InvalidOperationException("Không chạy được renderer Python.");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            if (process.ExitCode == 0)
            {
                status.Text = "Đã xuất video: " + destination;
                if (File.Exists(destination) && quality == "preview")
                {
                    // The simple layout has no embedded player. Play the
                    // preview in the user's default Windows video application.
                    if (simpleStudioLayout)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(destination) { UseShellExecute = true });
                        }
                        catch (System.ComponentModel.Win32Exception)
                        {
                            // MP4 was already rendered successfully; a missing
                            // default video player must not report render failure.
                            status.Text = "Đã tạo video xem thử: " + destination +
                                          ". Dùng Mở output để tìm MP4.";
                        }
                    }
                    else
                        OpenVideoInStudio(destination);
                }
            }
            else
            {
                status.Text = "Render đã dừng hoặc lỗi (exit " + process.ExitCode + ").";
                string tail;
                lock (log) tail = log.ToString();
                MessageBox.Show(this, tail.Length > 2500 ? tail[^2500..] : tail,
                    "Chi tiết lỗi Whiteboard / Video Render",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            status.Text = "Render lỗi: " + ex.Message;
            MessageBox.Show(this, ex.Message, "AI Video Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            renderProcess = null;
            preview.Enabled = true;
            youtube.Enabled = true;
            if (scenePreview != null) scenePreview.Enabled = true;
            stop.Enabled = false;
            if (simpleStudioLayout) stop.Visible = false;
        }
    }
}

