<?php

namespace app\models;

use yii\db\ActiveRecord;

class Submission extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%submissions}}';
    }

    public function rules(): array
    {
        return [
            [['exam_session_id', 'file_name', 'sha256', 'file_size'], 'required'],
            [['exam_session_id', 'student_id', 'file_size'], 'integer'],
            [['file_name', 'storage_path', 'sha256', 'status'], 'string', 'max' => 255],
        ];
    }
}
