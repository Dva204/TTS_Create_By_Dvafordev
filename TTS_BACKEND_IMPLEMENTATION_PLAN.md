# DvaForDev AI TTS Studio — Backend Implementation Plan

> Tài liệu này mô tả chi tiết toàn bộ backend cần xây dựng để phục vụ giao diện hiện tại của **DvaForDev AI TTS Studio**.
>
> Mục tiêu: mọi control trên Form đều có chức năng thật, không có nút giả/no-op. Backend ưu tiên chạy local, tận dụng GPU NVIDIA nếu có, có CPU fallback, sử dụng FFmpeg cho audio/video và hỗ trợ mở rộng nhiều TTS engine.

---

# 1. Kiến trúc tổng thể

```text
WinForms (.NET 8)
        |
        | HTTP localhost
        v
FastAPI Backend
        |
        +-----------------------------+
        |                             |
        v                             v
 TTS Services                   Video Services
        |                             |
        v                             v
 Chatterbox / Qwen / Kokoro      FFmpeg / FFprobe
        |
        v
 PyTorch / CUDA / CPU
```

Backend mặc định:

```text
http://127.0.0.1:7865
```

Thư mục dự kiến:

```text
backend/
├── main.py
├── requirements.txt
│
├── engines/
│   ├── base.py
│   ├── chatterbox_engine.py
│   ├── qwen_engine.py
│   └── kokoro_engine.py
│
├── api/
│   ├── system_routes.py
│   ├── voice_routes.py
│   ├── tts_routes.py
│   ├── batch_routes.py
│   ├── video_routes.py
│   └── settings_routes.py
│
├── services/
│   ├── engine_manager.py
│   ├── voice_service.py
│   ├── voice_store.py
│   ├── tts_service.py
│   ├── batch_service.py
│   ├── job_manager.py
│   ├── audio_service.py
│   ├── subtitle_service.py
│   ├── timeline_service.py
│   ├── video_service.py
│   ├── ffmpeg_service.py
│   ├── ffprobe_service.py
│   └── settings_service.py
│
├── models/
│   ├── voice.py
│   ├── tts.py
│   ├── batch.py
│   ├── video.py
│   ├── settings.py
│   └── jobs.py
│
└── data/
    ├── voices/
    ├── jobs/
    ├── batches/
    ├── video_jobs/
    ├── cache/
    ├── temp/
    └── settings.json
```

---

# 2. Nguyên tắc backend

- Không tạo audio/video giả.
- Mọi nút trên UI khi bật đều phải gọi chức năng thật.
- Một GPU 4 GB VRAM chỉ chạy **1 inference TTS tại một thời điểm**.
- Video rendering có thể chạy riêng nhưng nên hạn chế chạy song song với TTS khi VRAM thấp.
- TTS engine phải được load lazy, không load tất cả engine cùng lúc.
- Khi đổi engine phải unload engine trước nếu cần giải phóng VRAM.
- FFmpeg và FFprobe phải được kiểm tra khi backend khởi động.
- Mọi đường dẫn lưu bằng path tương đối hoặc cấu hình.
- Job phải có trạng thái: `queued`, `running`, `completed`, `failed`, `cancelled`.
- Backend phải log lỗi chi tiết nhưng UI chỉ hiển thị thông báo dễ hiểu.
- Không được để control UI "có nhưng không làm gì".

---

# 3. Backend hiện tại đã có

Backend hiện tại đã có nền tảng:

- FastAPI.
- Chatterbox Multilingual.
- CUDA PyTorch.
- Voice Library cơ bản.
- Upload voice reference.
- Normalize audio reference qua FFmpeg.
- Generate TTS.
- Job Manager.
- Download WAV.
- Health/System API.
- Serial GPU queue.
- WinForms tự khởi động backend.
- RTX 3050 Laptop GPU đã test chạy thực tế.
- Chatterbox đã generate WAV thật từ voice reference.

