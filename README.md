# ExamGuard

ExamGuard là hệ thống giám sát phòng thi/lớp học theo mô hình Teacher - Student, lấy cảm hứng từ NetOp School. Project hiện tại ưu tiên triển khai production bằng Docker Compose trên VPS: MySQL, Yii2 backend và TCP relay chạy trên server; TeacherForm và StudentForm chạy trên máy Windows của giáo viên/sinh viên.

## Kiến Trúc Hiện Tại

```text
├─ MySQL 8.4                 Chỉ publish localhost 127.0.0.1:3307
├─ Yii2 Backend               Public HTTP :8081
└─ ExamGuard Relay TCP        Public TCP :9090

Windows TeacherForm           Đăng nhập backend, mở phiên, kết nối relay
Windows StudentForm           Nhập mã phiên/mã bảo vệ/mã SV, tự lookup backend và kết nối relay
```

Luồng cùng mạng LAN vẫn hoạt động bằng discovery nội bộ. Luồng khác Wi-Fi/từ xa dùng VPS relay, không cần public IP hoặc port forwarding máy giáo viên.

## Cấu Trúc Repo

```text
backend/                         Yii2 REST API, Dockerfile, config, models, migrations
desktop/TeacherForm/             WinForms app máy giáo viên
desktop/StudentForm/             WinForms app máy sinh viên
desktop/ExamGuard.Protocol/      Shared socket protocol
desktop/StudentSimulator/        Tool mô phỏng nhiều student
services/ExamGuard.Relay/        TCP relay service chạy trên VPS
scripts/                         Script publish desktop/package
docker-compose.yml               Chạy MySQL + backend + relay
.env                             Cấu hình production/local Docker
```

## Yêu Cầu

- VPS Linux đã cài Docker và Docker Compose plugin.
- Windows 10/11 cho TeacherForm và StudentForm.
- .NET 8 SDK nếu build desktop từ source.
- Port VPS cần mở:
  - `8081/tcp` cho backend API.
  - `9090/tcp` cho relay.
- Không mở MySQL ra Internet. Giữ `MYSQL_PUBLISHED_PORT=127.0.0.1:3307`.

## Cấu Hình `.env`

File `.env` là nguồn cấu hình chính khi chạy Docker Compose:

```env
MYSQL_DATABASE=
MYSQL_USER=
MYSQL_PASSWORD=<random-secret>
MYSQL_ROOT_PASSWORD=<random-root-secret>
MYSQL_PUBLISHED_PORT=127.0.0.1:3307

DB_DSN=mysql:host=mysql;dbname=;charset=utf8mb4
DB_USERNAME=
DB_PASSWORD=<giống MYSQL_PASSWORD>
COOKIE_VALIDATION_KEY=<random-cookie-key>

BACKEND_PUBLISHED_PORT=8081
RELAY_PORT=9090
RELAY_PUBLISHED_PORT=9090
RELAY_SHARED_SECRET=<random-relay-secret>
RELAY_MAX_PAYLOAD_BYTES=52428800
```

Lưu ý: `RELAY_SHARED_SECRET` phải khớp với secret ẩn trong TeacherForm bản build đang dùng. Nếu đổi secret trong `.env`, cần rebuild/publish lại TeacherForm hoặc chuyển secret sang cấu hình ngoài ở phase sau.

## Deploy Backend + Database + Relay Trên VPS

Trên VPS:

```bash
git clone <repo-url> examguard
cd examguard
cp .env.example .env
nano .env
docker compose up -d --build
docker compose --profile tools run --rm migrate
```

Kiểm tra service:

```bash
docker compose ps
curl http://IP:8081/api/health
```

Kiểm tra service có trong compose:

```bash
docker compose config --services
```

Kết quả mong muốn:

```text
mysql
backend
relay
```

## Cập Nhật Production Sau Khi Pull Code Mới

```bash
cd examguard
git pull
docker compose up -d --build
docker compose --profile tools run --rm migrate
```

Nếu chỉ đổi code relay/backend, không cần xóa volume MySQL.

Không dùng lệnh xóa volume nếu database đã có dữ liệu thật. Chỉ recreate volume khi đang test và chấp nhận mất dữ liệu.

## Build Desktop Apps

Trên Windows:

```powershell
dotnet restore ExamGuard.sln
dotnet build ExamGuard.sln -c Release -nologo
dotnet run --project desktop\ExamGuard.Protocol.SmokeTests\ExamGuard.Protocol.SmokeTests.csproj -c Release --no-build
```

