using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TecNM.Api.Core.Entities;

namespace TecNM.Api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasMany(user => user.UserRoles).WithOne(userRole => userRole.User).HasForeignKey(userRole => userRole.UserId);
        builder.HasQueryFilter(user => user.DeletedAt == null);
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasQueryFilter(role => role.DeletedAt == null);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasOne(userRole => userRole.Role).WithMany().HasForeignKey(userRole => userRole.RoleId);
        builder.HasQueryFilter(userRole => userRole.DeletedAt == null);
    }
}
