# DopamineKick API

A modular monolith API built with .NET 10 for habit tracking and user notifications.

## Architecture

This project follows a **Modular Monolith** architecture pattern, combining the simplicity of a monolith with the organizational benefits of microservices. Each module is isolated with its own database and can communicate with other modules through integration events.

### Project Structure

```
src/
├── Host/
│   └── WebApi/                    # API host application
├── Modules/
│   ├── Users/                     # User management module
│   │   ├── Users.Api/            # Endpoints and module configuration
│   │   ├── Users.Application/    # Business logic and use cases
│   │   ├── Users.Domain/         # Domain entities and rules
│   │   └── Users.Infrastructure/ # Data access and external services
│   ├── Habits/                    # Habit tracking module
│   │   ├── Habits.Api/
│   │   ├── Habits.Application/
│   │   ├── Habits.Domain/
│   │   └── Habits.Infrastructure/
│   └── Notifications/             # Notification system module
│       ├── Notifications.Api/
│       ├── Notifications.Application/
│       ├── Notifications.Domain/
│       └── Notifications.Infrastructure/
└── Shared/
    ├── Common.Abstractions/       # Shared interfaces and contracts
    ├── Common.Infrastructure/     # Shared infrastructure code
    └── Common.IntegrationEvents/  # Integration event definitions
```

### Architectural Principles

- **Module Independence**: Each module has its own database and bounded context
- **Clean Architecture**: Each module follows Clean Architecture with Api, Application, Domain, and Infrastructure layers
- **Event-Driven Communication**: Modules communicate asynchronously through integration events via RabbitMQ
- **Database Per Module**: Each module maintains its own PostgreSQL database to enforce boundaries
- **Shared Kernel**: Common abstractions and infrastructure are shared across modules

## Technologies

- **.NET 10** - Core framework
- **ASP.NET Core Minimal APIs** - RESTful endpoints
- **Entity Framework Core 10** - ORM with PostgreSQL provider
- **PostgreSQL** - Database (separate database per module)
- **RabbitMQ** - Message broker for integration events
- **Redis** - Distributed caching with StackExchange.Redis
- **Keycloak** - Identity and access management (JWT authentication)
- **Quartz.NET** - Job scheduling and background tasks
- **FluentValidation** - Input validation
- **Mediator** - In-process messaging pattern
- **WebPush** - Push notifications support
- **Swagger/OpenAPI** - API documentation

## Prerequisites

- .NET 10 SDK
- Docker and Docker Compose (for infrastructure services)
- PostgreSQL 16+
- RabbitMQ
- Redis
- Keycloak instance

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd DopamineKick.API
```

### 2. Start Infrastructure Services

The project includes a `docker-compose.yml` file to run required infrastructure:

```bash
docker-compose up -d
```

This will start:
- PostgreSQL
- RabbitMQ
- Redis

### 3. Configure Application Settings

Update `src/Host/WebApi/appsettings.json` with your configuration:

```json
{
  "ConnectionStrings": {
    "UsersDBConnectionString": "Host=localhost;Database=UsersDB;Username=myuser;Password=mypassword;",
    "HabitsDBConnectionString": "Host=localhost;Database=HabitsDB;Username=myuser;Password=mypassword;",
    "RedisConnection": "localhost:6379"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  },
  "Jwt": {
    "Authority": "https://your-keycloak-instance/realms/your-realm",
    "Audience": "account"
  },
  "Keycloak": {
    "BaseUrl": "https://your-keycloak-instance",
    "Realm": "your-realm",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  },
  "WebPush": {
    "PrivateKey": "your-private-key",
    "PublicKey": "your-public-key",
    "Subject": "mailto:your-email@example.com"
  }
}
```

### 4. Run Database Migrations

Database migrations run automatically on application startup. Each module manages its own database schema.

### 5. Run the Application

```bash
cd src/Host/WebApi
dotnet run
```

The API will be available at:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:5001/swagger`

## Modules

### Users Module

Handles user management, authentication, and user-related operations.

