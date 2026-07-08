<?php

namespace app\controllers\api;

use app\models\BlockedApp;
use app\models\ChatMessage;
use app\models\ExamEvent;
use app\models\ExamSession;
use app\models\Student;
use app\models\StudentMachine;
use app\models\Submission;
use Yii;
use yii\web\BadRequestHttpException;
use yii\web\NotFoundHttpException;
use yii\web\ServerErrorHttpException;

class ExamSessionsController extends BaseApiController
{
    public function actionIndex(): array
    {
        $sessions = ExamSession::find()
            ->orderBy(['id' => SORT_DESC])
            ->limit(50)
            ->all();

        return array_map(fn(ExamSession $session) => $this->serializeSession($session), $sessions);
    }

    public function actionCreate(): array
    {
        $model = new ExamSession();
        $model->load(Yii::$app->request->bodyParams, '');
        $model->status = ExamSession::STATUS_DRAFT;
        $model->session_token = Yii::$app->security->generateRandomString(48);
        $model->created_by = Yii::$app->user->id;
        $this->applyPolicyDefaults($model);

        if (!$model->save()) {
            throw new BadRequestHttpException(json_encode($model->errors));
        }

        return $this->serializeSession($model);
    }

    public function actionStart(int $id): array
    {
        $model = $this->findSession($id);
        $model->status = ExamSession::STATUS_RUNNING;
        $model->started_at = gmdate('Y-m-d H:i:s');
        $model->finished_at = null;
        if (!$model->save(false)) {
            throw new ServerErrorHttpException('Could not start exam session.');
        }
        return $this->serializeSession($model);
    }

    public function actionFinish(int $id): array
    {
        $model = $this->findSession($id);
        $model->status = ExamSession::STATUS_FINISHED;
        $model->finished_at = gmdate('Y-m-d H:i:s');
        if (!$model->save(false)) {
            throw new ServerErrorHttpException('Could not finish exam session.');
        }
        return $this->serializeSession($model);
    }

    public function actionPolicy(int $id): array
    {
        $session = $this->findSession($id);
        $rules = BlockedApp::find()
            ->where(['exam_session_id' => $session->id, 'is_active' => 1])
            ->all();

        return [
            'sessionId' => (int)$session->id,
            'sessionCode' => $session->code,
            'sessionToken' => $session->session_token,
            'blockedProcesses' => array_values(array_map(
                fn(BlockedApp $rule) => $rule->pattern,
                array_filter($rules, fn(BlockedApp $rule) => $rule->rule_type === 'process')
            )),
            'blockedWindowKeywords' => array_values(array_map(
                fn(BlockedApp $rule) => $rule->pattern,
                array_filter($rules, fn(BlockedApp $rule) => $rule->rule_type === 'window_title')
            )),
            'screenIntervalMs' => (int)$session->screen_interval_ms,
            'screenJpegQuality' => (int)$session->screen_jpeg_quality,
            'webcamEnabled' => (bool)$session->webcam_enabled,
            'webcamSnapshotOnConnect' => (bool)$session->webcam_snapshot_on_connect,
            'webcamIntervalMs' => $this->resolveWebcamIntervalMs($session),
            'webcamIntervalSeconds' => (int)$session->webcam_interval_seconds,
            'webcamJpegQuality' => (int)$session->webcam_jpeg_quality,
            'examDurationMinutes' => (int)$session->exam_duration_minutes,
            'allowSubmissionAfterDeadline' => (bool)$session->allow_submission_after_deadline,
            'startedAtUtc' => $session->started_at ? gmdate('c', strtotime($session->started_at . ' UTC')) : null,
            'finishedAtUtc' => $session->finished_at ? gmdate('c', strtotime($session->finished_at . ' UTC')) : null,
            'examEndAtUtc' => $this->computeExamEndAt($session),
            'connectionMode' => $session->connection_mode ?: 'lan',
            'remoteJoinEnabled' => (bool)$session->remote_join_enabled,
            'publishedHost' => $session->published_host,
            'publishedPort' => $session->published_port !== null ? (int)$session->published_port : null,
            'relayEnabled' => (bool)$session->relay_enabled,
            'relayHost' => $session->relay_host,
            'relayPort' => $session->relay_port !== null ? (int)$session->relay_port : null,
            'relaySecret' => $session->relay_secret,
            'teacherMachine' => $session->teacher_machine,
            'blockClipboardShortcuts' => (bool)$session->block_clipboard_shortcuts,
            'websitePolicyMode' => $session->website_policy_mode ?: 'allowlist',
            'allowedWebsiteHosts' => $this->splitPolicyList((string)$session->allowed_website_hosts),
        ];
    }

