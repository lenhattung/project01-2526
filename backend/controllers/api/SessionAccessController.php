<?php

namespace app\controllers\api;

use app\models\BlockedApp;
use app\models\ExamSession;
use Yii;
use yii\filters\Cors;
use yii\rest\Controller;
use yii\web\BadRequestHttpException;
use yii\web\NotFoundHttpException;

class SessionAccessController extends Controller
{
    public $enableCsrfValidation = false;

    public function behaviors(): array
    {
        $behaviors = parent::behaviors();
        $behaviors['corsFilter'] = [
            'class' => Cors::class,
        ];

        return $behaviors;
    }

    public function actionLookup(): array
    {
        $body = Yii::$app->request->bodyParams;
        $sessionCode = trim((string)($body['sessionCode'] ?? $body['session_code'] ?? ''));
        $sessionToken = trim((string)($body['sessionToken'] ?? $body['session_token'] ?? ''));

        if ($sessionCode === '' || $sessionToken === '') {
            throw new BadRequestHttpException('Session code and password are required.');
        }

        $session = ExamSession::find()
            ->where([
                'code' => $sessionCode,
                'session_token' => $sessionToken,
                'status' => ExamSession::STATUS_RUNNING,
            ])
            ->one();

        if ($session === null) {
            throw new NotFoundHttpException('Session not found or not running.');
        }

        $rules = BlockedApp::find()
            ->where(['exam_session_id' => $session->id, 'is_active' => 1])
            ->all();
        $rulesByType = $this->rulesByType($rules);

        return [
            'sessionId' => (int)$session->id,
            'sessionCode' => $session->code,
            'title' => $session->title,
            'connectionMode' => $session->connection_mode ?: 'lan',
            'remoteJoinEnabled' => (bool)$session->remote_join_enabled,
            'host' => $session->published_host,
            'port' => $session->published_port !== null ? (int)$session->published_port : null,
            'relayEnabled' => (bool)$session->relay_enabled,
            'relayHost' => $session->relay_host,
            'relayPort' => $session->relay_port !== null ? (int)$session->relay_port : null,
            'relaySecret' => $session->relay_secret,
            'teacherMachine' => $session->teacher_machine,
            'blockClipboardShortcuts' => (bool)$session->block_clipboard_shortcuts,
            'websitePolicyMode' => $session->website_policy_mode ?: 'allowlist',
            'allowedWebsiteHosts' => array_values(array_filter(array_map('trim', explode(';', (string)$session->allowed_website_hosts)), static fn(string $item) => $item !== '')),
            'screenIntervalMs' => (int)$session->screen_interval_ms,
            'screenJpegQuality' => (int)$session->screen_jpeg_quality,
            'webcamEnabled' => (bool)$session->webcam_enabled,
            'webcamSnapshotOnConnect' => (bool)$session->webcam_snapshot_on_connect,
            'webcamIntervalMs' => !empty($session->webcam_interval_ms)
                ? (int)$session->webcam_interval_ms
                : ((int)$session->webcam_interval_seconds > 0 ? min(10000, max(250, (int)$session->webcam_interval_seconds * 1000)) : 0),
            'webcamIntervalSeconds' => (int)$session->webcam_interval_seconds,
            'webcamJpegQuality' => (int)$session->webcam_jpeg_quality,
            'examDurationMinutes' => (int)$session->exam_duration_minutes,
            'allowSubmissionAfterDeadline' => (bool)$session->allow_submission_after_deadline,
            'startedAtUtc' => $session->started_at ? gmdate('c', strtotime($session->started_at . ' UTC')) : null,
            'examEndAtUtc' => $this->computeExamEndAt($session),
            'blockedProcesses' => $rulesByType['process'] ?? [],
            'blockedWindowKeywords' => $rulesByType['window_title'] ?? [],
            'blockedAiCliTools' => $rulesByType['ai_cli'] ?? [],
            'blockedProxyTools' => $rulesByType['proxy_tool'] ?? [],
            'blockedIdeExtensions' => $rulesByType['ide_extension'] ?? [],
            'blockedWebsiteHosts' => $rulesByType['website_host'] ?? [],
        ];
    }

    private function rulesByType(array $rules): array
    {
        $grouped = [];
        foreach ($rules as $rule) {
            $grouped[$rule->rule_type][] = $rule->pattern;
        }

        return $grouped;
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
}
