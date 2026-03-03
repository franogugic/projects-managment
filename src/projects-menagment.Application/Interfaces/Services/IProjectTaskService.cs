using projects_menagment.Application.Dtos.Tasks;

namespace projects_menagment.Application.Interfaces.Services;

public interface IProjectTaskService
{
    Task<CreateTaskResponseDto> CreateAsync(
        Guid organizationId,
        CreateTaskRequestDto request,
        Guid requestUserId,
        CancellationToken cancellationToken);
}
