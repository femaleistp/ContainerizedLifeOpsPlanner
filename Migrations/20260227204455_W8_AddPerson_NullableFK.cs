using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LifeOpsPlanner_Project.Migrations
{
    /// <inheritdoc />
    public partial class W8_AddPerson_NullableFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityEntries_Activities",
                table: "ActivityEntries");

            migrationBuilder.DropColumn(
                name: "Person",
                table: "ActivityEntries");

            migrationBuilder.AddColumn<int>(
                name: "PersonId",
                table: "ActivityEntries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Persons",
                columns: table => new
                {
                    PersonId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Persons", x => x.PersonId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEntries_PersonId",
                table: "ActivityEntries",
                column: "PersonId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityEntries_Activities",
                table: "ActivityEntries",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "ActivityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityEntries_Persons",
                table: "ActivityEntries",
                column: "PersonId",
                principalTable: "Persons",
                principalColumn: "PersonId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityEntries_Activities",
                table: "ActivityEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityEntries_Persons",
                table: "ActivityEntries");

            migrationBuilder.DropTable(
                name: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_ActivityEntries_PersonId",
                table: "ActivityEntries");

            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "ActivityEntries");

            migrationBuilder.AddColumn<string>(
                name: "Person",
                table: "ActivityEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Me");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityEntries_Activities",
                table: "ActivityEntries",
                column: "ActivityId",
                principalTable: "Activities",
                principalColumn: "ActivityId");
        }
    }
}
