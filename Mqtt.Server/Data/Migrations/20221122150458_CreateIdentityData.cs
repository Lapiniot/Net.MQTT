using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mqtt.Server.Data.Migrations;

/// <inheritdoc />
public partial class CreateIdentityData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var columns = new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" };
        migrationBuilder.InsertData("AspNetRoles", columns, new[] { "79c881a5-e920-4472-8b68-4984a54a180e", "Admin", "ADMIN", null });
        migrationBuilder.InsertData("AspNetRoles", columns, new[] { "27ce91cf-4f0f-4c80-8d8a-a628a579d5e3", "Client", "CLIENT", null });

        migrationBuilder.InsertData("AspNetUsers",
            new[] { "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount" },
            new object[] { "013e7f3c-2ea2-4592-a191-9c58db323fcd", "admin", "ADMIN", "aossss@gmail.com", "AOSSSS@GMAIL.COM", true, "AQAAAAIAAYagAAAAEDt0n7z1mjMhw/tyGLa+lpRr7rZ4V1CxOX8W/P1Jkhx+rC4C5it9NczVsmUjAgMAiQ==", "QOJ5ZYOX4VSVUR3AFLWYJ76MEAGE6X5P", "39ff5671-eadf-493c-aed6-289b56899ef5", null, false, false, null, true, 0 });

        migrationBuilder.InsertData("AspNetUserRoles", new[] { "UserId", "RoleId" }, new[] { "013e7f3c-2ea2-4592-a191-9c58db323fcd", "79c881a5-e920-4472-8b68-4984a54a180e" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData("AspNetUserRoles", new[] { "UserId", "RoleId" }, new[] { "013e7f3c-2ea2-4592-a191-9c58db323fcd", "79c881a5-e920-4472-8b68-4984a54a180e" });
        migrationBuilder.DeleteData("AspNetUsers", "Id", "013e7f3c-2ea2-4592-a191-9c58db323fcd");
        migrationBuilder.DeleteData("AspNetRoles", "Id", "79c881a5-e920-4472-8b68-4984a54a180e");
        migrationBuilder.DeleteData("AspNetRoles", "Id", "27ce91cf-4f0f-4c80-8d8a-a628a579d5e3");
    }
}