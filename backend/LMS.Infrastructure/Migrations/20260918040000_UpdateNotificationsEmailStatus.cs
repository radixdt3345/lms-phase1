using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F-09 Notifications and Email — DB Layer completion.
    ///
    /// The notifications table was provisioned in 20260915000000_AddNotifications.cs.
    /// This migration finalises the F-09 schema requirements by:
    ///   1. Updating the email_status default to QUEUED (matching SendGrid queue semantics)
    ///   2. Adding a CHECK constraint to enforce valid email_status values
    ///   3. Adding the (recipient_user_id, is_read) composite index required by FR-75
    ///      (unread-count query used by the notification bell)
    ///   Note: the index may already exist from the initial migration — the CREATE INDEX
    ///   IF NOT EXISTS form is used to make this migration idempotent.
    /// </summary>
    public partial class UpdateNotificationsEmailStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Change email_status default from 'not_sent' to 'QUEUED'
            migrationBuilder.Sql(
                """
                ALTER TABLE notifications
                    ALTER COLUMN email_status SET DEFAULT 'QUEUED';
                """);

            // 2. Add CHECK constraint for valid email_status values
            //    Existing rows with 'not_sent' or 'pending' are preserved (legacy values).
            migrationBuilder.Sql(
                """
                ALTER TABLE notifications
                    ADD CONSTRAINT chk_notifications_email_status
                    CHECK (email_status IN ('QUEUED', 'SENT', 'DELIVERY_FAILED', 'not_sent', 'pending'));
                """);

            // 3. Ensure composite index for unread-count queries (notification bell — FR-75)
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS idx_notifications_recipient_user_id_is_read
                    ON notifications (recipient_user_id, is_read)
                    WHERE is_read = false;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE notifications
                    DROP CONSTRAINT IF EXISTS chk_notifications_email_status;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE notifications
                    ALTER COLUMN email_status SET DEFAULT 'not_sent';
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS idx_notifications_recipient_user_id_is_read;
                """);
        }
    }
}
