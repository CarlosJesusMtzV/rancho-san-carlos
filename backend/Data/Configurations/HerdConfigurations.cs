using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

// Las restricciones (llaves compuestas por especie, CHECK, triggers) viven en database.sql.
// Aquí solo se describe lo necesario para consultar y guardar.

public sealed class SpeciesConfiguration : IEntityTypeConfiguration<Species>
{
    public void Configure(EntityTypeBuilder<Species> builder)
    {
        builder.ToTable("species");
        builder.Property(species => species.Id).ValueGeneratedNever();
        builder.HasQueryFilter(species => species.DeletedAt == null);
    }
}

public sealed class BreedConfiguration : IEntityTypeConfiguration<Breed>
{
    public void Configure(EntityTypeBuilder<Breed> builder)
    {
        builder.ToTable("breeds");
        builder.HasOne(breed => breed.Species).WithMany().HasForeignKey(breed => breed.SpeciesId);
        builder.HasQueryFilter(breed => breed.DeletedAt == null);
    }
}

public sealed class AnimalLotConfiguration : IEntityTypeConfiguration<AnimalLot>
{
    public void Configure(EntityTypeBuilder<AnimalLot> builder)
    {
        builder.ToTable("animal_lots");
        builder.HasOne(lot => lot.Species).WithMany().HasForeignKey(lot => lot.SpeciesId);
        builder.HasQueryFilter(lot => lot.DeletedAt == null);
    }
}

public sealed class AnimalConfiguration : IEntityTypeConfiguration<Animal>
{
    public void Configure(EntityTypeBuilder<Animal> builder)
    {
        builder.ToTable("animals");
        builder.Property(animal => animal.BirthWeightKg).HasPrecision(6, 2);
        builder.HasOne(animal => animal.Species).WithMany().HasForeignKey(animal => animal.SpeciesId);
        builder.HasOne(animal => animal.Breed).WithMany().HasForeignKey(animal => animal.BreedId);
        builder.HasOne(animal => animal.Lot).WithMany().HasForeignKey(animal => animal.LotId);
        builder.HasOne(animal => animal.Sire).WithMany().HasForeignKey(animal => animal.SireId);
        builder.HasOne(animal => animal.Dam).WithMany().HasForeignKey(animal => animal.DamId);
        builder.HasMany(animal => animal.Weights).WithOne(weight => weight.Animal).HasForeignKey(weight => weight.AnimalId);
        builder.HasQueryFilter(animal => animal.DeletedAt == null);
    }
}

public sealed class WeightRecordConfiguration : IEntityTypeConfiguration<WeightRecord>
{
    public void Configure(EntityTypeBuilder<WeightRecord> builder)
    {
        builder.ToTable("weight_records");
        builder.Property(weight => weight.WeightKg).HasPrecision(7, 2);
        builder.HasQueryFilter(weight => weight.DeletedAt == null);
    }
}

public sealed class CalfSaleCandidateConfiguration : IEntityTypeConfiguration<CalfSaleCandidate>
{
    public void Configure(EntityTypeBuilder<CalfSaleCandidate> builder)
    {
        builder.ToView("v_calf_sale_candidates");
        builder.HasNoKey();
    }
}
