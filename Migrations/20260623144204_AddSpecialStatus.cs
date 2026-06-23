using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowSpecialStatus",
                table: "Students",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SpecialStatus",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowSpecialStatus",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SpecialStatus",
                table: "Students");
        }
    }
}
