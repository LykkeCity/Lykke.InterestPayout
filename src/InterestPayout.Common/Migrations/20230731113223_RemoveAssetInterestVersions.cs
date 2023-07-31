using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class RemoveAssetInterestVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidUntil",
                schema: "interest_payout",
                table: "asset_interests");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "interest_payout",
                table: "asset_interests",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "interest_payout",
                table: "asset_interests",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                schema: "interest_payout",
                table: "asset_interests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "interest_payout",
                table: "asset_interests",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "ix_asset_interest_asset_id_uq",
                schema: "interest_payout",
                table: "asset_interests",
                column: "AssetId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_asset_interest_asset_id_uq",
                schema: "interest_payout",
                table: "asset_interests");

            migrationBuilder.DropColumn(
                name: "Sequence",
                schema: "interest_payout",
                table: "asset_interests");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "interest_payout",
                table: "asset_interests");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                schema: "interest_payout",
                table: "asset_interests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "interest_payout",
                table: "asset_interests",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidUntil",
                schema: "interest_payout",
                table: "asset_interests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }
    }
}
