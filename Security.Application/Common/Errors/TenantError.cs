namespace Security.Application.Common.Errors;

public static class TenantErrors
{
    public static readonly Error NotFound = new("tenant.not_found", "Tenant was not found.");
    public static readonly Error SlugAlreadyExists = new("tenant.slug_already_exists", "A tenant with the same slug already exists.");
}
