using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HouseHeroes.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyMembershipVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MembershipVersion",
                table: "Families",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MembershipVersion",
                table: "Families");
        }
    }
}
