using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TTS_Create_By_DvaForDev.Models;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1 : Form
{
    private readonly AppSettings settings = new();
    private readonly TtsApiClient api;
    private readonly BackendProcessManager backend = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? generation;
    private string? activeJob, audioFile, lastConnectionError;

    public Form1()
    {
        InitializeComponent();
        api = new(settings);
        backend.OutputReceived += WriteLog;
        FormClosing += Form1_FormClosing;
    }

    private async void Form1_Load(object sender, EventArgs e)
    {
        // Designer already populated engine and language lists.
        if (engineComboBox.Items.Count > 0)
            engineComboBox.SelectedIndex = 0;

        // Prefer "vi" if present otherwise keep existing selection
        if (languageComboBox.Items.Cast<object?>().Any(x => (x?.ToString() ?? "") == "vi"))
            languageComboBox.SelectedItem = "vi";

        stopButton.Enabled = playButton.Enabled = saveWavButton.Enabled = false;

        try
        {
            await backend.StartAsync(settings.PythonExecutable, lifetime.Token);
        }
        catch (Exception ex)
        {
            WriteLog("Backend start: " + ex.Message);
        }

        for (var i = 0; i < 30 && !lifetime.IsCancellationRequested; i++)
        {
            try
            {
                await RefreshStatus(lifetime.Token);
                await RefreshVoices(lifetime.Token);
                _ = Monitor(lifetime.Token);
                return;
            }
            catch
            {
                try { await Task.Delay(1000, lifetime.Token); } catch { return; }
            }
        }

        WriteLog("Backend unavailable. Follow the setup steps in README.md.");
    }

    private async Task Monitor(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await RefreshStatus(token);
                lastConnectionError = null;
            }
            catch (Exception ex)
            {
                SetStatus("Backend: Offline", "GPU: Unknown");
                if (lastConnectionError != ex.Message)
                {
                    lastConnectionError = ex.Message;
                    WriteLog(ex.Message);
                }
            }

            try { await Task.Delay(2000, token); } catch { break; }
        }
    }

    private async Task RefreshStatus(CancellationToken token)
    {
        var health = await api.GetHealthAsync(token);
        var system = await api.GetSystemAsync(token);
        SetStatus(
            $"Backend: {health?.Status} / {(health?.Ready == true ? "Engine ready" : "Engine unavailable")}",
            system?.CudaAvailable == true ? $"GPU: {system.GpuName}" : "GPU: CUDA unavailable"
        );
    }

    private void SetStatus(string backendText, string gpuText)
    {
        if (InvokeRequired) { BeginInvoke(() => SetStatus(backendText, gpuText)); return; }
        backendStatusLabel.Text = backendText;
        gpuStatusLabel.Text = gpuText;
    }

    private async Task RefreshVoices(CancellationToken token)
    {
        var voices = await api.GetVoicesAsync(token);
        if (voices is null) return;
        voiceComboBox.DataSource = voices.ToList();
        voiceComboBox.DisplayMember = nameof(VoiceDto.Name);
        voiceComboBox.ValueMember = nameof(VoiceDto.Id);
    }

    private async void AddVoiceButton_Click(object sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "Audio (*.wav;*.mp3;*.m4a;*.flac)|*.wav;*.mp3;*.m4a;*.flac" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            SetBusy(true);
            var name = Path.GetFileNameWithoutExtension(dialog.FileName);
            var lang = languageComboBox.Text;
            var voice = await api.AddVoiceAsync(name, lang, dialog.FileName, lifetime.Token);
            await RefreshVoices(lifetime.Token);

            // try select imported voice
            if (voice is not null)
                voiceComboBox.SelectedValue = voice.Id;

            WriteLog($"Imported voice: {voice?.Name ?? name}");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void GenerateButton_Click(object sender, EventArgs e)
    {
        // Determine input text from available controls in Designer.
        string GetInputText()
        {
            // Prefer preview sentence box if present and non-empty
            if (Controls.OfType<TextBox>().Any(t => t.Name == "txtPreviewSentence" && !string.IsNullOrWhiteSpace(t.Text)))
                return Controls.Find("txtPreviewSentence", true).OfType<TextBox>().First().Text.Trim();

            // Fallback to voice name (not ideal but ensures UI works)
            if (!string.IsNullOrWhiteSpace(txtVoiceName.Text))
                return txtVoiceName.Text.Trim();

            return string.Empty;
        }

        var input = GetInputText();
        if (string.IsNullOrWhiteSpace(input) || voiceComboBox.SelectedItem is not VoiceDto voice)
        {
            MessageBox.Show(this, "Enter text (Preview sentence or Voice Name) and add or select a voice.");
            return;
        }

        generation?.Dispose();
        generation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);

        try
        {
            SetGenerating(true);
            CleanupAudio();

            var request = new GenerateRequest(input, voice.Id, languageComboBox.Text, engineComboBox.Text);
            var accepted = await api.GenerateAsync(request, generation.Token);
            activeJob = accepted.Id;
            WriteLog($"Job {activeJob} queued.");

            while (true)
            {
                await Task.Delay(800, generation.Token);
                var job = await api.GetJobAsync(activeJob, generation.Token) ?? throw new InvalidOperationException("Job not found.");
                if (job.Status == "failed") throw new InvalidOperationException(job.Error ?? "Generation failed.");
                if (job.Status == "cancelled") throw new OperationCanceledException();
                if (job.Status == "completed")
                {
                    audioFile = Path.Combine(Path.GetTempPath(), $"tts-{activeJob}.wav");
                    var audioBytes = await api.DownloadAudioAsync(activeJob, generation.Token);
                    await File.WriteAllBytesAsync(audioFile, audioBytes, generation.Token);
                    playButton.Enabled = saveWavButton.Enabled = true;
                    WriteLog("WAV generated successfully.");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            WriteLog("Generation stopped.");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            activeJob = null;
            SetGenerating(false);
        }
    }

    private async void StopButton_Click(object sender, EventArgs e)
    {
        generation?.Cancel();
        if (activeJob is not null)
        {
            try { await api.CancelJobAsync(activeJob, lifetime.Token); }
            catch (Exception ex) { WriteLog(ex.Message); }
        }
    }

    private void PlayButton_Click(object sender, EventArgs e)
    {
        if (audioFile is not null && File.Exists(audioFile))
            Process.Start(new ProcessStartInfo(audioFile) { UseShellExecute = true });
    }

    private void SaveWavButton_Click(object sender, EventArgs e)
    {
        if (audioFile is null) return;
        using var dialog = new SaveFileDialog { Filter = "WAV audio|*.wav", FileName = $"tts-{DateTime.Now:yyyyMMdd-HHmmss}.wav" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            File.Copy(audioFile, dialog.FileName, true);
    }

    private void SetBusy(bool busy)
    {
        addVoiceButton.Enabled = generateButton.Enabled = !busy;
    }

    private void SetGenerating(bool active)
    {
        SetBusy(active);
        stopButton.Enabled = active;
    }

    private void WriteLog(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => WriteLog(message)); return; }
        logsTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void ShowError(Exception ex)
    {
        WriteLog("ERROR: " + ex.Message);
        MessageBox.Show(this, ex.Message, "TTS Studio", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void CleanupAudio()
    {
        if (audioFile is not null)
            try { File.Delete(audioFile); } catch { }

        audioFile = null;
        playButton.Enabled = saveWavButton.Enabled = false;
    }

    private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        lifetime.Cancel();
        generation?.Cancel();
        CleanupAudio();
        await backend.DisposeAsync();
        api.Dispose();
    }
}
