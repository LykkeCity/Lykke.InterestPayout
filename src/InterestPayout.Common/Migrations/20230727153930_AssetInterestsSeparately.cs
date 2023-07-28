using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class AssetInterestsSeparately : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterestRate",
                schema: "interest_payout",
                table: "payout_schedules");

            migrationBuilder.CreateSequence(
                name: "id_generator_asset_interests",
                schema: "interest_payout",
                startValue: 110000000L);

            migrationBuilder.CreateTable(
                name: "asset_interests",
                schema: "interest_payout",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('interest_payout.id_generator_asset_interests')"),
                    AssetId = table.Column<string>(type: "text", nullable: true),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ValidUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_interests", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_interests",
                schema: "interest_payout");

            migrationBuilder.DropSequence(
                name: "id_generator_asset_interests",
                schema: "interest_payout");

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                schema: "interest_payout",
                table: "payout_schedules",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
