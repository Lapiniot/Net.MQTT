using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mqtt.Server.Identity.PostgreSQL.Migrations.SchemaV3;

/// <inheritdoc />
public partial class UpdateToSchemaV3 : Migration
{
    /// <inheritdoc />
    protected override void Up([NotNull] MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "phone_number",
            table: "asp_net_users",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "asp_net_user_tokens",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "login_provider",
            table: "asp_net_user_tokens",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "provider_key",
            table: "asp_net_user_logins",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.AlterColumn<string>(
            name: "login_provider",
            table: "asp_net_user_logins",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        migrationBuilder.CreateTable(
            name: "asp_net_user_passkeys",
            columns: table => new
            {
                credential_id = table.Column<byte[]>(type: "bytea", maxLength: 1024, nullable: false),
                user_id = table.Column<string>(type: "text", nullable: false),
                data = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_identity_user_passkey_string", x => x.credential_id);
                table.ForeignKey(
                    name: "fk_identity_user_passkey_string_asp_net_users_application_user_id",
                    column: x => x.user_id,
                    principalTable: "asp_net_users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_identity_user_passkey_string_user_id",
            table: "asp_net_user_passkeys",
            column: "user_id");
    }

    /// <inheritdoc />
    protected override void Down([NotNull] MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "asp_net_user_passkeys");

        migrationBuilder.AlterColumn<string>(
            name: "phone_number",
            table: "asp_net_users",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(256)",
            oldMaxLength: 256,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "name",
            table: "asp_net_user_tokens",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldMaxLength: 128);

        migrationBuilder.AlterColumn<string>(
            name: "login_provider",
            table: "asp_net_user_tokens",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldMaxLength: 128);

        migrationBuilder.AlterColumn<string>(
            name: "provider_key",
            table: "asp_net_user_logins",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldMaxLength: 128);

        migrationBuilder.AlterColumn<string>(
            name: "login_provider",
            table: "asp_net_user_logins",
            type: "text",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(128)",
            oldMaxLength: 128);
    }
}