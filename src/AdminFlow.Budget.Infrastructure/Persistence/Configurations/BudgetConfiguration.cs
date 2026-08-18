using AdminFlow.Budget.Domain.CostCenters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BudgetEntity = AdminFlow.Budget.Domain.Budgets.Budget;

namespace AdminFlow.Budget.Infrastructure.Persistence.Configurations;

internal sealed class BudgetConfiguration : IEntityTypeConfiguration<BudgetEntity>
{
    public void Configure(EntityTypeBuilder<BudgetEntity> builder)
    {
        builder.ToTable("budgets", table =>
        {
            table.HasCheckConstraint(
                "ck_budgets_fiscal_year",
                "fiscal_year BETWEEN 1 AND 9999");
            table.HasCheckConstraint(
                "ck_budgets_allocated_positive",
                "allocated > 0");
            table.HasCheckConstraint(
                "ck_budgets_committed_non_negative",
                "committed >= 0");
            table.HasCheckConstraint(
                "ck_budgets_committed_within_allocation",
                "committed <= allocated");
        });

        builder.HasKey(budget => budget.Id)
            .HasName("pk_budgets");

        builder.Property(budget => budget.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(budget => budget.CostCenterId)
            .HasColumnName("cost_center_id")
            .IsRequired();

        builder.Property(budget => budget.FiscalYear)
            .HasColumnName("fiscal_year")
            .IsRequired();

        builder.Property(budget => budget.Allocated)
            .HasColumnName("allocated")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(budget => budget.Committed)
            .HasColumnName("committed")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Ignore(budget => budget.Available);

        builder.Property<uint>("xmin")
            .IsRowVersion();

        builder.HasIndex(budget => new { budget.CostCenterId, budget.FiscalYear })
            .IsUnique()
            .HasDatabaseName("ux_budgets_cost_center_fiscal_year");

        builder.HasOne<CostCenter>()
            .WithMany()
            .HasForeignKey(budget => budget.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_budgets_cost_centers");
    }
}
