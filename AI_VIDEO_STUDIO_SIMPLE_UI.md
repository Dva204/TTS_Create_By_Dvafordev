# AI Video Studio — Giao diện cơ bản (không dùng bố cục CapCut)

Nhánh: `feature/ai-video-studio`.

## Thiết kế

Mục tiêu: dễ nhìn trên laptop và màn hình desktop, đặc biệt với workflow TTS tổng + SRT + ảnh Whiteboard. Không thay đổi scene, project JSON, AI Director, Whiteboard HD hoặc Python render engine.

### Bố cục hiện tại

1. Trên cùng: tên **AI VIDEO STUDIO**, nút xem thử MP4, xem một cảnh, dừng render; nút **XUẤT VIDEO 1080p** riêng ở góc phải.
2. Phần nguồn: tên project, audio TTS, SRT, thư mục ảnh, tùy chọn chia storyboard, nút Tạo storyboard.
3. Phần dự án: **Mở project**, **Lưu project**, **Lưu thành...**, **Import Scene Plan**.
4. Phần phân cảnh: nút điều chỉnh SRT +/- 1 câu, Nhạc nền / Phụ đề, Whiteboard HD.
5. Phần thư mục output: đường dẫn đã chọn và nút **Chọn output** và **Mở output** (File Explorer).
6. Phần dưới: **bảng storyboard có thể chỉnh trực tiếp**, hiển thị SRT từ/đến, thời điểm, hiệu ứng, chuyển cảnh, đường dẫn ảnh, lời thuyết minh, ghi chú.

Đã bỏ khu vực video player tích hợp và dải thumbnail khỏi màn hình chính, giúp bảng chiếm không gian. Video xem thử mở bằng trình phát mặc định của Windows sau khi render xong; bản YouTube hoàn tất không tự bật player để tránh gián đoạn. Bạn có thể dùng nút Mở output để truy cập MP4.

### Sử dụng

1. Chọn audio TTS tổng, SRT và thư mục ảnh.
2. Nếu đã có `scene_plan.json` chuẩn từ Claude → bấm **Import Scene Plan**; nếu không có → **Tạo storyboard**.
3. Kiểm tra bảng phân cảnh, chỉnh từng scene khi cần.
4. Chọn Whiteboard HD, nhạc nền/phụ đề nếu cần.
5. Bấm **Chọn output** nếu muốn đổi thư mục lưu mặc định, sau đó **XUẤT VIDEO 1080p**. Chọn tên MP4; vị trí sẽ được hiển thị trong ô Thư mục output và ghi nhớ cho lần mở phần mềm sau.
6. Bấm **Mở output** để vào thư mục lưu bằng Windows Explorer.

Lưu ý: nút Import đọc **SRT thật** và JSON; nếu thiếu ảnh, báo danh sách thiếu nhưng vẫn có thể nạp timeline; muốn render phải có đủ ảnh và audio.

### Quy tắc output

- Nút **Chọn output** mở hộp chọn thư mục và cập nhật vị trí lưu mặc định ngay lập tức.
- Save dialog của Windows mở tại thư mục output được chọn, vẫn cho phép đổi thư mục và tên MP4 mỗi lần xuất để tránh ghi đè nhầm.
- Ứng dụng ghi nhớ thư mục đã chọn vào cấu hình riêng trong LocalAppData của Windows: `DvaForDev/TTSStudio/last_video_output_dir.txt`. File này nằm ngoài thư mục code và ngoài repo Git.
- Nếu chưa từng xuất video, dùng mặc định `Videos/DvaForDev AI Videos`; nhấn Mở output sẽ tạo thư mục đó nếu chưa tồn tại.
- Không di chuyển, sửa, xóa ảnh, audio, SRT hoặc output cũ.

### Kiểm thử

- Release build và Python regressions.
- Smoke WinForms: trường dữ liệu, nút Import/Export/Mở output duy nhất, một bảng scene rộng, preview panel bị ẩn, 40 cảnh và không tạo thumbnail strip.
- Persist đường dẫn output và khôi phục cấu hình ban đầu sau smoke.
- Import Scene Plan giữ cue/time/style chính xác.
- 980×710 vẫn hiển thị bảng phân cảnh và các nút chính.
