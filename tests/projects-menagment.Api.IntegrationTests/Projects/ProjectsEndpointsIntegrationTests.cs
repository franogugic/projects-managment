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
        var owner = db.Users.Single(x => x.Email == ownerEmail);
        var creatorMembership = db.ProjectMembers.SingleOrDefault(x => x.ProjectId == created!.Id && x.UserId == owner.Id);

        Assert.NotNull(created);
        Assert.Equal(0, created!.TotalTasksCount);
        Assert.Equal(0, created.FinishedTasksCount);
        Assert.NotNull(creatorMembership);
        Assert.Equal(ProjectMemberRole.Menager, creatorMembership!.Role);
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

    [Fact]
    public async Task GetProjectsByOrganization_WhenUserIsMember_ReturnsProjects()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project List");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        await client.PostAsJsonAsync($"/api/organizations/{orgId}/projects", new
        {
            name = "Listed Project",
            description = "Should be returned by list endpoint.",
            budget = 2000m,
            status = "PLANNED"
        });

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<ProjectListItemContract>>();

        Assert.NotNull(payload);
        Assert.Contains(payload!, project => project.Name == "Listed Project");
    }

    [Fact]
    public async Task GetProjectById_WhenUserIsMember_ReturnsProject()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Detail");
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Detail");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects/{projectId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ProjectListItemContract>();
        Assert.NotNull(payload);
        Assert.Equal(projectId, payload!.Id);
    }

    [Fact]
    public async Task GetProjectsByOrganization_WhenUnauthenticated_ReturnsUnauthorized()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Unauthorized Projects");

        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectsByOrganization_WhenUserIsNotMember_ReturnsForbidden()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var outsiderEmail = $"outsider.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(outsiderEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var outsiderToken = await LoginAndGetAccessTokenAsync(outsiderEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Forbidden Projects");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectsByOrganization_WhenOrganizationDoesNotExist_ReturnsNotFound()
    {
        var userEmail = $"user.{Guid.NewGuid():N}@test.com";
        await SignupAsync(userEmail);
        var userToken = await LoginAndGetAccessTokenAsync(userEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var response = await client.GetAsync($"/api/organizations/{Guid.NewGuid()}/projects");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddProjectMember_WhenRequesterIsOwner_AddsMember()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Members");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);

        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project With Members");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var employee = db.Users.Single(x => x.Email == employeeEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.PostAsJsonAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/members",
            new { userId = employee.Id, role = "EMPLOYEE" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var exists = db.ProjectMembers.Any(x => x.ProjectId == projectId && x.UserId == employee.Id);
        Assert.True(exists);
    }

    [Fact]
    public async Task AddProjectMember_WhenRequesterIsEmployee_ReturnsForbidden()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        var targetEmail = $"target.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);
        await SignupAsync(targetEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var employeeToken = await LoginAndGetAccessTokenAsync(employeeEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Members Forbidden");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);
        await AddEmployeeMembershipAsync(orgId, targetEmail);

        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project No Permission");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var targetUser = db.Users.Single(x => x.Email == targetEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", employeeToken);

        var response = await client.PostAsJsonAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/members",
            new { userId = targetUser.Id, role = "EMPLOYEE" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WhenRequesterIsOwner_UpdatesProject()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);

        var orgId = await CreateOrganizationAsync(ownerToken, "Org Update Project");
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Before Update");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.PutAsJsonAsync(
            $"/api/organizations/{orgId}/projects/{projectId}",
            new
            {
                name = "Project After Update",
                description = "Updated description",
                deadline = DateTime.UtcNow.AddDays(7),
                budget = 2500m,
                status = "IN_PROGRESS"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var project = db.Projects.Single(x => x.Id == projectId);
        Assert.Equal("Project After Update", project.Name);
        Assert.Equal(ProjectStatus.InProgress, project.Status);
    }

    [Fact]
    public async Task ArchiveProject_WhenCompleted_ArchivesProject()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);

        var orgId = await CreateOrganizationAsync(ownerToken, "Org Archive Project");
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project To Archive");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/organizations/{orgId}/projects/{projectId}",
            new
            {
                name = "Project To Archive",
                description = "Ready",
                deadline = DateTime.UtcNow.AddDays(1),
                budget = 100m,
                status = "COMPLETED"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var archiveResponse = await client.PostAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/archive",
            content: null);

        Assert.Equal(HttpStatusCode.OK, archiveResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var project = db.Projects.Single(x => x.Id == projectId);
        Assert.True(project.IsArchived);
    }

    [Fact]
    public async Task GetProjectMembers_WhenRequesterIsOrganizationMember_ReturnsMembers()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Members List");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Members List");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = db.Users.Single(x => x.Email == employeeEmail);
            db.ProjectMembers.Add(ProjectMember.Create(projectId, employee.Id, ProjectMemberRole.Employee));
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects/{projectId}/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<ProjectMemberContract>>();
        Assert.NotNull(payload);
        Assert.Contains(payload!, member => member.Role == "EMPLOYEE");
    }

    [Fact]
    public async Task GetProjectMembers_WhenRequesterIsNotOrganizationMember_ReturnsForbidden()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var outsiderEmail = $"outsider.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(outsiderEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var outsiderToken = await LoginAndGetAccessTokenAsync(outsiderEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Project Members Forbidden");
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Members Forbidden");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", outsiderToken);

        var response = await client.GetAsync($"/api/organizations/{orgId}/projects/{projectId}/members");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProjectMemberRole_WhenRequesterIsOwner_UpdatesRole()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Member Role Update");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Role Update");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = db.Users.Single(x => x.Email == employeeEmail);
            if (!db.ProjectMembers.Any(x => x.ProjectId == projectId && x.UserId == employee.Id))
            {
                db.ProjectMembers.Add(ProjectMember.Create(projectId, employee.Id, ProjectMemberRole.Employee));
                await db.SaveChangesAsync();
            }
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var target = verifyDb.Users.Single(x => x.Email == employeeEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.PutAsJsonAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/members/{target.Id}/role",
            new { role = "MENAGER" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var member = verifyDb.ProjectMembers.Single(x => x.ProjectId == projectId && x.UserId == target.Id);
        Assert.Equal(ProjectMemberRole.Menager, member.Role);
    }

    [Fact]
    public async Task RemoveProjectMember_WhenRequesterIsOwner_RemovesMember()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        var employeeEmail = $"employee.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);
        await SignupAsync(employeeEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Remove Member");
        await AddEmployeeMembershipAsync(orgId, employeeEmail);
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Remove Member");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var employee = db.Users.Single(x => x.Email == employeeEmail);
            if (!db.ProjectMembers.Any(x => x.ProjectId == projectId && x.UserId == employee.Id))
            {
                db.ProjectMembers.Add(ProjectMember.Create(projectId, employee.Id, ProjectMemberRole.Employee));
                await db.SaveChangesAsync();
            }
        }

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var target = verifyDb.Users.Single(x => x.Email == employeeEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.DeleteAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/members/{target.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var exists = verifyDb.ProjectMembers.Any(x => x.ProjectId == projectId && x.UserId == target.Id);
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveProjectMember_WhenTargetIsProjectCreator_ReturnsConflict()
    {
        await EnsureDefaultPlanExistsAsync();

        var ownerEmail = $"owner.{Guid.NewGuid():N}@test.com";
        await SignupAsync(ownerEmail);

        var ownerToken = await LoginAndGetAccessTokenAsync(ownerEmail);
        var orgId = await CreateOrganizationAsync(ownerToken, "Org Remove Creator");
        var projectId = await CreateProjectAsync(ownerToken, orgId, "Project Creator Protected");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owner = db.Users.Single(x => x.Email == ownerEmail);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var response = await client.DeleteAsync(
            $"/api/organizations/{orgId}/projects/{projectId}/members/{owner.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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

    private async Task<Guid> CreateProjectAsync(string accessToken, Guid organizationId, string projectName)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync($"/api/organizations/{organizationId}/projects", new
        {
            name = projectName,
            description = "Project created for project member endpoint tests.",
            budget = 1000m,
            status = "PLANNED"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateProjectResponseContract>();
        Assert.NotNull(payload);
        return payload!.Id;
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

    private sealed record ProjectListItemContract(
        Guid Id,
        Guid OrganizationId,
        string Name,
        string? Description,
        DateTime? Deadline,
        decimal Budget,
        string Status,
        int TotalTasksCount,
        int FinishedTasksCount,
        decimal ProgressPercentage,
        Guid CreatedByUserId,
        DateTime CreatedAt,
        bool IsArchived);

    private sealed record CreateProjectResponseContract(
        Guid Id,
        Guid OrganizationId,
        string Name,
        string? Description,
        DateTime? Deadline,
        decimal Budget,
        string Status,
        int TotalTasksCount,
        int FinishedTasksCount,
        Guid CreatedByUserId,
        DateTime CreatedAt,
        bool IsArchived);

    private sealed record ProjectMemberContract(
        Guid UserId,
        string FirstName,
        string LastName,
        string Role,
        DateTime AddedAt);
}
