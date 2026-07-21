using Ardalis.GuardClauses;
using SharedKernel.Results;
using Mediator;
using Microsoft.Extensions.Caching.Hybrid;
using Users.Application.Abstractions;
using Users.Domain.Entities;
using Users.Domain.Errors;

namespace Users.Application.Users.Queries;

public record GetUserById(Guid UserId) : IRequest<Result<User>>;

public class GetUserByIdHandler : IRequestHandler<GetUserById, Result<User>>
{
    private readonly IUsersRepository _usersRepository;
    private readonly HybridCache _cache;

    public GetUserByIdHandler(IUsersRepository usersRepository, HybridCache cache)
    {
        _usersRepository = usersRepository ??
                           throw new ArgumentNullException(nameof(usersRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async ValueTask<Result<User>> Handle(GetUserById request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        var cacheKey = $"user:{request.UserId}";

        var user = await _cache.GetOrCreateAsync<User?>(
            cacheKey,
            async ct =>
            {
                var user = await _usersRepository.GetUser(request.UserId, ct);
                return user;
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(15),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            },
            cancellationToken: cancellationToken
        );

        if (user is null)
        {
            return Result<User>.Failure(UserErrors.NotFound(request.UserId));
        }

        return Result<User>.Success(user);
    }
}