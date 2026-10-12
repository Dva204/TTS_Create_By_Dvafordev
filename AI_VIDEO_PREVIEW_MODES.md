# AI Video Studio — Cải thiện khung xem trước MP4 (V2.0 RC1 UI)

Ngày: 12/10/2026

## Vì sao video trong giao diện bị bé?

Đã kiểm tra ai_video_youtube.mp4 từ FolderTestTTS: video gốc 1920 × 1080,
tỷ lệ 16:9 và khung hình video hiển thị đầy đủ khi trích frame.

Vấn đề là bố cục WinForms cũ phân phần lớn chiều cao cho bảng scene
và chỉ cho video 42% của phần còn lại. Windows Media Player phải giữ
tỷ lệ 16:9 nên nội dung hiển thị rất nhỏ. Không cần render lại.

## Những gì đã thay đổi

- Bảng storyboard và vùng xem video được ngăn cách bằng thanh chia,
  có thể kéo lên/xuống để ưu tiên phần bạn muốn làm việc.
- Nút **Ưu tiên video** thu gọn bảng storyboard để xem video rõ hơn;
  nút **Hiện storyboard** khôi phục bảng khi muốn chỉnh scene.
- Menu **Khung hình** cho phép:
  - **Tự động**: đọc kích thước file MP4 bằng ffprobe (không có thì mặc định 16:9).
  - **16:9**: video ngang YouTube.
  - **9:16**: khung xem dọc Shorts/Reels/TikTok.
  - **1:1**: vuông.
  - **4:5**: dọc kiểu mạng xã hội.
  - **4:3**: kiểu màn hình cổ điển.
- Player sử dụng tính toán contain/fit, căn giữa, giữ nguyên khung hình,
  không cắt hoặc kéo méo video để ép đầy mọi tỷ lệ.
- **Xem riêng / Phóng to** mở cửa sổ playback chỉnh kích thước được
  (ban đầu 980 × 650), có menu tỷ lệ riêng và Play/Pause/Seek.
- Khi mở cửa sổ xem riêng, player trong giao diện chính tạm dừng,
  tránh nghe hai bản audio TTS chồng lên nhau.
- Không sửa file MP4, ảnh WebP, SRT hay kết quả render; đây là thay
  đổi thuần giao diện.

## Cách sử dụng

1. Đóng TTS Studio đang mở, vào PowerShell:
       cd "D:\TestTTS\TTS Create By DvaForDev"
       dotnet run
2. Trong tab AI Video Studio, chọn **Mở MP4** để xem file đã render,
   hoặc bấm **Xuất YouTube** như bình thường.
3. Chọn **Tự động** nếu muốn hình khớp đúng tỷ lệ của file gốc.
4. Kéo thanh chia ngay trên vùng xem video để làm nó cao hơn.
5. Hoặc bấm **Ưu tiên video** nếu muốn xem chi tiết; xong thì chọn
   **Hiện storyboard**.
6. Bấm **Xem riêng / Phóng to** nếu cần một cửa sổ player độc lập,
   có thể kéo cạnh cửa sổ để thay kích thước.

## Phân biệt tỷ lệ xem và tỷ lệ xuất

Chọn 9:16 khi mở video 16:9 chỉ làm đổi **khung xem**, không biến
MP4 ngang thành MP4 dọc. Việc render ra tỷ lệ 9:16 thật là tính
năng output profile riêng, chưa có trong thay đổi giao diện này.

## Kiểm thử

- File ai_video_youtube.mp4 thực tế: 1920×1080, 16:9; trích frame
  cho thấy nội dung đủ khung hình, không có lỗi thu nhỏ từ engine.
- Build Release WinForms thành công (0 lỗi, 0 cảnh báo trong working tree).
- Test STA WinForms: 40 thumbnail, 6 WebP và việc hoán đổi ảnh vẫn đạt.
- Test hình học FitPreviewViewport cho 16:9, 9:16, 1:1, 4:5.
- Test đóng/mở storyboard bằng Ưu tiên video / Hiện storyboard đạt.
- Việc bấm play/pause/seek thực tế trong cửa sổ phát riêng vẫn nên được
  người dùng nghiệm thu bằng chuột trên Windows.
