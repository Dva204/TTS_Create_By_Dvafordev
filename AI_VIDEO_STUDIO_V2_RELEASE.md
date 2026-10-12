# AI Video Studio V2.0 — Release Candidate

Ngày: 12/10/2026
Nhánh: feature/ai-video-studio
Renderer: ai-video-render-v2.0.0-rc1

## Quy trình

Một file TTS tổng + SRT + ảnh PNG/JPG/WebP/SVG → Storyboard → Preview → MP4 YouTube.

Giữ nguyên TTS/Batch, Voice Library, Video Editor, Settings và Whiteboard cũ. AI Director không bắt buộc, không tự gọi API.

## Chức năng

- Chia scene theo số ảnh hoặc số câu SRT; chỉnh ranh giới SRT mà không chia nhỏ file audio.
- Hiệu ứng đã render được: Whiteboard, Layer, Zoom/Pan, Image Story, Text. Chuyển cảnh cut/fade từ nền trắng.
- Timeline thumbnail; kéo-thả hoán đổi ảnh không đổi audio; xem lời thoại và xem trước MP4 trong WinForms.
- Composer V2 có ba track: hình ảnh/scene, một TTS tổng, lớp SRT/nhạc nền tùy chọn.
- Nhạc nền WAV/MP3/M4A… có quyền sử dụng: loop đến hết video, trộn nhỏ hơn TTS chính, chỉnh 0–50%, mặc định 12%.
- Chèn phụ đề SRT trực tiếp vào video (burn-in ASS/libass), hỗ trợ preview một scene đúng timestamp.
- Nút Nhạc nền / Phụ đề để chỉnh track. Nút Kiểm tra dự án để rà soát đầu vào mà không render.
- Preview 640×360/20fps; YouTube 1920×1080/24fps H.264 + AAC. Rendering 1280×720 rồi upscale 1080p bằng NVENC, fallback libx264.
- Cache scene, cache video, có thể bấm Dừng render; thành phẩm mới được xuất ra file tạm và kiểm tra track/time trước khi ghi đè MP4 cũ.
- Project schemaVersion=1 có thêm musicPath, musicVolume, burnSubtitles; project cũ vẫn mở được.

## Sử dụng Windows

1. Mở PowerShell tại D:\TestTTS\TTS Create By DvaForDev, chạy dotnet run.
2. Chọn audio TTS tổng, SRT và thư mục ảnh đặt tên 001.webp, 002.webp... trong AI Video Studio.
3. Chọn Theo số ảnh, chọn Image Story hoặc Zoom/Pan làm mặc định cho ảnh AI có sẵn.
4. Tạo storyboard và thay ảnh/hiệu ứng nếu cần. Whiteboard cần ảnh nét vẽ đủ rõ.
5. Nhấn Nhạc nền / Phụ đề. Chọn nhạc, giảm âm lượng còn khoảng 8–15%, bật phụ đề nếu muốn.
6. Nhấn Kiểm tra dự án, rồi Xem cảnh đã chọn để nghe thử.
7. Lưu project JSON, nhấn Xem trước MP4, sau đó Xuất YouTube.
8. Kiểm tra âm thanh/phụ đề trực tiếp trên MP4 trước khi đăng.

Renderer CLI hỗ trợ --validate-only để kiểm tra mà không xuất video.

## Bài kiểm thử

- TTS WAV thật 19,24s + 6 cue SRT + 6 ảnh WebP thực tế từ FolderTestTTS: preview 640×360 và Full HD 1920×1080/24fps có nhạc và burn-in phụ đề thành công, duration 19,25s.
- Render Full HD bằng NVENC RTX 3050; cùng cấu hình, cache thành phẩm trong khoảng 0,52s.
- 40 Image Story + 80 cue SRT + BGM + phụ đề, audio giả lập 480s: Full HD 1920×1080, 24fps, 11520 frame, dài chính xác 480s, thời gian dựng khoảng 110s.
- Test file nhạc MP3 bị hỏng: xuất thất bại có thông báo, MP4 cũ không bị thay thế, tệp tạm được dọn.
- Unit test Python về track, thời gian ASS, cache; test .NET roundtrip dữ liệu và WinForms STA thumbnail.

## Giới hạn trước khi gọi V2.0 chính thức

- Cần người dùng nghiệm thu thao tác thực tế Play/Pause/Seek MP4, hộp thoại track và Kiểm tra dự án trên giao diện Windows.
- Timeline có lớp ảnh/scene, TTS, SRT và BGM nhưng CHƯA phải một NLE kéo thả nhiều track tùy ý như Premiere.
- Map, Timeline animation và Infographic chưa có engine render riêng, hiện vẫn báo không hỗ trợ.
- 8 phút Image Story là bài stress test, không thể coi là benchmark 40 cảnh Whiteboard nặng.
- Không có AI Director bắt buộc; model Vision chỉ là tùy chọn đã có từ nhánh trước.
- Nội dung, bản quyền nhạc và ảnh cần được người làm video kiểm tra trước khi đăng.
- Chưa merge vào main cho tới khi được nghiệm thu; đây là V2.0 Release Candidate.
