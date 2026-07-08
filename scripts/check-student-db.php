<?php

$config = require __DIR__ . '/../backend/config/local.php';
$pdo = new PDO($config['db']['dsn'], $config['db']['username'], $config['db']['password']);
$studentCode = $argv[1] ?? 'AUTO-SV-777';

$studentStmt = $pdo->prepare('SELECT id, code, full_name FROM students WHERE code = ? ORDER BY id DESC LIMIT 1');
$studentStmt->execute([$studentCode]);
$student = $studentStmt->fetch(PDO::FETCH_ASSOC);

if (!$student) {
    echo json_encode(['student' => null], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
    exit(0);
}

$machineStmt = $pdo->prepare('SELECT student_id, machine_name, ip_address FROM student_machines WHERE student_id = ? ORDER BY id DESC LIMIT 1');
$machineStmt->execute([$student['id']]);
$machine = $machineStmt->fetch(PDO::FETCH_ASSOC);

$eventStmt = $pdo->prepare('SELECT student_id, event_type, machine_name, ip_address FROM exam_events WHERE student_id = ? ORDER BY id DESC LIMIT 1');
$eventStmt->execute([$student['id']]);
$event = $eventStmt->fetch(PDO::FETCH_ASSOC);

echo json_encode([
    'student' => $student,
    'machine' => $machine,
    'event' => $event,
], JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
