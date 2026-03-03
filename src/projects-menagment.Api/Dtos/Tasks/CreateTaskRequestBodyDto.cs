namespace projects_menagment.Api.Dtos.Tasks;

public sealed record CreateTaskRequestBodyDto(
    Guid AssigneeUserId,
    string? Title,
    string? Description,
    DateTime? DueDate,
    string? Priority,
    decimal SpentAmount);
