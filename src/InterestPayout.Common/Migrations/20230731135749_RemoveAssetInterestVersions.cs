using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class RemoveAssetInterestVersions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ValidUntil",
                schema: "interest_payout",
                table: "asset_interests",
                newName: "UpdatedAt");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                schema: "interest_payout",
                table: "asset_interests",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                schema: "interest_payout",
                table: "asset_interests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "interest_payout",
                table: "asset_interests",
                newName: "ValidUntil");

            migrationBuilder.AlterColumn<int>(
                name: "Version",
                schema: "interest_payout",
                table: "asset_interests",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
