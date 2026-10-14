# AI Video Studio — Import Scene Plan từ Claude / ChatGPT

Nhánh phát triển: `feature/ai-video-studio`. Hỗ trợ từ bản Scene Plan Import v1.

## Mục đích

Thay vì chia đều 140 cue SRT thành 38 ảnh, import đúng cách Claude đã phân chia nội dung:
- `cue_from / cue_to` cho từng scene, tự gán từ file SRT thực;
- `filename` (001.png ... 038.png) từ thư mục ảnh bạn chọn;
- `style_id` Whiteboard / Image Story / Image Motion / Text / Layer và `transition` fade/cut;
- `visual_goal`, `visual_description_vi`, `continuity` gộp vào Ghi chú để người dùng biết lý do lựa chọn hình.

Chức năng không sửa audio TTS, SRT, ảnh gốc hay renderer; chỉ tạo storyboard để bạn kiểm tra, lưu và xuất.

## Cách dùng sau khi nhận file Claude

1. Bấm **Nguồn TTS / SRT / Ảnh** trong AI Video Studio.
2. Chọn **SRT cuối cùng** đã dùng để Claude chia cue; chọn thư mục ảnh chứa đúng `001.png`, `002.png`... (hoặc đúng phần đuôi ảnh trong JSON). Chọn audio TTS tổng tương ứng nếu đã có.
3. Bấm nút mới **Import Scene Plan (Claude)** trên thanh công cụ.
4. Chọn `scene_plan_<slug>.json`. Ứng dụng phân tích và đối chiếu cue, thời gian, số lượng, tên ảnh, hiệu ứng; xem màn hình xác nhận.
5. Nếu còn thiếu ảnh, ứng dụng thông báo rõ, vẫn cho nhập để chỉnh timeline. **Không thể render hoàn chỉnh nếu chưa có toàn bộ ảnh và audio thật.**
6. Nếu storyboard đã có dữ liệu, popup hỏi xác nhận trước khi thay thế. Nếu bấm Không hoặc JSON không hợp lệ, giữ nguyên storyboard.
7. Nhấn **Lưu** hoặc **Lưu thành...** để tạo `project.ai-video.json` ĐÚNG schema của phần mềm.
8. Xem trước từng scene, kiểm tra âm thanh và **XUẤT VIDEO** khi đầy đủ ảnh.

**Quan trọng:** Không dùng **Tạo storyboard Theo số ảnh** sau khi import vì nó sẽ ghi đè chia cue ngữ nghĩa của Claude bằng chia đều theo thời lượng.

## Các file Claude tạo

| Tên | Mục đích | Có thể import trực tiếp? |
|---|---|---|
| `scene_plan_<slug>.json` | Tập trung tất cả cue/ảnh/time/style | **Có**, nút Import Scene Plan |
| `scene_timing_<slug>.md` | Bảng đối chiếu người đọc | Không |
| `image_prompts_<slug>.txt` | Prompt tạo ảnh theo scene_id và filename | Không |
| `character_reference_<slug>.md` | Giữ nhân vật nhất quán | Không |

## Contract Import v1

Top-level object có `scenes` là array 1–200 phần tử, tùy chọn `scene_count`, `total_cues`, `total_duration_s`, `slug`, `schema_version` ("dva-scene-plan-v1"). Mỗi scene bắt buộc có:
`scene_id` liên tục ("001"...), `filename` chỉ tên ảnh hợp lệ, `cue_from` và `cue_to` số nguyên, `style_id` thuộc `whiteboard|image-story|image-motion|layer|text`.
Có thể bổ sung `start`, `end`, `duration_s` số giây, hoặc `start_ms`, `end_ms` số nguyên milli-giây; nếu có sẽ so với SRT với dung sai tối đa **150 ms**. Các trường `transition` (fade/cut), `narration_excerpt`, `visual_goal`, `visual_description_vi`, `image_prompt_en`, `continuity` được phép; ba trường mô tả hình được đưa vào Ghi chú, lời thoại ưu tiên SRT gốc.