Publish bản chạy:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-desktop.ps1
```

Output:

```text
artifacts\desktop\TeacherForm\TeacherForm.exe
artifacts\desktop\StudentForm\StudentForm.exe
artifacts\desktop\StudentSimulator\StudentSimulator.exe
```

## Chạy TeacherForm

Mở:

```text
artifacts\desktop\TeacherForm\TeacherForm.exe
```

Luồng sử dụng:

1. Nhấn `Đăng nhập máy chủ`.
2. Tải/chọn phiên thi.
3. Chọn kiểu kết nối:
   - `Cùng mạng LAN` nếu phòng máy cùng Wi-Fi/LAN.
   - `Qua máy chủ relay` nếu student khác mạng Wi-Fi/từ xa.
4. Nhấn `Mở phiên` nếu phiên backend chưa running.
5. Nhấn `Bắt đầu phiên`.

Backend URL, relay host, relay port đang được ẩn khỏi UI và dùng mặc định:

```text
Backend: http://IP:8081
Relay:   IP:9090
```

TeacherForm có thể:

- Giám sát màn hình sinh viên.
- Giám sát webcam sinh viên, tự nhận lại webcam nếu student bật webcam muộn.
- Chặn copy/paste theo policy.
- Chặn phần mềm/từ khóa website theo policy.
- Chat với một sinh viên hoặc toàn bộ sinh viên.
- Nhận giơ tay/cảnh báo mất kết nối.
- Phát file/folder đề thi.
- Nhận bài nộp dạng `.zip`.
- Ghi event/submission/chat metadata xuống backend/MySQL.

## Chạy StudentForm

Mở:

```text
artifacts\desktop\StudentForm\StudentForm.exe
```

Sinh viên nhập:

```text
Mã phiên
Mã bảo vệ
Mã sinh viên
Tên sinh viên
```

Sau đó nhấn một trong hai nút:

- `Tìm/Kết nối trong LAN` nếu cùng Wi-Fi/LAN.
- `Tìm/Kết nối khác mạng` nếu khác mạng Wi-Fi; app lookup backend và tự kết nối đến relay VPS.
- Student không cần nhập IP giáo viên, port giáo viên hoặc IP relay.

## Test Nhanh End-to-End

1. Trên VPS chạy:

```bash
docker compose up -d --build
docker compose --profile tools run --rm migrate
```

2. Mở TeacherForm, đăng nhập máy chủ, mở phiên, bắt đầu phiên.
3. Mở StudentForm, nhập mã phiên/mã bảo vệ/mã sinh viên, kết nối.
4. Trên TeacherForm kiểm tra:
   - Card sinh viên xuất hiện.
   - Trạng thái `Trực tuyến`.
   - Màn hình cập nhật.
   - Webcam cập nhật nếu webcam khả dụng.
5. Test nộp bài bằng file/folder. Teacher nhận file `.zip` tại Desktop:

```text
ExamGuard - Bai nop sinh vien
```

## Test 50 Student Giả Lập

```powershell
dotnet run --project desktop\StudentSimulator\StudentSimulator.csproj -- --count 50
```

Dùng để kiểm tra heartbeat, frame giả lập và tải dashboard. Test webcam thật vẫn cần máy Windows có webcam thật.

## Ghi Chú Bảo Mật

- Không commit `.env` thật lên Git public.
- Không đổi `MYSQL_PUBLISHED_PORT` thành IP public VPS.
- Chỉ public backend `8081` và relay `9090`.
- Đổi toàn bộ secret trước production thật.
- Không hard-code secret production lâu dài trong desktop app. Phase sau nên chuyển relay secret sang file cấu hình local được đóng gói riêng.
- Remote command/remote input là tính năng nhạy cảm, chỉ dùng trong môi trường thi/lab có kiểm soát.

## Troubleshooting

- Backend không health check được: kiểm tra `docker compose ps`, log `examguard-backend`, và port `8081`.
- Student khác Wi-Fi không vào được: kiểm tra relay container, firewall port `9090`, và `RELAY_SHARED_SECRET`.
- Backend lên nhưng login lỗi DB: kiểm tra `DB_PASSWORD` có khớp `MYSQL_PASSWORD` không.
- Đổi password MySQL sau khi volume đã tồn tại không tự đổi user password trong DB cũ. Cần cập nhật trong MySQL hoặc recreate volume nếu chỉ là môi trường test.
- Console PowerShell có thể hiển thị tiếng Việt bị mojibake; ưu tiên xem file bằng IDE/editor UTF-8.
