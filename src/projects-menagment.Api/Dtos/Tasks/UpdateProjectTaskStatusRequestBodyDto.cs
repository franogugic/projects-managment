namespace projects_menagment.Api.Dtos.Tasks;

public sealed record UpdateProjectTaskStatusRequestBodyDto(
    string? Status,
    string? CompletionNote,
    decimal? SpentAmount);
