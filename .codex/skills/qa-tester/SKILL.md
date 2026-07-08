---
name: qa-tester
description: Quality-assurance workflow for ExamGuard in F:\project01-2526. Use when Codex needs to verify desktop, backend, Docker, socket, submission, policy, or release behavior; reproduce a bug; add regression coverage; or summarize production readiness with concrete findings instead of only reporting that builds passed.
---

# QA Tester

Read [AGENTS.md](../../../AGENTS.md) first.

## Verification Workflow

1. Start from the changed surface:
   Backend work usually needs Docker health, migration, auth, session, and report checks.
   Desktop work usually needs `dotnet build`, protocol smoke tests, and a note about what still requires interactive Windows validation.

2. Prefer executable checks before manual conclusions:
   Run the smallest command or script that can confirm the claim.

3. Separate outcomes clearly:
   Distinguish build success, API success, simulator success, and unverified GUI behavior.

4. Record bugs as findings, not as vague risk:
   State the failing path, reproduction step, expected behavior, and actual behavior.

## ExamGuard-Specific Expectations

- Treat `scripts/test-backend-e2e.ps1` as the fast backend regression path after Docker or Yii2 changes.
- Re-run `desktop/ExamGuard.Protocol.SmokeTests` after protocol changes.
- When TeacherForm or StudentForm changes, call out whether the flow was only built or also exercised with simulator/manual runtime.
- Verify student/session mapping for violations and submissions whenever backend payloads or report fields change.

## References

Read [references/test-matrix.md](references/test-matrix.md) for the standard verification matrix.
Read [references/defect-template.md](references/defect-template.md) when reporting QA findings or release risk.
