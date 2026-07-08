<?php

use yii\db\Migration;

class m260706_000003_add_remote_access_and_exam_timing extends Migration
{
    public function safeUp(): void
    {
        $this->addColumn('{{%exam_sessions}}', 'connection_mode', $this->string(16)->notNull()->defaultValue('lan')->after('session_token'));
        $this->addColumn('{{%exam_sessions}}', 'published_host', $this->string(255)->null()->after('connection_mode'));
        $this->addColumn('{{%exam_sessions}}', 'published_port', $this->integer()->null()->after('published_host'));
        $this->addColumn('{{%exam_sessions}}', 'teacher_machine', $this->string(255)->null()->after('published_port'));
        $this->addColumn('{{%exam_sessions}}', 'remote_join_enabled', $this->boolean()->notNull()->defaultValue(false)->after('teacher_machine'));
        $this->addColumn('{{%exam_sessions}}', 'exam_duration_minutes', $this->integer()->notNull()->defaultValue(0)->after('webcam_jpeg_quality'));
        $this->addColumn('{{%exam_sessions}}', 'allow_submission_after_deadline', $this->boolean()->notNull()->defaultValue(false)->after('exam_duration_minutes'));
    }

    public function safeDown(): void
    {
        $this->dropColumn('{{%exam_sessions}}', 'allow_submission_after_deadline');
        $this->dropColumn('{{%exam_sessions}}', 'exam_duration_minutes');
        $this->dropColumn('{{%exam_sessions}}', 'remote_join_enabled');
        $this->dropColumn('{{%exam_sessions}}', 'teacher_machine');
        $this->dropColumn('{{%exam_sessions}}', 'published_port');
        $this->dropColumn('{{%exam_sessions}}', 'published_host');
        $this->dropColumn('{{%exam_sessions}}', 'connection_mode');
    }
}
