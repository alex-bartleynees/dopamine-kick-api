using Microsoft.EntityFrameworkCore;
using Users.Application.Abstractions;
using Users.Domain.Entities;
using Users.Infrastructure.Configuration;

namespace Users.Infrastructure.DbContexts;

public class UsersContext(DbContextOptions<UsersContext> options) : DbContext(options), IUsersUnitOfWork
{
   public DbSet<User> Users { get; set; }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
   }
}