using Users.Domain.Entities;

namespace Users.Application.Abstractions;

public interface IUsersRepository
{
    Task<User?> GetUser(Guid userId, CancellationToken ct = default);

    Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default);

    Task CreateUserAsync(User user, CancellationToken ct = default);
}