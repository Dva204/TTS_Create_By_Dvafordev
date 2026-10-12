# DvaForDev AI Video Studio — Hướng dẫn render V1.1

Ngày: 12/10/2026
Nhánh: feature/ai-video-studio
Đây là module BỔ SUNG. TTS, Voice Library, Video Editor cũ vẫn hoạt động độc lập.

## Cách sử dụng

1. Mở ứng dụng bằng PowerShell: dotnet run
2. Vào AI Video Studio, chọn audio TTS tổng, SRT và thư mục ảnh.
3. Chọn số câu mỗi cảnh, chọn kiểu mặc định, bấm Tạo storyboard.
4. Chỉnh ảnh và hiệu ứng từng scene; nhấp đúp ô ảnh để chọn file khác.
5. Bấm Lưu project để lưu file JSON; hoặc xuất trực tiếp qua nút mới.
6. Xem trước MP4 xuất 640x360 (20 FPS). Xuất YouTube tạo 1920x1080 (24 FPS).
7. Chọn đường dẫn xuất MP4; chương trình lưu snapshot JSON cạnh video.
8. Có nút Dừng render. Khi chạy lại cùng tên đầu ra, các clip đã render sẽ được cache.

## Hiệu ứng có thể render hiện tại

- whiteboard: bàn tay vẽ nét rồi tô màu từ PNG/JPG/SVG.
- layer: vẽ theo thứ tự các PNG trong suốt ở thư mục images/layers/tên-ảnh/*.png.
- image-motion: camera phóng to ảnh dần.
- image-story: trình chiếu ảnh trong suốt phân cảnh.
- text: chữ xuất hiện theo ký tự trên card nền sáng.

Map, Timeline, Infographic mới có trong danh mục UI, CHƯA có renderer.
Nếu chọn chúng, chương trình báo lỗi rõ ràng, không giả vờ đã dựng thành công.

## Quy trình dựng video

Python script: whiteboard_tools/ai_video_pipeline.py
Đọc schemaVersion=1 với các trường startMs, endMs, styleId và imagePath.
Thời lượng phân cảnh lấy từ storyboard theo câu SRT.
Render từng cảnh thành H.264 CFR, nối FFmpeg rồi gắn file TTS AAC.
Có thể kéo dài cảnh cuối theo khoảng lặng cuối audio (không quá 10 giây).
Preview 640x360 / 20FPS; Full HD dựng 1280x720 / 24FPS rồi upscale Lanczos
lên 1920x1080 bằng NVENC, fallback libx264 khi mã hóa GPU không hoạt động.

Engine Whiteboard local độc lập với TTS backend (Python venv riêng), không gọi API AI video.
Đường dẫn renderer mặc định:
D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe

## Kiểm thử đã thực hiện

- Video 5 scene, gồm whiteboard/layer/image-motion/text/image-story: thành công.
- Preview H.264 + AAC, 640x360, 20FPS, dài 10.000 giây: đạt.
- YouTube H.264 NVENC + AAC, 1920x1080, 24FPS, dài 10.000 giây: đạt.
- Render lặp lại: dùng cache 5/5 cảnh không thay đổi.
- 8/8 unit test pipeline: đạt.
- Chưa kiểm thử UI bằng chuột hoặc video dài 8–12 phút với TTS thật.

## Gỡ lỗi

Mở PowerShell tại thư mục project, chạy script Python bằng venv của whiteboard:

python whiteboard_tools/ai_video_pipeline.py --project "D:\Video\project.ai-video.json" --output "D:\Video\preview.mp4" --quality preview --engine "D:\TestTTS\_whiteboard_engine_lab\.venv\Scripts\whiteboard.exe"

Tùy chọn quality=youtube sẽ xuất Full HD.
Cache nằm trong thư mục đuôi .ai_video_work ở cạnh MP4; metadata đuôi
.ai-video-manifest.json.

Không thay đổi các thư viện trong backend/.venv của phần TTS.

