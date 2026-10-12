# Whiteboard AI V0.3 — Hướng dẫn sử dụng

Cập nhật: 12/10/2026. Máy thử nghiệm: Ryzen 5 5600, RAM 16GB, RTX 3050 Laptop GPU 4GB.

## Mở công cụ

Mở PowerShell rồi chạy:

    cd "D:\TestTTS\TTS Create By DvaForDev"
    dotnet run

Ứng dụng có hai tab: Whiteboard AI (một ảnh) và Whiteboard Batch (nhiều ảnh + lời thuyết minh).

## Các bước tạo video YouTube

1. Tạo file TTS tổng và SRT trong tab TTS/Batch như bình thường.
2. Chuẩn bị thư mục ảnh theo số thứ tự: 001.png, 002.png, 003.png...
3. Mở tab Whiteboard Batch. Chọn thư mục ảnh, audio TTS, phụ đề SRT và nơi lưu MP4.
4. Có thể bấm Từ Video Editor để lấy các đường dẫn đã chọn trước đó.
5. Nếu cần chỉnh ảnh tương ứng với các câu TTS, bấm Tạo JSON mẫu rồi mở JSON đó để sửa.
6. Chọn Toàn cảnh cho ảnh màu nhiều chi tiết; dùng Theo nhóm để thử hiệu ứng vẽ từng cụm đối tượng.
7. Khuyến nghị chế độ YouTube nhanh: vẽ 720p rồi xuất 1080p.
8. Bấm Render Batch và theo dõi log. Nút Dừng có thể hủy quá trình đang chạy.

## JSON cho phân cảnh

File JSON có mảng scenes. Mỗi cảnh chứa image, cue_from và cue_to, chẳng hạn
ảnh 001.png ứng với câu 1 đến 3; ảnh 002.png ứng với câu 4 đến 6.
Các câu phải được phủ liên tiếp, không bỏ sót và không trùng lặp. Thời lượng
cảnh được tính theo mốc SRT, không phải bằng cách chia đều tùy ý.

Mỗi cảnh có thể chỉnh:
- preset: classic hoặc block-speedpaint.
- draw_blocks: số nhóm nét gợi ý (1–12).
- block_order: reading hoặc source.
- block_sequence: danh sách số ID các nhóm nét do engine suy ra, không phải tên vật thể.
- lineart: đường dẫn PNG/SVG nét vẽ nằm trong thư mục ảnh, nếu muốn thay nét tự động.

Nếu không chọn JSON, tool tự nhóm các câu SRT theo thời lượng gần đều.

## Chế độ tách nét

- Cân bằng (edges): thích hợp để thử trước.
- Chi tiết (detail): giữ nhiều cạnh, dễ có nhiễu hơn.
- Mực đậm (ink): phù hợp tranh có nền trắng, viền tối.

Đây là cách xử lý ảnh bằng CPU/Pillow, không phải AI nhận diện ngữ nghĩa.
Ảnh minh họa có đường nét rõ hoặc SVG thường cho kết quả tốt hơn ảnh chụp.

## Hiệu năng và xuất video

Preview: 640x360, 20 FPS.
HD: 1280x720, 24 FPS.
YouTube nhanh: dựng 1280x720, 24 FPS rồi upscale 1920x1080 bằng FFmpeg/NVENC.
YouTube gốc: dựng trực tiếp 1920x1080, 30 FPS, chậm hơn đáng kể.

Benchmark Full HD trực tiếp trên máy: clip nét đơn giản 3 giây mất 68,16 giây.
Nâng từ 720p lên 1080p không tạo thêm chi tiết thật, nhưng tiết kiệm công vẽ khung hình.

Thư mục xuất chứa MP4, tệp .whiteboard.json lưu mapping và thư mục
.whiteboard_work giữ cache từng clip để render lại nhanh.

## Đã kiểm thử

- Render 8 giây line-art; vẽ rồi tô màu tranh ở chế độ Classic.
- 3 ảnh + 6 câu SRT + audio thử, ra video 12 giây.
- JSON tùy chỉnh nhóm câu 2s/8s/2s.
- Xuất MP4 H.264/AAC và NVENC upscale.
- 7/7 unit test kế hoạch phân cảnh đạt.
- .NET Debug và Release đều build 0 lỗi, 0 cảnh báo.

Chưa kiểm thử video thực 8–12 phút với TTS thật, chưa kiểm tra thao tác giao diện
bằng chuột và chưa có AI nhận diện/thứ tự vẽ các đối tượng theo ngữ nghĩa.

Môi trường renderer nằm ở D:\TestTTS\_whiteboard_engine_lab, độc lập với
backend TTS. Những thay đổi Git chưa commit của người dùng được giữ nguyên.


## V0.4 — Vẽ lần lượt nhiều layer

Tab Whiteboard Batch có 2 nút mới:

- Đề xuất vùng: chọn ảnh trong thư mục ảnh. Ứng dụng tạo
  images/layers/<tên ảnh>/regions.json và regions_preview.jpg.
  Mở preview để kiểm tra các vùng được đánh số, sửa toạ độ/thứ tự trong
  regions.json nếu cần.
- Tạo PNG layer: đọc regions.json và xuất layer PNG trong suốt ở
  images/layers/<tên ảnh>/01_object_01.png, 02_object_02.png...
  Nếu đã có PNG trùng tên, ứng dụng hỏi trước khi ghi đè.

Sau đó render bình thường. Tool tự tìm các PNG layer trong
images/layers/<tên ảnh>/, vẽ theo thứ tự tên file, giữ lại các đối tượng
đã vẽ khi chuyển sang layer mới.

Ví dụ thư mục:
    images/
      001.png
      002.png
      layers/
        001/
          regions.json
          regions_preview.jpg
          01_character.png
          02_timeline.png
        002/
          01_map.png
          02_event.png

Nếu dùng JSON scene plan thì trường layers có thể chứa:
    "layers": ["layers/001/01_character.png", "layers/001/02_timeline.png"]

Lưu ý: khi xuất layer mới bằng giao diện, scene plan cũ được bỏ chọn
để tool tự nhận diện các thư mục layer. Nếu cần điều chỉnh số câu SRT
thủ công, hãy tạo lại JSON mẫu sau khi xuất layer.

Giới hạn kỹ thuật: Đề xuất vùng là thuật toán phân nhóm điểm ảnh trên
nền trắng, không phải AI hiểu vật thể. Tranh phức tạp, background màu,
đối tượng chồng lấn dễ cho kết quả sai; cần chỉnh tay hoặc dùng PNG layer
đã tách từ công cụ khác. Tất cả PNG layer phải có cùng kích thước canvas
với ảnh nguồn. Tool tự co đều toàn bộ canvas lúc dựng preview.

Kiểm thử:
- 8 giây, 2 layer nhân vật rồi timeline, H.264/AAC: đạt.
- 640x360 nguồn -> vẽ 320x180 -> xuất 640x360 bằng NVENC: đạt.
- Ảnh phẳng nền trắng -> đề xuất 2 vùng -> PNG layer -> video: đạt.
- Đã sửa lỗi tràn số khi tính alpha làm mất chi tiết tối.
- Chưa test giao diện bằng chuột và chưa render video 8-12 phút với TTS thật.

Git: feature/whiteboard-ai-local, main không nhận commit Whiteboard.
Renderer upstream được cài riêng, script dựng lại môi trường nằm tại
whiteboard_tools/setup_renderer.ps1. Yêu cầu Git, Python 3.13, FFmpeg.

