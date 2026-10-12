# AI Video Studio V1.4 — Timeline ảnh và trình xem MP4 tích hợp

Nhánh triển khai: feature/ai-video-studio
Ngày: 2026-10-12

## Quy trình chính: 1 file audio TTS tổng + SRT + nhiều ảnh

Các phần TTS, Voice Library, Video Editor cũ được giữ nguyên.
AI Video Director không bắt buộc; không có yêu cầu API khi chỉ ghép cảnh.

### Các chức năng giao diện mới

- Timeline hiển thị thumbnail nhỏ cho từng scene (PNG/JPG/BMP được GDI+ hỗ trợ).
- Bấm vào thumbnail/nhãn để chọn cảnh; bảng storyboard đồng bộ lựa chọn.
- Ở khu vực bên dưới bảng, nhìn thấy ảnh nguồn, thời lượng, style,
  kiểu chuyển cảnh, lời thuyết minh tương ứng.
- Kéo thumbnail cảnh A và thả vào cảnh B để **đổi chỗ ảnh minh họa**.
  Không đổi thứ tự scene, không thay đổi SRT, không cắt/sắp xếp lại TTS.
- Thumbnail kích thước nhỏ được cache theo đường dẫn và thời điểm cập nhật
  ảnh để hạn chế đọc/giải mã lại khi bạn chỉnh ghi chú từng scene.
- Bấm **Xem cảnh đã chọn** để render riêng ảnh động với đoạn âm thanh
  tương ứng lấy từ file TTS tổng.
- Khi xuất cảnh riêng hoặc xuất cả preview, file MP4 sẽ được đưa vào
  cửa sổ phát video tích hợp (Windows Media Player ActiveX có sẵn trên
  nhiều bản Windows). Có Play/Pause/Seek của trình phát gốc.
- Bấm **Mở MP4** trong khu vực preview để phát lại file đã render mà
  không cần render lại.
- Nếu máy không có hoặc không chạy được Windows Media Player ActiveX,
  tool tự mở video bằng trình phát mặc định của Windows. Chức năng
  xem ảnh nguồn vẫn dùng được.

### Giới hạn và lưu ý

- Thumbnail SVG được hiện bằng placeholder; animation Whiteboard SVG
  vẫn có thể render qua engine. Một số định dạng WebP có thể không
  hiển thị thumbnail trên bản Windows GDI+; video vẫn xử lý bởi Pillow.
- Khung xem ảnh nguồn là ảnh tĩnh; muốn xem tay vẽ/Zoom/Pan cần render
  clip trước khi play.
- Không tự kéo scene sang chỗ khác theo thứ tự âm thanh, vì điều này
  sẽ làm sai mốc TTS/SRT. Drag/drop chỉ hoán đổi ảnh.
- Nếu mục tiêu là thêm track nhạc nền hoặc nhiều tầng hình theo timeline,
  cần nâng cấp editor nhiều track ở phiên bản sau.
- Thành phần Media Player phụ thuộc cấu hình Windows và có thể thiếu trên
  Windows N / một số máy; hiện đã có fallback ngoài.
- Chưa có thao tác nghiệm thu play/pause/seek bằng chuột thực tế sau
  khi hiển thị ứng dụng trên màn hình.

### Kiểm thử

- Build Release .NET 8 WinForms trên Windows: 0 lỗi.
- Test STA WinForms khởi tạo UserControl, 40 thumbnail từ ảnh 640×360:
  thành công.
- Test thay đổi ảnh cảnh 1 và 2: đường dẫn ảnh hoán đổi, thời gian SRT
  không đổi.
- Thử mở ảnh và đóng stream: không khóa file PNG nguồn.
- Trình hiển thị ảnh tĩnh inline: đạt trong test program.
- Windows Media Player COM có ProgID được đăng ký trên Admin-PC.
- Phát/tua trực tiếp bằng chuột chưa được chứng minh tự động;
  cần thử trong ứng dụng thực tế trên Windows.
- Renderer của V1.3 vẫn giữ một file audio tổng với cache scene,
  test 40 scene/8 phút dùng audio mô phỏng; chưa chạy toàn bộ
  40 cảnh bằng model Whiteboard nặng với giọng TTS thật.

### Test mẫu

Project có thể chạy trong PowerShell:
    cd "D:\TestTTS\TTS Create By DvaForDev"
    dotnet run

Bộ ảnh/audio giả lập 40 cảnh đã tạo tại:
D:\TestTTS\_whiteboard_engine_lab\out\studio_8min_40scenes

Lưu project JSON trước khi đóng ứng dụng để bảo toàn thay đổi ảnh/hiệu ứng.


## Sửa lỗi thumbnail WebP (12/10/2026)

Nguyên nhân đã kiểm chứng trên Admin-PC: cả 6 ảnh mẫu trong
FolderTestTTS/ImageTestFolder có đuôi .webp. Windows GDI+ khi mở
001.webp bằng Image.FromFile báo "Out of memory" dù ảnh hợp lệ:
Pillow đọc được ảnh WEBP 1920x1280.

Cách khắc phục:

1. AI Video Studio gọi whiteboard_tools/preview_image_bridge.py qua
   Python/Pillow của Whiteboard renderer khi gặp ảnh WebP.
2. Script chuyển nhiều ảnh WebP trong một tiến trình sang PNG
   thu nhỏ (tối đa 960x540), cache tại:
   %LOCALAPPDATA%\DvaForDev\TTSStudio\PreviewCache
3. Timeline và khung xem ảnh nguồn dùng PNG cache; trường ImagePath
   của scene và pipeline xuất video vẫn dùng ảnh WebP gốc.
4. File nguồn không bị thay đổi. Khi ảnh bị cập nhật, cache được
   tạo lại theo kích thước file và thời gian sửa cuối.
5. Nếu thiếu Python/Pillow của renderer, UI thông báo lỗi. Kiểm tra
   môi trường Python của Whiteboard Studio.
6. Sau khi cập nhật mã, đóng ứng dụng đang chạy, mở lại bằng dotnet run
   rồi tạo lại storyboard hoặc mở lại project.
   Không cần chuyển WebP thành PNG thủ công.

Kiểm thử: 6/6 ảnh WebP thật chuyển preview PNG và GDI+ mở được;
WinForms smoke khởi tạo đủ 6 thumbnail, đổi chỗ ảnh không làm lệch
TTS/SRT, không khóa file ảnh nguồn. Unit test Python kiểm tra cache
reuse/invalidation. Play/Pause/Seek MP4 vẫn chờ nghiệm thu.
