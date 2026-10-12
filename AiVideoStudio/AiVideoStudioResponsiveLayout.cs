using System;
using System.Drawing;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev.AiVideoStudio;

/// <summary>
/// Wide: editable scene table and large preview side by side.
/// Narrow: scene table above preview, preserving the user's saved scene state.
/// Only layout changes; renderer, audio and SRT are untouched.
/// </summary>
public sealed partial class AiVideoStudioControl
{
    private SplitContainer workspaceSplit = null!;
    private int studioLayoutMode = -1;

    private void UpdateStudioLayout()
    {
        if (workspaceSplit.IsDisposed || workspaceSplit.Width < 340 ||
            workspaceSplit.Height < 245) return;

        // The threshold is based on the Studio viewport, not screen pixels,
        // so restored windows and OS display scaling continue to work.
        bool wide = workspaceSplit.Width >= 1180;
        int nextMode = wide ? 1 : 0;
        if (studioLayoutMode == nextMode) return;

        studioLayoutMode = nextMode;
        workspaceSplit.SuspendLayout();
        try
        {
            // Reset minimums before changing orientation to avoid a WinForms
            // SplitterDistance exception in cramped/restored windows.
            workspaceSplit.Panel1MinSize = 0;
            workspaceSplit.Panel2MinSize = 0;
            if (!workspaceSplit.Panel1Collapsed)
            {
                workspaceSplit.Orientation = wide
                    ? Orientation.Vertical : Orientation.Horizontal;
                int available = (wide ? workspaceSplit.Width : workspaceSplit.Height)
                                - workspaceSplit.SplitterWidth;
                int desired = (int)(available * (wide ? .49 : .46));
                workspaceSplit.SplitterDistance = Math.Clamp(
                    desired, 0, Math.Max(0, available));
            }
            LayoutPreviewPanels(wide);
        }
        finally
        {
            workspaceSplit.ResumeLayout(true);
        }
    }
}
