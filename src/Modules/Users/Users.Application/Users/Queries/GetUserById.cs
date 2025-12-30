using Ardalis.GuardClauses;
using Common.Abstractions.Results;
using Mediator;
using Users.Application.Abstractions;
using Users.Domain.Entities;

namespace Users.Application.Users.Queries;

public record GetUserById(Guid userId) : IRequest<Result<User>>;
public class GetUserByIdHandler : IRequestHandler<GetUserById, Result<User>>
{
    private readonly IUsersRepository _usersRepository;

    public GetUserByIdHandler(IUsersRepository usersRepository)
    {
        _usersRepository = usersRepository ??
                                 throw new ArgumentNullException(nameof(usersRepository));
    }

    public async ValueTask<Result<User>> Handle(GetUserById request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        return await _usersRepository.GetUser(request.userId);
    }
}