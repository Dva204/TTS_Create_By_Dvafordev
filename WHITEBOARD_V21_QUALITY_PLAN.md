# V2.1-A — Whiteboard nét cao (sprint đầu)

Cập nhật: 12/10/2026
Nhánh phát triển: feature/ai-video-studio
Mục tiêu: tăng độ chi tiết nét vẽ và hình hoàn thiện so với dựng 720p rồi upscale.

## Nguyên nhân tìm thấy trong code

Bản V2.0 RC1 dựng toàn bộ scene YouTube ở 1280x720, chuẩn hóa
clip ở 720p và sau đó FFmpeg upscale lên 1920x1080 bằng Lanczos.
Ảnh gốc WebP 1920x1280 bị giảm chi tiết trước khi bàn tay vẽ xuất hiện;
upscale không thể phục hồi đường nét mất trong khâu render 720p.

## Thiết kế V2.1-A

Trong AI Video Studio > Quản lý phân cảnh có nút Độ nét Whiteboard.
Có ba chế độ (chỉ áp dụng với Xuất YouTube; Preview tiếp tục 640x360):

- Nhanh 1.0x: Whiteboard gốc 1280x720, clip 720p, xuất upscale 1080p.
  Giữ đường render cũ, tốc độ ưu tiên.
- Nét cao 1.5x (khuyên dùng): Whiteboard render trực tiếp 1920x1080,
  trích nét ở độ phân giải cao từ ảnh gốc, stroke-detail=rich,
  clip chuẩn hóa trực tiếp 1920x1080, xuất MP4 1920x1080.
  Các scene không phải Whiteboard chỉ được scale trong bước normalize
  để toàn bộ clip tương thích khi concat.
- Siêu nét 2.0x: Whiteboard nội bộ 2560x1440,
  cuối cùng downscale Lanczos về 1920x1080. Chi phí tài nguyên cao,
  cần cân nhắc RAM 16GB và GPU RTX 3050 4GB.

Mỗi mode có unsharp rất nhẹ 0..0.5, người dùng điều chỉnh 0..50%.
Default scale=1.0, sharpen=0, nên project cũ không bị thay đổi chất lượng
hay thời gian render nếu không bật.

Các field mới project schemaVersion=1:
  "whiteboardRenderScale": 1.5,
  "whiteboardSharpenAmount": 0.15

- Python và C# đều validate khoảng dữ liệu, từ chối NaN/giá trị lạ.
- Digest cache scene ghi nhận scale, độ nét và resolution normalized,
  tự render lại khi người dùng đổi quality.
- Audio TTS/SRT được giữ nguyên, không thay đổi hiệu ứng tay vẽ
  mặc định; không can thiệp ảnh nguồn.
- Dữ liệu bổ sung vẫn sống trong project JSON; project cũ được
  gán scale=1.0 và sharpen=0 khi tải.
- Không tự động cài model AI, không phát sinh API hay chi phí.

## Cách dùng

1. Đóng phần mềm đang chạy, vào project:
       cd "D:\TestTTS\TTS Create By DvaForDev"
       dotnet run
2. Trong AI Video Studio, chọn TTS tổng, SRT, bộ ảnh AI và tạo storyboard.
3. Chọn Tay vẽ Whiteboard cho những scene cần vẽ tay.
4. Bấm Độ nét Whiteboard > Nét cao (1.5x), tăng nét nhẹ khoảng 15%.
5. Bấm Lưu project, Xem cảnh đã chọn để kiểm tra nhịp vẽ;
   preview vẫn nhanh (640x360).
6. Bấm Xuất YouTube. So sánh MP4 Full HD với bản cũ trên cùng
   một frame. Không cần tạo lại ảnh AI.

## Giới hạn chưa thuộc sprint này

V2.1-A KHÔNG có image reveal bằng mask để hiển thị nguyên ảnh gốc sau
khi vẽ. Điều này là V2.1-B, cần nghiên cứu loại mask và cách giữ hoạt
họa đồng bộ với cử động bàn tay. Whiteboard rich có thể vẽ nhiều nét hơn
nên cần kiểm thử từng loại hình, đặc biệt ảnh phức tạp. 1.5x không bảo
đảm độ nét tăng với mọi ảnh, bởi phụ thuộc chất lượng nguồn và encoder.

Nếu model Whiteboard không có nguồn SVG/vector thì không thể đảm bảo
đường nét sắc không giới hạn như vector.

## Kiểm thử

- Kiểm thử Python: scale, sharpen hợp lệ/từ chối invalid, cache thay
  theo kích thước nội bộ và lượng sharpen.
- C# round-trip: JSON lưu/mở được quality mới, giá trị sai bị từ chối.
- Render thật: scene Whiteboard với một ảnh WebP 1920x1280 và
  một audio TTS tổng từ FolderTestTTS ở 1080p internal.
- Không dùng dữ liệu TTS/ảnh của người dùng trong commit; video và
  kết quả benchmark lưu ngoài Git ở _whiteboard_engine_lab.


## Kết quả thử V2.1-A với ảnh thực tế

Từ bộ FolderTestTTS của người dùng: ảnh WebP 001.webp 1920x1280,
cảnh dài 3,54 giây có tiếng TTS tổng. So sánh ở khoảng 3,45 giây:

- Chế độ gốc 1.0x: trích nét ở 1280x720 rồi upscale 1920x1080.
- Nét cao 1.5x: trích nét 1920x1080, thêm rich stroke detail,
  chuẩn hóa ở 1920x1080, unsharp nhẹ 0.15.
- Cả hai: 1920x1080, H.264/AAC, 85 frame, 3,541 giây.
- Thử nghiệm chế độ Nét cao render riêng mất khoảng 59 giây.
- Sai lệch tuyệt đối trung bình với ảnh nguồn (thang RGB 0..255):
  gốc 6,775; nét cao 5,300. RMSE: 10,357 xuống 8,316.
  Độ biến thiên biên luminance: 2,734 lên 2,962.
- Đây chỉ là một ảnh và một frame; không đảm bảo cải thiện tương tự
  cho mọi ảnh hoặc mọi giai đoạn vẽ. Xem ảnh trực tiếp trước khi dùng.

Bộ ảnh thử, MP4 và báo cáo chỉ được lưu tại
D:\TestTTS\_whiteboard_engine_lab\out\v2_real_tts,
không đưa dữ liệu người dùng lên repository.
