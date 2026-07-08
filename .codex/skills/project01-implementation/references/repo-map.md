# Repo Map

## Desktop

- `desktop/TeacherForm/`: teacher operator console, TCP server, LAN discovery broadcast, control commands, submissions, backend posting.
- `desktop/StudentForm/`: student client, capture, process monitor, teacher command execution, file receipt, remote input.
- `desktop/ExamGuard.Protocol/`: transport message names, framed protocol, policy snapshot, discovery payload.
- `desktop/StudentSimulator/`: multi-client simulator for LAN testing.
- `desktop/ExamGuard.Protocol.SmokeTests/`: no-package smoke tests for transport and serialization.

## Backend

- `backend/config/`: Yii2 web and console config, DB settings.
- `backend/controllers/api/`: auth, health, class, student, blocked-app, and exam-session endpoints.
- `backend/models/`: ActiveRecord models.
- `backend/migrations/`: MySQL schema and seed data.
- `backend/Dockerfile`, `docker-compose.yml`: production-like backend and MySQL startup.

## Verification

- `dotnet build ExamGuard.sln -c Release -nologo`
- `dotnet run --project desktop\ExamGuard.Protocol.SmokeTests\ExamGuard.Protocol.SmokeTests.csproj -c Release --no-build`
- `docker compose config`
- `powershell -ExecutionPolicy Bypass -File scripts\publish-desktop.ps1`

## Release Paths

- Desktop publish output: `artifacts/desktop/TeacherForm`, `artifacts/desktop/StudentForm`, `artifacts/desktop/StudentSimulator`
- Student teacher-file drop folder: `Documents\ExamGuard\TeacherFiles`
