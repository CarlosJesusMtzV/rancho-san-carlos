using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class SupplyTypeConfiguration : IEntityTypeConfiguration<SupplyType>
{
    public void Configure(EntityTypeBuilder<SupplyType> builder)
    {
        builder.ToTable("supply_types");
        builder.HasQueryFilter(type => type.DeletedAt == null);
    }
}

public sealed class SupplyConfiguration : IEntityTypeConfiguration<Supply>
{
    public void Configure(EntityTypeBuilder<Supply> builder)
    {
        builder.ToTable("supplies");
        builder.HasOne(supply => supply.SupplyType).WithMany().HasForeignKey(supply => supply.SupplyTypeId);
        builder.HasQueryFilter(supply => supply.DeletedAt == null);
    }
}

public sealed class VetVisitConfiguration : IEntityTypeConfiguration<VetVisit>
{
    public void Configure(EntityTypeBuilder<VetVisit> builder)
    {
        builder.ToTable("vet_visits");
        builder.Property(visit => visit.ServiceFee).HasPrecision(10, 2);
        builder.Property(visit => visit.MedicationCost).HasPrecision(10, 2);
        builder.HasMany(visit => visit.Events).WithOne(item => item.VetVisit).HasForeignKey(item => item.VetVisitId);
        builder.HasQueryFilter(visit => visit.DeletedAt == null);
    }
}

public sealed class HealthEventConfiguration : IEntityTypeConfiguration<HealthEvent>
{
    public void Configure(EntityTypeBuilder<HealthEvent> builder)
    {
        builder.ToTable("health_events");
        builder.HasOne(item => item.Animal).WithMany().HasForeignKey(item => item.AnimalId);
        builder.HasMany(item => item.Treatments).WithOne(treatment => treatment.HealthEvent).HasForeignKey(treatment => treatment.HealthEventId);
        builder.HasQueryFilter(item => item.DeletedAt == null);
    }
}

public sealed class TreatmentConfiguration : IEntityTypeConfiguration<Treatment>
{
    public void Configure(EntityTypeBuilder<Treatment> builder)
    {
        builder.ToTable("treatments");
        builder.HasOne(treatment => treatment.Supply).WithMany().HasForeignKey(treatment => treatment.SupplyId);
        builder.HasQueryFilter(treatment => treatment.DeletedAt == null);
    }
}

public sealed class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.ToTable("lab_tests");
        builder.HasOne(test => test.Animal).WithMany().HasForeignKey(test => test.AnimalId);
        builder.HasQueryFilter(test => test.DeletedAt == null);
    }
}

public sealed class ActiveWithdrawalConfiguration : IEntityTypeConfiguration<ActiveWithdrawal>
{
    public void Configure(EntityTypeBuilder<ActiveWithdrawal> builder)
    {
        builder.ToView("v_active_withdrawals");
        builder.HasNoKey();
    }
}
