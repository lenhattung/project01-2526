<?php

$local = [];
$localPath = __DIR__ . '/local.php';
if (is_file($localPath)) {
    $local = require $localPath;
}

$dbConfig = $local['db'] ?? [];

return [
    'class' => yii\db\Connection::class,
    'dsn' => $dbConfig['dsn'] ?? getenv('DB_DSN') ?: 'mysql:host=127.0.0.1;dbname=examguard;charset=utf8mb4',
    'username' => $dbConfig['username'] ?? getenv('DB_USERNAME') ?: 'root',
    'password' => $dbConfig['password'] ?? getenv('DB_PASSWORD') ?: '',
    'charset' => $dbConfig['charset'] ?? 'utf8mb4',
];
