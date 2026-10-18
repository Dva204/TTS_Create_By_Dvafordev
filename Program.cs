namespace TTS_Create_By_DvaForDev
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Open as a normal resizable window instead of maximizing on startup.
            using var mainWindow = new Form1 {
                StartPosition = FormStartPosition.CenterScreen,
                WindowState = FormWindowState.Maximized
            };
            Application.Run(mainWindow);
        }
    }
}