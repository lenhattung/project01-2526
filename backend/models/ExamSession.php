<?php

namespace app\models;

use yii\db\ActiveRecord;

class ExamSession extends ActiveRecord
{
    public const STATUS_DRAFT = 'draft';
    public const STATUS_RUNNING = 'running';
    public const STATUS_FINISHED = 'finished';

    public static function tableName(): string
    {
        return '{{%exam_sessions}}';
    }

    public function rules(): array
    {
        return [
            [['class_id', 'code', 'title'], 'required'],
            [['class_id', 'created_by', 'screen_interval_ms', 'screen_jpeg_quality', 'webcam_interval_seconds', 'webcam_interval_ms', 'webcam_jpeg_quality', 'published_port', 'relay_port', 'exam_duration_minutes'], 'integer'],
            [['webcam_enabled', 'webcam_snapshot_on_connect', 'remote_join_enabled', 'relay_enabled', 'allow_submission_after_deadline', 'block_clipboard_shortcuts'], 'boolean'],
            [['code'], 'string', 'max' => 64],
            [['title', 'published_host', 'relay_host', 'teacher_machine'], 'string', 'max' => 255],
            [['allowed_website_hosts'], 'string'],
            [['website_policy_mode'], 'in', 'range' => ['off', 'allowlist']],
            [['session_token', 'relay_secret'], 'string', 'max' => 128],
            [['connection_mode'], 'in', 'range' => ['lan', 'remote', 'relay']],
            [['status'], 'in', 'range' => [self::STATUS_DRAFT, self::STATUS_RUNNING, self::STATUS_FINISHED]],
            [['screen_interval_ms'], 'default', 'value' => 2000],
            [['screen_interval_ms'], 'integer', 'min' => 250, 'max' => 10000],
            [['screen_jpeg_quality'], 'default', 'value' => 40],
            [['screen_jpeg_quality'], 'integer', 'min' => 20, 'max' => 85],
            [['webcam_interval_ms'], 'default', 'value' => 500],
            [['webcam_interval_ms'], 'integer', 'min' => 250, 'max' => 10000],
            [['webcam_interval_seconds'], 'default', 'value' => 0],
            [['webcam_interval_seconds'], 'integer', 'min' => 0, 'max' => 3600],
            [['webcam_jpeg_quality'], 'default', 'value' => 55],
            [['webcam_jpeg_quality'], 'integer', 'min' => 25, 'max' => 90],
            [['exam_duration_minutes'], 'default', 'value' => 0],
            [['exam_duration_minutes'], 'integer', 'min' => 0, 'max' => 1440],
            [['connection_mode'], 'default', 'value' => 'lan'],
            [['website_policy_mode'], 'default', 'value' => 'allowlist'],
            [['code'], 'unique'],
        ];
    }
}
