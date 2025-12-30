using Common.Abstractions.Results;
using Users.Domain.Entities;

namespace Users.Application.Abstractions;

public interface IUsersRepository
{
    Task<Result<User>> GetUser(Guid userId);

    Task<Result<User>> GetUserByEmailAsync(string email);

    Task<Result<User>> CreateUserAsync(User user); 
}