Các phần dưới đây là phần cần hoàn thiện tiếp.

---

# 4. TAB: TTS / Batch

## 4.1. Voice Group

Các control:

```text
Name
Search
Voice
Model
Language
+ Add to Library
Import Voice
Voice Library
```

### Name / Search

UI:
- `txtVoiceName`
- `btnSearchVoice`

Backend cho phép tìm voice theo:
- tên
- language
- gender
- tag
- description
- voice id

API:

```http
GET /api/voices?q=american
```

Response:

```json
[
  {
    "id": "voice_xxx",
    "name": "US Documentary",
    "language": "en",
    "gender": "male",
    "description": "Documentary narrator",
    "reference_file": "reference.wav",
    "created_at": "..."
  }
]
```

### Voice ComboBox

UI:
- `voiceComboBox`

API:

```http
GET /api/voices
```

WinForms bind:

```text
DisplayMember = Name
ValueMember   = Id
```

### Model

UI:
- `engineComboBox`

Giá trị:

```text
chatterbox_multilingual
qwen3_tts
kokoro
```

Backend cần `EngineManager`:

```python
class EngineManager:
    current_engine = None

    load_engine(name)
    unload_engine()
    get_engine(name)
    get_status()
```

API:

```http
GET  /api/engines
POST /api/engines/load
POST /api/engines/unload
```

Mỗi engine expose:

```python
load()
unload()
generate()
supports_voice_clone()
supported_languages()
get_capabilities()
```

Không load Chatterbox + Qwen cùng lúc trên máy 4 GB VRAM.

### Language

UI:
- `languageComboBox`

Giá trị:

```text
Auto
en
vi
es
fr
de
it
pt
ru
ja
ko
zh
```

Generate request:

```json
{
  "text": "...",
  "voice_id": "...",
  "language": "en",
  "engine": "chatterbox_multilingual"
}
```

### + Add to Library

Lưu voice/reference vào thư viện.

```http
POST /api/voices
```

### Import Voice

UI:
- `addVoiceButton`
- `btnImportVoice`

Input:

```text
.wav
.mp3
.m4a
.flac
```

Workflow:

```text
audio input
   ↓
validate
   ↓
FFprobe
   ↓
FFmpeg normalize
   ↓
mono / 24kHz / PCM WAV
   ↓
data/voices/{voice_id}/reference.wav
   ↓
voice.json
```

Validation:
- file tồn tại
- audio đọc được
- duration hợp lệ
- file không rỗng
- max file size
- cảnh báo nếu reference quá ngắn/dài

### Voice Library button

UI:
- `btnOpenVoiceLibrary`

Không cần API mới. Chỉ chuyển tab ở C#.

---

# 5. Voice Settings

Các control:

```text
Change voice settings
Speed
CFG Weight
Temperature
Exaggeration
Speaker Boost
Reset
```

Model:

```python
class TtsGenerationSettings(BaseModel):
    speed: float = 1.0
    cfg_weight: float = 0.5
    temperature: float = 0.8
    exaggeration: float = 0.5
    speaker_boost: bool = True
    seed: int | None = None
```

Request:

```json
{
  "text": "...",
  "voice_id": "...",
  "language": "en",
  "engine": "chatterbox_multilingual",
  "settings": {
    "speed": 1.0,
    "cfg_weight": 0.5,
    "temperature": 0.8,
    "exaggeration": 0.5,
    "speaker_boost": true
  }
}
```

## Speed

Nếu engine không hỗ trợ native speed:
- hậu kỳ bằng FFmpeg `atempo`.

## CFG Weight / Temperature / Exaggeration

Phải inspect signature thực tế của:

```python
model.generate(...)
```

Không truyền tham số mà package không hỗ trợ.

## Speaker Boost

Nếu engine không có native setting:
- normalize
- gain
- limiter nhẹ
- `loudnorm`
- `acompressor`

## Reset

Default:

