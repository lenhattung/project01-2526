# ExamGuard Test Matrix

## Backend

- `docker compose config`
- `docker compose up -d mysql backend`
- `docker compose --profile tools run --rm migrate`
- `.\scripts\test-backend-e2e.ps1`

Verify:
- login returns a bearer token
- session list and policy load with that token
- session lifecycle moves `draft -> running -> finished`
- report shows event and submission totals
- recent event/submission rows resolve the expected student code

## Desktop

- `dotnet build ExamGuard.sln -c Release -nologo`
- `dotnet run --project desktop\ExamGuard.Protocol.SmokeTests\ExamGuard.Protocol.SmokeTests.csproj -c Release --no-build`
- `.\scripts\publish-desktop.ps1`

Verify:
- build is clean
- protocol framing still passes
- publish output lands in `artifacts\desktop`

## Interactive Runtime

Use when changes touch TeacherForm, StudentForm, or socket orchestration.

Suggested pass:
- start TeacherForm against the Docker backend
- load session policy from backend
- connect StudentForm or `StudentSimulator`
- confirm online state, frames, policy push, and submission status

Record separately which flows were automated and which were only manually spot-checked.
