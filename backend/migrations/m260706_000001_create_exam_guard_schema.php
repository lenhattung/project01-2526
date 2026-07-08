<?php

use yii\db\Migration;

class m260706_000001_create_exam_guard_schema extends Migration
{
    public function safeUp(): void
    {
        $this->createTable('{{%users}}', [
            'id' => $this->primaryKey(),
            'username' => $this->string(64)->notNull()->unique(),
            'password_hash' => $this->string(255)->notNull(),
            'auth_token' => $this->string(128)->null()->unique(),
            'full_name' => $this->string(255)->notNull(),
            'role' => $this->string(32)->notNull()->defaultValue('teacher'),
            'is_active' => $this->boolean()->notNull()->defaultValue(true),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
            'updated_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP'),
        ]);

        $this->createTable('{{%classes}}', [
            'id' => $this->primaryKey(),
            'code' => $this->string(32)->notNull()->unique(),
            'name' => $this->string(255)->notNull(),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
            'updated_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP'),
        ]);

        $this->createTable('{{%students}}', [
            'id' => $this->primaryKey(),
            'class_id' => $this->integer()->notNull(),
            'code' => $this->string(64)->notNull(),
            'full_name' => $this->string(255)->notNull(),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
            'updated_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_students_class', '{{%students}}', 'class_id', '{{%classes}}', 'id', 'CASCADE');
        $this->createIndex('idx_students_class_code', '{{%students}}', ['class_id', 'code'], true);

        $this->createTable('{{%student_machines}}', [
            'id' => $this->primaryKey(),
            'student_id' => $this->integer()->null(),
            'machine_name' => $this->string(255)->notNull(),
            'ip_address' => $this->string(64)->null(),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_student_machines_student', '{{%student_machines}}', 'student_id', '{{%students}}', 'id', 'CASCADE');

        $this->createTable('{{%exam_sessions}}', [
            'id' => $this->primaryKey(),
            'class_id' => $this->integer()->notNull(),
            'code' => $this->string(64)->notNull()->unique(),
            'title' => $this->string(255)->notNull(),
            'session_token' => $this->string(128)->notNull(),
            'status' => $this->string(32)->notNull()->defaultValue('draft'),
            'created_by' => $this->integer()->null(),
            'started_at' => $this->dateTime()->null(),
            'finished_at' => $this->dateTime()->null(),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
            'updated_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_exam_sessions_class', '{{%exam_sessions}}', 'class_id', '{{%classes}}', 'id', 'CASCADE');
        $this->addForeignKey('fk_exam_sessions_user', '{{%exam_sessions}}', 'created_by', '{{%users}}', 'id', 'SET NULL');

        $this->createTable('{{%blocked_apps}}', [
            'id' => $this->primaryKey(),
            'exam_session_id' => $this->integer()->notNull(),
            'rule_type' => $this->string(32)->notNull(),
            'pattern' => $this->string(255)->notNull(),
            'is_active' => $this->boolean()->notNull()->defaultValue(true),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_blocked_apps_session', '{{%blocked_apps}}', 'exam_session_id', '{{%exam_sessions}}', 'id', 'CASCADE');

        $this->createTable('{{%exam_events}}', [
            'id' => $this->primaryKey(),
            'exam_session_id' => $this->integer()->notNull(),
            'student_id' => $this->integer()->null(),
            'event_type' => $this->string(255)->notNull(),
            'machine_name' => $this->string(255)->null(),
            'ip_address' => $this->string(64)->null(),
            'payload_json' => $this->text()->null(),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_exam_events_session', '{{%exam_events}}', 'exam_session_id', '{{%exam_sessions}}', 'id', 'CASCADE');
        $this->addForeignKey('fk_exam_events_student', '{{%exam_events}}', 'student_id', '{{%students}}', 'id', 'SET NULL');

        $this->createTable('{{%submissions}}', [
            'id' => $this->primaryKey(),
            'exam_session_id' => $this->integer()->notNull(),
            'student_id' => $this->integer()->null(),
            'file_name' => $this->string(255)->notNull(),
            'storage_path' => $this->string(255)->null(),
            'sha256' => $this->string(64)->notNull(),
            'file_size' => $this->bigInteger()->notNull(),
            'status' => $this->string(32)->notNull()->defaultValue('submitted'),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_submissions_session', '{{%submissions}}', 'exam_session_id', '{{%exam_sessions}}', 'id', 'CASCADE');
        $this->addForeignKey('fk_submissions_student', '{{%submissions}}', 'student_id', '{{%students}}', 'id', 'SET NULL');

        $this->insert('{{%users}}', [
            'username' => 'teacher',
            'password_hash' => Yii::$app->security->generatePasswordHash('teacher123'),
            'full_name' => 'Default Teacher',
            'role' => 'teacher',
        ]);

        $this->insert('{{%classes}}', [
            'code' => 'LAB-A',
            'name' => 'Default Lab A',
        ]);

        $this->insert('{{%students}}', [
            'class_id' => 1,
            'code' => 'SV001',
            'full_name' => 'Sample Student',
        ]);

        $this->insert('{{%exam_sessions}}', [
            'class_id' => 1,
            'code' => 'EXAM-001',
            'title' => 'Default Exam Session',
            'session_token' => 'classroom-token',
            'status' => 'draft',
            'created_by' => 1,
        ]);

        $this->batchInsert('{{%blocked_apps}}', ['exam_session_id', 'rule_type', 'pattern'], [
            [1, 'process', 'zalo'],
            [1, 'process', 'messenger'],
            [1, 'process', 'chatgpt'],
            [1, 'process', 'claude'],
            [1, 'window_title', 'ChatGPT'],
            [1, 'window_title', 'Claude'],
            [1, 'window_title', 'Gemini'],
            [1, 'window_title', 'Messenger'],
            [1, 'window_title', 'Zalo'],
        ]);
    }

    public function safeDown(): void
    {
        $this->dropTable('{{%submissions}}');
        $this->dropTable('{{%exam_events}}');
        $this->dropTable('{{%blocked_apps}}');
        $this->dropTable('{{%exam_sessions}}');
        $this->dropTable('{{%student_machines}}');
        $this->dropTable('{{%students}}');
        $this->dropTable('{{%classes}}');
        $this->dropTable('{{%users}}');
    }
}
