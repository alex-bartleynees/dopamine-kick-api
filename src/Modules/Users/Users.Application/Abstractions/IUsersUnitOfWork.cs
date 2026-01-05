namespace Users.Application.Abstractions;

public interface IUsersUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
