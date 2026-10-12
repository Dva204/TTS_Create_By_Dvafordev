using System.Windows.Forms;
using TTS_Create_By_DvaForDev.AiVideoStudio;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private void InitializeAiVideoStudioTab()
    {
        var tab = new TabPage("AI Video Studio");
        tab.Controls.Add(new AiVideoStudioControl());
        mainTabControl.TabPages.Add(tab);
    }
}

