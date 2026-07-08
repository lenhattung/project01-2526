# API Workflow

## Current Flows

- `POST /api/auth/login`: teacher auth and bearer token issue
- `GET /api/health`: backend liveness and DB reachability
- `GET/POST /api/classes`: class management
- `GET/POST /api/students`: student records
- `GET/POST /api/blocked-apps`: policy records
- `POST /api/exam-sessions`: create exam session
- `POST /api/exam-sessions/{id}/start`: mark running
- `POST /api/exam-sessions/{id}/finish`: mark finished
- `GET /api/exam-sessions/{id}/policy`: return session token and blocked policy
- `POST /api/exam-sessions/{id}/events`: record violations and other auditable events
- `POST /api/exam-sessions/{id}/submissions`: record submission metadata

## Contract Rules

- Keep request and response keys stable for desktop callers.
- Prefer explicit status strings over implicit booleans for session lifecycle.
- When adding a new teacher control action, decide whether it belongs only on the socket plane, only in MySQL, or in both.
