namespace projects_menagment.Application.Dtos.Tasks;

public sealed record ProjectTaskListItemDto(
    Guid Id,
    Guid ProjectId,
    Guid? AssigneeUserId,
    string Title,
    string? Description,
    DateTime? DueDate,
    string Status,
    string Priority,
    decimal SpentAmount,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    string? CompletionNote);
