using Ardalis.GuardClauses;
using Common.Abstractions.Results;
using Mediator;
using Users.Application.Abstractions;
using Users.Application.Common.Models;
using Users.Domain.Entities;

namespace Users.Application.Users.Commands;

    public record CreateUser(UserForCreationDto User) : IRequest<Result<User>>;

    public class CreateUserHandler : IRequestHandler<CreateUser, Result<User>>
    {
        private readonly IUsersRepository _usersRepository;
        private readonly IKeycloakService _keycloakService;

        public CreateUserHandler(IUsersRepository usersRepository, IKeycloakService keycloakService)
        {
            _usersRepository = usersRepository ?? throw new ArgumentNullException(nameof(usersRepository));
            _keycloakService = keycloakService ?? throw new ArgumentNullException(nameof(keycloakService));
        }

        public async ValueTask<Result<User>> Handle(CreateUser request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            Guard.Against.Null(request.User);

            // 1. Check if user exists in local DB
            var localUserResult = await _usersRepository.GetUserByEmailAsync(request.User.Email);
            if (localUserResult.IsSuccess)
            {
                return Result<User>.Failure(new Error(409, "Conflict", $"User with email {request.User.Email} already exists"));
            }

            // 2. Check if user exists in Keycloak
            var keycloakUserResult = await _keycloakService.GetUserByEmailAsync(request.User.Email);

            if (keycloakUserResult.IsSuccess)
            {
                // User exists in Keycloak but not in local DB - sync to local DB
                var keycloakUser = keycloakUserResult.ValueOrThrow;
                var syncedUser = new User(
                    $"{keycloakUser.FirstName} {keycloakUser.LastName}".Trim(),
                    keycloakUser.Username,
                    request.User.Image)
                {
                    Id = Guid.Parse(keycloakUser.Id),
                    Email = keycloakUser.Email
                };

                var createResult = await _usersRepository.CreateUserAsync(syncedUser);
                if (createResult.IsFailure)
                {
                    return Result<User>.Failure(createResult.Error);
                }

                return Result<User>.Success(createResult.ValueOrThrow);
            }

            // 3. User doesn't exist in Keycloak - create in both
            var createKeycloakResult = await _keycloakService.CreateUserAsync(request.User);
            if (createKeycloakResult.IsFailure)
            {
                return Result<User>.Failure(createKeycloakResult.Error);
            }

            var createdKeycloakUser = createKeycloakResult.ValueOrThrow;
            var newUser = new User(request.User.Name, request.User.Username, request.User.Image)
            {
                Id = Guid.Parse(createdKeycloakUser.Id),
                Email = request.User.Email
            };

            var localCreateResult = await _usersRepository.CreateUserAsync(newUser);
            if (localCreateResult.IsFailure)
            {
                return Result<User>.Failure(localCreateResult.Error);
            }

            return Result<User>.Success(localCreateResult.ValueOrThrow);
        }
    }