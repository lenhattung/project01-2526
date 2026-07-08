<?php

namespace app\models;

use yii\db\ActiveRecord;

class Student extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%students}}';
    }

    public function rules(): array
    {
        return [
            [['class_id', 'code', 'full_name'], 'required'],
            [['class_id'], 'integer'],
            [['code'], 'string', 'max' => 64],
            [['full_name'], 'string', 'max' => 255],
        ];
    }
}
