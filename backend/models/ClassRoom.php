<?php

namespace app\models;

use yii\db\ActiveRecord;

class ClassRoom extends ActiveRecord
{
    public static function tableName(): string
    {
        return '{{%classes}}';
    }

    public function rules(): array
    {
        return [
            [['code', 'name'], 'required'],
            [['code'], 'string', 'max' => 32],
            [['name'], 'string', 'max' => 255],
            [['code'], 'unique'],
        ];
    }
}
