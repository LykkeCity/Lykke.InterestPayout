using Microsoft.EntityFrameworkCore.Migrations;

namespace InterestPayout.Common.Migrations
{
    public partial class AddAccuracyToSchedules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Accuracy",
                schema: "interest_payout",
                table: "payout_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Accuracy",
                schema: "interest_payout",
                table: "payout_schedules");
        }
    }
}
