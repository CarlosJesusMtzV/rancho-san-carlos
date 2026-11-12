using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data;

public sealed partial class TecNMDbContext(DbContextOptions<TecNMDbContext> options) : DbContext(options)
{
    public DbSet<Species> Species => Set<Species>();
    public DbSet<Breed> Breeds => Set<Breed>();
    public DbSet<AnimalLot> AnimalLots => Set<AnimalLot>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<WeightRecord> WeightRecords => Set<WeightRecord>();
    public DbSet<CalfSaleCandidate> CalfSaleCandidates => Set<CalfSaleCandidate>();
    public DbSet<SupplyType> SupplyTypes => Set<SupplyType>();
    public DbSet<Supply> Supplies => Set<Supply>();
    public DbSet<VetVisit> VetVisits => Set<VetVisit>();
    public DbSet<HealthEvent> HealthEvents => Set<HealthEvent>();
    public DbSet<Treatment> Treatments => Set<Treatment>();
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<ActiveWithdrawal> ActiveWithdrawals => Set<ActiveWithdrawal>();
    public DbSet<BreedingEvent> BreedingEvents => Set<BreedingEvent>();
    public DbSet<PregnancyCheck> PregnancyChecks => Set<PregnancyCheck>();
    public DbSet<BirthEvent> BirthEvents => Set<BirthEvent>();
    public DbSet<MilkLot> MilkLots => Set<MilkLot>();
    public DbSet<MilkRecord> MilkRecords => Set<MilkRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TecNMDbContext).Assembly);

        // database.sql usa snake_case: cada propiedad PascalCase se mapea a su columna equivalente.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public static string ToSnakeCase(string name) => UpperCaseBoundary().Replace(name, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex UpperCaseBoundary();
}
