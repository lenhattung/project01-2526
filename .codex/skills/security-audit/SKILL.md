---
name: security-audit
description: Security review workflow for ExamGuard in F:\project01-2526. Use when Codex is changing or reviewing Yii2 APIs, Docker or environment configuration, socket protocol behavior, WinForms remote-control features, file transfer, authentication, or production hardening and needs to identify concrete security weaknesses, abuse paths, and mitigation steps.
---

# Security Audit

Read [AGENTS.md](../../../AGENTS.md) first.

## Audit Workflow

1. Identify the trust boundary first:
   Teacher machine, student machine, LAN socket, backend API, MySQL, and exported files are different risk zones.

2. Review the real data path:
   Check who can send the message, what gets trusted, what is persisted, and what can execute on the target machine.

3. Prioritize findings by exploitability:
   Authentication bypass, arbitrary command execution, file overwrite, token leakage, and unsafe production defaults come first.

4. Recommend mitigations that fit the current architecture:
   Prefer token validation, input validation, storage-path constraints, safer defaults, and documented operator controls before proposing heavy redesign.

## ExamGuard-Specific Focus Areas

- Teacher-to-student control features are high risk: execute command, clipboard set, remote input, broadcast, and file distribution.
- Submission and distribution paths must stay inside intended directories and should not trust client-provided paths.
- Docker and Yii2 checks should include secret defaults, exposed ports, auth requirements, and migration safety.
- Socket checks should include session token validation, replay assumptions, oversized payload handling, and error logging quality.
- When a security issue is not fixed in the current change, document the residual risk and the operational workaround clearly.

## References

Read [references/checklist.md](references/checklist.md) for the audit checklist by subsystem.
Read [references/findings-template.md](references/findings-template.md) when writing security findings or hardening notes.
