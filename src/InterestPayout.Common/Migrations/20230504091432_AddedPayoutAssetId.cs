using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class AddedPayoutAssetId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PayoutAssetId",
                schema: "interest_payout",
                table: "payout_schedules",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayoutAssetId",
                schema: "interest_payout",
                table: "payout_schedules");
        }
    }
}
