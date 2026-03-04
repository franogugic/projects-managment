namespace projects_menagment.Application.Dtos.Tasks;

public sealed record UpdateProjectTaskStatusRequestDto(
    Guid TaskId,
    string? Status,
    string? CompletionNote,
    decimal? SpentAmount);
