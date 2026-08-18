using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminFlow.Budget.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "decided_at",
                table: "expense_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "decision_maker_id",
                table: "expense_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "expense_requests",
                type: "text",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_expense_requests_decision_consistent",
                table: "expense_requests",
                sql: "(status = 1 AND decision_maker_id IS NULL AND decided_at IS NULL AND rejection_reason IS NULL) OR (status = 2 AND decision_maker_id IS NOT NULL AND decided_at IS NOT NULL AND rejection_reason IS NULL) OR (status = 3 AND decision_maker_id IS NOT NULL AND decided_at IS NOT NULL AND rejection_reason IS NOT NULL AND btrim(rejection_reason) <> '')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_expense_requests_decision_consistent",
                table: "expense_requests");

            migrationBuilder.DropColumn(
                name: "decided_at",
                table: "expense_requests");

            migrationBuilder.DropColumn(
                name: "decision_maker_id",
                table: "expense_requests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "expense_requests");

        }
    }
}
