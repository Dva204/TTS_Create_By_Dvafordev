# AI Video Director Mức 2 — Hướng dẫn sử dụng

Ngày cập nhật: 12/10/2026. Nhánh: feature/ai-video-studio.

## Phạm vi

Director Mức 2 cho phép một mô hình AI suy luận về SRT và ảnh minh họa,
từ đó đề xuất chữ, thời điểm và điểm zoom trong mỗi scene. Đây là chức
năng TÙY CHỌN, không tự chạy và không bắt buộc có API.

Có hai nguồn mô hình:
- Ollama local trên laptop, endpoint mặc định http://127.0.0.1:11434
- API chat completions tương thích OpenAI qua HTTPS, mặc định
  https://api.openai.com/v1

AI được gửi lời thoại của scene, một ít ngữ cảnh trước/sau và ảnh JPEG
thu nhỏ tối đa 768x768 nếu người dùng bật tùy chọn gửi ảnh.
Không gửi file audio TTS, toàn bộ video gốc hay API key lên project.
Nếu dùng API ngoài, người dùng phải xác nhận việc gửi dữ liệu và có thể
phát sinh chi phí.

## Thao tác trong ứng dụng

1. Chạy ứng dụng bằng dotnet run tại thư mục TTS Create By DvaForDev.
2. Vào AI Video Studio, chọn TTS, SRT, ảnh và tạo Storyboard.
3. Bấm AI Director Mức 2 để mở cửa sổ thiết lập.
4. Chọn Ollama hoặc API, tên model và endpoint.
5. Chọn có gửi hình ảnh hay không. Với API ngoài, đọc và xác nhận
   thông báo gửi dữ liệu.
6. Chờ nhận kế hoạch; có thể bấm Dừng AI để hủy.
7. Xem cột Director và Chỉnh Director của cảnh để sửa chữ/thời gian,
   zoom, FocusX/FocusY, đọc lý do model đã đề xuất.
8. Lưu project JSON và xem trước MP4 trước khi xuất YouTube.

Theo mặc định, AI không ghi đè những hiệu ứng đã chỉnh tay. Có ô lựa
chọn ghi đè nếu cần thay đổi toàn bộ kế hoạch đã có.

## Dùng Ollama

Cài Ollama từ https://ollama.com/download, kiểm tra lệnh ollama --version.
Bạn có thể thử model Vision, ví dụ qua lệnh:

    ollama pull gemma3:4b

Sau khi model đã tải xong, chọn Ollama và tên model tương ứng trên UI.
Đây chỉ là ví dụ model; chưa benchmark Gemma3:4b trên RTX 3050 4GB.
Có thể thiếu VRAM hoặc dùng RAM/CPU nên chậm. Không tự động tải model
và không yêu cầu người dùng cấp API key cho Ollama.

## Dùng API tương thích OpenAI

Cài biến môi trường OPENAI_API_KEY cho tiến trình ứng dụng. Không lưu
key vào JSON, file .env commit Git, URL hoặc ảnh. Chọn nhà cung cấp API,
endpoint HTTPS và model được tài khoản của bạn hỗ trợ.
Đây là thao tác gửi dữ liệu ra máy chủ khác, có thể tính phí.

## Kiểm tra an toàn và giới hạn

- Chỉ giữ caption là chuỗi được trích nguyên văn từ SRT. AI không được
  tự tạo mốc năm hoặc sự kiện không được lời thuyết minh đề cập.
- Giới hạn zoom từ 1.05 đến 1.18, tâm zoom trong khoảng 0..1.
- Chỉ đề xuất zoom khi có ảnh thực sự được đưa vào request Vision.
- Mỗi scene tối đa một caption và một zoom. Không làm trùng hiệu ứng
  với Text hoặc Image Motion.
- Thời gian hiệu ứng lấy theo mốc cue trong SRT, tính tương đối theo scene.
- Khi model/API lỗi, project gốc không bị ghi đè.
- Gợi ý có lý do và nguồn ai-level2 trong project JSON; bạn có thể sửa.
- AI Vision vẫn có thể nhận diện sai vị trí vật thể dù đã nhận ảnh.
- Chưa có bounding-box detector chính xác, không bảo đảm camera trúng mặt.
- Chưa có UI kéo tâm zoom bằng chuột.

## Kết quả thử nghiệm

Đã chạy bài tích hợp HTTP với máy chủ Ollama GIẢ LẬP chạy local. Phản hồi
giả lập đề xuất chữ 1903 và zoom X=0.72/Y=0.43. Tool xác thực phản hồi,
lưu JSON và render MP4 6 giây H.264/AAC thành công.
Các unit test còn kiểm tra ảnh base64, JSON không hợp lệ, thiếu API key,
URL HTTP từ xa, câu chữ bịa và giữ hiệu ứng chỉnh thủ công.

CHƯA kiểm thử với mô hình Vision thật, chưa kiểm thử toàn bộ giao diện bằng
chuột, chưa thử video dài 8-12 phút với giọng TTS thật. Kết quả mô phỏng
chỉ chứng minh đường tích hợp, không chứng minh chất lượng quyết định AI.

TTS, Voice Library, Video Editor cũ và Smart Rules Mức 1 được giữ nguyên.
