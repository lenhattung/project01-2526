<?php

use yii\db\Migration;

class m260706_000002_add_monitoring_policy_to_exam_sessions extends Migration
{
    public function safeUp(): void
    {
        $this->addColumn('{{%exam_sessions}}', 'screen_interval_ms', $this->integer()->notNull()->defaultValue(2000)->after('status'));
        $this->addColumn('{{%exam_sessions}}', 'screen_jpeg_quality', $this->integer()->notNull()->defaultValue(40)->after('screen_interval_ms'));
        $this->addColumn('{{%exam_sessions}}', 'webcam_enabled', $this->boolean()->notNull()->defaultValue(true)->after('screen_jpeg_quality'));
        $this->addColumn('{{%exam_sessions}}', 'webcam_snapshot_on_connect', $this->boolean()->notNull()->defaultValue(true)->after('webcam_enabled'));
        $this->addColumn('{{%exam_sessions}}', 'webcam_interval_seconds', $this->integer()->notNull()->defaultValue(0)->after('webcam_snapshot_on_connect'));
        $this->addColumn('{{%exam_sessions}}', 'webcam_jpeg_quality', $this->integer()->notNull()->defaultValue(55)->after('webcam_interval_seconds'));
    }

    public function safeDown(): void
    {
        $this->dropColumn('{{%exam_sessions}}', 'webcam_jpeg_quality');
        $this->dropColumn('{{%exam_sessions}}', 'webcam_interval_seconds');
        $this->dropColumn('{{%exam_sessions}}', 'webcam_snapshot_on_connect');
        $this->dropColumn('{{%exam_sessions}}', 'webcam_enabled');
        $this->dropColumn('{{%exam_sessions}}', 'screen_jpeg_quality');
        $this->dropColumn('{{%exam_sessions}}', 'screen_interval_ms');
    }
}
