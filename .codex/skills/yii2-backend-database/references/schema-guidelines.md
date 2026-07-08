# Schema Guidelines

## Table Intent

- `users`: teacher identities and auth token
- `classes`: classroom grouping
- `students`: student identity inside a class
- `student_machines`: machine assignment and IP context
- `exam_sessions`: exam lifecycle and session token
- `blocked_apps`: process or title policy rules
- `exam_events`: auditable event trail
- `submissions`: submission metadata and storage location

## Migration Rules

- Use additive migrations with clear foreign keys.
- Seed only the minimum safe local bootstrap data.
- Keep file payloads out of MySQL; store metadata and paths only.
- Add indexes when a new query path or report needs them.
