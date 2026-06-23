using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolBoard.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CanEdit",
                table: "Students",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Students",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanEdit",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Students");
        }
    }
}
