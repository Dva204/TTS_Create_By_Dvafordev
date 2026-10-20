using System.Text.Json;
using TTS_Create_By_DvaForDev.Services;

namespace TTS_Create_By_DvaForDev;

public partial class Form1
{
    private async Task ImportVoiceFoldersAsync()
    {
        using var picker = new FolderBrowserDialog {
            Description = "Chọn thư mục cha chứa các thư mục giọng (mỗi thư mục có voice.json và file âm thanh)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        if (picker.ShowDialog(this) != DialogResult.OK) return;

        string[] metadataFiles;
        try {
            metadataFiles = Directory.GetFiles(picker.SelectedPath, "voice.json", SearchOption.AllDirectories);
        }
        catch (Exception ex) {
            MessageBox.Show(this, "Không đọc được thư mục: " + ex.Message);
            return;
        }

        if (metadataFiles.Length == 0) {
            MessageBox.Show(this, "Không tìm thấy voice.json trong các thư mục đã chọn.");
            return;
        }

        var confirmed = MessageBox.Show(this,
            $"Tìm thấy {metadataFiles.Length} thư mục có voice.json. Nhập các giọng chưa có, bỏ qua ID trùng?",
            "Import Voice Folder", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirmed != DialogResult.Yes) return;

        int imported = 0, skipped = 0, failed = 0;
        var errors = new List<string>();
        SetBusy(true);
        try {
            var existing = await api.GetVoicesAsync(null, lifetime.Token);
            var ids = new HashSet<string>(existing.Select(v => v.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var metaPath in metadataFiles) {
                if (lifetime.IsCancellationRequested) break;
                try {
                    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(metaPath, lifetime.Token));
                    var data = doc.RootElement;
                    string? Field(string key) => data.TryGetProperty(key, out var p)
                        && p.ValueKind == JsonValueKind.String ? p.GetString()?.Trim() : null;
                    var id = Field("id") ?? Path.GetFileName(Path.GetDirectoryName(metaPath));
                    var name = Field("name");
                    var reference = Field("reference_file");
                    if (!VoiceWorkspaceStore.IsValidVoiceId(id) || string.IsNullOrWhiteSpace(name))
                        throw new InvalidDataException("ID hoặc tên giọng không hợp lệ.");
                    if (ids.Contains(id!)) { skipped++; continue; }
                    if (string.IsNullOrWhiteSpace(reference) ||
                        Path.GetFileName(reference) != reference ||
                        !new[] { ".wav", ".mp3", ".m4a", ".flac" }.Contains(
                            Path.GetExtension(reference), StringComparer.OrdinalIgnoreCase))
                        throw new InvalidDataException("reference_file phải là tên file âm thanh hợp lệ.");
                    var audio = Path.Combine(Path.GetDirectoryName(metaPath)!, reference);
                    if (!File.Exists(audio))
                        throw new FileNotFoundException("Không tìm thấy audio: " + reference);
                    await api.AddVoiceAsync(name, Field("language"), Field("gender"),
                        Field("description"), id, audio, lifetime.Token);
                    ids.Add(id!);
                    imported++;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) {
                    failed++;
                    errors.Add($"{Path.GetFileName(Path.GetDirectoryName(metaPath))}: {ex.Message}");
                }
            }
            await RefreshVoices(null, lifetime.Token);
        }
        catch (Exception ex) {
            errors.Add("Không thể kết nối backend: " + ex.Message);
        }
        finally { SetBusy(false); }

        var result = $"Đã nhập: {imported}\nBỏ qua ID trùng: {skipped}\nLỗi: {failed}";
        if (errors.Count > 0)
            result += "\n\nChi tiết lỗi:\n" + string.Join("\n", errors.Take(12));
        WriteLog($"Import Folder: {imported} thành công, {skipped} trùng, {failed} lỗi.");
        MessageBox.Show(this, result, "Kết quả Import Folder",
            MessageBoxButtons.OK, failed > 0 || errors.Count > 0
                ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }
}
