# Kiểm tra TTS bằng SRT có khoảng nghỉ

File mẫu: SRT_tam_nghi_giua_cac_cau.srt

## Sử dụng

1. Mở DvaForDev TTS Studio → Tạo audio → TTS / Hàng loạt.
2. Chọn giọng đọc, ngôn ngữ, engine và thư mục xuất.
3. Bấm Nhập SRT, chọn file mẫu trong thư mục Examples.
4. Kiểm tra bảng 4 câu, đặc biệt cột SRT từ và SRT đến.
5. Chạy RUN BATCH, chờ mọi câu có trạng thái Completed.
6. Bấm Join MP3 (hoặc bật Tự ghép) để ghép audio có khoảng nghỉ.
7. Bấm Create SRT (hoặc bật Auto create SRT) để xuất phụ đề theo thời gian gốc.

## Khoảng nghỉ dự kiến

- Câu 1: 01s đến 06s; có 1 giây im lặng mở đầu.
- Câu 2: 09s đến 14s; nghỉ 3 giây trước câu 2.
- Câu 3: 18s đến 24s; nghỉ 4 giây trước câu 3.
- Câu 4: 28s đến 35s; nghỉ 4 giây trước câu 4.

Audio ghép sẽ có timeline 35 giây nếu các câu đọc đều nằm gọn
trong các khung thời gian.

Lưu ý: Không tự cắt lời TTS và không thay đổi mốc SRT để che lỗi.
Nếu một câu TTS dài hơn khung thời gian, quá trình ghép sẽ báo lỗi
chỉ rõ câu bị vượt. Hãy rút gọn câu hoặc tăng tốc độ đọc và tạo lại
audio câu đó; file xuất hiện có không bị ghi đè.

Dùng file TXT như trước bằng nút Nhập TXT. TTS giọng nói, model
VieNeu/Chatterbox và dữ liệu giọng trong backend/data/voices không thay đổi.

## Tự căn chênh lệch nhỏ giữa TTS và SRT

Khi đoạn audio dài hơn khung SRT không quá **0,15 giây** và **2%**,
bộ ghép sẽ nén thời gian **toàn bộ sóng âm** rất nhẹ (nội suy tuyến tính)
thay vì cắt đuôi. Bản TTS gốc không bị thay đổi; log hiển thị câu
được tự căn. Điều chỉnh nhẹ có thể thay đổi cao độ một chút.

Ví dụ: SRT cho câu 4 là 28,000–35,000 (7 giây) nhưng MP3 được tạo dài
7,020 giây. Bộ ghép tự căn về 7,000 giây và giữ toàn bộ mốc nghỉ.

Nếu lệch quá ngưỡng, chương trình báo rõ số giây vượt và yêu cầu
điều chỉnh lời thoại/tốc độ. Không kéo dài hay ghi đè file SRT gốc.

Trong trường hợp nút Join báo lỗi từ bản cũ, hãy đóng ứng dụng,
chạy lại bản đã cập nhật, nhập lại file SRT và chọn cùng giọng +
thư mục output. Tích **Bỏ qua đã có** trước khi RUN BATCH để dùng lại
các đoạn âm thanh hợp lệ; không cần tạo lại toàn bộ.


## Tên file xuất từ TXT và SRT

File audio đã ghép lấy tên từ file nguồn ban đầu, không dùng tiền tố 000_:

- Video_01.txt -> Video_01.mp3
- Video_01.srt -> Video_01.mp3 (giữ cả khoảng nghỉ SRT)
- Chương 1.txt -> Chương 1.mp3
- Nhập nhiều TXT trong một thư mục -> mỗi file nguồn có một MP3 ghép cùng tên.
- Nếu file đích đã tồn tại, dùng hậu tố _2, _3... để tránh ghi đè.
- Xuất SRT tạo tên Video_01_subtitles.srt.
- Các MP3 của từng câu vẫn giữ dạng 001_TenGiong.mp3 để
  chức năng Bỏ qua đã có tiếp tục hoạt động.

Nếu một Batch cũ được nhập trước bản cập nhật và cột Source File đang
trống, hãy nhập lại TXT/SRT gốc để ứng dụng biết tên nguồn.
Không cần xóa những MP3 đã tạo từng câu.


## Xóa các MP3 từng câu sau khi ghép SRT

- Bật **Xóa đoạn tạm** trong phần TTS nếu muốn dọn các MP3/WAV từng câu.
- Chức năng áp dụng cho cả nhập TXT và SRT.
- Với SRT, việc xóa diễn ra **sau khi tất cả nhóm được ghép thành công**.
- Nếu bất kỳ nhóm nào lỗi, các MP3/WAV từng câu vẫn được giữ để thử lại.
- File MP3/WAV đã ghép cuối và file SRT gốc không bị xóa.
- Bạn vẫn có thể bấm **Create SRT** sau khi xóa các audio từng câu:
  mốc bắt đầu/kết thúc phụ đề được lấy từ dữ liệu đã nhập.
- Đây là thao tác xóa file thật, không chỉ ẩn khỏi danh sách.
  Nếu muốn chỉnh sửa từng câu sau này, hãy để tùy chọn tắt.
