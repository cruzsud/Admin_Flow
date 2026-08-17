using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminFlow.Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBudgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budgets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cost_center_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fiscal_year = table.Column<int>(type: "integer", nullable: false),
                    allocated = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    committed = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_budgets", x => x.id);
                    table.CheckConstraint("ck_budgets_allocated_positive", "allocated > 0");
                    table.CheckConstraint("ck_budgets_committed_non_negative", "committed >= 0");
                    table.CheckConstraint("ck_budgets_committed_within_allocation", "committed <= allocated");
                    table.CheckConstraint("ck_budgets_fiscal_year", "fiscal_year BETWEEN 1 AND 9999");
                    table.ForeignKey(
                        name: "fk_budgets_cost_centers",
                        column: x => x.cost_center_id,
                        principalTable: "cost_centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_budgets_cost_center_fiscal_year",
                table: "budgets",
                columns: new[] { "cost_center_id", "fiscal_year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "budgets");
        }
    }
}
