<?php

namespace app\controllers\api;

use app\models\Student;
use yii\filters\auth\HttpBearerAuth;
use yii\filters\Cors;
use yii\rest\ActiveController;

class StudentsController extends ActiveController
{
    public $modelClass = Student::class;

    public function behaviors(): array
    {
        $behaviors = parent::behaviors();
        $behaviors['corsFilter'] = ['class' => Cors::class];
        $behaviors['authenticator'] = ['class' => HttpBearerAuth::class, 'except' => ['options']];
        return $behaviors;
    }
}
