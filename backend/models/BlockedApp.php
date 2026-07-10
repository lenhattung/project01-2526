<?php

namespace app\models;

use yii\db\ActiveRecord;

class BlockedApp extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%blocked_apps}}';
    }

    public function rules(): array
    {
        return [
            [['exam_session_id', 'rule_type', 'pattern'], 'required'],
            [['exam_session_id'], 'integer'],
            [['rule_type'], 'in', 'range' => ['process', 'ai_cli', 'proxy_tool', 'window_title', 'ide_extension', 'website_host']],
            [['pattern'], 'string', 'max' => 255],
            [['is_active'], 'boolean'],
        ];
    }
}
