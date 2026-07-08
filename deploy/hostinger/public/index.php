<?php

$backendRoot = getenv('EXAMGUARD_BACKEND_ROOT');
if (!$backendRoot) {
    $backendRoot = realpath(__DIR__ . '/../examguard-app/backend');
}

if (!$backendRoot || !is_dir($backendRoot)) {
    http_response_code(500);
    echo 'ExamGuard backend root is not configured.';
    exit;
}

require $backendRoot . '/vendor/autoload.php';
require $backendRoot . '/vendor/yiisoft/yii2/Yii.php';

$config = require $backendRoot . '/config/web.php';
(new yii\web\Application($config))->run();
