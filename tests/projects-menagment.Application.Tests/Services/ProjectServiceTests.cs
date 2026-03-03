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

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository);
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

        var creator = User.Create("Ana", "Employee", "ana.employee@test.com", "hash");
        var plan = Plan.Create(PlanCode.Free, "Free", 5, 10, 0m);
        var organization = Organization.Create("Org B", plan.Id, creator.Id);

        userRepository.Setup(x => x.GetByIdAsync(creator.Id, It.IsAny<CancellationToken>())).ReturnsAsync(creator);
        organizationRepository.Setup(x => x.GetByIdAsync(organization.Id, It.IsAny<CancellationToken>())).ReturnsAsync(organization);
        organizationMemberRepository
            .Setup(x => x.GetUserRoleInOrganizationAsync(organization.Id, creator.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrganizationMemberRole.Employee);

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository);
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

        var sut = CreateSut(userRepository, organizationRepository, organizationMemberRepository, planRepository, projectRepository);
        var request = new CreateProjectRequestDto("Project Gamma", null, null, 3000m, "IN_PROGRESS");

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(organization.Id, request, creator.Id, CancellationToken.None));
        projectRepository.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ProjectService CreateSut(
        Mock<IUserRepository> userRepository,
        Mock<IOrganizationRepository> organizationRepository,
        Mock<IOrganizationMemberRepository> organizationMemberRepository,
        Mock<IPlanRepository> planRepository,
        Mock<IProjectRepository> projectRepository)
    {
        return new ProjectService(
            userRepository.Object,
            organizationRepository.Object,
            organizationMemberRepository.Object,
            planRepository.Object,
            projectRepository.Object,
            NullLogger<ProjectService>.Instance);
    }
}
