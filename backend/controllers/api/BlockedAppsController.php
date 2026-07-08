<?php

namespace app\controllers\api;

use app\models\BlockedApp;
use yii\filters\auth\HttpBearerAuth;
use yii\filters\Cors;
use yii\rest\ActiveController;

class BlockedAppsController extends ActiveController
{
    public $modelClass = BlockedApp::class;

    public function behaviors(): array
    {
        $behaviors = parent::behaviors();
        $behaviors['corsFilter'] = ['class' => Cors::class];
        $behaviors['authenticator'] = ['class' => HttpBearerAuth::class, 'except' => ['options']];
        return $behaviors;
    }
}
