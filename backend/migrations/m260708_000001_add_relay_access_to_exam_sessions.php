<?php

use yii\db\Migration;

class m260708_000001_add_relay_access_to_exam_sessions extends Migration
{
    public function safeUp(): void
    {
        $this->addColumn('{{%exam_sessions}}', 'relay_enabled', $this->boolean()->notNull()->defaultValue(false)->after('remote_join_enabled'));
        $this->addColumn('{{%exam_sessions}}', 'relay_host', $this->string(255)->null()->after('relay_enabled'));
        $this->addColumn('{{%exam_sessions}}', 'relay_port', $this->integer()->null()->after('relay_host'));
        $this->addColumn('{{%exam_sessions}}', 'relay_secret', $this->string(128)->null()->after('relay_port'));
    }

    public function safeDown(): void
    {
        $this->dropColumn('{{%exam_sessions}}', 'relay_secret');
        $this->dropColumn('{{%exam_sessions}}', 'relay_port');
        $this->dropColumn('{{%exam_sessions}}', 'relay_host');
        $this->dropColumn('{{%exam_sessions}}', 'relay_enabled');
    }
}
