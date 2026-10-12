namespace TTS_Create_By_DvaForDev
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Normal maximized window: fills the working area while keeping
            // the Windows title bar, taskbar, Restore and Minimize controls.
            // Users can restore the window at any time.
            using var mainWindow = new Form1 {
                StartPosition = FormStartPosition.CenterScreen,
                WindowState = FormWindowState.Maximized
            };
            Application.Run(mainWindow);
        }
    }
}