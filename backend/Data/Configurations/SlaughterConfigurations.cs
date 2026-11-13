using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class SlaughterRunConfiguration : IEntityTypeConfiguration<SlaughterRun>
{
    public void Configure(EntityTypeBuilder<SlaughterRun> builder)
    {
        builder.ToTable("slaughter_runs");
        builder.Property(run => run.Cost).HasPrecision(10, 2);
        builder.HasMany(run => run.Kids).WithOne(kid => kid.SlaughterRun).HasForeignKey(kid => kid.SlaughterRunId);
        builder.HasQueryFilter(run => run.DeletedAt == null);
    }
}

public sealed class KidSlaughterConfiguration : IEntityTypeConfiguration<KidSlaughter>
{
    public void Configure(EntityTypeBuilder<KidSlaughter> builder)
    {
        builder.ToTable("kid_slaughters");
        builder.Property(kid => kid.LiveWeightKg).HasPrecision(6, 2);
        builder.Property(kid => kid.CarcassWeightKg).HasPrecision(6, 2);
        builder.HasOne(kid => kid.Animal).WithMany().HasForeignKey(kid => kid.AnimalId);
        builder.HasQueryFilter(kid => kid.DeletedAt == null);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasQueryFilter(order => order.DeletedAt == null);
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.Property(item => item.Quantity).HasPrecision(10, 3);
        builder.Property(item => item.UnitPrice).HasPrecision(10, 2);
        builder.HasOne(item => item.Order).WithMany().HasForeignKey(item => item.OrderId);
        builder.HasQueryFilter(item => item.DeletedAt == null);
    }
}
