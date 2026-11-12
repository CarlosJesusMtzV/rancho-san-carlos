using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class MilkLotConfiguration : IEntityTypeConfiguration<MilkLot>
{
    public void Configure(EntityTypeBuilder<MilkLot> builder)
    {
        builder.ToTable("milk_lots");
        builder.Property(lot => lot.TotalLiters).HasPrecision(8, 2);
        builder.HasMany(lot => lot.Records).WithOne(record => record.MilkLot).HasForeignKey(record => record.MilkLotId);
        builder.HasQueryFilter(lot => lot.DeletedAt == null);
    }
}

public sealed class MilkRecordConfiguration : IEntityTypeConfiguration<MilkRecord>
{
    public void Configure(EntityTypeBuilder<MilkRecord> builder)
    {
        builder.ToTable("milk_records");
        builder.HasOne(record => record.Animal).WithMany().HasForeignKey(record => record.AnimalId);
        builder.HasQueryFilter(record => record.DeletedAt == null);
    }
}
