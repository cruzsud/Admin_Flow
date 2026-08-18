using AdminFlow.Budget.Domain.ExpenseRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseRequestConfiguration : IEntityTypeConfiguration<ExpenseRequest>
{
    public void Configure(EntityTypeBuilder<ExpenseRequest> builder)
    {
        builder.ToTable("expense_requests", table =>
        {
            table.HasCheckConstraint("ck_expense_requests_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_expense_requests_status_valid", "status IN (1, 2, 3)");
            table.HasCheckConstraint(
                "ck_expense_requests_decision_consistent",
                "(status = 1 AND decision_maker_id IS NULL AND decided_at IS NULL " +
                "AND rejection_reason IS NULL) OR " +
                "(status = 2 AND decision_maker_id IS NOT NULL AND decided_at IS NOT NULL " +
                "AND rejection_reason IS NULL) OR " +
                "(status = 3 AND decision_maker_id IS NOT NULL AND decided_at IS NOT NULL " +
                "AND rejection_reason IS NOT NULL AND btrim(rejection_reason) <> '')");
        });

        builder.HasKey(request => request.Id).HasName("pk_expense_requests");

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(request => request.BudgetId)
            .HasColumnName("budget_id")
            .IsRequired();

        builder.Property(request => request.RequesterId)
            .HasColumnName("requester_id")
            .IsRequired();

        builder.Property(request => request.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(request => request.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(request => request.DecisionMakerId)
            .HasColumnName("decision_maker_id");

        builder.Property(request => request.DecidedAt)
            .HasColumnName("decided_at");

        builder.Property(request => request.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasColumnType("text");

        builder.HasIndex(request => request.BudgetId)
            .HasDatabaseName("ix_expense_requests_budget_id");

        builder.HasOne<BudgetEntity>()
            .WithMany()
            .HasForeignKey(request => request.BudgetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expense_requests_budgets");
    }
}
