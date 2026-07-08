<?php

namespace app\controllers\api;

use Yii;
use yii\rest\Controller;

class HealthController extends Controller
{
    public function actionIndex(): array
    {
        Yii::$app->db->createCommand('SELECT 1')->queryScalar();
        return [
            'status' => 'ok',
            'service' => 'examguard-backend',
            'time' => gmdate('c'),
        ];
    }
}
