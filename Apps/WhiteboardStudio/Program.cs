using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new WhiteboardStudioForm {
            StartPosition = FormStartPosition.CenterScreen,
            WindowState = FormWindowState.Maximized
        };
        Application.Run(form);
    }
}
