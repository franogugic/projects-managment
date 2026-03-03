using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using projects_menagment.Application.Dtos.Tasks;
using projects_menagment.Application.Exceptions;
using projects_menagment.Application.Interfaces.Repositories;
using projects_menagment.Application.Services;
using projects_menagment.Domain.Entities;
using projects_menagment.Domain.Enums;
using Xunit;

namespace projects_menagment.Application.Tests.Services;

public sealed class ProjectTaskServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRequestIsValid_CreatesTaskWithTodoStatus()
    {
        var userRepository = new Mock<IUserRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();
        var projectTaskRepository = new Mock<IProjectTaskRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project A", requesterId, 100m);
        SetEntityId(project, projectId);

        var requesterMembership = ProjectMember.Create(projectId, requesterId, ProjectMemberRole.Menager);
        var assignee = User.Create("Ana", "Employee", "ana.employee@test.com", "hash");
        SetEntityId(assignee, assigneeId);

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        projectMemberRepository
            .Setup(x => x.GetForUpdateAsync(projectId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requesterMembership);
        userRepository.Setup(x => x.GetByIdAsync(assigneeId, It.IsAny<CancellationToken>())).ReturnsAsync(assignee);
        projectMemberRepository
            .Setup(x => x.ExistsAsync(projectId, assigneeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ProjectTask? createdTask = null;
        projectTaskRepository
            .Setup(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()))
            .Callback<ProjectTask, CancellationToken>((task, _) => createdTask = task)
            .Returns(Task.CompletedTask);

        projectRepository.Setup(x => x.UpdateAsync(project, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = new ProjectTaskService(
            userRepository.Object,
            projectRepository.Object,
            projectMemberRepository.Object,
            projectTaskRepository.Object,
            NullLogger<ProjectTaskService>.Instance);

        var response = await sut.CreateAsync(
            organizationId,
            new CreateTaskRequestDto(
                projectId,
                assigneeId,
                "Task 1",
                "Task description",
                DateTime.UtcNow.AddDays(2),
                "HIGH",
                300m),
            requesterId,
            CancellationToken.None);

        Assert.NotNull(createdTask);
        Assert.Equal("TODO", response.Status);
        Assert.Equal("HIGH", response.Priority);
        Assert.Equal(300m, response.SpentAmount);
        Assert.Equal(1, project.TotalTasksCount);
        Assert.Equal(projectId, response.ProjectId);

        projectTaskRepository.Verify(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Once);
        projectRepository.Verify(x => x.UpdateAsync(project, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenRequesterIsNotManager_ThrowsForbiddenException()
    {
        var userRepository = new Mock<IUserRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();
        var projectTaskRepository = new Mock<IProjectTaskRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project A", requesterId, 100m);
        SetEntityId(project, projectId);
        var requesterMembership = ProjectMember.Create(projectId, requesterId, ProjectMemberRole.Employee);

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        projectMemberRepository
            .Setup(x => x.GetForUpdateAsync(projectId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requesterMembership);

        var sut = new ProjectTaskService(
            userRepository.Object,
            projectRepository.Object,
            projectMemberRepository.Object,
            projectTaskRepository.Object,
            NullLogger<ProjectTaskService>.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.CreateAsync(
                organizationId,
                new CreateTaskRequestDto(projectId, assigneeId, "Task 1", null, null, "LOW", 0m),
                requesterId,
                CancellationToken.None));

        projectTaskRepository.Verify(x => x.AddAsync(It.IsAny<ProjectTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenRequesterIsProjectMember_ReturnsTasks()
    {
        var userRepository = new Mock<IUserRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();
        var projectTaskRepository = new Mock<IProjectTaskRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project A", requesterId, 100m);
        SetEntityId(project, projectId);

        var tasks = new List<ProjectTaskListItemDto>
        {
            new(
                Guid.NewGuid(),
                projectId,
                requesterId,
                "Task A",
                "Desc",
                DateTime.UtcNow.AddDays(1),
                "TODO",
                "MEDIUM",
                12m,
                requesterId,
                DateTime.UtcNow)
        };

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        projectMemberRepository
            .Setup(x => x.ExistsAsync(projectId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        projectTaskRepository
            .Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        var sut = new ProjectTaskService(
            userRepository.Object,
            projectRepository.Object,
            projectMemberRepository.Object,
            projectTaskRepository.Object,
            NullLogger<ProjectTaskService>.Instance);

        var result = await sut.GetByProjectIdAsync(
            organizationId,
            projectId,
            requesterId,
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Task A", result.First().Title);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WhenRequesterIsNotProjectMember_ThrowsForbiddenException()
    {
        var userRepository = new Mock<IUserRepository>();
        var projectRepository = new Mock<IProjectRepository>();
        var projectMemberRepository = new Mock<IProjectMemberRepository>();
        var projectTaskRepository = new Mock<IProjectTaskRepository>();

        var organizationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var requesterId = Guid.NewGuid();

        var project = Project.Create(organizationId, "Project A", requesterId, 100m);
        SetEntityId(project, projectId);

        projectRepository.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>())).ReturnsAsync(project);
        projectMemberRepository
            .Setup(x => x.ExistsAsync(projectId, requesterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new ProjectTaskService(
            userRepository.Object,
            projectRepository.Object,
            projectMemberRepository.Object,
            projectTaskRepository.Object,
            NullLogger<ProjectTaskService>.Instance);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.GetByProjectIdAsync(organizationId, projectId, requesterId, CancellationToken.None));
    }

    private static void SetEntityId<T>(T entity, Guid id)
    {
        var field = typeof(T).GetField("<Id>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(entity, id);
    }
}
