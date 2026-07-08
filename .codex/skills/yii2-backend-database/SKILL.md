---
name: yii2-backend-database
description: Architecture and business-safe guidance for ExamGuard Yii2 backend and MySQL. Use when Codex is designing or implementing API endpoints, request workflows, migrations, ActiveRecord models, Hostinger/shared-host backend behavior, data validation, security-sensitive schema changes, or exam business rules in this repository.
---

# Yii2 Backend Database

Read [AGENTS.md](../../../AGENTS.md) and inspect the current controller, model, and migration set before editing.

## Backend Workflow

1. Start from the business event:
   teacher login, class/session setup, policy retrieval, violation logging, submission metadata, or reporting.

2. Trace the full contract:
   route -> controller action -> model validation -> schema columns -> seed or migration impact -> desktop caller expectations.

3. Prefer additive migrations:
   do not rewrite prior schema history unless the user explicitly asks for destructive cleanup.

4. Keep business data and transport data separated:
   event logs and submission metadata belong in MySQL;
   continuous screen frames stay out of the database.

## Security Rules

- Require authenticated access for teacher-only resources.
- Validate input at controller and model level.
- Keep production secrets out of git; support either environment variables or `backend/config/local.php` for shared hosting.
- Treat execute-command, remote-control, and policy changes as auditable operations.

## ExamGuard-Specific Rules

- Session code and session token must remain aligned with TeacherForm and StudentForm defaults or configured values.
- Blocked app policy must stay easy to serialize into protocol metadata.
- Seed data should support local smoke usage without changing production assumptions.

## References

Read [references/api-workflow.md](references/api-workflow.md) for endpoint and desktop interaction mapping.
Read [references/schema-guidelines.md](references/schema-guidelines.md) for table design, migration style, and audit expectations.