```text
Speed          1.00
CFG Weight     0.50
Temperature    0.80
Exaggeration   0.50
Speaker Boost  true
```

---

# 6. System Panel

Các control:

```text
Device
Backend
GPU
CUDA
VRAM
```

### Device

UI:

```text
Auto
CUDA
CPU
```

Logic:

```python
if device == "auto":
    use_cuda_if_available()
elif device == "cuda":
    require_cuda()
else:
    use_cpu()
```

API:

```http
GET  /api/system
POST /api/system/device
```

### Backend status

```http
GET /api/health
```

### GPU / CUDA / VRAM

Response đề xuất:

```json
{
  "python_version": "3.x",
  "torch_version": "...",
  "cuda_available": true,
  "cuda_version": "...",
  "gpu_name": "NVIDIA GeForce RTX 3050 Laptop GPU",
  "gpu_memory_total_mb": 4096,
  "gpu_memory_used_mb": 3100,
  "gpu_memory_free_mb": 996,
  "ffmpeg_available": true,
  "ffmpeg_path": "...",
  "ffprobe_available": true
}
```

---

# 7. Batch Job

Các control:

```text
Folder
...
Auto create SRT
Run Batch
Batch Info
```

### Folder

UI:
- `txtBatchFolder`
- `btnBrowseFolder`

Folder có thể chứa:

```text
.txt
.srt
```

### Import File

UI:
- `btnImportFile`

TXT:

```text
parse
↓
auto split
↓
segments
↓
dgvJobs
```

SRT:

```text
parse index/start/end/content
↓
dgvJobs
```

### Import Folder

UI:
- `btnImportFolder`

Scan:

```text
*.txt
*.srt
```

### Auto Split

UI:
- `chkAutoSplit`
- `txtSplitChars`

Logic:
- max characters
- punctuation
- ưu tiên xuống dòng
- không cắt giữa từ
- tránh segment quá ngắn

### Run Batch

UI:
- `btnRunBatch`

API:

```http
POST /api/batches
GET  /api/batches/{batch_id}
POST /api/batches/{batch_id}/cancel
POST /api/batches/{batch_id}/retry-failed
```

Queue:

```text
Pending
↓
GPU Queue
↓
ONE AT A TIME
↓
WAV
↓
duration probe
↓
next segment
```

### Retry Failed

UI:
- `chkRetryFailed`

Settings:

```json
{
  "retry_count": 2
}
```

### Skip Existing

UI:
- `chkSkipExisting`

Nếu output có sẵn → `Skipped`.

### Auto Join

UI:
- `chkAutoJoin`

```text
001.wav
002.wav
003.wav
↓
FFmpeg concat
↓
final.wav / final.mp3
```

### Auto create SRT

UI:
- `chkAutoSrt`

Dựa trên duration thực tế của WAV.

### Batch Info

UI:
- `txtBatchInfo`

Hiển thị:
- Files
- Segments
- Pending
- Completed
- Failed
- Output
- Estimated duration

---

# 8. Toolbar

### Start
- `generateButton`
- start queue hoặc generate current job.

### Stop
- `stopButton`

```http
DELETE /api/jobs/{job_id}
```

### Open Audio Output
C# mở Explorer.

### Create SRT

```http
POST /api/subtitles/from-batch
```

### Join MP3

```http
POST /api/audio/join
```

### Play
C# mở file local.

### Save WAV
C# SaveFileDialog.

---

# 9. Job Grid

Columns:

```text
Id
Output
Timing
Content
Voice
Language
Status
```

Job model:

```json
{
  "id": "job_xxx",
  "index": 1,
  "output": "001.wav",
  "start": 0.0,
  "end": 4.2,
  "duration": 4.2,
  "text": "...",
  "voice_id": "...",
  "voice_name": "...",
  "language": "en",
  "status": "completed",
  "error": null
}
```

Statuses:

