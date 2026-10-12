# AI Video Director V1.2 - Huong dan su dung

Cap nhat: 12/10/2026. Nhanh: feature/ai-video-studio.

## Chuc nang

AI Video Director v1.2 de xuat khi nao can zoom va khi nao can hien chu tu
phu de SRT, sau do render nhung hieu ung nay cung voi TTS vao video.
Day la Smart Rules local, KHONG phai model AI hieu hinh anh / ngam nghia.

Cac tab TTS, Voice Library va Video Editor cu duoc giu nguyen.

## Cach dung giao dien

1. Mo tab AI Video Studio, nhap audio TTS, SRT, hinh anh.
2. Bam Tao storyboard de chia canh, chon kieu hinh cua moi canh.
3. Bam Goi y hieu ung (local) de lap ke hoach director tu SRT.
4. Xem cot Director trong bang storyboard.
5. Chon canh va bam Chinh Director cua canh de them/bot/sua hieu ung:
   - kind: text hoac zoom
   - startMs: bat dau tinh tu dau canh (mili giay)
   - durationMs: thoi luong hieu ung
   - text: noi dung chu, toi da 90 ky tu
   - zoom: 1.00 - 1.22
   - focusX, focusY: 0..1 (0.5 la trung tam)
6. Luu project JSON de luu cac chinh sua.
7. Xem truoc MP4, chinh lai neu can, roi bam Xuat YouTube.

Bam goi y lai khong ghi de nhung scene da co directionEvents chinh thu cong.

## Quy tac Smart Director

- Nam (vi du 1903, 1945, 2026), phan tram va so lieu -> goi y hien chu.
- Tu khoa first, hidden, secret, look closer, discover... -> co the goi y zoom.
- Moi scene toi da 2 goi y mac dinh, 1 text va 1 zoom.
- Hieu ung bang thoi gian cue SRT, luu thoi gian tuong doi voi scene.
- Khong tu zoom vao image-motion, khong chen chu vao scene Text mac dinh.
- Neu khong tim thay tin hieu noi bat thi khong bat buoc co hieu ung nao.
- Focus zoom mac dinh la giua anh. KHONG nhan dien duoc khuon mat / vat the.

## JSON directionEvents

Cau truc duoc luu ben trong moi scene, van dung schemaVersion=1:

    "directionEvents": [
      {
        "kind": "text", "startMs": 160, "durationMs": 1800,
        "text": "1903", "zoom": 1.1, "focusX": 0.5, "focusY": 0.5
      },
      {
        "kind": "zoom", "startMs": 2100, "durationMs": 2500,
        "text": "", "zoom": 1.1, "focusX": 0.5, "focusY": 0.5
      }
    ]

Cac project cu thieu directionEvents van hoat dong nhu truoc.

## Engine va validation

- C# va Python kiem tra kind, thoi gian, toa do, muc zoom.
- Toi da 4 effect moi canh, toi da 1 zoom.
- Text duoc ve len PNG transparency bang Pillow, ghep bang FFmpeg.
- Camera zoom mượt rồi tro ve muc 1.0 khi het hieu ung.
- Cache scene thay doi khi directionEvents thay doi.
- Chay offline, khong yeu cau API; co the dung GPU de encode NVENC.
- Khong co model nhin anh hay AI phan tich toan bo cau chuyen.
- Chua kiem thu video dai 8-12 phut va UI bang thao tac chuot.

## Test da thuc hien

- Cau SRT nhac "In 1903..." -> text 1903 xuat hien o 160ms.
- Cau co tu khoa first / hidden -> zoom nhe theo thoi luong canh.
- Video sample 6 giay preview 640x360 20FPS va Full HD 1920x1080 24FPS:
  ca hai da render thanh cong, H.264 / AAC.
- Da xem frame giay 1, 3 va 5: chu xuat hien, zoom, tro ve khung ban dau.
- Test roundtrip project JSON bang C# va bo test quy tac Python da dat.
- Audio test la sine WAV gia lap, KHONG phai giong noi TTS that.

## Ke hoach sau nay

Muc 2: AI Director dung LLM de doc toan bo kich ban, de xuat canh, text,
camera va nhac theo ngu canh. Neu co image understanding model thi moi
xac dinh duoc dung vat the can zoom, can co che xac nhan truoc khi render.
Muc 3: tu review video, canh bao loi va quan ly queue render video dai.
