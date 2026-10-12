# AI Video Studio — Editor UI (CapCut-inspired)

Ngày: 12/10/2026
Nhánh: feature/ai-video-studio
Phạm vi: tái cấu trúc WinForms, **không đổi renderer/audio/SRT/project schema**.

## 1. Bố cục đã chốt và triển khai

**Thanh công cụ trên cùng**
- Tên dự án; Nguồn TTS / SRT / Ảnh; Thư viện ảnh
- Tạo storyboard; mở/lưu project JSON
- Xem trước MP4; Xem cảnh đã chọn; Dừng render
- **Xuất video** (YouTube Full HD): nút màu xanh lá chữ trắng in đậm
  cỡ lớn, ở cột riêng cố định phía trên bên phải; luôn cùng một handler
  render cũ, **không có nút xuất trùng lặp** trong phần nguồn dự án.
- Bảng chi tiết; Nhạc / Phụ đề; Whiteboard HD
- Các nút tự xuống dòng trên màn hình nhỏ hơn, không bị che vì toolbar một hàng.

**Workspace (màn hình rộng)**
- Cột trái ~225px: thư viện thumbnail ảnh, bấm gán ảnh cho scene đang chọn.
  Chỉ dùng 120 ảnh đầu tiên theo tên để hạn chế RAM; ảnh WebP được tạo
  PNG cache thông qua Pillow. Ảnh gốc không bị sửa.
- Giữa: preview ảnh hoặc video đã render, phát MP4, chọn tỷ lệ,
  nút Ưu tiên video và Xem riêng / Phóng to.
- Cột phải ~300px: panel thuộc tính scene, chỉnh hiệu ứng, kiểu chuyển
  cảnh, thay ảnh, dịch ranh giới SRT ±1 câu, xem TTS và ghi chú.
- Khi cửa sổ nhỏ hơn 1080px, ẩn thư viện ảnh, ưu tiên preview và thuộc tính.
- Nút Ưu tiên video ẩn tạm cả thư viện và panel thuộc tính,
  sau đó có thể khôi phục để tiếp tục chỉnh cảnh.

**Timeline phía dưới**
- Thẻ ảnh theo scene, chọn cảnh và kéo thumbnail để hoán đổi ảnh.
- Track TTS/SRT dựa theo thời lượng scene, hiển thị các khối tương ứng
  mốc cue thực, có thể nhấp để chọn scene. **Không tạo waveform giả.**
- Phụ đề/nhạc nền vẫn cấu hình bằng hộp thoại sẵn có.

**Bảng chi tiết**
- DataGridView vốn chiếm nửa màn hình được chuyển thành chế độ chỉnh bảng
  toàn màn hình vùng dựng, mở bằng nút Bảng chi tiết; có nút quay lại.
- Các cột đầy đủ SRT/ảnh/Director/ghi chú không bị xóa, có thể sửa như cũ.

**Nguồn dữ liệu**
- Đường dẫn TTS, SRT, thư mục ảnh, kiểu storyboard và các tùy chọn cũ
  không còn chiếm cố định 7 hàng trên đầu màn hình.
- Bấm Nguồn TTS / SRT / Ảnh để mở phần thiết lập, bấm lần nữa để thu gọn.

## 2. Tương thích được giữ nguyên

- TTS / Batch, Voice Library, Video Editor, Whiteboard AI và những tab khác.
- Cùng AiVideoProject + AiVideoScene, một TTS tổng, timecode SRT.
- Renderer Whiteboard V2.1-B giữ màu ảnh gốc, Layer, Zoom/Pan,
  Image Story, Text, nhạc, burn-in subtitle, NVENC/CPU fallback,
  cache và output Full HD.
- Không tự thêm model AI, API, đăng tải, hoặc thay đổi file dữ liệu người dùng.

## 3. Cách dùng

Mở PowerShell:

    cd "D:\TestTTS\TTS Create By DvaForDev"
    dotnet run

1. Bấm **Nguồn TTS / SRT / Ảnh**, nhập file TTS tổng, SRT và thư mục ảnh.
2. Chọn mode Theo số ảnh và kiểu hiệu ứng mặc định; bấm Tạo storyboard.
3. Chọn scene trên dải ảnh hoặc track SRT cuối màn hình.
4. Bấm ảnh trong thư viện trái để thay ảnh cho scene đang chọn.
5. Chọn hiệu ứng/chuyển cảnh, chỉnh ghi chú tại panel phải.
6. Nhấn Xem cảnh đã chọn để dựng preview chính cảnh và đoạn TTS tương ứng.
7. Nếu cần sửa nhiều trường cùng lúc, mở Bảng chi tiết.
8. Nhấn Lưu, Xem trước MP4, Xuất YouTube.

## 4. Giới hạn và công việc tiếp theo

Đây là giao diện lấy cảm hứng từ CapCut, **không phải phần mềm dựng
đa-track tùy ý tương đương CapCut hoặc Premiere**. Scene vẫn được xác
định bởi SRT. Audio lane hiển thị đoạn cue/timecode, chưa có waveform
thực hay khả năng dịch audio, thêm nhiều video clip đồng thời hoặc
cắt/chồng track tuỳ ý. Nút kéo thumbnail hiện **hoán đổi ảnh**, không
đảo thời gian TTS hoặc thứ tự lời thoại.

Cần nghiệm thu bằng chuột trên giao diện thật trên laptop trước
khi merge main; chức năng phát video qua Windows Media Player ActiveX
có fallback mở MP4 ngoài.

## 5. Kiểm thử

- Build WinForms Release.
- STA UI: 6 ảnh WebP thực tế trong thư viện.
- Chọn scene và chỉnh StyleId, Transition, Notes trực tiếp trong inspector.
- Bấm thư viện chọn ảnh cho đúng scene, không đổi SRT.
- Mở/đóng Nguồn và Bảng chi tiết; chế độ tập trung preview.
- Tự co giãn editor khi resize 1650px → 930px.
- 40 scene với 480s TTS giữ đúng timeline và track mốc SRT.
- MP4 1920×1080 đang có phát được trong preview giữa.
- Kiểm thử Python renderer, C# project JSON và 6 tab cũ.