```text
Pending
Queued
Processing
Done
Failed
Cancelled
Skipped
```

---

# 10. Progress / Status

UI:

```text
Done
Processing
Total
Failed
Elapsed
Current
ProgressBar
```

Response:

```json
{
  "total": 100,
  "pending": 80,
  "running": 1,
  "completed": 18,
  "failed": 1,
  "cancelled": 0,
  "progress_percent": 18,
  "elapsed_seconds": 320,
  "current_job": "segment_019"
}
```

---

# 11. TAB: Voice Library

Grid:

```text
Voice Name
Voice ID
Language
Gender
Created
```

Metadata:

```json
{
  "id": "voice_xxx",
  "name": "US Documentary",
  "language": "en",
  "gender": "male",
  "description": "Deep documentary voice",
  "reference_file": "reference.wav",
  "created_at": "...",
  "updated_at": "..."
}
```

Storage:

```text
data/voices/voice_xxx/
├── reference.wav
└── voice.json
```

### Search

```http
GET /api/voices?q=...
```

### Refresh

```http
GET /api/voices
```

### + Add New Voice

Focus panel Add New Voice và clear input.

---

# 12. Selected Voice

Các field:

```text
Name
Voice ID
Language
Gender
Description
Reference
Play
Preview
Generate Preview
Use This Voice
Copy ID
Replace
Delete
```

### Voice details

```http
GET /api/voices/{voice_id}
```

### Play Reference

```http
GET /api/voices/{voice_id}/reference
```

### Generate Preview

```http
POST /api/voices/{voice_id}/preview
```

hoặc reuse:

```http
POST /api/tts/generate
```

### Use This Voice

Không cần backend.

### Copy ID

Không cần backend.

### Replace reference

```http
PUT /api/voices/{voice_id}/reference
```

### Delete voice

```http
DELETE /api/voices/{voice_id}
```

Phải kiểm tra voice có đang được job sử dụng không.

---

# 13. Add New Voice

Fields:

```text
Name
Language
Gender
Description
Reference
Preview
Add Voice
Clear
```

Request:

```http
POST /api/voices
```

Multipart:

```text
name
language
gender
description
audio_file
```

Validation:
- name required
- language hợp lệ
- gender = unknown/male/female
- audio hợp lệ
- supported format

Preview reference có thể phát local, chưa cần backend.

---

# 14. TAB: Video Editor

Mục tiêu:

```text
Audio + SRT + Images
        ↓
Analyze Timeline
        ↓
Auto map image
        ↓
Effects
        ↓
FFmpeg Render
        ↓
MP4
```

---

# 15. Video Source

UI:

```text
Audio
Subtitle
Images
Timeline
Mapping
Analyze Timeline
```

### Audio

UI:
- `txtVideoAudio`
- `btnBrowseVideoAudio`

Hỗ trợ:

```text
.wav
.mp3
.m4a
.aac
.flac
```

Probe:

```http
POST /api/video/probe
```

### Subtitle

UI:
- `txtVideoSrt`
- `btnBrowseVideoSrt`

Parser SRT output:

```json
[
  {
    "index": 1,
    "start": 0.0,
    "end": 4.2,
    "text": "..."
  }
]
```

Validation:
- timeline tăng dần
- end > start
- không timestamp âm
- warning overlap

### Images Folder

UI:
- `txtImageFolder`
- `btnBrowseImageFolder`

Formats:

```text
.jpg
.jpeg
.png
.webp
```

---

# 16. Timeline Mode

UI:

```text
From SRT
From Image Filename
From File Order
```

### From SRT
SRT là timeline chính.

### From Image Filename

Ví dụ:

```text
00_00_00_000.jpg
00_00_05_420.jpg
00_00_09_800.jpg
```

Parser:

```text
HH_MM_SS_mmm
```

Ảnh tồn tại từ timestamp của nó đến timestamp ảnh tiếp theo.

Ảnh cuối kéo tới audio duration.

