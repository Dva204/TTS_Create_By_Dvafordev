using System;
using System.Windows.Forms;

namespace TTS_Create_By_DvaForDev;

// Windows Media Player ActiveX. No third-party video library required.
internal sealed class WhiteboardVideoPlayer : AxHost
{
    public WhiteboardVideoPlayer() : base("6BF52A52-394A-11D3-B153-00C04F79FAA6")
    {
        Dock = DockStyle.Fill;
    }

    public void LoadVideo(string path)
    {
        if (!IsHandleCreated) CreateControl();
        dynamic player = GetOcx();
        player.URL = path;
        player.uiMode = "full"; // native seek, volume, play and pause controls
        player.stretchToFit = true;
        player.settings.autoStart = true;
    }

    public void StopVideo()
    {
        if (!IsHandleCreated) return;
        dynamic player = GetOcx();
        player.controls.stop();
        player.URL = "";
    }
}
