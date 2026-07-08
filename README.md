# ExamGuard

ExamGuard is a LAN classroom exam-monitoring system inspired by NetOp School. It includes:

- `TeacherForm`: teacher console, LAN socket server, student mosaic, control actions, reports.
- `StudentForm`: student client, screen capture, process monitoring, teacher command receiver, submission client.
- `ExamGuard.Protocol`: shared TCP framing, policy metadata, and LAN discovery payloads.
- `backend/`: Yii2 REST API with MySQL and Hostinger-ready deployment support.
- `services/ExamGuard.Relay/`: TCP relay service foundation for VPS-based remote exams.

## Repository Layout

```text
backend/                           Yii2 REST API, Dockerfile, config, models, migrations
desktop/TeacherForm/               Teacher WinForms app
desktop/StudentForm/               Student WinForms app
desktop/ExamGuard.Protocol/        Shared socket protocol library
desktop/StudentSimulator/          LAN simulator for multiple students
desktop/ExamGuard.Protocol.SmokeTests/ smoke tests
services/ExamGuard.Relay/          VPS relay socket service
docs/                              setup and roadmap docs
scripts/                           release and automation scripts
.codex/skills/                     repo-local agent skills
```

## Prerequisites

1. Windows 10 or 11 for desktop apps.
2. .NET 8 SDK or newer with Windows Desktop support.
3. PHP 8.2+ and Composer for the Yii2 backend.
4. Local LAN access between teacher and student machines.

## Step 1: Build and verify desktop code

```powershell
dotnet restore ExamGuard.sln
dotnet build ExamGuard.sln -c Release -nologo
dotnet run --project desktop\ExamGuard.Protocol.SmokeTests\ExamGuard.Protocol.SmokeTests.csproj -c Release --no-build
```

Expected result:

- solution builds with `0 Error(s)`
- smoke test prints `ExamGuard.Protocol smoke tests passed.`

## Step 2: Run backend locally without Docker

Install backend dependencies:

```powershell
cd backend
composer install
Copy-Item config\local.php.example config\local.php
php yii migrate
php -S 127.0.0.1:8081 -t web web/router.php
```

Edit `backend\config\local.php` with your real database and cookie key values before migration.

Backend health check:

```text
http://127.0.0.1:8081/api/health
```

Backend regression check:

```powershell
.\scripts\test-backend-e2e.ps1
```

Hostinger deployment guide:

```text
docs/hostinger-deploy.md
docs/deploy-hostinger-vi.md
docs/chay-local.md
```

VPS Docker deployment guide:

```text
docs/deploy-vps-docker-vi.md
docs/deploy-vps-git-docker-compose-vi.md
docs/ket-noi-teacher-student-khac-mang-wifi.md
```

## Step 3: Use seeded backend defaults

Default teacher account:

- Username: `teacher`
- Password: `teacher123`

Default backend session:

- Backend session id: `1`
- Session code: `EXAM-001`
- Socket token: `classroom-token`

Each login issues a new bearer token for the same user. If you test APIs manually, reuse the latest token for all follow-up requests.

The seeded backend policy includes Task Manager, Zalo, Messenger, ChatGPT, Claude, and Gemini-related blocking rules.

## Step 4: Run TeacherForm

Start TeacherForm:

```powershell
dotnet run --project desktop\TeacherForm\TeacherForm.csproj
```

Inside TeacherForm:

1. Enter backend URL `http://127.0.0.1:8081` for local work or `https://project1.titv.vn` after Hostinger deployment.
2. Login with `teacher / teacher123`.
3. Click `Refresh Sessions` to load backend sessions.
4. Select the session `EXAM-001 | Default Exam Session | draft` or use backend session id `1`.
5. Click `Backend Start` if you want the backend lifecycle to move to `running`.
6. Click `Load Policy` to pull session code, token, and block rules from the backend.
7. Click `Start Session` to start the TCP teacher server on port `9090`.

TeacherForm can now:

