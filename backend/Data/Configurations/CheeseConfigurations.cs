using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class ProductTypeConfiguration : IEntityTypeConfiguration<ProductType>
{
    public void Configure(EntityTypeBuilder<ProductType> builder)
    {
        builder.ToTable("product_types");
        builder.HasQueryFilter(type => type.DeletedAt == null);
    }
}

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.Property(product => product.UnitPrice).HasPrecision(10, 2);
        builder.HasOne(product => product.ProductType).WithMany().HasForeignKey(product => product.ProductTypeId);
        builder.HasQueryFilter(product => product.DeletedAt == null);
    }
}

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasQueryFilter(supplier => supplier.DeletedAt == null);
    }
}

public sealed class CheeseBatchConfiguration : IEntityTypeConfiguration<CheeseBatch>
{
    public void Configure(EntityTypeBuilder<CheeseBatch> builder)
    {
        builder.ToTable("cheese_batches");
        builder.Property(batch => batch.LitersUsed).HasPrecision(8, 2);
        builder.HasOne(batch => batch.MilkLot).WithMany().HasForeignKey(batch => batch.MilkLotId);
        builder.HasMany(batch => batch.Outputs).WithOne(output => output.CheeseBatch).HasForeignKey(output => output.CheeseBatchId);
        builder.HasQueryFilter(batch => batch.DeletedAt == null);
    }
}

public sealed class CheeseBatchOutputConfiguration : IEntityTypeConfiguration<CheeseBatchOutput>
{
    public void Configure(EntityTypeBuilder<CheeseBatchOutput> builder)
    {
        builder.ToTable("cheese_batch_outputs");
        builder.Property(output => output.Quantity).HasPrecision(10, 3);
        builder.HasOne(output => output.Product).WithMany().HasForeignKey(output => output.ProductId);
        builder.HasQueryFilter(output => output.DeletedAt == null);
    }
}

public sealed class CheesePurchaseConfiguration : IEntityTypeConfiguration<CheesePurchase>
{
    public void Configure(EntityTypeBuilder<CheesePurchase> builder)
    {
        builder.ToTable("cheese_purchases");
        builder.Property(purchase => purchase.Quantity).HasPrecision(10, 3);
        builder.Property(purchase => purchase.UnitCost).HasPrecision(10, 2);
        builder.HasOne(purchase => purchase.Supplier).WithMany().HasForeignKey(purchase => purchase.SupplierId);
        builder.HasOne(purchase => purchase.Product).WithMany().HasForeignKey(purchase => purchase.ProductId);
        builder.HasQueryFilter(purchase => purchase.DeletedAt == null);
    }
}

public sealed class CheeseStockAdjustmentConfiguration : IEntityTypeConfiguration<CheeseStockAdjustment>
{
    public void Configure(EntityTypeBuilder<CheeseStockAdjustment> builder)
    {
        builder.ToTable("cheese_stock_adjustments");
        builder.Property(adjustment => adjustment.Quantity).HasPrecision(10, 3);
        builder.HasQueryFilter(adjustment => adjustment.DeletedAt == null);
    }
}

public sealed class CheeseStockConfiguration : IEntityTypeConfiguration<CheeseStock>
{
    public void Configure(EntityTypeBuilder<CheeseStock> builder)
    {
        builder.ToView("v_cheese_stock");
        builder.HasNoKey();
    }
}
