using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

// Las llaves compuestas por especie (hembra y semental de la misma especie) viven en database.sql.

public sealed class BreedingEventConfiguration : IEntityTypeConfiguration<BreedingEvent>
{
    public void Configure(EntityTypeBuilder<BreedingEvent> builder)
    {
        builder.ToTable("breeding_events");
        builder.HasOne(breeding => breeding.Female).WithMany().HasForeignKey(breeding => breeding.FemaleId);
        builder.HasOne(breeding => breeding.Male).WithMany().HasForeignKey(breeding => breeding.MaleId);
        builder.HasMany(breeding => breeding.Checks).WithOne(check => check.BreedingEvent).HasForeignKey(check => check.BreedingEventId);
        builder.HasQueryFilter(breeding => breeding.DeletedAt == null);
    }
}

public sealed class PregnancyCheckConfiguration : IEntityTypeConfiguration<PregnancyCheck>
{
    public void Configure(EntityTypeBuilder<PregnancyCheck> builder)
    {
        builder.ToTable("pregnancy_checks");
        builder.HasOne(check => check.VetVisit).WithMany().HasForeignKey(check => check.VetVisitId);
        builder.HasQueryFilter(check => check.DeletedAt == null);
    }
}

public sealed class BirthEventConfiguration : IEntityTypeConfiguration<BirthEvent>
{
    public void Configure(EntityTypeBuilder<BirthEvent> builder)
    {
        builder.ToTable("birth_events");
        builder.HasOne(birth => birth.Dam).WithMany().HasForeignKey(birth => birth.DamId);
        builder.HasOne(birth => birth.BreedingEvent).WithMany().HasForeignKey(birth => birth.BreedingEventId);
        builder.HasMany(birth => birth.Offspring).WithOne().HasForeignKey(animal => animal.BirthEventId);
        builder.HasQueryFilter(birth => birth.DeletedAt == null);
    }
}
