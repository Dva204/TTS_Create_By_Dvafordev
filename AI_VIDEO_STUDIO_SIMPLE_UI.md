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
