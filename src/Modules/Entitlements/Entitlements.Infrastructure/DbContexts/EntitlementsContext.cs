using Entitlements.Domain.Entities;
using Entitlements.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Entitlements.Infrastructure.DbContexts;

public class EntitlementsContext(DbContextOptions<EntitlementsContext> options) : DbContext(options)
{
    public DbSet<Entitlement> Entitlements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EntitlementConfiguration).Assembly);
    }
}
