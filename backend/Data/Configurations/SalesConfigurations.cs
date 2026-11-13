using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class CustomerTypeConfiguration : IEntityTypeConfiguration<CustomerType>
{
    public void Configure(EntityTypeBuilder<CustomerType> builder)
    {
        builder.ToTable("customer_types");
        builder.HasQueryFilter(type => type.DeletedAt == null);
    }
}

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customers");
        builder.HasOne(customer => customer.CustomerType).WithMany().HasForeignKey(customer => customer.CustomerTypeId);
        builder.HasQueryFilter(customer => customer.DeletedAt == null);
    }
}

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasOne(order => order.Customer).WithMany().HasForeignKey(order => order.CustomerId);
        builder.HasMany(order => order.Items).WithOne(item => item.Order).HasForeignKey(item => item.OrderId);
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
        builder.HasOne(item => item.Product).WithMany().HasForeignKey(item => item.ProductId);
        builder.HasQueryFilter(item => item.DeletedAt == null);
    }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.Property(invoice => invoice.Subtotal).HasPrecision(12, 2);
        builder.Property(invoice => invoice.Taxes).HasPrecision(12, 2);
        builder.Property(invoice => invoice.Total).HasPrecision(12, 2);
        builder.HasQueryFilter(invoice => invoice.DeletedAt == null);
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");
        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.HasQueryFilter(payment => payment.DeletedAt == null);
    }
}
