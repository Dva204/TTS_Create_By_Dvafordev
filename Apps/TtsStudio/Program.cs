using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var form = new Form1 {
            Text = DvaForDev.Shared.StudioBranding.TtsName,
            StartPosition = FormStartPosition.CenterScreen,
            WindowState = FormWindowState.Normal,
        };
        Application.Run(form);
    }
}