### From File Order

Natural sort:

```text
001.jpg
002.jpg
003.jpg
```

---

# 17. Mapping Mode

UI:

```text
Timestamp Filename
SRT Number
File Order
```

### Timestamp Filename
Tên ảnh là start time.

### SRT Number

```text
1.jpg
2.jpg
3.jpg
```

map với subtitle tương ứng.

### File Order
Natural sort, không sort string kiểu `1,10,2`.

---

# 18. Analyze Timeline

UI:
- `btnAnalyzeTimeline`

API:

```http
POST /api/video/analyze
```

Request:

```json
{
  "audio_path": "...",
  "srt_path": "...",
  "image_folder": "...",
  "timeline_mode": "srt",
  "mapping_mode": "timestamp"
}
```

Response:

```json
{
  "audio_duration": 1800.0,
  "items": [
    {
      "id": 1,
      "start": 0.0,
      "end": 5.42,
      "duration": 5.42,
      "image": "00_00_00_000.jpg",
      "subtitle": "...",
      "status": "Ready"
    }
  ],
  "warnings": []
}
```

---

# 19. Video Timeline Grid

Columns:

```text
Id
Start
End
Duration
Image
Subtitle
Status
```

Statuses:

```text
Ready
Missing Image
Invalid Timestamp
Overlap
Too Short
Too Long
Out of Audio Range
```

---

# 20. Render Options

UI:

```text
Resolution
FPS
Aspect
Encoder
Quality
Ken Burns
Crossfade
Burn Subtitle
Hardware Encode
Transition
```

### Resolution

```text
1920x1080
2560x1440
3840x2160
```

### FPS

```text
24
30
60
```

Default nên là 30.

### Aspect

```text
16:9
9:16
1:1
```

### Encoder

```text
Auto
NVIDIA NVENC
CPU x264
CPU x265
```

Auto:
- NVENC nếu có.
- fallback libx264.

### Quality

```text
Fast
Balanced
Maximum Quality
```

Backend tự map preset theo encoder.

---

# 21. Ken Burns

UI:
- `chkKenBurns`

FFmpeg:
- `zoompan`
- zoom in nhẹ
- zoom out nhẹ
- pan trái/phải

Không random quá mạnh.

---

# 22. Crossfade

UI:
- `chkCrossfade`

FFmpeg:
- `xfade`

Phải tính offset chính xác theo duration.

---

# 23. Burn Subtitle

UI:
- `chkBurnSubtitle`

FFmpeg:
- subtitles/libass

Sau này có thể thêm:
- font
- size
- outline
- shadow
- alignment
- margin

---

# 24. Hardware Encode

UI:
- `chkHardwareEncoding`

Nếu bật:
- ưu tiên NVENC.

Nếu user chọn CPU explicit:
- disable/ignore checkbox.

---

# 25. Transition Duration

UI:
- `numTransitionDuration`

Default:

```text
0.35 sec
```

Validation:
- >= 0
- không lớn hơn clip ngắn nhất
- clip quá ngắn thì tự giảm transition

---

# 26. Preview Video

UI:
- `btnPreviewVideo`

API:

```http
POST /api/video/preview
```

Không render toàn video.

Chỉ render:
- 15–30 giây đầu
- hoặc N timeline items.

Output:

```text
preview.mp4
```

---

# 27. Render Video

UI:
- `btnRenderVideo`

API:

```http
POST /api/video/render
```

Request:

```json
{
  "audio_path": "...",
  "subtitle_path": "...",
  "image_folder": "...",
  "timeline": [],
  "settings": {
    "width": 1920,
    "height": 1080,
    "fps": 30,
    "encoder": "auto",
    "quality": "balanced",
    "ken_burns": true,
    "crossfade": true,
    "burn_subtitle": false,
    "transition_duration": 0.35
  }
}
```

Response:

```json
{
  "job_id": "video_xxx",
  "status": "queued"
}
```

