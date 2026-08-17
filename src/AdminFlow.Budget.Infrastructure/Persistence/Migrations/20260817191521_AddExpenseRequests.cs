using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminFlow.Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    budget_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_requests", x => x.id);
                    table.CheckConstraint("ck_expense_requests_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_expense_requests_status_valid", "status IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "fk_expense_requests_budgets",
                        column: x => x.budget_id,
                        principalTable: "budgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_expense_requests_budget_id",
                table: "expense_requests",
                column: "budget_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_requests");
        }
    }
}
