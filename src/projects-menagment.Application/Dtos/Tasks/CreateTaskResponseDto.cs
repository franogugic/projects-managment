namespace projects_menagment.Application.Dtos.Tasks;

public sealed record CreateTaskResponseDto(
    Guid Id,
    Guid ProjectId,
    Guid AssigneeUserId,
    string Title,
    string? Description,
    DateTime? DueDate,
    string Status,
    string Priority,
    decimal SpentAmount,
    Guid CreatedByUserId,
    DateTime CreatedAt);
