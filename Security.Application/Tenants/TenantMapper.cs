using Security.Application.Tenants.Dtos;
using Security.Domain.Tenancy;

namespace Security.Application.Tenants;

internal static class TenantMapper
{
    public static TenantDto ToDto(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc);
}
