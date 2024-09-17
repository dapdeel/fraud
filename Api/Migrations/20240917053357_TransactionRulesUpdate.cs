using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class TransactionRulesUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransactionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ObservatoryId = table.Column<int>(type: "integer", nullable: false),
                    AlertFrequencyMinutes = table.Column<int>(type: "integer", nullable: false),
                    RiskAppetiteAmount = table.Column<float>(type: "real", nullable: false),
                    AllowSuspiciousAccounts = table.Column<bool>(type: "boolean", nullable: false),
                    BlockFraudulentAccounts = table.Column<bool>(type: "boolean", nullable: false),
                    AlertFraudulentAccounts = table.Column<bool>(type: "boolean", nullable: false),
                    AlertHighRiskTransactions = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionRules_Observatories_ObservatoryId",
                        column: x => x.ObservatoryId,
                        principalTable: "Observatories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransactionRules_ObservatoryId",
                table: "TransactionRules",
                column: "ObservatoryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionRules");
        }
    }
}
