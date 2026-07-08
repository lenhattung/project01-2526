---
name: winforms-ui-ux
description: Design and implementation guidance for polished WinForms UI in ExamGuard. Use when Codex is improving TeacherForm or StudentForm layout, theming, controls, ergonomics, discoverability, readability, workflow speed, or overall visual quality so the apps feel modern, friendly, and professional without breaking existing classroom operations.
---

# WinForms UI UX

Read [AGENTS.md](../../../AGENTS.md) and then inspect the current form before changing layout.

## Design Workflow

1. Preserve the operator's real task flow:
   TeacherForm is a control room.
   StudentForm is a focused client with minimal distraction.

2. Prefer structural improvements over decoration:
   Improve spacing, grouping, alignment, contrast, and action hierarchy before adding visual effects.

3. Use a consistent theme:
   Keep shared colors, borders, fonts, and button states centralized when possible.

4. Protect usability under load:
   Teacher controls must stay legible with many students, long logs, and narrow windows.
   Student controls must remain simple during stress conditions such as exam start or submission time.

## ExamGuard-Specific Rules

- TeacherForm should prioritize status visibility: session, online state, violations, submissions, and current target.
- StudentForm should prioritize connection state, exam submission, and teacher messages.
- Keep primary actions obvious and secondary actions quieter.
- Avoid modal-heavy workflows on TeacherForm unless the action is safety-sensitive.
- Treat remote-control and execute-command features as high-risk; make them explicit and target-aware.

## References

Read [references/theme-guidelines.md](references/theme-guidelines.md) for palette, spacing, and control behavior.
Read [references/layout-guidelines.md](references/layout-guidelines.md) for TeacherForm and StudentForm layout heuristics.
