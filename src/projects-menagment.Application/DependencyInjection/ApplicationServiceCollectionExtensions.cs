using Microsoft.Extensions.DependencyInjection;
using projects_menagment.Application.Interfaces.Services;
using projects_menagment.Application.Services;

namespace projects_menagment.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IProjectService, ProjectService>();
        return services;
    }
}
