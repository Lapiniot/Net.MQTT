using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861
#nullable disable

namespace Mqtt.Server.Identity.Data.Migrations;

/// <inheritdoc />
public partial class CreateIdentityData : Migration
{
    /// <inheritdoc />
    protected override void Up([NotNull] MigrationBuilder migrationBuilder)
    {
        var adminRoleId = Guid.NewGuid().ToString();
        var clientRoleId = Guid.NewGuid().ToString();
        var adminUserId = Guid.NewGuid().ToString();
        string[] columns = ["Id", "Name", "NormalizedName", "ConcurrencyStamp"];
        migrationBuilder.InsertData("AspNetRoles", columns, [adminRoleId, "Admin", "ADMIN", null]);
        migrationBuilder.InsertData("AspNetRoles", columns, [clientRoleId, "Client", "CLIENT", null]);

        // Create default admin user "admin:mqtt-admin"
        migrationBuilder.InsertData("AspNetUsers",
            ["Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount"],
            [adminUserId, "admin", "ADMIN", "", "", true, "AQAAAAIAAYagAAAAEEQdyDpdd6xTzS+wuQlIDjhQvIraquzo/G4FTTEkGxV8LaVE0VF4h71K4uNSX5vP5g==", "QOJ5ZYOX4VSVUR3AFLWYJ76MEAGE6X5P", null, null, false, false, null, true, 0]);

        migrationBuilder.InsertData("AspNetUserRoles", ["UserId", "RoleId"], [adminUserId, adminRoleId]);
    }

    /// <inheritdoc />
    protected override void Down([NotNull] MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM AspNetUserRoles WHERE UserId=(SELECT Id FROM AspNetUsers WHERE UserName='admin') AND RoleId=(SELECT Id FROM AspNetRoles WHERE Name='Admin')");
        migrationBuilder.Sql("DELETE FROM AspNetRoles WHERE Name IN ('Admin', 'Client')");
        migrationBuilder.Sql("DELETE FROM AspNetUsers WHERE UserName='admin'");
    }
}