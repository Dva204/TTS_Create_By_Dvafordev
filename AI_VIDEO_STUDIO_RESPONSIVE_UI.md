# AI Video Studio — Giao diện tối ưu cho cửa sổ phóng to (UI V2.1)

Ngày cập nhật: 12/10/2026
Nhánh: feature/ai-video-studio

## Thay đổi toàn ứng dụng

- Khi mở từ `dotnet run` hoặc exe, cửa sổ Form1 mặc định ở
  `FormWindowState.Maximized`: phóng tối đa trong vùng làm việc Windows,
  **giữ taskbar và thanh tiêu đề**. Đây không phải chế độ kiosk/borderless.
- Vẫn có thể bấm Restore/Minimize/Close bình thường.
- Không chỉnh sửa Form1.cs hoặc Form1.Designer.cs đang có thay đổi cục bộ.
  Thay đổi mặc định được đặt tại Program.cs.

## AI Video Studio sau nâng cấp

- Hàng nhập TTS tổng, SRT, ảnh, tên project, cấu hình storyboard, thao tác
  lưu/xuất, thiết lập scene: từ 7×40 px xuống 7×33 px, giảm khoảng trống.
- Dải thumbnail giữ nguyên chức năng drag-and-drop, hiển thị
  trạng thái TTS/SRT/nhạc nền, căn lại chiều cao để không cắt ảnh.
- Màn hình đủ rộng (vùng Studio >= 1180px): **bảng scene bên trái** và
  **video preview + nội dung TTS bên phải**, dùng SplitContainer có thể kéo.
- Cửa sổ hẹp hơn: **bảng scene phía trên và video ở dưới**; panel preview
  tự chuyển thành video trái / lời thoại phải. Thay đổi bố cục không đổi
  thứ tự scene hoặc ảnh gốc.
- Nút Ưu tiên video vẫn ẩn bảng scene khi cần xem rõ hơn; Hiện storyboard
  sẽ khôi phục và tự tính lại hướng bố cục sau khi resize.
- Nút **Ẩn lời thoại** tận dụng toàn bộ vùng xem cho video; bấm lần nữa
  thành **Hiện lời thoại** để trả phần văn bản TTS.
- Dùng font Segoe UI 9.5pt, bảng scene với header rõ hơn, hàng xen kẽ
  màu nhẹ, giãn hàng 31px và tên cột ngắn hơn để có nhiều cột hữu ích
  trong panel bên trái.
- Header BẢNG PHÂN CẢNH và dòng hướng dẫn ở cuối cho thao tác thay ảnh.
- Player MP4, chuyển tỷ lệ 16:9/9:16/1:1/4:5/4:3/tự động, cửa sổ xem
  riêng, audio TTS tổng, SRT, thay ảnh WebP, Zoom/Pan/Whiteboard,
  render Full HD vẫn giữ nguyên.

## Cách chạy

Đóng phần mềm hiện có và chạy:

    cd "D:\TestTTS\TTS Create By DvaForDev"
    dotnet run

Vào tab AI Video Studio. Kéo thanh chia giữa bảng scene và video nếu cần
tăng vùng xem. Bấm Ưu tiên video để chỉ xem video, Ẩn lời thoại để bỏ
phần text chiếm chỗ, hoặc Xem riêng / Phóng to để xem trong cửa sổ riêng.

## Kiểm thử

- Build Release trong thư mục làm việc: 0 lỗi, 0 cảnh báo.
- Test WinForms STA ở 1160px: bố cục scene phía trên, video phía dưới.
- Test ở 1680px: hai cột scene/video; vùng video rộng hơn 600px.
- Resize trở lại 1160px: đổi lại bố cục ngang, giữ nguyên 40 thumbnail,
  các scene, trạng thái phát và ảnh.
- Test Ẩn/Hiện lời thoại: hoạt động, không xóa nội dung.
- Test WinForms embedded MP4 1920×1080, cửa sổ xem riêng, tỷ lệ video:
  giữ các bài test trước đạt.
- Test ảnh WebP thật và điều chỉnh thuộc tính Whiteboard: giữ nguyên.

Lưu ý: cửa sổ tự maximize chỉ khi khởi chạy; người dùng vẫn có thể restore.
Đây là nâng cấp UI thuần túy, không chỉnh sửa hoặc đẩy lên Git dữ liệu
ảnh/video hay các thay đổi TTS chưa commit của người dùng.
