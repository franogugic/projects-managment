namespace projects_menagment.Application.Dtos.Projects;

public sealed record ArchiveProjectResponseDto(
    Guid Id,
    Guid OrganizationId,
    string Status,
    bool IsArchived);