- monitor student screens
- monitor student webcam snapshots and optional periodic webcam frames
- send chat and attention messages
- lock and unlock screens
- execute commands
- distribute files
- broadcast teacher screen
- send remote click, remote text, and clipboard text
- export logs and backend session reports
- start and finish backend exam sessions from the desktop UI

## Step 5: Run StudentForm

Start StudentForm:

```powershell
dotnet run --project desktop\StudentForm\StudentForm.csproj
```

Inside StudentForm:

1. Click `Discover` to find the teacher on the LAN, or enter host and port manually.
2. Confirm session `EXAM-001` and token `classroom-token`.
3. Click `Connect`.
4. Keep the app open during the exam.
5. Use `Submit Folder` to send the answer folder to TeacherForm.

StudentForm receives:

- policy updates
- teacher chat
- attention popup
- lock overlay
- teacher screen broadcast
- distributed files
- remote click, text input, and clipboard updates

Teacher policy also controls:

- screen capture interval and JPEG quality
- webcam enabled or disabled
- one webcam snapshot when the student connects
- optional periodic webcam monitoring

If a webcam is unavailable or broken, the student can still stay connected and continue the exam. TeacherForm records the webcam status instead of blocking the workstation.

## Step 6: Simulate multiple students locally

For local testing on one machine:

```powershell
dotnet run --project desktop\StudentSimulator\StudentSimulator.csproj -- --count 5
```

This simulates five student clients sending heartbeats and screen frames to TeacherForm.

## Step 7: Publish desktop release artifacts

```powershell
.\scripts\publish-desktop.ps1
```

Output folder:

```text
artifacts\desktop\TeacherForm
artifacts\desktop\StudentForm
artifacts\desktop\StudentSimulator
```

## Step 8: Package backend for Hostinger

After backend dependencies are installed locally:

```powershell
.\scripts\package-backend-hostinger.ps1
```

If `backend\vendor` is not available locally yet:

```powershell
.\scripts\package-backend-hostinger.ps1 -SourceOnly
```

Package output:

```text
artifacts\backend-hostinger\backend\
artifacts\backend-hostinger\public_html\
artifacts\backend-hostinger\backend-hostinger.zip
```

## Local Agent Skills

Repo-local skills live in `.codex/skills/`:

- `project01-implementation`
- `winforms-ui-ux`
- `yii2-backend-database`
- `qa-tester`
- `security-audit`

## Production Notes

- Open inbound TCP `9090` on the teacher machine firewall.
- Keep the teacher socket on a trusted LAN only.
- Do not expose teacher socket control ports directly to the Internet.
- Replace `backend\config\local.php` placeholder secrets before real deployment.
- Student file distribution lands in `Documents\ExamGuard\TeacherFiles`.
- Teacher exports logs and reports into the app `Exports` folder.
- Deploy only the Yii2 backend to Hostinger. TeacherForm and StudentForm still run on Windows endpoints in the exam LAN.
- For VPS production, use Docker Compose from `docker-compose.yml` to run MySQL, backend, and the optional `relay` profile.
- The relay service is the recommended next architecture for remote exams without public IP on the teacher machine. WebRTC/STUN/TURN should be treated as a later, larger media architecture phase.
- Image transport between TeacherForm and StudentForm uses binary JPEG payloads over the socket. Base64 is intentionally avoided for the default path because it increases bandwidth and CPU cost.
- For classrooms around 50 machines, start with screen interval `2000-3000 ms`, screen JPEG quality `35-45`, webcam snapshot on connect enabled, and periodic webcam interval disabled or set to `15-30 s` if needed.

## Troubleshooting

- If Hostinger returns `ExamGuard backend root is not configured.`, upload the backend to the expected sibling path or edit `public_html\index.php` to the real backend folder.
- If Student discovery does not find the teacher, enter the teacher IP manually and verify UDP/TCP firewall rules.
- If backend login works but policy load fails, confirm migration ran and backend session id `1` exists.
- If remote command or remote input is blocked, run StudentForm with sufficient Windows permissions on the student machine.
