using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using projects_menagment.Application.Dtos.Projects;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Application.Services;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;
using Xunit;

namespace projects_menagment.Application.Tests.Services;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesProject()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var creator = User.Create("John", "Owner", "john.owner@test.com", "hash");
        var plan = Plan.Create(PlanCode.Free, "Free", 5, 10, 0m);
        var organization = Organization.Create("Org A", plan.Id, creator.Id);

        userRepository.Setup(x => x.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        organizationRepository.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organization.Id, creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Owner);
        planRepository.Setup(x => x.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        projectRepository.Setup(x => x.CountActiveByOrganizationIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        Project? createdProject = null;
        projectRepository
            .Setup(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((project, _) => createdProject = project)
            .Returns(Task.CompletedTask);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);
        var request = new CreateProjectRequestDto(
            "Project Alpha",
            "Description",
            DateTime.UtcNow.AddDays(20),
            5000m,
            "PLANNED");

        var response = await sut.CreateAsync(organization.Id, request, creator.Id, CancellationToken.None);

        Assert.NotNull(createdProject);
        Assert.Equal(createdProject!.Id, response.Id);
        Assert.Equal("Project Alpha", response.Name);
        Assert.Equal(0, response.TotalTasksCount);
        Assert.Equal(0, response.FinishedTasksCount);
        projectRepository.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUserRoleIsEmployee_ThrowsForbiddenException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var creator = User.Create("Ana", "Employee", "ana.employee@test.com", "hash");
        var plan = Plan.Create(PlanCode.Free, "Free", 5, 10, 0m);
        var organization = Organization.Create("Org B", plan.Id, creator.Id);

        userRepository.Setup(x => x.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        organizationRepository.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organization.Id, creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Employee);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);
        var request = new CreateProjectRequestDto("Project Beta", null, null, 1200m, null);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateAsync(organization.Id, request, creator.Id, CancellationToken.None));
        projectRepository.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenPlanProjectLimitIsReached_ThrowsConflictException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var creator = User.Create("Mark", "Owner", "mark.owner@test.com", "hash");
        var plan = Plan.Create(PlanCode.Free, "Free", 1, 10, 0m);
        var organization = Organization.Create("Org C", plan.Id, creator.Id);

        userRepository.Setup(x => x.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        organizationRepository.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organization.Id, creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Owner);
        planRepository.Setup(x => x.GetByIdAsync(plan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(plan);
        projectRepository.Setup(x => x.CountActiveByOrganizationIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);
        var request = new CreateProjectRequestDto("Project Gamma", null, null, 3000m, "IN_PROGRESS");

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(organization.Id, request, creator.Id, CancellationToken.None));
        projectRepository.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenRequestIsValid_ReturnsProjects()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var requesterId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var organization = Organization.Create("Org List", Guid.NewGuid(), Guid.NewGuid());
        SetEntityId(organization, organizationId);

        var projects = new List<ProjectListItemDto>
        {
            new(
                Guid.NewGuid(),
                organizationId,
                "Project 1",
                "Desc",
                null,
                100m,
                "PLANNED",
                0,
                0,
                0m,
                Guid.NewGuid(),
                DateTime.UtcNow,
                false)
        };

        organizationRepository.Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organizationId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Employee);
        projectRepository.Setup(x => x.GetByOrganizationIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(projects);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        var result = await sut.GetByOrganizationIdAsync(organizationId, requesterId, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Project 1", result.First().Name);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenOrganizationDoesNotExist_ThrowsNotFoundException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var organizationId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        organizationRepository.Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync((Organization?)null);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByOrganizationIdAsync(organizationId, requesterId, CancellationToken.None));
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenUserIsNotMember_ThrowsForbiddenException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var organizationId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var organization = Organization.Create("Org X", Guid.NewGuid(), Guid.NewGuid());
        SetEntityId(organization, organizationId);

        organizationRepository.Setup(x => x.GetByIdAsync(organizationId, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organizationId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrganizationMemberRole?)null);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetByOrganizationIdAsync(organizationId, requesterId, CancellationToken.None));
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenIdsAreEmpty_ThrowsValidationException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        await Assert.ThrowsAsync<ValidationException>(() => sut.GetByOrganizationIdAsync(Guid.Empty, Guid.NewGuid(), CancellationToken.None));
        await Assert.ThrowsAsync<ValidationException>(() => sut.GetByOrganizationIdAsync(Guid.NewGuid(), Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task AddMemberAsync_WhenRequestIsValid_AddsProjectMember()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project", requesterId, 200m);
        SetEntityId(project, projectId);
        var targetUser = User.Create("Target", "User", "target@test.com", "hash");
        SetEntityId(targetUser, targetUserId);

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organizationId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Owner);
        userRepository.Setup(x => x.GetByIdAsync(targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);
        organizationMemberRepository.Setup(x => x.ExistsAsync(organizationId, targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        projectMemberRepository.Setup(x => x.ExistsAsync(projectId, targetUserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        ProjectMember? createdMember = null;
        projectMemberRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectMember>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectMember, CancellationToken>((member, _) => createdMember = member)
            .Returns(Task.CompletedTask);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        var result = await sut.AddMemberAsync(
            organizationId,
            new AddProjectMemberRequestDto(projectId, targetUserId, "EMPLOYEE"),
            requesterId,
            CancellationToken.None);

        Assert.NotNull(createdMember);
        Assert.Equal(targetUserId, createdMember!.UserId);
        Assert.Equal(projectId, createdMember.ProjectId);
        Assert.Equal(result.Id, createdMember.Id);
    }

    [Fact]
    public async Task AddMemberAsync_WhenRequesterIsEmployee_ThrowsForbiddenException()
    {
        var userRepository = new Mock<IUserRepository>();
        var organizationRepository = new Mock<IOrganizationRepository>();
        var organizationMemberRepository = new Mock<IOrganizationMemberRepository>();
        var planRepository = new Mock<IPlanRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project", requesterId, 200m);
        SetEntityId(project, projectId);

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organizationId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Employee);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository, projectMemberRepository);

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.AddMemberAsync(
            organizationId,
            new AddProjectMemberRequestDto(projectId, targetUserId, "EMPLOYEE"),
            requesterId,
            CancellationToken.None));
    }

    private static ProjectService CreateSut(
        Mock<IUserRepository> userRepository,
        Mock<IOrganizationRepository> organizationRepository,
        Mock<IOrganizationMemberRepository> organizationMemberRepository,
        Mock<IPlanRepository> planRepository,
        Mock<IProjectRepository> projectRepository,
        Mock<IProjectMemberRepository> projectMemberRepository)
    {
        return new ProjectService(
            userRepository.Object,
            organizationRepository.Object,
            organizationMemberRepository.Object,
            planRepository.Object,
            projectRepository.Object,
            projectMemberRepository.Object,
            NullLogger<ProjectService>.Instance);
    }

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var field = typeof(T).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(entity, id);
    }
}
