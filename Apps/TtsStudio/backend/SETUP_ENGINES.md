# DvaForDev TTS Studio — hai engine độc lập

Thư mục nguồn: `Apps/TtsStudio/backend/` trên Git Worktree `feature/tts-studio`.

## Cấu trúc đã cài đặt

- `.venv/`: Python 3.13, Chatterbox Multilingual 0.1.7, PyTorch 2.6.0+cu124, torchaudio 2.6.0+cu124 (CUDA RTX 3050).
- `.venv-vieneu/`: Python 3.13, VieNeu 3.8.3 và ONNX Runtime (CPU).
- `data/voices/`: thư viện giọng, 544 voice; giữ nguyên dữ liệu này.
- `data/jobs/`: job/wav do backend mới tạo.
- Model được tải vào cache của thư viện khi suy luận; không nằm trong Git và không được xóa khi dọn cache project.

## Cài lại môi trường khi cần

Từ PowerShell trong `backend`:

```powershell
.\install_chatterbox_cuda.ps1
.\install_vieneu_cpu.ps1
```

Hai script chỉ thao tác với hai venv của project mới; không cài vào Python hệ thống và không xóa dữ liệu giọng.

Để khởi động riêng backend, từ `backend`:

```powershell
.\.venv\Scripts\python.exe -u -m uvicorn main:app --host 127.0.0.1 --port 7865
```

Chỉ chạy một backend trên cổng 7865. Ứng dụng WinForms có cơ chế sử dụng lại backend đang hoạt động.

Kiểm tra trạng thái:

```powershell
Invoke-RestMethod http://127.0.0.1:7865/api/health
(Invoke-RestMethod http://127.0.0.1:7865/api/voices).Count
```

Đừng đổi đường dẫn `backend_root` trên kết quả health: đây phải là thư mục backend của TTS Studio mới. Với trạng thái hiện tại, API đã trả về cả hai engine `ready=true` và 544 giọng.

## Cách kiểm tra trong giao diện

Mở `DvaForDev_Workspaces.sln`, chọn `DvaForDev.TtsStudio` rồi chạy.
Trên màn hình Thư viện giọng, bấm **Refresh** nếu danh sách chưa hiện. Chọn giọng phù hợp, rồi Generate Preview.

- Giọng tiếng Việt -> VieNeu V3 Turbo.
- Giọng tiếng Anh/ngôn ngữ khác có hỗ trợ -> Chatterbox Multilingual.
- VieNeu sử dụng Temperature ở bước suy luận, Speed ở hậu xử lý (FFmpeg).
- Chatterbox sử dụng GPU RTX 3050. Với VRAM 4 GB, chỉ nên chạy một lượt suy luận nặng mỗi lần.

## Kết quả test setup 2026-10-19

- Import VieNeu thành công; đã xuất WAV tiếng Việt qua worker.
- Chatterbox đã tạo WAV tiếng Anh qua API, file dài 2.94 giây.
- Sau khi cả hai environment cài xong, health trả về ready=true và API voices trả về 544 giọng.
- Những bản audio thử nghiệm được lưu tại `%TEMP%\dvafordev_engine_smoke`, không nằm trong thư viện giọng.

Ghi chú: `ready=true` xác nhận thư viện có thể nạp; chất lượng giọng phải được kiểm tra bằng nghe. Không xóa backend gộp cũ cho đến khi toàn bộ workflow mới được xác nhận.
