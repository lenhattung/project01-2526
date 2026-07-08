<?php

$db = require __DIR__ . '/db.php';

return [
    'id' => 'examguard-console',
    'basePath' => dirname(__DIR__),
    'controllerNamespace' => 'app\\commands',
    'components' => [
        'db' => $db,
    ],
];