---

# 28. Video Job Manager

States:

```text
queued
analyzing
preparing
rendering
muxing
completed
failed
cancelled
```

API:

```http
GET    /api/video/jobs/{id}
DELETE /api/video/jobs/{id}
GET    /api/video/jobs/{id}/output
```

Response:

```json
{
  "id": "video_xxx",
  "status": "rendering",
  "progress": 42.6,
  "current_time": 767.2,
  "total_duration": 1800.0,
  "fps": 85.4,
  "speed": 2.8,
  "eta_seconds": 370
}
```

---

# 29. FFmpeg Progress Parsing

FFmpeg nên chạy:

```text
-progress pipe:1
```

Parse:
- `out_time_ms`
- `fps`
- `speed`
- `progress`

Tính:

```text
progress = current_time / total_duration * 100
```

---

# 30. Video Render Strategy

Ưu tiên:

```text
images
↓
filter_complex
↓
scale/crop
↓
zoompan
↓
xfade
↓
subtitle
↓
audio
↓
encode
↓
MP4
```

Nếu filter graph quá dài:
- chia chunks
- render temp segments
- concat cuối

---

# 31. Image Quality

Backend:
- không upscale vô lý
- giữ aspect
- scale chất lượng cao
- sau này thêm Fill/Fit/Crop

Advanced settings sau này:

```text
Image Fit:
- Fill
- Fit
- Crop

Background:
- Black
- Blur
```

---

# 32. Open Video Output

UI:
- `btnOpenVideoOutput`

C# mở Explorer.

---

# 33. Video Progress

UI:
- `progressVideoRender`
- `lblVideoRenderStatus`

Hiển thị:

```text
Rendering 42%
FPS: 85
Speed: 2.8x
ETA: 06:10
```

---

# 34. TAB: Settings

UI:

```text
Output folder
Max characters
Silence (ms)
Output format
MP3 bitrate
Save Settings
```

Storage:

```text
backend/data/settings.json
```

Model:

```json
{
  "output_folder": "...",
  "max_characters": 300,
  "silence_ms": 250,
  "output_format": "WAV",
  "mp3_bitrate": 192,
  "device": "auto",
  "video": {
    "resolution": "1920x1080",
    "fps": 30,
    "encoder": "auto",
    "quality": "balanced"
  }
}
```

API:

```http
GET /api/settings
PUT /api/settings
```

---

# 35. Output Folder

Structure:

```text
output/
├── audio/
│   ├── segments/
│   └── joined/
├── subtitles/
└── video/
```

---

# 36. Max Characters

Dùng Auto Split.

Default:

```text
300
```

Không cắt cứng ở đúng ký tự 300; ưu tiên punctuation.

---

# 37. Silence

Default:

```text
250 ms
```

Dùng khi join audio.

Có thể mở rộng:

```text
sentence_silence_ms
paragraph_silence_ms
chapter_silence_ms
```

---

# 38. Output Format

```text
WAV
MP3
```

TTS generate WAV trước, convert MP3 sau nếu cần.

---

# 39. MP3 Bitrate

```text
128
192
256
320 kbps
```

FFmpeg:

```text
-b:a 192k
```

---

# 40. Logging

File:

```text
backend/logs/backend.log
```

Log:
- startup
- engine load
- GPU
- voice import
- TTS jobs
- batch jobs
- video jobs
- FFmpeg
- errors
- timings

---

# 41. Error Handling

Chuẩn response:

```json
{
  "code": "VOICE_NOT_FOUND",
  "message": "Voice was not found.",
  "details": null
}
```

Error code:

