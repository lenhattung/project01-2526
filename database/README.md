# Database

Yii2 migrations live in `backend/migrations`.

The initial migration creates:

- `users`
- `classes`
- `students`
- `student_machines`
- `exam_sessions`
- `blocked_apps`
- `exam_events`
- `submissions`

Use `php yii migrate` from `backend/` after installing Composer dependencies.
