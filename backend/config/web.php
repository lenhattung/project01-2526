<?php

$db = require __DIR__ . '/db.php';
$local = [];
$localPath = __DIR__ . '/local.php';
if (is_file($localPath)) {
    $local = require $localPath;
}

return [
    'id' => 'examguard-api',
    'basePath' => dirname(__DIR__),
    'bootstrap' => ['log'],
    'language' => 'en-US',
    'components' => [
        'request' => [
            'cookieValidationKey' => $local['app']['cookieValidationKey'] ?? getenv('COOKIE_VALIDATION_KEY') ?: 'change-this-key-for-production',
            'parsers' => [
                'application/json' => yii\web\JsonParser::class,
            ],
        ],
        'response' => [
            'format' => yii\web\Response::FORMAT_JSON,
        ],
        'user' => [
            'identityClass' => app\models\User::class,
            'enableSession' => false,
            'loginUrl' => null,
        ],
        'db' => $db,
        'urlManager' => [
            'enablePrettyUrl' => true,
            'showScriptName' => false,
            'rules' => [
                'POST api/auth/login' => 'api/auth/login',
                'GET api/health' => 'api/health/index',
                'POST api/session-access/lookup' => 'api/session-access/lookup',
                'GET api/classes' => 'api/classes/index',
                'POST api/classes' => 'api/classes/create',
                'GET api/classes/<id:\d+>' => 'api/classes/view',
                'PUT api/classes/<id:\d+>' => 'api/classes/update',
                'DELETE api/classes/<id:\d+>' => 'api/classes/delete',
                'GET api/exam-sessions' => 'api/exam-sessions/index',
                'GET api/students' => 'api/students/index',
                'POST api/students' => 'api/students/create',
                'GET api/blocked-apps' => 'api/blocked-apps/index',
                'POST api/blocked-apps' => 'api/blocked-apps/create',
                'POST api/exam-sessions' => 'api/exam-sessions/create',
                'POST api/exam-sessions/<id:\d+>/start' => 'api/exam-sessions/start',
                'POST api/exam-sessions/<id:\d+>/finish' => 'api/exam-sessions/finish',
                'GET api/exam-sessions/<id:\d+>/policy' => 'api/exam-sessions/policy',
                'GET api/exam-sessions/<id:\d+>/roster' => 'api/exam-sessions/roster',
                'POST api/exam-sessions/<id:\d+>/policy' => 'api/exam-sessions/update-policy',
                'POST api/exam-sessions/<id:\d+>/publish-access' => 'api/exam-sessions/publish-access',
                'GET api/exam-sessions/<id:\d+>/report' => 'api/exam-sessions/report',
                'POST api/exam-sessions/<id:\d+>/events' => 'api/exam-sessions/events',
                'POST api/exam-sessions/<id:\d+>/submissions' => 'api/exam-sessions/submissions',
                'POST api/exam-sessions/<id:\d+>/chat-messages' => 'api/exam-sessions/chat-messages',
            ],
        ],
        'log' => [
            'targets' => [
                [
                    'class' => yii\log\FileTarget::class,
                    'levels' => ['error', 'warning'],
                ],
            ],
        ],
    ],
];
