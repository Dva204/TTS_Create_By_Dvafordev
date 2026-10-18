namespace DvaForDev.Shared;

public static class StudioBranding
{
    public const string TtsName = "DvaForDev TTS Studio";
    public const string WhiteboardName = "DvaForDev Whiteboard Studio";
    public static string UserDataFolder(string appName) =>
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "DvaForDev", appName);
}
