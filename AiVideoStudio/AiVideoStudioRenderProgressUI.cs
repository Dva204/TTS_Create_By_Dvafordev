using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Scene-by-scene render progress without changing persisted project JSON.
/// Progress is driven only by actual renderer stdout, not estimates/timers.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private const string SceneProgressColumnName = "SceneRenderProgress";
    private readonly Label sceneRenderProgressLabel = new() {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight,
        Text = "Chưa render", AutoEllipsis = true,
        ForeColor = Color.FromArgb(55, 76, 100)
    };
    private readonly ProgressBar sceneRenderProgressBar = new() {
        Name = "sceneRenderProgressBar",
        Dock = DockStyle.Fill, Minimum = 0, Maximum = 1,
        Value = 0, Style = ProgressBarStyle.Continuous,
        Margin = new Padding(5, 7, 8, 7)
    };
    private int sceneRenderExpectedTotal;
    private int sceneRenderActiveIndex;
    private int sceneRenderSelectedIndex;
    private bool sceneRenderActive;
    private bool sceneRenderInMux;
    private readonly HashSet<int> sceneRenderCompleted = new();

    private void ConfigureSceneProgressUi(TableLayoutPanel view)
    {
        var column = new DataGridViewTextBoxColumn {
            Name = SceneProgressColumnName, HeaderText = "Tiến trình",
            ReadOnly = true, Width = 142,
            SortMode = DataGridViewColumnSortMode.NotSortable
        };
        storyboardGrid.Columns.Add(column);
        column.DisplayIndex = 4;
        storyboardGrid.DataBindingComplete += (_, _) => {
            if (!sceneRenderActive) ResetSceneProgressCells();
        };

        // Compact footer; no extra large video preview or busy log window.
        view.Controls.Remove(status);
        var footer = new TableLayoutPanel {
            Name = "sceneProgressFooter", Dock = DockStyle.Fill,
            RowCount = 1, ColumnCount = 3, BackColor = Color.White,
            Margin = new Padding(0)
        };
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        footer.Controls.Add(status, 0, 0);
        footer.Controls.Add(sceneRenderProgressLabel, 1, 0);
        footer.Controls.Add(sceneRenderProgressBar, 2, 0);
        view.Controls.Add(footer, 0, 4);
    }

    private void ResetSceneProgressCells()
    {
        if (!storyboardGrid.Columns.Contains(SceneProgressColumnName)) return;
        foreach (DataGridViewRow row in storyboardGrid.Rows)
            if (!row.IsNewRow) row.Cells[SceneProgressColumnName].Value = "Chưa dựng";
    }

    private void StartSceneRenderProgress(int renderCount, int selectedIndex = 0)
    {
        sceneRenderExpectedTotal = Math.Max(1, renderCount);
        sceneRenderProgressBar.Maximum = sceneRenderExpectedTotal;
        sceneRenderProgressBar.Value = 0;
        sceneRenderProgressLabel.Text = $"0/{sceneRenderExpectedTotal} cảnh xong";
        sceneRenderCompleted.Clear();
        sceneRenderActiveIndex = 0;
        sceneRenderSelectedIndex = selectedIndex;
        sceneRenderInMux = false;
        sceneRenderActive = true;
        ResetSceneProgressCells();
    }

    private void UpdateSceneRenderProgressSafe(string? line)
    {
        if (!AiVideoSceneRenderProgress.TryParse(line, out var progress) ||
            progress is null || !sceneRenderActive)
            return;
        if (!IsHandleCreated || IsDisposed) return;
        try
        {
            if (InvokeRequired)
                BeginInvoke(new Action(() => ApplySceneRenderProgress(progress)));
            else
                ApplySceneRenderProgress(progress);
        }
        catch (InvalidOperationException) { }
    }

    private void SetSceneCellStatus(int index, string text)
    {
        if (!storyboardGrid.Columns.Contains(SceneProgressColumnName)) return;
        var row = storyboardGrid.Rows.Cast<DataGridViewRow>()
            .FirstOrDefault(x => x.DataBoundItem is AiVideoScene scene && scene.Index == index);
        if (row is not null)
            row.Cells[SceneProgressColumnName].Value = text;
    }

    private void ApplySceneRenderProgress(AiVideoSceneRenderProgress evt)
    {
        if (!sceneRenderActive || evt.Total != sceneRenderExpectedTotal) return;
        switch (evt.Stage)
        {
            case "start":
                sceneRenderActiveIndex = evt.SceneIndex;
                SetSceneCellStatus(evt.SceneIndex, "Đang dựng...");
                break;
            case "done":
            case "cached":
                sceneRenderCompleted.Add(evt.SceneIndex);
                sceneRenderActiveIndex = 0;
                SetSceneCellStatus(evt.SceneIndex, evt.Stage == "cached"
                    ? "Có sẵn (cache)" : "Đã dựng");
                sceneRenderProgressBar.Value = Math.Min(
                    sceneRenderProgressBar.Maximum, sceneRenderCompleted.Count);
                break;
            case "mux":
                sceneRenderInMux = true;
                sceneRenderActiveIndex = 0;
                sceneRenderProgressBar.Value = sceneRenderProgressBar.Maximum;
                break;
            case "complete":
                sceneRenderInMux = false;
                sceneRenderProgressBar.Value = sceneRenderProgressBar.Maximum;
                break;
        }
        sceneRenderProgressLabel.Text = evt.Stage switch {
            "mux" => $"Đủ {evt.Total} cảnh · đang ghép MP4",
            "complete" => $"Đã xuất xong {evt.Total} cảnh",
            "start" => $"Cảnh {evt.Ordinal}/{evt.Total} · {sceneRenderCompleted.Count} xong",
            _ => $"{sceneRenderCompleted.Count}/{evt.Total} cảnh xong"
        };
    }

    private void FinishSceneRenderProgress(bool success)
    {
        if (!sceneRenderActive) return;
        if (success)
        {
            sceneRenderProgressBar.Value = sceneRenderProgressBar.Maximum;
            sceneRenderProgressLabel.Text = $"Hoàn tất {sceneRenderExpectedTotal} cảnh · MP4";
            // All clips have necessarily been generated successfully by this
            // point, even if final stdout events were queued on the UI thread.
            if (storyboardGrid.Columns.Contains(SceneProgressColumnName))
            {
                foreach (DataGridViewRow row in storyboardGrid.Rows)
                {
                    if (row.DataBoundItem is AiVideoScene s &&
                        (sceneRenderSelectedIndex == 0 || s.Index == sceneRenderSelectedIndex) &&
                        !sceneRenderCompleted.Contains(s.Index))
                        SetSceneCellStatus(s.Index, "Đã dựng");
                }
            }
        }
        else
        {
            sceneRenderProgressLabel.Text = sceneRenderInMux
                ? $"{sceneRenderCompleted.Count}/{sceneRenderExpectedTotal} cảnh · lỗi ghép"
                : $"{sceneRenderCompleted.Count}/{sceneRenderExpectedTotal} cảnh · dừng/lỗi";
            if (sceneRenderActiveIndex > 0)
                SetSceneCellStatus(sceneRenderActiveIndex, "Dừng / lỗi");
        }
        sceneRenderActive = false;
        sceneRenderInMux = false;
        sceneRenderActiveIndex = 0;
    }
}
