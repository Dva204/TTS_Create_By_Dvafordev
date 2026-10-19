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
