# ExamGuard Socket Protocol

TeacherForm acts as the TCP server. StudentForm connects over the classroom LAN.

## Framing

Each frame is:

```text
4-byte big-endian JSON header length
UTF-8 JSON header
optional binary payload
```

The header shape is:

```json
{
  "messageType": "SCREEN_FRAME",
  "sessionId": "EXAM-001",
  "studentId": "SV001",
  "machineName": "LAB-PC-01",
  "timestamp": "2026-07-06T06:00:00Z",
  "payloadLength": 12345,
  "metadata": {}
}
```

## Message Types

- `HELLO`: first Student message. Metadata must include `token`.
- `HELLO_ACK`: Teacher accepted the Student and may include policy metadata.
- `HEARTBEAT`: Student liveness update.
- `SCREEN_FRAME`: JPEG payload captured from the Student screen.
- `POLICY_UPDATE`: Teacher sends semicolon-separated `blockedProcesses` and `blockedWindowKeywords`.
- `PROCESS_VIOLATION`: Student reports a blocked process/window title and enforcement action.
- `SUBMISSION_START`: Student begins a zip submission.
- `SUBMISSION_CHUNK`: binary chunk of the zip file.
- `SUBMISSION_COMPLETE`: final metadata, including `sha256`.
- `CHAT_MESSAGE`: Teacher or Student text message.
- `ATTENTION`: Teacher notice shown as a Student popup.
- `LOCK_SCREEN`: Teacher lock overlay command.
- `UNLOCK_SCREEN`: Teacher unlock command.
- `EXECUTE_COMMAND`: Teacher command to execute on Student.
- `COMMAND_RESULT`: Student command result sent back to Teacher.
- `FILE_DISTRIBUTION_START`: Teacher begins sending a file.
- `FILE_DISTRIBUTION_CHUNK`: binary chunk of a teacher-distributed file.
- `FILE_DISTRIBUTION_COMPLETE`: teacher-distributed file complete.
- `TEACHER_FRAME`: JPEG payload for teacher screen broadcast.
- `REMOTE_MOUSE_CLICK`: Teacher remote click on the selected Student detail screen.
- `REMOTE_TEXT_INPUT`: Teacher sends text into the active Student window.
- `CLIPBOARD_SET`: Teacher sets text clipboard content on Student.
- `ERROR`: protocol or authorization failure.

## Security Notes

V1 validates a shared session token and is intended for trusted LAN use only.
Do not expose the Teacher socket port directly to the Internet.

## Discovery

TeacherForm broadcasts a UDP JSON `DiscoveryAnnouncement` on port `9091` every two seconds while a session is running. StudentForm uses it to populate teacher host, port, and session code before connecting.
