using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddCanApply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanApply",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanApply",
                table: "AspNetUsers");
        }
    }
}
