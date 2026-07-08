# Repository Guidelines

## Project Structure & Module Organization

This is a monorepo for a LAN exam-monitoring system.

- `backend/` contains the Yii2 REST API, models, config, and migrations.
- `desktop/TeacherForm/` contains the WinForms teacher console and TCP server.
- `desktop/StudentForm/` contains the WinForms student client, screen capture, process monitor, and submission sender.
- `desktop/ExamGuard.Protocol/` contains shared socket frame/message code.
- `shared/protocol/` and `docs/` contain protocol and operation documentation.
- `.codex/skills/` contains repo-local AI skills for implementation workflow, WinForms UI/UX, Yii2 backend/database work, QA/testing, and security audit workflow.

Keep feature code inside the owning subsystem and share only transport contracts through `ExamGuard.Protocol`.

## Build, Test, and Development Commands

Build the desktop solution:

```powershell
dotnet restore ExamGuard.sln
dotnet build ExamGuard.sln -nologo
dotnet run --project desktop\ExamGuard.Protocol.SmokeTests\ExamGuard.Protocol.SmokeTests.csproj -c Release --no-build
```

Run desktop apps locally:

```powershell
dotnet run --project desktop\TeacherForm\TeacherForm.csproj
dotnet run --project desktop\StudentForm\StudentForm.csproj
```

Backend commands, after PHP and Composer are installed:

```powershell
cd backend
composer install
Copy-Item config\local.php.example config\local.php
php yii migrate
php -S 127.0.0.1:8081 -t web web/router.php
```

Local agent skills:

```text
.codex/skills/project01-implementation
.codex/skills/winforms-ui-ux
.codex/skills/yii2-backend-database
.codex/skills/qa-tester
.codex/skills/security-audit
```

## Coding Style & Naming Conventions

Use C# nullable reference types and keep WinForms UI code in explicit form classes, not designer-generated logic when the layout is hand-built. Use PascalCase for C# types/methods, camelCase for local variables, and semicolon-separated policy strings only at UI boundaries.

For PHP/Yii2, use PSR-style namespaces under `app\`, one ActiveRecord per table, and controller names that match API resources.

## Testing Guidelines

At minimum, run `dotnet build ExamGuard.sln -nologo`, the protocol smoke-test command above, and `.\scripts\test-backend-e2e.ps1` after backend changes.

When adding tests, cover socket frame parsing, submission hash validation, policy parsing, and backend endpoint validation. Name tests by behavior, for example `RejectsStudentWithInvalidToken`.

## Commit & Pull Request Guidelines

The current history contains only an initial commit, so use short imperative commit messages:

```text
Add teacher socket dashboard
Fix submission hash validation
```

Pull requests should include a concise summary, testing notes, and screenshots or logs when behavior changes are visible. Link related issues when available, and call out any migration, configuration, or dependency changes.

## Agent-Specific Instructions

Inspect the repository before editing and keep changes scoped to the request. Do not introduce network/WAN behavior, admin-level lockdown, or new package dependencies without documenting setup and operational impact. Treat Hostinger shared hosting as the primary backend deployment target unless the user explicitly switches back to containers or VPS infrastructure.
