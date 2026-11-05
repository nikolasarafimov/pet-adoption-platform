using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetAdoption.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSinglePendingRequestPerPet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_PetId",
                table: "AdoptionRequests");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_PetId",
                table: "AdoptionRequests",
                column: "PetId",
                unique: true,
                filter: "\"Status\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdoptionRequests_PetId",
                table: "AdoptionRequests");

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionRequests_PetId",
                table: "AdoptionRequests",
                column: "PetId");
        }
    }
}
