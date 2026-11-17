namespace TecNM.Api.Core.Security;

/// <summary>Permisos del panel y los que otorga cada rol. Matriz en Modules/Access/specs/access.spec.md.</summary>
public static class Permissions
{
    public const string HerdRead = "herd.read";
    public const string HerdWrite = "herd.write";
    public const string HerdDelete = "herd.delete";
    public const string HealthRead = "health.read";
    public const string HealthWrite = "health.write";
    public const string HealthDelete = "health.delete";
    public const string ReproRead = "repro.read";
    public const string ReproWrite = "repro.write";
    public const string ReproDelete = "repro.delete";
    public const string MilkingRead = "milking.read";
    public const string MilkingWrite = "milking.write";
    public const string MilkingDelete = "milking.delete";
    public const string SlaughterRead = "slaughter.read";
    public const string SlaughterWrite = "slaughter.write";
    public const string SlaughterDelete = "slaughter.delete";
    public const string CheeseRead = "cheese.read";
    public const string CheeseProduce = "cheese.produce";
    public const string CheeseDelete = "cheese.delete";
    public const string CheesePurchases = "cheese.purchases";
    public const string SalesCheese = "sales.cheese";
    public const string SalesLivestock = "sales.livestock";
    public const string SalesCustomers = "sales.customers";
    public const string SalesCancel = "sales.cancel";
    public const string PaymentsView = "payments.view";
    public const string UsersManage = "users.manage";

    public const string RoleOwner = "owner";

    public static readonly IReadOnlyList<string> All =
    [
        HerdRead, HerdWrite, HerdDelete, HealthRead, HealthWrite, HealthDelete, ReproRead, ReproWrite, ReproDelete,
        MilkingRead, MilkingWrite, MilkingDelete, SlaughterRead, SlaughterWrite, SlaughterDelete,
        CheeseRead, CheeseProduce, CheeseDelete, CheesePurchases,
        SalesCheese, SalesLivestock, SalesCustomers, SalesCancel, PaymentsView, UsersManage,
    ];

    private static readonly Dictionary<string, string[]> ByRole = new()
    {
        [RoleOwner] = [.. All],
        ["livestock_admin"] =
        [
            HerdRead, HerdWrite, HerdDelete, HealthRead, HealthWrite, HealthDelete, ReproRead, ReproWrite, ReproDelete,
            MilkingRead, MilkingWrite, MilkingDelete, SlaughterRead, SlaughterWrite, SlaughterDelete,
            SalesLivestock, SalesCustomers, SalesCancel, PaymentsView,
        ],
        ["cowboy"] = [HerdRead, HerdWrite, HealthRead, HealthWrite, ReproRead, ReproWrite, MilkingRead, MilkingWrite, SlaughterRead, SlaughterWrite],
        ["milking_supervisor"] = [HerdRead, HealthRead, MilkingRead, MilkingWrite, MilkingDelete],
        ["cheese_admin"] = [MilkingRead, CheeseRead, CheeseProduce, CheeseDelete, CheesePurchases, SalesCheese, SalesCustomers, SalesCancel, PaymentsView],
        ["cheesemaker"] = [CheeseRead, CheeseProduce],
        ["cheese_seller"] = [CheeseRead, SalesCheese, SalesCustomers],
    };

    /// <summary>Permisos que suman los roles indicados, en el orden de <see cref="All"/>.</summary>
    public static IReadOnlyList<string> For(IEnumerable<string> roleCodes)
    {
        var granted = roleCodes.SelectMany(code => ByRole.GetValueOrDefault(code, [])).ToHashSet();
        return All.Where(granted.Contains).ToList();
    }
}
