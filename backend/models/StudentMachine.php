<?php

namespace app\models;

use yii\db\ActiveRecord;

class StudentMachine extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%student_machines}}';
    }

    public function rules(): array
    {
        return [
            [['student_id', 'machine_name'], 'required'],
            [['student_id'], 'integer'],
            [['machine_name', 'ip_address'], 'string', 'max' => 255],
        ];
    }
}
