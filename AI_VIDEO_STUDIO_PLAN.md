# DvaForDev AI Video Studio — Kế hoạch triển khai

**Nhánh phát triển:** feature/ai-video-studio  
**Ngày:** 12/10/2026  
**Nguyên tắc:** THÊM module mới, giữ nguyên TTS / Voice Library / Video Editor / Settings đang hoạt động.

## 0. Bảo vệ mã nguồn

- main là bản TTS gốc, không commit/merge AI Studio vào main khi chưa nghiệm thu.
- backup/tts-before-ai-video-studio đã push và trỏ đúng commit main a852d81.
- feature/whiteboard-ai-local chứa 2 commit Whiteboard đã push.
- feature/ai-video-studio phát triển trên Whiteboard, tất cả thay đổi AI Studio commit độc lập.
- Những thay đổi cũ chưa commit trong Form1.cs, README và file dữ liệu không đưa vào commit mới.
- Engine local: Python riêng, không thay .venv của TTS.

## 1. Kiến trúc mở rộng (một ứng dụng, nhiều module)

Giữ: TTS / Batch, Voice Library, Video Editor cũ và Settings.
Thêm:
1. AI Video Studio: quản lý project, nhập TTS/SRT/ảnh, tạo storyboard và chỉnh loại hiệu ứng mỗi cảnh.
2. Animation Engine: whiteboard, layered, timeline, maps, infographic, typography, image motion (triển khai từng backend).
3. Advanced Composer: render theo project.json, track/preview, SRT, nhạc, xuất MP4; tích hợp FFmpeg/NVENC.
4. Project queue/cache: render scene độc lập, resume, đo tài nguyên.
5. AI-assisted storyboard (optional, không bắt buộc API, luôn cho người dùng xác nhận).

Các module phải có thể bị vô hiệu hóa mà không làm hỏng TTS hiện tại.

## 2. Mốc phát triển

### V1.0 — Nền tảng dự án / storyboard
- [x] Tạo tab AI Video Studio riêng, không thay Video Editor cũ.
- [x] Tạo/mở/lưu file project.ai-video.json (schemaVersion=1).
- [x] Chọn đường dẫn WAV/MP3 và SRT từ TTS hiện có hoặc file bên ngoài (chưa gắn trực tiếp vào hàng đợi TTS).
- [x] Chia scene bằng số cue SRT, cho phép sửa hiệu ứng và hình minh họa.
- [x] Lưu các thiết lập scene và chọn kiểu hiệu ứng mỗi cảnh (template ngách ở giai đoạn sau).
- [x] Test JSON/SRT/WinForms build, không render AI trong giai đoạn này.

### V1.1 — Render pipeline thống nhất
- [x] Adapter Whiteboard/Layer dùng lại engine local, ánh xạ từ project JSON.
- [x] Adapter Image Motion/ZoomPan, Image Story và Text hoạt họa local.
- [x] Giữ timeline tạo từ SRT, thêm audio tổng sau khi nối scene.
- [x] Render preview 360p / Full HD nhanh (720p -> 1080p NVENC).
- [ ] Cache theo scene đã có; kiểm thử thực tế video ≥8 phút vẫn chưa thực hiện.

### V1.2 — Đa định dạng animation
- [ ] Map/Timeline SVG với tọa độ có nguồn xác thực; hỗ trợ từng layer.
- [ ] Infographic, chart, motion graphics và phụ đề nhiều kiểu.
- [ ] Timeline nhiều track với ảnh, audio, title, nhạc.

### V2 — Storyboard AI hỗ trợ
- [ ] Phân tích nội dung, đề xuất cảnh/prompt ảnh/hiệu ứng (model local hoặc API tùy chọn).
- [ ] Người dùng duyệt/chỉnh storyboard trước khi render.
- [ ] Template History, Psychology, Internet Mystery, Geography, Science, Business.
- [ ] Batch tạo nhiều video, xuất 16:9 và 9:16.

## 3. Quy tắc dữ liệu

- JSON là nguồn sự thật duy nhất của project; không chứa audio/video dưới dạng base64.
- Mỗi scene có cueStart/cueEnd, thời điểm bắt đầu/kết thúc, styleId, ảnh và ghi chú.
- Dữ liệu khởi tạo thủ công/từ SRT, không đánh dấu là đã tạo bằng AI.
- Không tự động tải model; không gửi dữ liệu người dùng ra dịch vụ ngoài.
- Không cho phép đường dẫn traversal gây truy cập ngoài thư mục khi sinh đầu ra.
- File project được lưu atomically; lỗi không làm mất file cũ.
- Source paths có thể là file tuyệt đối để MVP dễ dùng, nhưng dự án phải hỗ trợ tương đối khi save.

## 4. Chấp nhận V1.0

- Hai tab gốc TTS và Video Editor không bị chỉnh sửa logic.
- WinForms .NET 8 build Release trên máy hiện tại.
- SRT hợp lệ tạo đúng số scene và timestamp, SRT lỗi báo rõ.
- Lưu rồi mở lại project không mất thông tin.
- Không commit dữ liệu TTS riêng tư, file voice, ảnh, MP4.
- Luôn kiểm thử từ clean checkout trước khi push.


## 5. Kết quả V1.0 (12/10/2026)