    public function actionUpdatePolicy(int $id): array
    {
        $session = $this->findSession($id);
        $body = Yii::$app->request->bodyParams;

        $session->screen_interval_ms = $body['screenIntervalMs'] ?? $body['screen_interval_ms'] ?? $session->screen_interval_ms;
        $session->screen_jpeg_quality = $body['screenJpegQuality'] ?? $body['screen_jpeg_quality'] ?? $session->screen_jpeg_quality;
        $session->webcam_enabled = $body['webcamEnabled'] ?? $body['webcam_enabled'] ?? $session->webcam_enabled;
        $session->webcam_snapshot_on_connect = $body['webcamSnapshotOnConnect'] ?? $body['webcam_snapshot_on_connect'] ?? $session->webcam_snapshot_on_connect;
        $session->webcam_interval_ms = $body['webcamIntervalMs'] ?? $body['webcam_interval_ms'] ?? $session->webcam_interval_ms;
        $session->webcam_interval_seconds = $body['webcamIntervalSeconds'] ?? $body['webcam_interval_seconds'] ?? $session->webcam_interval_seconds;
        if (array_key_exists('webcamIntervalMs', $body) || array_key_exists('webcam_interval_ms', $body)) {
            $intervalMs = (int)($body['webcamIntervalMs'] ?? $body['webcam_interval_ms'] ?? 0);
            $session->webcam_interval_seconds = $intervalMs <= 0 ? 0 : max(1, (int)ceil($intervalMs / 1000));
        }
        $session->webcam_jpeg_quality = $body['webcamJpegQuality'] ?? $body['webcam_jpeg_quality'] ?? $session->webcam_jpeg_quality;
        $session->exam_duration_minutes = $body['examDurationMinutes'] ?? $body['exam_duration_minutes'] ?? $session->exam_duration_minutes;
        $session->allow_submission_after_deadline = $body['allowSubmissionAfterDeadline'] ?? $body['allow_submission_after_deadline'] ?? $session->allow_submission_after_deadline;
        $session->block_clipboard_shortcuts = $body['blockClipboardShortcuts'] ?? $body['block_clipboard_shortcuts'] ?? $session->block_clipboard_shortcuts;
        $session->website_policy_mode = $body['websitePolicyMode'] ?? $body['website_policy_mode'] ?? $session->website_policy_mode;
        $allowedHosts = $body['allowedWebsiteHosts'] ?? $body['allowed_website_hosts'] ?? null;
        if ($allowedHosts !== null) {
            $session->allowed_website_hosts = is_array($allowedHosts) ? implode(';', $allowedHosts) : (string)$allowedHosts;
        }

        if (!$session->validate(['screen_interval_ms', 'screen_jpeg_quality', 'webcam_enabled', 'webcam_snapshot_on_connect', 'webcam_interval_ms', 'webcam_interval_seconds', 'webcam_jpeg_quality', 'exam_duration_minutes', 'allow_submission_after_deadline', 'block_clipboard_shortcuts', 'website_policy_mode', 'allowed_website_hosts'])) {
            throw new BadRequestHttpException(json_encode($session->errors));
        }

        if (!$session->save(false, ['screen_interval_ms', 'screen_jpeg_quality', 'webcam_enabled', 'webcam_snapshot_on_connect', 'webcam_interval_ms', 'webcam_interval_seconds', 'webcam_jpeg_quality', 'exam_duration_minutes', 'allow_submission_after_deadline', 'block_clipboard_shortcuts', 'website_policy_mode', 'allowed_website_hosts'])) {
            throw new ServerErrorHttpException('Could not update exam session policy.');
        }

        if (array_key_exists('blockedProcesses', $body) || array_key_exists('blocked_processes', $body)) {
            $processRules = $body['blockedProcesses'] ?? $body['blocked_processes'] ?? [];
            $this->replaceRules($session->id, 'process', is_array($processRules) ? $processRules : []);
        }

        if (array_key_exists('blockedWindowKeywords', $body) || array_key_exists('blocked_window_keywords', $body)) {
            $keywordRules = $body['blockedWindowKeywords'] ?? $body['blocked_window_keywords'] ?? [];
            $this->replaceRules($session->id, 'window_title', is_array($keywordRules) ? $keywordRules : []);
        }

        return $this->actionPolicy($id);
    }

