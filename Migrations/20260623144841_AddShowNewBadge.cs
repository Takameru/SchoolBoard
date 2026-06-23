using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddShowNewBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowNewBadge",
                table: "Students",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowNewBadge",
                table: "Students");
        }
    }
}
