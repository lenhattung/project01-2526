---
name: project01-implementation
description: Repo-specific workflow for building, reviewing, and extending ExamGuard in F:\project01-2526. Use when Codex is asked to implement features, fix bugs, improve UX, adjust backend or Hostinger deployment behavior, or continue roadmap phases in this repository so work stays aligned with AGENTS.md, local skills, WinForms patterns, and Yii2/MySQL business rules.
---

# Project01 Implementation

Read [AGENTS.md](../../../AGENTS.md) first.

## Core Workflow

1. Inspect the real subsystem before editing:
   Desktop work usually starts in `desktop/TeacherForm`, `desktop/StudentForm`, and `desktop/ExamGuard.Protocol`.
   Backend work usually starts in `backend/config`, `backend/controllers/api`, `backend/models`, and `backend/migrations`.

2. Match the task to the owning layer:
   Use [references/repo-map.md](references/repo-map.md) for the current module map and verification commands.

3. Pull in the specialized skill when needed:
   Read [../winforms-ui-ux/SKILL.md](../winforms-ui-ux/SKILL.md) for TeacherForm or StudentForm layout, theming, operator workflow, and WinForms interaction work.
   Read [../yii2-backend-database/SKILL.md](../yii2-backend-database/SKILL.md) for API, schema, migration, security, and business workflow changes.

4. Keep changes production-oriented:
   Build the desktop solution after desktop edits.
   Run protocol smoke tests after protocol or transport edits.
   Run `.\scripts\test-backend-e2e.ps1` after backend contract changes when the API is available.

## Delivery Rules

- Preserve the classroom LAN model unless the user explicitly expands scope.
- Keep transport contracts in `ExamGuard.Protocol`; do not duplicate message definitions in Teacher or Student apps.
- Prefer additive feature work over rewrites; extend existing forms and services first.
- Update repo docs when behavior, setup, or release workflow changes.

## References

Read [references/repo-map.md](references/repo-map.md) for subsystem ownership, verification commands, and release paths.
Read [references/phase-checklist.md](references/phase-checklist.md) when continuing roadmap phases and deciding what "done" means for the current increment.
