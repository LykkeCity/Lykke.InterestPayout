using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class AddPayoutSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "interest_payout");

            migrationBuilder.CreateSequence(
                name: "id_generator_payout_schedules",
                schema: "interest_payout",
                startValue: 100000000L);

            migrationBuilder.CreateTable(
                name: "id_generator",
                schema: "interest_payout",
                columns: table => new
                {
                    IdempotencyId = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_id_generator", x => x.IdempotencyId);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "interest_payout",
                columns: table => new
                {
                    IdempotencyId = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    Events = table.Column<string>(type: "text", nullable: true),
                    Commands = table.Column<string>(type: "text", nullable: true),
                    IsDispatched = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.IdempotencyId);
                });

            migrationBuilder.CreateTable(
                name: "payout_schedules",
                schema: "interest_payout",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('interest_payout.id_generator_payout_schedules')"),
                    AssetId = table.Column<string>(type: "text", nullable: true),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CronSchedule = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payout_schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payout_schedule_asset_id_uq",
                schema: "interest_payout",
                table: "payout_schedules",
                column: "AssetId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "id_generator",
                schema: "interest_payout");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "interest_payout");

            migrationBuilder.DropTable(
                name: "payout_schedules",
                schema: "interest_payout");

            migrationBuilder.DropSequence(
                name: "id_generator_payout_schedules",
                schema: "interest_payout");
        }
    }
}
