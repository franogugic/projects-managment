namespace projects_menagment.Api.Dtos.Organizations;

public sealed record CreateOrganizationRequestBodyDto(
    string? Name,
    Guid PlanId);
