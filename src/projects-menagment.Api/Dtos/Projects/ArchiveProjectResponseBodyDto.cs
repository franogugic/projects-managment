namespace projects_menagment.Api.Dtos.Projects;

public sealed record ArchiveProjectResponseBodyDto(
    Guid Id,
    Guid OrganizationId,
    string Status,
    bool IsArchived);