    public function actionPublishAccess(int $id): array
    {
        $session = $this->findSession($id);
        $body = Yii::$app->request->bodyParams;

        $session->connection_mode = trim((string)($body['connectionMode'] ?? $body['connection_mode'] ?? $session->connection_mode));
        $session->published_host = trim((string)($body['publishedHost'] ?? $body['published_host'] ?? $session->published_host));
        $publishedPort = $body['publishedPort'] ?? $body['published_port'] ?? $session->published_port;
        $session->published_port = $publishedPort === null || $publishedPort === '' ? null : (int)$publishedPort;
        $session->teacher_machine = trim((string)($body['teacherMachine'] ?? $body['teacher_machine'] ?? $session->teacher_machine));
        $session->remote_join_enabled = $body['remoteJoinEnabled'] ?? $body['remote_join_enabled'] ?? $session->remote_join_enabled;
        $session->relay_enabled = $body['relayEnabled'] ?? $body['relay_enabled'] ?? $session->relay_enabled;
        $session->relay_host = trim((string)($body['relayHost'] ?? $body['relay_host'] ?? $session->relay_host));
        $relayPort = $body['relayPort'] ?? $body['relay_port'] ?? $session->relay_port;
        $session->relay_port = $relayPort === null || $relayPort === '' ? null : (int)$relayPort;
        $session->relay_secret = trim((string)($body['relaySecret'] ?? $body['relay_secret'] ?? $session->relay_secret));

        if (!$session->validate(['connection_mode', 'published_host', 'published_port', 'teacher_machine', 'remote_join_enabled', 'relay_enabled', 'relay_host', 'relay_port', 'relay_secret'])) {
            throw new BadRequestHttpException(json_encode($session->errors));
        }

        if (!$session->save(false, ['connection_mode', 'published_host', 'published_port', 'teacher_machine', 'remote_join_enabled', 'relay_enabled', 'relay_host', 'relay_port', 'relay_secret'])) {
            throw new ServerErrorHttpException('Could not publish exam session access.');
        }

        return $this->serializeSession($session);
    }

    public function actionReport(int $id): array
    {
        $session = $this->findSession($id);
        $events = ExamEvent::find()
            ->where(['exam_session_id' => $session->id])
            ->orderBy(['id' => SORT_DESC])
            ->limit(20)
            ->all();
        $submissions = Submission::find()
            ->where(['exam_session_id' => $session->id])
            ->orderBy(['id' => SORT_DESC])
            ->limit(20)
            ->all();
        $studentIds = array_values(array_unique(array_filter(array_merge(
            array_map(static fn(ExamEvent $event) => $event->student_id, $events),
            array_map(static fn(Submission $submission) => $submission->student_id, $submissions),
        ))));
        $studentMap = Student::find()
            ->where(['id' => $studentIds])
            ->indexBy('id')
            ->all();

        return [
            'sessionId' => (int)$session->id,
            'sessionCode' => $session->code,
            'status' => $session->status,
            'totalEvents' => (int)ExamEvent::find()->where(['exam_session_id' => $session->id])->count(),
            'totalSubmissions' => (int)Submission::find()->where(['exam_session_id' => $session->id])->count(),
            'recentEvents' => array_map(fn(ExamEvent $event) => [
                'studentId' => $event->student_id !== null ? (int)$event->student_id : null,
                'studentCode' => $event->student_id !== null && isset($studentMap[$event->student_id]) ? $studentMap[$event->student_id]->code : null,
                'eventType' => $event->event_type,
                'machineName' => $event->machine_name,
                'payloadJson' => $event->payload_json,
                'createdAt' => $event->created_at,
            ], $events),
            'recentSubmissions' => array_map(fn(Submission $submission) => [
                'studentId' => $submission->student_id !== null ? (int)$submission->student_id : null,
                'studentCode' => $submission->student_id !== null && isset($studentMap[$submission->student_id]) ? $studentMap[$submission->student_id]->code : null,
                'fileName' => $submission->file_name,
                'status' => $submission->status,
                'fileSize' => (int)$submission->file_size,
                'createdAt' => $submission->created_at,
            ], $submissions),
        ];
    }

