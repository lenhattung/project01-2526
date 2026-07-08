<?php

use yii\db\Migration;

class m260706_000004_add_webcam_interval_ms extends Migration
{
    public function safeUp()
    {
        $this->addColumn('{{%exam_sessions}}', 'webcam_interval_ms', $this->integer()->notNull()->defaultValue(500)->after('webcam_snapshot_on_connect'));
        $this->update(
            '{{%exam_sessions}}',
            ['webcam_interval_ms' => new \yii\db\Expression("CASE WHEN webcam_interval_seconds > 0 THEN LEAST(webcam_interval_seconds * 1000, 10000) ELSE 0 END")]
        );
    }

    public function safeDown()
    {
        $this->dropColumn('{{%exam_sessions}}', 'webcam_interval_ms');
    }
}
