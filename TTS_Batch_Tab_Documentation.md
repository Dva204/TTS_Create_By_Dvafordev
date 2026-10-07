# HƯỚNG DẪN SỬ DỤNG TAB "TTS / BATCH" (AI TTS STUDIO)

Tab **TTS / Batch** là trái tim của phần mềm, nơi bạn có thể tạo ra giọng nói AI từ một đoạn văn bản ngắn hoặc chạy tự động (Batch) hàng trăm câu thoại để tạo thành một bộ phim/audiobook dài.

Dưới đây là giải thích chi tiết về toàn bộ các hộp công cụ (Toolbox) và nút bấm có trên giao diện.

---

## 1. Khu vực VOICE (Thiết lập Giọng AI)
Đây là nơi bạn chọn "diễn viên lồng tiếng" và điều chỉnh cảm xúc của họ.

* **Voice (Dropdown):** Danh sách các giọng nói mẫu đã được nạp sẵn vào hệ thống.
* **Model:** Chọn bộ não AI (Ví dụ: `chatterbox_multilingual` hỗ trợ nhiều ngôn ngữ).
* **Language:** Chọn ngôn ngữ mà bạn muốn AI đọc (Ví dụ: `vi` cho tiếng Việt, `en` cho tiếng Anh). Nếu để `Auto`, AI sẽ tự động đoán ngôn ngữ.
* **Nút [+ Add to Library] / [Import Voice]:** Dùng để nạp thêm một giọng nói bất kỳ từ file âm thanh có sẵn trên máy của bạn (wav, mp3...) vào danh sách để AI "bắt chước" giọng đó.
* **Change voice settings (Cài đặt chuyên sâu):**
  * **Speed (Tốc độ):** Tốc độ đọc. Mặc định là `1.00`. Lớn hơn 1 là đọc nhanh, nhỏ hơn 1 là đọc chậm.
  * **CFG Weight:** Độ ổn định của AI bám sát văn bản (Gợi ý: 50).
  * **Temperature:** Độ sáng tạo/bay bổng của giọng đọc. Càng cao giọng càng có nhiều cảm xúc bất ngờ, nhưng dễ bị vấp (Gợi ý: 80).
  * **Exaggeration:** Độ cường điệu của cảm xúc (Gợi ý: 50).
  * **Speaker Boost:** Tự động chuẩn hóa và nén âm lượng (Normalize/Loudnorm) giúp giọng đọc to, rõ ràng và uy lực hơn. Rất khuyên dùng.

---

## 2. Khu vực BATCH JOB (Chạy hàng loạt)
Dùng để thiết lập đường dẫn lưu file và kích hoạt chuỗi công việc tự động.

* **Folder (Ô Textbox & Nút `...`):** Đây là nơi quy định thư mục sẽ chứa toàn bộ các file âm thanh (`.wav`) sau khi AI đọc xong. Hãy bấm nút `...` để chọn thư mục.
* **Auto create SRT (Checkbox):** Nếu tích vào ô này, sau khi AI đọc xong toàn bộ danh sách, phần mềm sẽ tự động đo đạc thời gian của từng file âm thanh và xuất ra một file phụ đề `subtitles.srt` siêu chuẩn xác.
* **Run Batch (Nút bấm):** Phát lệnh cho AI bắt đầu đọc toàn bộ các dòng chữ đang có trạng thái `Pending` trong bảng danh sách bên dưới.

---

## 3. Khu vực OPTIONS (Tùy chọn nâng cao)
Nằm ở góc phải màn hình, hỗ trợ tinh chỉnh đầu vào của văn bản.

* **Auto Split:** Tự động "băm" một đoạn văn bản dài thành các câu ngắn. Điều này giúp AI không bị tràn bộ nhớ (VRAM) khi đọc quá nhiều chữ một lúc.
* **Split (Ô điền ký tự):** Định nghĩa các dấu câu dùng để cắt đoạn (Ví dụ: `.!?` sẽ cắt câu mỗi khi gặp dấu chấm, chấm hỏi, chấm than).
* **Retry Failed / Skip Existing / Auto Join:** Các tùy chọn tự động hóa khi chạy Batch (sẽ được mở rộng trong tương lai).

---

## 4. THANH CÔNG CỤ (Toolbar)
Thanh nằm ngang chứa các tính năng điều khiển nhanh.

* **Start:** Dùng để đọc nhanh câu văn bạn gõ trực tiếp (không dùng Batch).
* **Stop:** Dừng khẩn cấp toàn bộ tiến trình AI đang chạy.
* **Import File:** Chọn một file `.txt` hoặc phụ đề `.srt` từ máy tính. Phần mềm sẽ đọc file và chèn toàn bộ các câu văn vào bảng danh sách bên dưới.
* **Import Folder:** Chọn một thư mục chứa nhiều file `.txt`. Phần mềm sẽ quét sạch và gộp tất cả chữ trong đó đưa vào bảng chờ AI đọc.
* **Import Voice:** Cùng chức năng với nút Import Voice màu tím ở trên cùng.
* **Open Audio Output:** Nút Mở Nhanh. Bấm vào đây, hệ thống sẽ mở ngay cửa sổ File Explorer trỏ thẳng vào thư mục Output bạn đã chọn, giúp bạn xem kết quả mà không cần đi tìm.
* **Create SRT:** Nút tạo file Phụ đề thủ công. Nếu bạn quên tích ô `Auto create SRT`, bạn có thể bấm nút này sau khi Batch chạy xong để tạo file `.srt`.
* **Join MP3:** Gọi bộ lọc FFmpeg để gộp toàn bộ các file âm thanh lẻ tẻ trong thư mục Output lại thành một file dài duy nhất (`joined_output.wav`). Rất tiện để làm Audiobook hoặc Podcast.
* **Play:** Phát trực tiếp file âm thanh (bằng phần mềm nghe nhạc mặc định của Windows).
* **Save WAV:** Copy file âm thanh vừa đọc ra một chỗ khác.

---

## 5. BẢNG DANH SÁCH (DataGridView)
Nơi hiển thị các "Kịch bản" đang chờ AI lên sóng.

* **Id:** Số thứ tự của câu thoại.
* **Output:** Tên file âm thanh sẽ được lưu ra ổ cứng.
* **Timing:** Thời gian (dùng khi đọc file `.srt`).
* **Content:** Nội dung câu văn AI cần đọc.
* **Voice / Language:** Giọng đọc và ngôn ngữ được chỉ định cho dòng đó.
* **Status:** Trạng thái (Pending = Đang chờ, Running = Đang đọc, Completed = Đã xong).
