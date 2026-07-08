<?php

namespace app\models;

use yii\db\ActiveRecord;

class ChatMessage extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%chat_messages}}';
    }

    public function rules(): array
    {
        return [
            [['exam_session_id', 'sender_role', 'sender_code', 'message', 'scope'], 'required'],
            [['exam_session_id'], 'integer'],
            [['message'], 'string'],
            [['sender_role', 'sender_code', 'target_code', 'scope'], 'string', 'max' => 255],
        ];
    }
}
