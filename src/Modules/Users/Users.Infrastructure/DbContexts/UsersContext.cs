using Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Users.Domain.Entities;
using Users.Infrastructure.Configuration;

namespace Users.Infrastructure.DbContexts;

public class UsersContext(IConfiguration configuration) : DbContext, IUnitOfWork
{
   public DbSet<User> Users { get; set; }

   protected override void OnConfiguring(DbContextOptionsBuilder options)
   {
      options.UseNpgsql(configuration.GetConnectionString("UsersDBConnectionString") ??
                        throw new ArgumentNullException(nameof(options), "No connection string provided"));
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
   }
}