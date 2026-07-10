namespace Security.API.Contracts.Tenants;

public sealed record CreateTenantRequest(
    string Name,
    string Slug
);
