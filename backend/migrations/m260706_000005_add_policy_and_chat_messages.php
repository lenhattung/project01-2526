<?php

use yii\db\Migration;

class m260706_000005_add_policy_and_chat_messages extends Migration
{
    public function safeUp()
    {
        $this->addColumn('{{%exam_sessions}}', 'block_clipboard_shortcuts', $this->boolean()->notNull()->defaultValue(true)->after('allow_submission_after_deadline'));
        $this->addColumn('{{%exam_sessions}}', 'website_policy_mode', $this->string(32)->notNull()->defaultValue('allowlist')->after('block_clipboard_shortcuts'));
        $this->addColumn('{{%exam_sessions}}', 'allowed_website_hosts', $this->text()->null()->after('website_policy_mode'));

        $this->createTable('{{%chat_messages}}', [
            'id' => $this->primaryKey(),
            'exam_session_id' => $this->integer()->notNull(),
            'sender_role' => $this->string(32)->notNull(),
            'sender_code' => $this->string(255)->notNull(),
            'target_code' => $this->string(255)->null(),
            'message' => $this->text()->notNull(),
            'scope' => $this->string(32)->notNull()->defaultValue('one'),
            'created_at' => $this->timestamp()->defaultExpression('CURRENT_TIMESTAMP'),
        ]);
        $this->addForeignKey('fk_chat_messages_session', '{{%chat_messages}}', 'exam_session_id', '{{%exam_sessions}}', 'id', 'CASCADE');
    }

    public function safeDown()
    {
        $this->dropForeignKey('fk_chat_messages_session', '{{%chat_messages}}');
        $this->dropTable('{{%chat_messages}}');
        $this->dropColumn('{{%exam_sessions}}', 'allowed_website_hosts');
        $this->dropColumn('{{%exam_sessions}}', 'website_policy_mode');
        $this->dropColumn('{{%exam_sessions}}', 'block_clipboard_shortcuts');
    }
}
