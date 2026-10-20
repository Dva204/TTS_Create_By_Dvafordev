using System.Drawing;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    // Restore Windows Forms' standard look without changing existing controls or their events.
    private void InitializeNativeWinFormsUi()
    {
        SuspendLayout();
        BackColor = SystemColors.Control;
        ForeColor = SystemColors.ControlText;
        mainTabControl.Appearance = TabAppearance.Normal;
        mainTabControl.SizeMode = TabSizeMode.Normal;
        mainTabControl.Multiline = false;
        mainTabControl.Dock = DockStyle.Fill;
        RestoreNativeControlStyles(mainTabControl);
        ResumeLayout(true);
    }

    private static void RestoreNativeControlStyles(Control parent)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Standard;
                    button.UseVisualStyleBackColor = true;
                    button.BackColor = SystemColors.Control;
                    button.ForeColor = SystemColors.ControlText;
                    break;
                case DataGridView table:
                    table.BackgroundColor = SystemColors.AppWorkspace;
                    table.BorderStyle = BorderStyle.FixedSingle;
                    table.EnableHeadersVisualStyles = true;
                    table.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
                    table.ColumnHeadersDefaultCellStyle.ForeColor = SystemColors.ControlText;
                    table.DefaultCellStyle.BackColor = SystemColors.Window;
                    table.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                    table.GridColor = SystemColors.ControlDark;
                    break;
                case TextBox text:
                    if (!text.ReadOnly) text.BackColor = SystemColors.Window;
                    text.ForeColor = SystemColors.WindowText;
                    break;
                case TabPage page:
                    page.UseVisualStyleBackColor = true;
                    page.ForeColor = SystemColors.ControlText;
                    break;
                case GroupBox or Panel or FlowLayoutPanel or TableLayoutPanel:
                    control.BackColor = SystemColors.Control;
                    control.ForeColor = SystemColors.ControlText;
                    break;
            }
            if (control.HasChildren) RestoreNativeControlStyles(control);
        }
    }
}
