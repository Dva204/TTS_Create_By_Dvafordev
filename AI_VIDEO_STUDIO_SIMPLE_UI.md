# AI Video Studio — Giao diện tối giản

Mặc định mở vào giao diện phục vụ công việc chính: chọn audio TTS / SRT / ảnh, import Scene Plan Claude, chọn thư mục output và xuất video YouTube.

## Màn hình chính (10 nút)

- Ba dòng nguồn: **Âm thanh TTS**, **Phụ đề SRT**, **Thư mục ảnh**, mỗi dòng có nút **Chọn...**.
- Dòng **Thư mục output**: **Chọn output** để chọn nơi lưu MP4 và **Mở output** để xem file trong Explorer.
- Dòng **Thao tác**: **Import Scene Plan**, **Mở dự án**, **Lưu dự án**, **Tùy chọn**.
- Góc phải phía trên: nút lớn **XUẤT VIDEO**.
- Bảng bên dưới: **#**, **SRT từ**, **SRT đến**, **Hiệu ứng**, **Ảnh (nháy đúp để thay)** và **Lời đọc**. Đây là toàn bộ bảng cần dùng để kiểm tra video theo Scene Plan.

Các nút xem thử, chỉnh ranh giới và cấu hình không còn làm rối màn hình chính. Không có video player nhúng, không có kiểu giao diện CapCut hoặc dải thumbnail.

## Tùy chọn nâng cao (đóng mặc định)

Bấm **Tùy chọn ▾** khi cần dùng:
- Tên dự án, **Dự án mới**, **Lưu thành...**, **Xem ảnh đã chọn**.
- Chế độ nhóm SRT, số câu/cảnh, hiệu ứng mặc định, **Tạo storyboard tự động** khi **không có** Scene Plan.
- **Xem thử MP4**, **Xem cảnh**, **Cảnh +1/-1 câu**, **Nhạc nền/Phụ đề**, **Kiểm tra dự án**, **Whiteboard HD**.
- Bảng cảnh bổ sung các cột thời gian, chuyển cảnh và ghi chú để kiểm tra chi tiết.

Chức năng nâng cao không bị xóa, chỉ ẩn mặc định. **Dừng render** tự xuất hiện trên thanh trên cùng khi đang render và biến mất khi render kết thúc.

## Quy trình khuyến nghị

1. Chọn Audio tổng, SRT và thư mục ảnh (có thể chọn SRT và ảnh trước nếu chưa có audio).
2. Chọn thư mục output; ứng dụng ghi nhớ vị trí cho các lần xuất sau.
3. Bấm **Import Scene Plan**, chọn file JSON Claude; nếu lệch timestamp nhưng văn bản khớp SRT thì ứng dụng hỏi có đồng bộ không.
4. Kiểm tra các cảnh trong bảng, nháy đúp vào đường dẫn ảnh nếu muốn đổi ảnh.
5. Bấm **XUẤT VIDEO**; trình Save As mở tại thư mục output đã chọn. Sau khi xong, bấm **Mở output** để tìm MP4.

## Bảo toàn dữ liệu

Giao diện chỉ sắp xếp/ẩn các nút đã có, không xóa chức năng, không tự ghi đè ảnh/audio/SRT hoặc scene JSON. Nút mở dự án được giữ trên màn hình chính. Việc import thay storyboard vẫn hỏi trước khi ghi đè cảnh đang mở. Các project JSON cũ được hỗ trợ.

## Kiểm thử

- Smoke WinForms: 10 nút trên giao diện chính, panel nâng cao đóng/mở, các cột được hiển thị đúng, output picker và export hiện rõ ở 980x710.
- Import Scene Plan/SRT và 40 cảnh hoạt động bình thường.
- Build .NET Release, bộ kiểm thử importer C#, project JSON và Python renderer.

## Mặc định số câu SRT và tiến trình từng scene (2026-10-14)

- Khi mở Tùy chọn, kiểu nhóm khi tự tạo storyboard mặc định là Theo số câu SRT; ô Câu/cảnh bật sẵn ở giá trị 3 (có thể tự đổi). Chọn Theo số ảnh nếu cần phân đều ảnh. Riêng Import Scene Plan Claude sử dụng đúng cue_from/cue_to của JSON, không thay đổi theo kiểu nhóm thủ công.
- Bảng phân cảnh có thêm cột Tiến trình. Khi xuất video, mỗi cảnh đi qua: Chưa dựng → Đang dựng... → Đã dựng; video clip cache hiển thị Có sẵn (cache); cảnh thất bại ghi Dừng / lỗi.
- Thanh tiến trình bên dưới bảng hiển thị số cảnh hoàn tất, ví dụ 12/38 cảnh xong. Sau tất cả clip là bước riêng Đủ 38 cảnh · đang ghép MP4; chỉ khi MP4 có thật mới hiển thị Hoàn tất 38 cảnh · MP4.
- Tiến trình phản ánh các sự kiện thực của Python renderer, không dùng timer giả hoặc ước lượng thời gian còn lại. Một cảnh Whiteboard có thể mất lâu dù số cảnh xong chưa tăng; dòng scene vẫn thể hiện Đang dựng...
- Xem thử một scene chỉ cập nhật đúng scene đó; các scene chưa dựng khác không bị đánh dấu xong.
- Nếu hủy/lỗi, không đánh dấu cảnh đang chờ là đã hoàn thành. Chạy lại sẽ tận dụng được clip cache.
- Protocol stdout: [SCENE_PROGRESS] start|done|cached n/N scene=ID, [SCENE_PROGRESS] mux N/N, [SCENE_PROGRESS] complete N/N.
- Smoke test FFmpeg thật với 2 cảnh, render mới và render lại từ cache, cùng bài kiểm thử UI và import 38 cảnh/140 cue.