Chấp nhận ảnh `png, jpg, jpeg, webp, bmp, svg` khớp chính xác tên trong thư mục. Không chấp nhận đường dẫn đầy đủ, `../`, trùng tên hoặc `style_id` placeholder (`map`, `timeline`, `infographic`). Nếu Google Flow tạo `.webp` thay vì `.png`, cần đổi tên trong JSON cho đúng file thật (hoặc xuất ra PNG), không âm thầm gán nhầm.

Importer kiểm:
- đủ scene, cue 1..N liên tiếp, không trùng/bỏ cue;
- SRT không chồng thời gian; timecode JSON khớp SRT thực;
- độ dài và tổng cue/duration khớp khi JSON cung cấp;
- tên ảnh không gây truy cập ngoài thư mục và hiệu ứng thực sự có renderer;
- không thay storyboard cũ nếu chưa xác nhận.

Hạn chế:
- Không xác nhận đồng bộ audio chỉ từ JSON + SRT; người dùng vẫn phải cung cấp audio và kiểm tra preview.
- Nếu thiếu ảnh, chỉ chuẩn bị timeline và ghi chú.
- Hình ảnh prompt không tự sinh thành ảnh; công cụ tạo ảnh là bước riêng.
- Đây không phải chức năng cắt audio theo từ hoặc đa track tùy ý; mỗi scene vẫn nằm trên một TTS tổng.

## Kiểm thử (fixture tổng hợp dựa trên mốc dự án Zuschauereffekt)

- Các mốc ranh giới 38 scene / 140 cue / 601,559 giây, gồm 31 Whiteboard và 7 Image Story, lấy từ scene plan Claude đã gửi.
- File SRT trong bài test là **SRT mô phỏng** có ranh giới giống kế hoạch; đây KHÔNG phải bản xác nhận của SRT/audio gốc người dùng.
- Đã kiểm thử 13 trường hợp JSON lỗi: cue bị nhảy/chồng/vượt giới hạn, sai duration/tổng, sai style, tên ảnh trùng, path traversal...
- WinForms: nhập hai cảnh vào bảng và timeline, từ chối JSON lỗi mà không mất scene.
- Đã kiểm thử C# serialize project và các test renderer hiện có.

## Sửa lỗi lệch thời gian khi import Scene Plan (2026-10-14)

Nếu timestamp của JSON Claude không khớp SRT thực, importer hiện kiểm tra lại toàn bộ narration_excerpt theo cue_from/cue_to của từng cảnh.
Nếu lời đọc từng cảnh KHỚP chính xác sau chuẩn hóa dấu câu, hoa thường, ứng dụng hỏi người dùng có muốn đồng bộ timestamp theo SRT không.
Chỉ khi người dùng đồng ý, phần mềm lấy thời gian từ SRT để tạo storyboard, giữ nguyên ảnh, cue và style; JSON gốc và SRT không bị sửa.
Nếu lời đọc khác SRT, cue thiếu/trùng, hoặc JSON hỏng, ứng dụng từ chối và giữ storyboard hiện tại. InvalidDataException được xử lý bằng MessageBox của ứng dụng, không bật lỗi Windows chưa xử lý.

### Đã kiểm tra bằng SRT thật của Zuschauereffekt

- JSON gốc: C:\Users\Admin\Downloads\scene_plan_zuschauereffekt.json (601.559 giây).
- JSON đã sửa: C:\Users\Admin\Downloads\scene_plan_zuschauereffekt_srt_synced.json (599.599 giây).
- SRT gốc: D:\TestTTS\TTS Create By DvaForDev\TestOutput\000_Full_Audio_subtitles.srt.
- Thư mục ảnh: C:\Users\Admin\Downloads\img_flow_png (001.png đến 038.png).
- 140 cue khớp chính xác với narration_excerpt của 38/38 cảnh. 38 ảnh PNG đều có mặt.
- Smoke C# importer: bản JSON đã đồng bộ nhập strict thành công, bản JSON gốc căn lại bằng SRT cho timeline tương đương, giả lập văn bản sai bị từ chối.
- SRT không thay cho kiểm tra audio thực: khi xuất video, vẫn cần xác nhận WAV/MP3 khớp SRT.
