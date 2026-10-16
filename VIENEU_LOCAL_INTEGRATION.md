# VieNeu v3 Turbo — Clone giọng tiếng Việt local trong DvaForDev

## Tình trạng tích hợp

- Engine **vieneu_v3_turbo** trong TTS Create By DvaForDev WinForms .NET 8.
- ONNX/CPU, VieNeu 3.8.3, tạo WAV 48 kHz từ giọng mẫu WAV.
- Giữ các engine Chatterbox/Edge hiện tại, không thay đổi Voice Library.
- Backend: `backend/engines/vieneu_engine.py`; tiến trình riêng: `backend/vieneu_worker.py`.
- Dùng lại các API `POST /api/tts/generate`, `GET /api/jobs/{id}`, `GET /api/jobs/{id}/audio` và health.
- Cài trong `backend/.venv-vieneu` để không gây xung đột môi trường Chatterbox.

## Cài đặt và chạy

Trong PowerShell (lần đầu):

```powershell
cd "D:\TestTTS\TTS Create By DvaForDev"
powershell -ExecutionPolicy Bypass -File ".\backend\install_vieneu_cpu.ps1"
```

Sau đó đóng ứng dụng cũ và chạy lại:

```powershell
cd "D:\TestTTS\TTS Create By DvaForDev"
dotnet run
```

Mô hình VieNeu sẽ tải từ Hugging Face trong lần khởi tạo đầu tiên nếu máy chưa có cache. RTX 3050 4GB / 16GB RAM: mặc định dùng **CPU**, không cần CUDA.

## Sử dụng

1. Vào Voice Library > Thêm giọng, chọn file WAV/MP3 3–8 giây rõ ràng, chỉ một người nói. Chọn ngôn ngữ tiếng Việt (vi). Chỉ clone giọng bản thân hoặc giọng đã được người sở hữu cho phép.
2. Vào tab TTS: chọn engine `vieneu_v3_turbo`, ngôn ngữ `Tiếng Việt (vi)` và giọng mới thêm.
3. Nhập một câu tiếng Việt ngắn, bấm Generate hoặc Preview. Nghe thử WAV và kiểm tra giọng, nhịp, phát âm trước khi làm script dài.
4. Dùng WAV tổng và **SRT đã căn từ audio thật** cho AI Video Studio. Engine này không tự xác nhận SRT đồng bộ nếu chưa chạy bước căn phụ đề.

VieNeu v3 Turbo ở đây **chỉ hỗ trợ tiếng Việt**. Với kênh de-DE, es-MX, pt-BR hãy dùng engine đa ngôn ngữ khác.

## Sửa lỗi ONNX trên Windows

Khi ONNX Runtime báo `External data path escapes model directory`, worker tự tạo hardlink đến dữ liệu model Hugging Face đã tải trong:

`%USERPROFILE%\.cache\vieneu\onnx-local\<snapshot-hash>`

Điều này giúp ONNX đọc .onnx và .data cùng một thư mục hợp lệ mà không sửa/xóa cache model nguồn. Nếu không tạo được hardlink sẽ sao chép tệp. Dữ liệu model/cache không được đưa vào Git.

## Kiểm thử đã thực hiện

- Build .NET 8 Release thành công.
- Backend nhận đủ Chatterbox, Edge và VieNeu.
- Worker ping và import VieNeu SDK 3.8.3 thành công.
- Mô hình VieNeu nạp được trên CPU Windows sau bước xử lý cache.
- Tạo WAV từ voice preset thật: 48 kHz; clone từ WAV đó để đọc câu mới: 48 kHz.
- Ba unit/integration tests (giao thức worker, reuse model, API TTS + WAV) đạt.
- API thật POST generate > poll job > download WAV 48 kHz: thành công với audio mẫu tự tạo từ VieNeu.

## Xử lý lỗi và lưu ý

- Không thấy engine: đóng hẳn app và backend cũ, chạy lại `dotnet run`.
- Engine báo chưa cài: chạy `backend/install_vieneu_cpu.ps1`.
- Worker lỗi: xem `backend/data/logs/vieneu_worker.log`.
- Audio chất lượng thấp: thay reference sạch 3–8 giây và thử lại.
- Hủy một job TTS không đảm bảo ngắt ngay suy luận ONNX đã bắt đầu; job có thể còn chạy đến hết đoạn.
