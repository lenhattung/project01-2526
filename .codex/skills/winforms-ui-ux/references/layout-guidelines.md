# Layout Guidelines

## TeacherForm

- Top toolbar: session and backend setup first, then control actions, then status.
- Main area: student mosaic and detail preview must stay dominant.
- Bottom area: student table and event log should support monitoring without hiding critical actions.

## StudentForm

- Keep the first row compact: discovery, connect, session, and submission.
- Keep teacher-driven interactions readable but secondary to exam focus.
- Use overlays only for explicit teacher control states such as lock mode.

## Interaction Rules

- If a command can target one student or all students, the UI should make that distinction discoverable from selection state.
- Whenever the teacher sends content-driven actions, reuse the same message input where practical to reduce clutter.