    public function actionEvents(int $id): ExamEvent
    {
        $session = $this->findSession($id);
        $event = new ExamEvent();
        $body = Yii::$app->request->bodyParams;
        $event->load($body, '');
        $event->exam_session_id = $id;
        $event->student_id = $this->resolveStudentId($session, $body);
        $this->syncStudentMachine($event->student_id, $body);
        if (is_array($event->payload_json)) {
            $event->payload_json = json_encode($event->payload_json);
        }

        if (!$event->save()) {
            throw new BadRequestHttpException(json_encode($event->errors));
        }

        return $event;
    }

    public function actionSubmissions(int $id): Submission
    {
        $session = $this->findSession($id);
        $submission = new Submission();
        $body = Yii::$app->request->bodyParams;
        $submission->load($body, '');
        $submission->exam_session_id = $id;
        $submission->student_id = $this->resolveStudentId($session, $body);
        $this->syncStudentMachine($submission->student_id, $body);
        $submission->status = $submission->status ?: 'submitted';

        if (!$submission->save()) {
            throw new BadRequestHttpException(json_encode($submission->errors));
        }

        return $submission;
    }

    public function actionChatMessages(int $id): ChatMessage
    {
        $this->findSession($id);
        $message = new ChatMessage();
        $message->load(Yii::$app->request->bodyParams, '');
        $message->exam_session_id = $id;

        if (!$message->save()) {
            throw new BadRequestHttpException(json_encode($message->errors));
        }

        return $message;
    }

    private function findSession(int $id): ExamSession
    {
        $model = ExamSession::findOne($id);
        if ($model === null) {
            throw new NotFoundHttpException('Exam session not found.');
        }
        return $model;
    }

    private function serializeSession(ExamSession $session): array
    {
        return [
            'id' => (int)$session->id,
            'code' => $session->code,
            'title' => $session->title,
            'status' => $session->status,
            'sessionToken' => $session->session_token,
            'screenIntervalMs' => (int)$session->screen_interval_ms,
            'screenJpegQuality' => (int)$session->screen_jpeg_quality,
            'webcamEnabled' => (bool)$session->webcam_enabled,
            'webcamSnapshotOnConnect' => (bool)$session->webcam_snapshot_on_connect,
            'webcamIntervalMs' => $this->resolveWebcamIntervalMs($session),
            'webcamIntervalSeconds' => (int)$session->webcam_interval_seconds,
            'webcamJpegQuality' => (int)$session->webcam_jpeg_quality,
            'examDurationMinutes' => (int)$session->exam_duration_minutes,
            'allowSubmissionAfterDeadline' => (bool)$session->allow_submission_after_deadline,
            'startedAtUtc' => $session->started_at ? gmdate('c', strtotime($session->started_at . ' UTC')) : null,
            'finishedAtUtc' => $session->finished_at ? gmdate('c', strtotime($session->finished_at . ' UTC')) : null,
            'examEndAtUtc' => $this->computeExamEndAt($session),
            'connectionMode' => $session->connection_mode ?: 'lan',
            'remoteJoinEnabled' => (bool)$session->remote_join_enabled,
            'publishedHost' => $session->published_host,
            'publishedPort' => $session->published_port !== null ? (int)$session->published_port : null,
            'relayEnabled' => (bool)$session->relay_enabled,
            'relayHost' => $session->relay_host,
            'relayPort' => $session->relay_port !== null ? (int)$session->relay_port : null,
            'relaySecret' => $session->relay_secret,
            'teacherMachine' => $session->teacher_machine,
            'blockClipboardShortcuts' => (bool)$session->block_clipboard_shortcuts,
            'websitePolicyMode' => $session->website_policy_mode ?: 'allowlist',
            'allowedWebsiteHosts' => $this->splitPolicyList((string)$session->allowed_website_hosts),
        ];
    }

    private function replaceRules(int $sessionId, string $ruleType, array $patterns): void
    {
        BlockedApp::deleteAll([
            'exam_session_id' => $sessionId,
            'rule_type' => $ruleType,
        ]);

        foreach (array_values(array_unique(array_filter(array_map('trim', $patterns), static fn(string $value) => $value !== ''))) as $pattern) {
            $rule = new BlockedApp([
                'exam_session_id' => $sessionId,
                'rule_type' => $ruleType,
                'pattern' => $pattern,
                'is_active' => 1,
            ]);

            if (!$rule->save()) {
                throw new BadRequestHttpException(json_encode($rule->errors));
            }
        }
    }

