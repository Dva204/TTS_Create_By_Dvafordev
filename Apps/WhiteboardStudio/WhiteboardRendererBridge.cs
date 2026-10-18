namespace TTS_Create_By_DvaForDev.AiVideoStudio;

public sealed partial class AiVideoStudioControl
{
    public void SetWhiteboardRenderer(string executable)
    {
        if (!File.Exists(executable) ||
            !string.Equals(Path.GetFileName(executable), "whiteboard.exe",
                StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Vui lòng chọn whiteboard.exe hợp lệ.", executable);
        string interpreter = Path.Combine(Path.GetDirectoryName(executable)!, "python.exe");
        if (!File.Exists(interpreter))
            throw new FileNotFoundException("Cần python.exe cùng thư mục với Whiteboard Engine.", interpreter);
        rendererExecutable = executable;
    }
}
