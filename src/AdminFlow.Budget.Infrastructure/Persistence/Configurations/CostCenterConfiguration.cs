using AdminFlow.Budget.Domain.CostCenters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AdminFlow.Budget.Infrastructure.Persistence.Configurations;

internal sealed class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> builder)
    {
        builder.ToTable("cost_centers");

        builder.HasKey(costCenter => costCenter.Id)
            .HasName("pk_cost_centers");

        builder.Property(costCenter => costCenter.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(costCenter => costCenter.Code)
            .HasColumnName("code")
            .IsRequired();

        builder.Property(costCenter => costCenter.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.HasIndex(costCenter => costCenter.Code)
            .IsUnique()
            .HasDatabaseName("ux_cost_centers_code");
    }
}
