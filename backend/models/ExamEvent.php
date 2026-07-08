<?php

namespace app\models;

use yii\db\ActiveRecord;

class ExamEvent extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%exam_events}}';
    }

    public function rules(): array
    {
        return [
            [['exam_session_id', 'event_type'], 'required'],
            [['exam_session_id', 'student_id'], 'integer'],
            [['event_type', 'machine_name', 'ip_address'], 'string', 'max' => 255],
            [['payload_json'], 'string'],
        ];
    }
}
