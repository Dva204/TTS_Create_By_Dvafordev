# AI Video Studio V1.3 — Scene + TTS tổng (2026-10-12)

Mục tiêu: tạo video dài từ **một file MP3/WAV/M4A TTS tổng**, file SRT của audio và một thư mục ảnh đặt tên theo thứ tự, không phải cắt TTS thủ công.

## Sử dụng

1. Mở ứng dụng TTS Create By DvaForDev như cũ. Ba phần TTS, Voice Library, Video Editor cũ được giữ nguyên.
2. Vào **AI Video Studio**, chọn audio TTS tổng, SRT và thư mục ảnh. Đặt tên ảnh 001.png, 002.png,...
3. Mặc định chọn chế độ **Theo số ảnh (khuyên dùng)**. Tool tạo đúng một scene ứng với mỗi ảnh (yêu cầu số câu SRT ít nhất bằng số ảnh). Mốc cắt là ranh giới câu SRT, được phân bố gần đều theo thời gian, không cắt vào giữa câu.
4. Hoặc chọn **Theo số câu SRT**, nhập số câu/cảnh để tạo scene khi số ảnh khác số cảnh.
5. Chọn loại hoạt họa từng scene (gợi ý cho ảnh AI bình thường: Image Motion hoặc Image Story; Whiteboard cần ảnh nét rõ; Layer cần PNG layer có alpha).
6. Xem **thanh timeline** và nhấp vào ô scene để chọn. Nhấn **Cảnh +1 câu** hoặc **Cảnh -1 câu** để chuyển một câu qua lại giữa scene đang chọn và cảnh tiếp theo. Chức năng này tự cập nhật lời thoại/thời gian, đồng thời xóa các hiệu ứng Director cũ của hai cảnh nếu có để không bị lệch.
7. Trong cột **Chuyển cảnh**, chọn **fade** (hiện dần từ nền trắng khoảng 0,28s) hoặc **cut**. Fade không cắt audio, không chồng lấn thời lượng scene.
8. Bấm **Xem cảnh đã chọn** để render riêng scene với đúng đoạn TTS tương ứng từ audio tổng, rồi mở MP4 mặc định của Windows.
9. Bấm **Xem trước MP4** để xuất toàn bộ preview 640×360 / 20 FPS, hoặc **Xuất YouTube** để dựng 720p, upscale 1080p bằng NVENC.
10. Lưu project JSON. Khi render lại một cảnh bị chỉnh, cảnh khác có thể dùng cache. Cùng một video không thay đổi được bỏ qua cả bước mux cuối.

## Phạm vi V1.3

- AI Director Mức 1/2 đã được phát triển nhưng tạm **ẩn khỏi giao diện chính**, không bắt buộc phân tích nội dung và không tự gọi AI/API.
- Có năm kiểu animation đã thử: whiteboard, layer, image-motion, image-story, text.
- Map/timeline/infographic mới hiện tên trong danh sách và chưa render được; cần phát triển engine riêng.
- Scene Manager hiện có timeline dạng thanh lựa chọn, chưa phải trình biên tập kéo thả nhiều track.
- SRT và audio tổng là nguồn thời gian, không tự nhận dạng câu nói từ audio và không kiểm chứng nội dung thực của TTS với SRT.
- File ảnh có thể thay bằng cách nhấp đúp ô ảnh minh họa, không cần đổi SRT.
- Khi **số ảnh lớn hơn số cue SRT**, mode Theo số ảnh báo lỗi rõ ràng; chuyển sang mode theo số câu hoặc tạo thêm cue.
- Với các scene rất ngắn, whiteboard/Layer có thể cần thời lượng tối thiểu. Luôn preview.

## Hiệu năng đã kiểm thử

- Unit test dịch vụ .NET: 80 cue chia thành 40 scene cho 10 phút, ranh giới liên tục và dịch chuyển một câu qua hai cảnh: đạt.
- Preview một cảnh trong file TTS tổng: 2 giây, video H.264/AAC: đạt.
- Render toàn bộ 5 cảnh với 5 hiệu ứng: 10 giây, H.264/AAC: đạt.
- Render **40 cảnh ảnh tĩnh với audio tổng 480 giây (8 phút)**: H.264/AAC, 640×360, 20 FPS, 9600 frame, 480.000 giây: đạt.
- Render riêng scene số 23 từ dự án 8 phút: clip 12 giây cùng phần audio tổng: đạt.
- Bài test audio 8 phút sử dụng âm thanh sine giả lập, chưa phải giọng TTS người đọc thực.
- Thử nghiệm với 8 phút toàn Whiteboard, tương tác UI bằng chuột và timeline editor kéo thả chưa thực hiện.
- Có cache cảnh và cache đầu ra hoàn chỉnh khi nội dung không thay đổi.
- Máy thử: Windows 16GB RAM, RTX 3050 Laptop 4GB; renderer Python riêng.
- Dữ liệu test và video được lưu ở D:\TestTTS\_whiteboard_engine_lab\out\studio_8min_40scenes

## Git

Phát triển trên feature/ai-video-studio. Giữ nguyên main và các file TTS cũ chưa commit.
Không đưa file MP4, ảnh người dùng, audio hoặc dữ liệu riêng vào GitHub.
