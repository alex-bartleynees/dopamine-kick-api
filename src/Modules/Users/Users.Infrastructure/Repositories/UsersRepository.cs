using Common.Abstractions.Results;
using Microsoft.EntityFrameworkCore;
using Users.Application.Abstractions;
using Users.Domain.Entities;
using Users.Infrastructure.DbContexts;

namespace Users.Infrastructure.Repositories;

public class UsersRepository(UsersContext context) : IUsersRepository
{
    public async Task<Result<User>> GetUser(Guid userId)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId).FirstOrDefaultAsync();

        if (user == null)
        {
            return Result<User>.Failure(new Error(404, "Not Found", $"User with id: {userId} was not found"));
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Email == email).FirstOrDefaultAsync();

        if (user == null)
        {
            return Result<User>.Failure(new Error(404, "Not Found", $"User with email: {email} was not found"));
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<User>> CreateUserAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        return Result<User>.Success(user);
    }
}