**Responsibilities:**
- User registration and profile management
- Integration with Keycloak for authentication
- User data persistence

**Database:** UsersDB

### Habits Module

Manages habit tracking, completions, and reminders.

**Responsibilities:**
- Create and manage habits
- Track habit completions
- Schedule habit reminders
- Publish integration events when habits require notifications

**Database:** HabitsDB

**Background Services:**
- Outbox publisher for reliable event publishing

### Notifications Module

Handles all notification-related functionality including push notifications and scheduled reminders.

**Responsibilities:**
- Consume integration events from other modules
- Manage WebPush subscriptions
- Send push notifications
- Schedule and execute reminder jobs using Quartz.NET
- Maintain idempotent event processing

**Database:** NotificationsDB (includes Quartz.NET tables)

**Background Services:**
- Integration event consumers
- Quartz.NET job scheduler for habit reminders

## Integration Events

Modules communicate asynchronously through integration events published to RabbitMQ. This ensures loose coupling and independent scalability.

**Key Integration Events:**
- `HabitReminderCreatedIntegrationEvent` - Published when a habit reminder is created
- Additional events as modules evolve

For detailed information on integration events, see [Integration Events Guide](docs/INTEGRATION_EVENTS_GUIDE.md).

**Event Processing Features:**
- Idempotent event processing (events are processed exactly once)
- Outbox pattern for reliable event publishing
- Dead letter queue handling for failed events

## API Documentation

When running in development mode, Swagger UI is available at `/swagger` and provides interactive API documentation.

**Authentication:**
All endpoints (except health checks) require a valid JWT token from Keycloak. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Development

### Project Guidelines

1. **Module Boundaries**: Never reference other modules directly. Use integration events for inter-module communication.
2. **Database Access**: Each module only accesses its own database.
3. **Shared Code**: Only place truly shared abstractions in the `Common.*` projects.
4. **Clean Architecture**: Maintain dependency rules (Domain → Application → Infrastructure → Api).

### Adding a New Module

1. Create the module structure following the existing pattern:
   ```
   src/Modules/YourModule/
   ├── YourModule.Api/
   ├── YourModule.Application/
   ├── YourModule.Domain/
   └── YourModule.Infrastructure/
   ```

2. Create a module registration class in `YourModule.Api`:
   ```csharp
   public static class YourModuleModule
   {
       public static IServiceCollection AddYourModuleModule(
           this IServiceCollection services,
           IConfiguration configuration)
       {
           // Register services
           return services;
       }
   }
   ```

3. Register the module in `WebApi.Extensions.WebApiExtensions.RegisterServices()`:
   ```csharp
   builder.Services.AddYourModuleModule(builder.Configuration);
   ```

4. Add database migration support if needed.

### Running Tests

```bash
dotnet test
```

## Configuration

### Environment Variables

The application supports configuration through:
- `appsettings.json`
- `appsettings.Development.json`
- Environment variables
- User secrets (for development)

### User Secrets

For development, use user secrets to avoid committing sensitive data:

```bash
cd src/Host/WebApi
dotnet user-secrets set "Jwt:Authority" "your-keycloak-url"
dotnet user-secrets set "Keycloak:ClientSecret" "your-secret"
```

## Deployment

### Docker

Build the Docker image:

```bash
docker build -t dopaminekick-api .
```

Run the container:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__UsersDBConnectionString="your-connection-string" \
  -e ConnectionStrings__HabitsDBConnectionString="your-connection-string" \
  dopaminekick-api
```

## Monitoring and Observability

The application is configured with OpenTelemetry support. Update the OTLP endpoint in configuration to enable telemetry:

```json
"ConnectionStrings": {
  "OTLP_Endpoint": "http://your-otel-collector:4317"
}
```

## Contributing

1. Follow the existing architecture patterns
2. Maintain module boundaries
3. Write tests for new features
4. Update documentation as needed

## License

[Add your license information here]

## Support

For issues and questions, please [open an issue](link-to-issues) in the repository.
