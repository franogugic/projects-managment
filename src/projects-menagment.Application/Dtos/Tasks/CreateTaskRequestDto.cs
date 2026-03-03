namespace projects_menagment.Application.Dtos.Tasks;

public sealed record CreateTaskRequestDto(
    Guid ProjectId,
    Guid AssigneeUserId,
    string Title,
    string? Description,
    DateTime? DueDate,
    string? Priority,
    decimal SpentAmount);
