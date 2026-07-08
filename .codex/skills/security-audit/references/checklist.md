# ExamGuard Security Checklist

## Backend and API

- confirm every non-health endpoint requires auth as intended
- check seeded credentials and placeholder secrets are documented as non-production defaults
- validate server-side mapping for student/session identifiers instead of trusting UI-only data
- review payload parsing for oversized or malformed JSON
- confirm reports and exports do not leak secrets or internal paths unnecessarily

## Docker and Operations

- review exposed ports and confirm LAN-only assumptions remain documented
- verify `.env` values override insecure defaults in production
- confirm migrations are deterministic on a clean database
- avoid baking environment-specific secrets into images or committed files

## Teacher and Student Desktop

- review execute-command, remote input, clipboard, and file distribution as privileged actions
- verify target-aware actions cannot silently hit the wrong student
- review local file writes for path traversal or unsafe overwrite risk
- review screen capture and submission storage for unintended disclosure

## Socket Protocol

- confirm teacher validates the session token on connect
- review large frame/file handling for memory pressure or unbounded writes
- ensure rejection paths log enough context without leaking sensitive values
- note replay or spoofing assumptions when traffic stays on a trusted LAN