- AiVideoStudio/AiVideoProject.cs: JSON schema v1, đọc/kiểm tra SRT, sinh storyboard, lưu file atomically, đường dẫn tương đối.
- AiVideoStudio/AiVideoStudioControl.cs: giao diện nhập audio/SRT/ảnh, tạo scene, sửa style/ảnh/ghi chú, mở/lưu dự án.
- AiVideoStudio/AiVideoStudioIntegration.cs: gắn tab mới, không thay tab TTS/Voice/Video Editor cũ.
- Test headless: 4 cues -> 2 scene, JSON roundtrip, từ chối SRT và scene lỗi.
- Chưa có AI tự chọn style và chưa có pipeline render đa engine từ tab này.
- Lưu ý: đường dẫn tuyệt đối của file media chỉ dùng cục bộ; file JSON ưu tiên lưu đường dẫn tương đối đến vị trí project.


## 6. Bản V1.1 (12/10/2026)

- whiteboard_tools/ai_video_pipeline.py: render thống nhất qua project JSON.
- Hiệu ứng đã chạy thử: whiteboard, layer, image-motion, image-story, text.
- UI có nút Xem trước MP4, Xuất YouTube, Dừng render; hiển thị lỗi render.
- Map, Timeline, Infographic chưa có renderer: hiện được đánh dấu sắp có và từ chối render.
- Test nội bộ: 5 cảnh, tổng 10 giây, xuất preview H.264/AAC 640x360 20 FPS,
  Full HD NVENC 1920x1080 24 FPS, cả hai đều 10.000 giây.
- 8 bài test pipeline đạt; chưa test thực tế UI bằng chuột, chưa thử file TTS thật dài.
- Hướng dẫn: AI_VIDEO_RENDER_GUIDE.md.


## 7. V1.2 — AI Video Director (Smart Rules local)

- [x] Đọc nội dung SRT và đề xuất thời điểm chèn chữ cho năm / phần trăm / số liệu.
- [x] Đề xuất zoom nhẹ cho các câu nhấn mạnh, ưu tiên không trùng thời điểm caption.
- [x] Lưu directionEvents trong AiVideoScene của schemaVersion=1; tương thích project cũ.
- [x] Giao diện đề xuất hiệu ứng + chỉnh chi tiết từng cảnh (thời gian, chữ, zoom và tiêu điểm).
- [x] Render text overlay có thời gian bằng Pillow/FFmpeg và camera zoom bằng zoompan.
- [x] Cache nhận diện khi directionEvents thay đổi; validation C# và Python.
- [x] Smoke test 6 giây preview/1080p NVENC có chữ và zoom.
- [ ] Model ngôn ngữ hiểu ngữ nghĩa toàn bộ script + AI Vision xác định vị trí đối tượng trong ảnh.
- [ ] Thử nghiệp thu UI trực tiếp bằng chuột và video dài >=8 phút với giọng TTS thật.

### Phạm vi V1.2
Đây là bộ gợi ý quy tắc chạy local, không phải AI VLM. Không khẳng định nó hiểu
vị trí nhân vật hay quyết định chính xác mọi thời điểm zoom/chèn chữ.
Tài liệu hướng dẫn: AI_VIDEO_DIRECTOR_GUIDE.md.


## 8. Director Mức 2 — AI text + vision (tùy chọn)

- [x] Cho người dùng chọn Ollama local hoặc API chat completions HTTPS.
- [x] Chỉ gửi dữ liệu khi người dùng chủ động yêu cầu; xác nhận rõ khi dùng
      API từ xa, không lưu API key vào file project.
- [x] Gửi SRT cảnh hiện tại, ngữ cảnh cảnh trước/sau và tóm lược đầu/cuối câu
      chuyện. Có tùy chọn gửi ảnh JPEG thu nhỏ <=768 px cho Vision.
- [x] Nhận JSON gợi ý text và zoom cùng thời điểm cue, target và lý do.
- [x] Từ chối caption bịa không có trong SRT, toạ độ/zoom ngoài giới hạn.
- [x] Quy đổi tâm zoom từ ảnh dọc/ngang sang canvas 16:9; giữ gợi ý sửa tay.
- [x] Giao diện có nút AI Director Mức 2, thiết lập model, quyền gửi ảnh,
      nút dừng và chế độ xem/chỉnh từng hiệu ứng trước khi render.
- [x] Bài kiểm thử giả lập server Ollama (HTTP) -> JSON -> video preview MP4.
- [ ] Thử thực tế với một mô hình Vision đã cài / tài khoản API đã được kết nối.
- [ ] Đánh giá sai lệch vùng focus/độ tự nhiên trên 20-40 cảnh lịch sử có thật.
- [ ] Preview kéo tâm zoom trực tiếp và detector định vị bounding-box.
- [ ] Kiểm thử Video 8-12 phút và giao diện WinForms bằng chuột.

Lưu ý: Mức 2 đã có luồng tích hợp với model nhưng chỉ mới thử bằng mô hình
giả lập. Chưa được khẳng định model thật chọn hiệu ứng chính xác, không có
trợ lý AI nào tự ý tải mô hình lớn hoặc dùng API trả phí trên laptop.
Hướng dẫn: AI_VIDEO_DIRECTOR_LEVEL2_GUIDE.md.
