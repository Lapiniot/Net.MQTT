using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mqtt.Server.Identity.PostgreSQL.Migrations;

/// <inheritdoc />
public partial class CreateIdentityData : Migration
{
    /// <inheritdoc />
    protected override void Up([NotNull] MigrationBuilder migrationBuilder)
    {
        var adminRoleId = Guid.NewGuid().ToString();
        var clientRoleId = Guid.NewGuid().ToString();
        var adminUserId = Guid.NewGuid().ToString();
        string[] columns = ["id", "name", "normalized_name", "concurrency_stamp"];
        migrationBuilder.InsertData("asp_net_roles", columns, [adminRoleId, "Admin", "ADMIN", null]);
        migrationBuilder.InsertData("asp_net_roles", columns, [clientRoleId, "Client", "CLIENT", null]);

        // Create default admin user "admin:mqtt-admin"
        migrationBuilder.InsertData("asp_net_users",
            ["id", "user_name", "normalized_user_name", "email", "normalized_email", "email_confirmed", "password_hash", "security_stamp", "concurrency_stamp", "phone_number", "phone_number_confirmed", "two_factor_enabled", "lockout_end", "lockout_enabled", "access_failed_count"],
            [adminUserId, "admin", "ADMIN", "", "", true, "AQAAAAIAAYagAAAAEEQdyDpdd6xTzS+wuQlIDjhQvIraquzo/G4FTTEkGxV8LaVE0VF4h71K4uNSX5vP5g==", "QOJ5ZYOX4VSVUR3AFLWYJ76MEAGE6X5P", null, null, false, false, null, true, 0]);

        migrationBuilder.InsertData("asp_net_user_roles", ["user_id", "role_id"], [adminUserId, adminRoleId]);
    }

    /// <inheritdoc />
    protected override void Down([NotNull] MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE
            FROM asp_net_user_roles
            WHERE user_id =
                    (SELECT id
                    FROM asp_net_users
                    WHERE normalized_user_name = 'ADMIN')
                AND role_id =
                    (SELECT id
                    FROM asp_net_roles
                    WHERE normalized_name = 'ADMIN')
            """);

        migrationBuilder.Sql("""
            DELETE
            FROM asp_net_users
            WHERE normalized_user_name = 'ADMIN'
            """);

        migrationBuilder.Sql("""
            DELETE
            FROM asp_net_roles
            WHERE normalized_name IN ('ADMIN', 'CLIENT')
            """);
    }
}