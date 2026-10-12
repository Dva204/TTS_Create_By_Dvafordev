# V2.1-B — Whiteboard giữ màu ảnh gốc bằng mask

Ngày triển khai: 12/10/2026
Nhánh: feature/ai-video-studio
Renderer: ai-video-render-v2.1.0-b1

## Vì sao nâng cấp?

Bản V2.1-A đã tăng chất lượng nét vẽ (render nội bộ 1080p/1440p)
nhưng màu ảnh chỉ hiện lại bằng hiệu ứng fade, có thể trông nhạt trong
phần lớn thời lượng. V2.1-B tận dụng engine Whiteboard local đã có
cơ chế `contour-wipe` để hiện pixel **nguyên bản từ ảnh nguồn** theo
mask di chuyển, bị ảnh hưởng bởi đường viền đã vẽ. Không cần
AI segmentation hay API.

## Cách hoạt động

- **Cổ điển** (mặc định, tương thích project cũ): tay vẽ nét trước,
  màu ảnh gốc xuất hiện dần bằng `--color-fill fade`.
- **Giữ màu ảnh gốc — reveal theo đường viền**:
  tay vẽ nét trước; giai đoạn tô màu dùng `--color-fill contour-wipe`
  để lộ ảnh gốc đã căn lên canvas đúng tỷ lệ qua mặt nạ vùng màu,
  chuyển động từ trên xuống và thay đổi theo contour. Thời gian tô
  màu kéo dài khoảng 42% scene (tối đa 3 giây), giúp nhìn rõ chuyển
  động thay vì hiện toàn bộ ảnh đột ngột.
- Các nét vẫn được giữ nguyên trong giai đoạn tay vẽ. Đến cuối
  scene mask mở toàn phần để hình hoàn thiện lấy màu trực tiếp từ
  ảnh nguồn; không vẽ lại bằng mô hình AI.
- Với xuất Full HD, scene mask + Whiteboard 1.5x được chuẩn hóa ở
  **1920×1080 / H.264 CRF18**, giảm mất chi tiết qua nén video.
  Nếu không chèn phụ đề và clip cuối đã là Full HD, xuất YouTube
  copy video stream thay vì mã hóa lại một lần nữa.
- Nếu bật phụ đề, vẫn cần encode để ghi phụ đề lên khung hình;
  trong trường hợp đó xuất NVENC CQ20 hoặc libx264 CRF20.
- V2.1-B dùng **cơ chế mask của engine hiện có**; không phân đoạn
  từng đối tượng hay tô màu đồng thời với mỗi nét bút. Màu xuất
  hiện trong giai đoạn reveal sau khi vẽ nét, không phải AI phục
  hồi được chi tiết đã mất từ ảnh nguồn.

## Giao diện và JSON

Trong AI Video Studio → Độ nét Whiteboard:
1. Chọn **Nét cao (1.5x)** (khuyên dùng cho RTX 3050 4GB).
2. Mục **Cách hiện màu** → **Giữ màu ảnh gốc — reveal theo đường viền**.
3. Bấm Áp dụng và Lưu project.
4. Bấm Xem cảnh đã chọn để xem nhanh mask ở 640×360,
   sau đó Xuất YouTube để kiểm tra độ nét Full HD.

Project JSON có thêm:
  "whiteboardRevealMode": "source-mask"

`classic` là mặc định nếu file cũ không có key mới. Thông số chỉ
áp dụng cho scene dùng kiểu **Tay vẽ Whiteboard**, không thay đổi
TTS, Voice Library, Video Editor, track nhạc, SRT, các scene khác.
Chọn mode khác sẽ làm mới cache scene tương ứng.

## Kiểm thử thực tế trên ảnh và TTS của người dùng

Nguồn: FolderTestTTS/000_Full_Audio.wav và
ImageTestFolder/001.webp (1920×1280), không copy lên GitHub.

- Cảnh 1, 3,54 giây: V2.1-B xuất 1920×1080/24fps,
  85 frame, audio AAC, H.264; mất khoảng 93 giây để dựng lần đầu.
- Ở 2,7s (một frame thử), tỷ lệ pixel có màu rõ:
  fade classic ~1,9%; source-mask ~31,6%.
- Ở 3,35s, độ lệch tuyệt đối trung bình (RGB 0..255) so với
  ảnh nguồn sau khi căn khung: fade ~13,55; source-mask ~2,47.
  Chỉ số càng nhỏ nghĩa là pixel gần nguồn hơn, **không phải
  đánh giá thẩm mỹ tự động**; phụ thuộc hình đang hiện đến đâu.
- Bản scene mask dùng khoảng 93 giây cho video 3,54 giây nên
  không nên khẳng định tốc độ tương tự cho 40 scene Whiteboard.
- Bài test mixed 6 scene gồm Whiteboard + Zoom/Pan + Image Story:
  Full HD 1920×1080, 462 frame, dài 19,25 giây, dùng chung
  một TTS tổng: thành công.
- Unit test từ chối mode không hợp lệ, kiểm tra cache khi đổi
  mode, kiểm tra đúng tham số `contour-wipe`, `source-image`
  và `source-fit exact`.
- Smoke WinForms có thể chọn mode và áp dụng mà không mất các
  scene; C# roundtrip JSON hỗ trợ `source-mask`.

## Giới hạn và các bước về sau

- Chưa phải AI dựng mask đối tượng tự động theo ngữ nghĩa của ảnh,
  và không bảo đảm mọi frame hoặc ảnh đều đẹp hơn classic.
- Ảnh quá phức tạp, video dài hàng chục cảnh Whiteboard sẽ mất
  nhiều thời gian hơn; cần benchmark trước khi chạy hàng loạt.
- Chưa thử mode 2.0x (1440p) với 40 cảnh.
- Không chuyển đổi tỷ lệ video khi chọn tỷ lệ trong khung xem;
  video xuất hiện vẫn 16:9 như trước.

Kết quả MP4/ảnh test nằm ngoài Git tại:
D:\TestTTS\_whiteboard_engine_lab\out\v2_real_tts.