    private function applyPolicyDefaults(ExamSession $session): void
    {
        $session->screen_interval_ms = $session->screen_interval_ms ?: 2000;
        $session->screen_jpeg_quality = $session->screen_jpeg_quality ?: 40;
        $session->webcam_enabled = $session->webcam_enabled ?? true;
        $session->webcam_snapshot_on_connect = $session->webcam_snapshot_on_connect ?? true;
        $session->webcam_interval_ms = $session->webcam_interval_ms ?: ($session->webcam_interval_seconds > 0 ? ((int)$session->webcam_interval_seconds * 1000) : 500);
        $session->webcam_interval_seconds = $session->webcam_interval_seconds ?: 0;
        $session->webcam_jpeg_quality = $session->webcam_jpeg_quality ?: 55;
        $session->connection_mode = $session->connection_mode ?: 'lan';
        $session->remote_join_enabled = $session->remote_join_enabled ?? false;
        $session->relay_enabled = $session->relay_enabled ?? false;
        $session->exam_duration_minutes = $session->exam_duration_minutes ?: 0;
        $session->allow_submission_after_deadline = $session->allow_submission_after_deadline ?? false;
        $session->block_clipboard_shortcuts = $session->block_clipboard_shortcuts ?? true;
        $session->website_policy_mode = $session->website_policy_mode ?: 'allowlist';
    }

    private function resolveStudentId(ExamSession $session, array $body): ?int
    {
        $studentId = isset($body['student_id']) ? (int)$body['student_id'] : null;
        if ($studentId !== null && $studentId > 0) {
            return $studentId;
        }

        $studentCode = trim((string)($body['student_code'] ?? $body['studentCode'] ?? ''));
        if ($studentCode === '') {
            return null;
        }

        $student = Student::find()
            ->where([
                'class_id' => $session->class_id,
                'code' => $studentCode,
            ])
            ->one();

        if ($student !== null && $student->id !== null) {
            return (int)$student->id;
        }

        $student = new Student([
            'class_id' => (int)$session->class_id,
            'code' => $studentCode,
            'full_name' => trim((string)($body['student_name'] ?? $body['studentName'] ?? $studentCode)),
        ]);

        if (!$student->save()) {
            throw new BadRequestHttpException(json_encode($student->errors));
        }

        return (int)$student->id;
    }

    private function syncStudentMachine(?int $studentId, array $body): void
    {
        if ($studentId === null || $studentId <= 0) {
            return;
        }

        $machineName = trim((string)($body['machine_name'] ?? $body['machineName'] ?? ''));
        if ($machineName === '') {
            return;
        }

        $ipAddress = trim((string)($body['ip_address'] ?? $body['ipAddress'] ?? ''));
        $machine = StudentMachine::find()
            ->where([
                'student_id' => $studentId,
                'machine_name' => $machineName,
            ])
            ->one();

        if ($machine === null) {
            $machine = new StudentMachine([
                'student_id' => $studentId,
                'machine_name' => $machineName,
            ]);
        }

        if ($ipAddress !== '') {
            $machine->ip_address = $ipAddress;
        }

        if (!$machine->save()) {
            throw new BadRequestHttpException(json_encode($machine->errors));
        }
    }

    private function computeExamEndAt(ExamSession $session): ?string
    {
        if (empty($session->started_at) || (int)$session->exam_duration_minutes <= 0) {
            return null;
        }

        $started = strtotime($session->started_at . ' UTC');
        if ($started === false) {
            return null;
        }

        return gmdate('c', $started + ((int)$session->exam_duration_minutes * 60));
    }

    private function resolveWebcamIntervalMs(ExamSession $session): int
    {
        if (!empty($session->webcam_interval_ms)) {
            return (int)$session->webcam_interval_ms;
        }

        if ((int)$session->webcam_interval_seconds <= 0) {
            return 0;
        }

        return min(10000, max(250, (int)$session->webcam_interval_seconds * 1000));
    }

    private function splitPolicyList(string $value): array
    {
        return array_values(array_filter(array_map('trim', explode(';', $value)), static fn(string $item) => $item !== ''));
    }
}
