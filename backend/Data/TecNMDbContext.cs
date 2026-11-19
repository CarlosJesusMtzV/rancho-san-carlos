using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TecNM.Api.Core.Entities;
using TecNM.Api.Core.Security;

namespace TecNM.Api.Data;

public sealed partial class TecNMDbContext(DbContextOptions<TecNMDbContext> options, ICurrentUser currentUser) : DbContext(options)
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
    public DbSet<BreedingSeason> BreedingSeasons => Set<BreedingSeason>();
    public DbSet<BreedingEvent> BreedingEvents => Set<BreedingEvent>();
    public DbSet<PregnancyCheck> PregnancyChecks => Set<PregnancyCheck>();
    public DbSet<BirthEvent> BirthEvents => Set<BirthEvent>();
    public DbSet<MilkLot> MilkLots => Set<MilkLot>();
    public DbSet<MilkRecord> MilkRecords => Set<MilkRecord>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<CheeseBatch> CheeseBatches => Set<CheeseBatch>();
    public DbSet<CheeseBatchOutput> CheeseBatchOutputs => Set<CheeseBatchOutput>();
    public DbSet<CheesePurchase> CheesePurchases => Set<CheesePurchase>();
    public DbSet<CheeseStockAdjustment> CheeseStockAdjustments => Set<CheeseStockAdjustment>();
    public DbSet<CheeseStock> CheeseStock => Set<CheeseStock>();
    public DbSet<SlaughterRun> SlaughterRuns => Set<SlaughterRun>();
    public DbSet<KidSlaughter> KidSlaughters => Set<KidSlaughter>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CustomerType> CustomerTypes => Set<CustomerType>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

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
        // Auditoría: cuándo y quién creó, modificó o eliminó (borrado lógico) cada registro.
        var now = DateTime.UtcNow;
        var userId = currentUser.Id;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                var deletedAt = entry.Property(entity => entity.DeletedAt);
                if (deletedAt.IsModified && deletedAt.OriginalValue is null && deletedAt.CurrentValue is not null)
                {
                    entry.Entity.DeletedBy = userId;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public static string ToSnakeCase(string name) => UpperCaseBoundary().Replace(name, "$1_$2").ToLowerInvariant();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex UpperCaseBoundary();
}
