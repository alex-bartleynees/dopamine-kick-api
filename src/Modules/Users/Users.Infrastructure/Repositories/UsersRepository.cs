using Microsoft.EntityFrameworkCore;
using Users.Application.Abstractions;
using Users.Domain.Entities;
using Users.Infrastructure.DbContexts;

namespace Users.Infrastructure.Repositories;

public class UsersRepository(UsersContext context) : IUsersRepository
{
    public async Task<User?> GetUser(Guid userId, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId).FirstOrDefaultAsync(ct);

        return user;
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Email == email).FirstOrDefaultAsync(ct);

        return user;
    }

    public async Task CreateUserAsync(User user, CancellationToken ct = default)
    {
        await context.Users.AddAsync(user, ct);
    }
}