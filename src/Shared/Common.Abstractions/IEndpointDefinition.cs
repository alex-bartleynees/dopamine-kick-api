using Microsoft.AspNetCore.Builder;

namespace Common.Abstractions;

public interface IEndpointDefinition
{
    void RegisterEndpoints(WebApplication app);
}