```text
BACKEND_NOT_READY
CUDA_NOT_AVAILABLE
ENGINE_NOT_AVAILABLE
ENGINE_LOAD_FAILED
VOICE_NOT_FOUND
VOICE_INVALID_AUDIO
VOICE_IN_USE
INVALID_LANGUAGE
INVALID_TEXT
JOB_NOT_FOUND
JOB_CANCELLED
FFMPEG_NOT_FOUND
FFPROBE_NOT_FOUND
INVALID_SRT
INVALID_IMAGE_TIMELINE
MISSING_IMAGE
INVALID_TIMESTAMP
VIDEO_RENDER_FAILED
OUTPUT_WRITE_FAILED
```

---

# 42. Startup Checks

Khi backend start:

```text
1. Python
2. torch
3. CUDA
4. GPU
5. FFmpeg
6. FFprobe
7. data folders
8. settings
9. voice database
10. engine availability
```

Không load TTS model ngay nếu chưa cần.

---

# 43. API Tổng Hợp

## System

```http
GET  /api/health
GET  /api/system
GET  /api/engines
POST /api/engines/load
POST /api/engines/unload
```

## Voices

```http
GET    /api/voices
GET    /api/voices/{id}
POST   /api/voices
DELETE /api/voices/{id}
GET    /api/voices/{id}/reference
PUT    /api/voices/{id}/reference
POST   /api/voices/{id}/preview
```

## TTS

```http
POST   /api/tts/generate
GET    /api/jobs
GET    /api/jobs/{id}
DELETE /api/jobs/{id}
GET    /api/jobs/{id}/audio
```

## Batch

```http
POST /api/batches
GET  /api/batches/{id}
POST /api/batches/{id}/cancel
POST /api/batches/{id}/retry-failed
GET  /api/batches/{id}/summary
```

## Audio / Subtitle

```http
POST /api/audio/join
POST /api/subtitles/from-batch
```

## Video

```http
POST   /api/video/probe
POST   /api/video/analyze
POST   /api/video/preview
POST   /api/video/render
GET    /api/video/jobs/{id}
DELETE /api/video/jobs/{id}
GET    /api/video/jobs/{id}/output
```

## Settings

```http
GET /api/settings
PUT /api/settings
```

---

# 44. Storage / Database

Phase đầu:

```text
JSON hoặc SQLite
```

Khuyến nghị SQLite khi job nhiều.

Tables:

```text
voices
tts_jobs
batches
batch_items
video_jobs
video_items
settings
```

Media vẫn lưu filesystem, không lưu binary vào DB.

---

# 45. GPU Strategy

Máy 4 GB VRAM:

```text
ONE TTS engine loaded
ONE TTS inference at a time
```

Video NVENC có thể dùng song song nếu benchmark ổn, nhưng backend nên có `gpu_busy`.

Expose:

```text
gpu_busy
loaded_engine
memory_used
```

---

# 46. Background Queues

Hai queue:

```text
TTSQueue
VideoQueue
```

HTTP chỉ tạo job và trả ID, không giữ request 30 phút.

---

# 47. Cancellation

FFmpeg:
- terminate process.

TTS:
- cancellation flag
- stop future jobs
- discard output nếu job đã bị cancel.

---

# 48. File Naming

```text
001.wav
002.wav
003.wav
final.wav
final.mp3
final.srt
final.mp4
```

Video temp:

```text
data/temp/video_job_id/
```

Cleanup sau completed.

---

# 49. Local API Security

Bind:

```text
127.0.0.1
```

Không dùng `0.0.0.0` mặc định.

Validate path và tránh path traversal.

---

# 50. Phase triển khai đề xuất

## Phase 1 — Voice Library

- [ ] metadata voice
- [ ] list/search
- [ ] selected voice
- [ ] play reference
- [ ] add voice
- [ ] delete voice
- [ ] replace reference
- [ ] preview voice
- [ ] Use This Voice

## Phase 2 — TTS Settings

- [ ] inspect Chatterbox generate signature
- [ ] CFG Weight
- [ ] Temperature
- [ ] Exaggeration
- [ ] Speed
- [ ] Speaker Boost
- [ ] engine capabilities

## Phase 3 — Batch

