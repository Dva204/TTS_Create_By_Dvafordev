using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

public sealed partial class AiVideoStudioControl
{
    private void ImportScenePlan()
    {
        if (string.IsNullOrWhiteSpace(subtitleInput.Text) ||
            !File.Exists(subtitleInput.Text) ||
            !Directory.Exists(imageFolderInput.Text))
        {
            MessageBox.Show(this,
                "Trước tiên chọn file SRT thật và thư mục ảnh trong 'Nguồn TTS / SRT / Ảnh'.\n" +
                "Audio có thể chọn sau, nhưng SRT và thư mục ảnh là bắt buộc để đối chiếu JSON.",
                "Import Scene Plan", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ToggleCapcutSourcesIfNeeded();
            return;
        }
        using var dialog = new OpenFileDialog {
            Filter = "Claude / ChatGPT Scene Plan (*.json)|*.json",
            Title = "Chọn scene_plan.json từ Claude hoặc ChatGPT",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            // All validation occurs before touching the current storyboard.
            var preview = AiVideoScenePlanImporter.Read(
                dialog.FileName, subtitleInput.Text, imageFolderInput.Text);

            string warnings = preview.Warnings.Count == 0
                ? "Tất cả ảnh đã có trong thư mục."
                : string.Join(Environment.NewLine, preview.Warnings);
            string message =
                $"Nhập {preview.Scenes.Count} cảnh · {preview.CueCount} câu SRT · " +
                $"{TimeSpan.FromMilliseconds(preview.DurationMs):hh\\:mm\\:ss\\.fff}\n\n" +
                $"Whiteboard: {preview.Scenes.Count(x => x.StyleId == "whiteboard")}; " +
                $"Image Story: {preview.Scenes.Count(x => x.StyleId == "image-story")}.\n" +
                $"{warnings}\n\n" +
                (scenes.Count > 0
                    ? "THAO TÁC NÀY THAY THẾ storyboard hiện tại (chưa lưu sẽ mất).\n"
                    : "") +
                "Bạn muốn áp dụng Scene Plan này?";
            if (MessageBox.Show(this, message, "Xác nhận Import Scene Plan",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            ApplyScenePlanPreview(preview);
            status.Text = $"Đã import {preview.Scenes.Count} cảnh từ {Path.GetFileName(dialog.FileName)}; " +
                          $"thiếu {preview.MissingImages.Count} ảnh. Hãy Lưu project để giữ kết quả.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or
                                   System.Text.Json.JsonException or ArgumentException)
        {
            MessageBox.Show(this,
                "Không nhập Scene Plan. Storyboard hiện tại vẫn được giữ nguyên.\n\n" + ex.Message,
                "Scene Plan không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ToggleCapcutSourcesIfNeeded()
    {
        if (capcutSources?.Visible == false) ToggleCapcutSources();
    }

    private void ApplyScenePlanPreview(AiVideoScenePlanImporter.ImportPreview preview)
    {
        storyboardGrid.EndEdit();
        scenes = new BindingList<AiVideoScene>(preview.Scenes.ToList());
        storyboardGrid.DataSource = scenes;
        RebuildSceneTimeline();
        ResetSelectedScenePreviewTracking();
        UpdateCapcutInspector();
        dirty = true;
    }
}
