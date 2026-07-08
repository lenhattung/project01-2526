<?php

namespace app\models;

use Yii;
use yii\db\ActiveRecord;
use yii\web\IdentityInterface;

class User extends ActiveRecord implements IdentityInterface
{
    public static function tableName(): string
    {
        return '{{%users}}';
    }

    public static function findIdentity($id): ?self
    {
        return static::findOne(['id' => $id, 'is_active' => 1]);
    }

    public static function findIdentityByAccessToken($token, $type = null): ?self
    {
        return static::findOne(['auth_token' => $token, 'is_active' => 1]);
    }

    public static function findByUsername(string $username): ?self
    {
        return static::findOne(['username' => $username, 'is_active' => 1]);
    }

    public function getId(): int
    {
        return (int)$this->id;
    }

    public function getAuthKey(): ?string
    {
        return $this->auth_token;
    }

    public function validateAuthKey($authKey): bool
    {
        return $this->auth_token === $authKey;
    }

    public function validatePassword(string $password): bool
    {
        return Yii::$app->security->validatePassword($password, $this->password_hash);
    }

    public function issueToken(): string
    {
        $this->auth_token = Yii::$app->security->generateRandomString(64);
        $this->updated_at = gmdate('Y-m-d H:i:s');
        $this->save(false, ['auth_token', 'updated_at']);
        return $this->auth_token;
    }
}