- [ ] TXT parser
- [ ] SRT parser
- [ ] auto split
- [ ] queue
- [ ] DataGrid status
- [ ] retry
- [ ] skip existing
- [ ] auto SRT
- [ ] auto join
- [ ] progress

## Phase 4 — Settings

- [ ] settings.json
- [ ] load/save
- [ ] output folder
- [ ] max chars
- [ ] silence
- [ ] bitrate
- [ ] device

## Phase 5 — Video Analyze

- [ ] FFprobe audio
- [ ] SRT parser
- [ ] image scanner
- [ ] natural sort
- [ ] timestamp filename parser
- [ ] SRT number mapping
- [ ] file-order mapping
- [ ] timeline validation
- [ ] fill `dgvVideoTimeline`

## Phase 6 — Video Rendering

- [ ] FFmpeg command builder
- [ ] resolution/FPS/aspect
- [ ] image scaling
- [ ] audio mux
- [ ] H.264
- [ ] NVENC
- [ ] progress parsing
- [ ] cancellation
- [ ] output MP4

## Phase 7 — Video Effects

- [ ] Ken Burns
- [ ] crossfade
- [ ] burn subtitle
- [ ] quality presets
- [ ] preview render
- [ ] 1440p
- [ ] 4K

## Phase 8 — Hardening

- [ ] temp cleanup
- [ ] structured logging
- [ ] SQLite
- [ ] crash recovery
- [ ] resume batch
- [ ] resume render
- [ ] settings migration
- [ ] error codes
- [ ] tests

---

# 51. Full Workflow Cuối Cùng

```text
SCRIPT
   ↓
Import TXT/SRT
   ↓
Auto Split
   ↓
Batch Queue
   ↓
Voice Clone / TTS
   ↓
001.wav / 002.wav / 003.wav
   ↓
Duration Probe
   ↓
SRT
   ↓
Join Audio
   ↓
VOICEOVER

        +

IMAGE FOLDER
   ↓
Timeline Mapping
   ↓
Analyze
   ↓
Video Timeline
   ↓
Ken Burns / Crossfade
   ↓
Subtitle
   ↓
FFmpeg + NVENC/x264
   ↓
FINAL VIDEO
```

---

# 52. Definition of Done

Backend được coi là hoàn thành khi:

- [ ] UI không còn nút giả.
- [ ] Voice Library CRUD đầy đủ.
- [ ] TTS tạo audio thật.
- [ ] TTS settings có mapping thật hoặc bị disable nếu engine không hỗ trợ.
- [ ] Batch chạy tuần tự ổn định.
- [ ] SRT timing đúng.
- [ ] Audio join đúng thứ tự.
- [ ] Settings persist sau restart.
- [ ] Video timeline parse đúng.
- [ ] Image mapping đúng.
- [ ] Render video MP4 đúng duration.
- [ ] Audio không lệch video.
- [ ] Subtitle không lệch timeline.
- [ ] NVENC fallback được sang CPU.
- [ ] Progress hiển thị chính xác.
- [ ] Stop/Cancel hoạt động.
- [ ] Temp files được cleanup.
- [ ] Backend restart không làm mất Voice Library.
- [ ] Error được hiển thị rõ trên UI.

---

# 53. Việc nên làm tiếp ngay

Thứ tự triển khai phù hợp nhất với project hiện tại:

```text
1. Hoàn thiện Voice Library backend + Form1.cs
2. Hoàn thiện Batch queue
3. Hoàn thiện Settings
4. Làm Video Analyze trước
5. Khi timeline đúng 100% mới làm Render Video
6. Sau render cơ bản mới thêm Ken Burns / Crossfade / Burn Subtitle
7. Cuối cùng mới tối ưu tốc độ và chất lượng render
```

Nguyên tắc quan trọng:

```text
Audio + SRT + Images
↓
Timeline đúng
↓
Render cơ bản đúng
↓
Effects
↓
Quality optimization
```
