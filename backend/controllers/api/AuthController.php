<?php

namespace app\controllers\api;

use app\models\User;
use Yii;
use yii\filters\Cors;
use yii\rest\Controller;
use yii\web\BadRequestHttpException;
use yii\web\UnauthorizedHttpException;

class AuthController extends Controller
{
    public function behaviors(): array
    {
        $behaviors = parent::behaviors();
        $behaviors['corsFilter'] = ['class' => Cors::class];
        return $behaviors;
    }

    public function actionLogin(): array
    {
        $body = Yii::$app->request->bodyParams;
        $username = trim((string)($body['username'] ?? ''));
        $password = (string)($body['password'] ?? '');

        if ($username === '' || $password === '') {
            throw new BadRequestHttpException('Username and password are required.');
        }

        $user = User::findByUsername($username);
        if ($user === null || !$user->validatePassword($password)) {
            throw new UnauthorizedHttpException('Invalid credentials.');
        }

        return [
            'token' => $user->issueToken(),
            'user' => [
                'id' => (int)$user->id,
                'username' => $user->username,
                'fullName' => $user->full_name,
            ],
        ];
    }
}
