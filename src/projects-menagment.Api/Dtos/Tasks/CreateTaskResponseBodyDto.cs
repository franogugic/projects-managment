namespace projects_menagment.Api.Dtos.Tasks;

public sealed record CreateTaskResponseBodyDto(
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
