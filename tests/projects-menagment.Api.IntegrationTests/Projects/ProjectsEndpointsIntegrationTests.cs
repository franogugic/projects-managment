using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using projects_menagment.Api.IntegrationTests.Infrastructure;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;
using projects_menagment.Infrastructure.Persistence;
using Xunit;

namespace projects_menagment.Api.IntegrationTests.Projects;

public sealed class ProjectsEndpointsIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly Guid DefaultPlanId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly ApiWebApplicationFactory _factory;

    public ProjectsEndpointsIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateProject_WhenUserIsOwner_CreatesProject()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);

        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Owner");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var createProjectRequest = new
        {
            name = "Project Integration Alpha",
            description = "Project created in integration test.",
            deadline = DateTime.UtcNow.AddDays(10),
            budget = 1500m,
            status = "PLANNED"
        };

        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/projects", createProjectRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var created = db.Projects.SingleOrDefault(x => x.OrganizationId == orgId && x.Name == "Project Integration Alpha");

        Assert.NotNull(created);
        Assert.Equal(0, created!.TotalTasksCount);
        Assert.Equal(0, created.FinishedTasksCount);
    }

    [Fact]
    public async Task CreateProject_WhenUserIsEmployee_ReturnsForbidden()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var employeeToken = await LoginAndGetAccessTokenAsync(employeeEmail);

        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Employee");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var response = await client.PostAsJsonAsync($"/api/organizations/{orgId}/projects", new
        {
            name = "Project Should Fail",
            description = "Employee should not be allowed.",
            budget = 500m,
            status = "PLANNED"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task EnsureDefaultPlanExistsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Plans.Any(plan => plan.Id == DefaultPlanId))
        {
            return;
        }

        var plan = Plan.Create(PlanCode.Free, "Free", 5, 50, 0m);
        SetEntityId(plan, DefaultPlanId);
        db.Plans.Add(plan);
        await db.SaveChangesAsync();
    }

    private async Task SignupAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/signup", new
        {
            firstName = "Test",
            lastName = "User",
            email,
            password = "Valid1!Pass"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<string> LoginAndGetAccessTokenAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "Valid1!Pass"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<LoginResponseContract>();
        Assert.NotNull(payload);

        return payload!.AccessToken;
    }

    private async Task<Guid> CreateOrganizationAsync(string accessToken, string organizationName)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/organizations", new
        {
            name = organizationName,
            planId = DefaultPlanId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateOrganizationResponseContract>();
        Assert.NotNull(payload);

        return payload!.Id;
    }

    private async Task AddEmployeeMembershipAsync(Guid organizationId, string employeeEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employee = db.Users.Single(x => x.Email == employeeEmail);
        var exists = db.OrganizationMembers.Any(x => x.OrganizationId == organizationId && x.UserId == employee.Id);
        if (exists)
        {
            return;
        }

        db.OrganizationMembers.Add(OrganizationMember.Create(
            organizationId,
            employee.Id,
            OrganizationMemberRole.Employee));

        await db.SaveChangesAsync();
    }

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var field = typeof(T).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(entity, id);
    }

    private sealed record LoginResponseContract(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt,
        DateTime RefreshTokenExpiresAt);

    private sealed record CreateOrganizationResponseContract(
        Guid Id,
        string Name,
        Guid PlanId,
        Guid CreatedByUserId,
        DateTime CreatedAt);
}
