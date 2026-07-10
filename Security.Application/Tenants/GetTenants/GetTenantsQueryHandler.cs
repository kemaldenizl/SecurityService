using MediatR;
using Security.Application.Abstractions.Persistence;
using Security.Application.Common.Results;
using Security.Application.Tenants.Dtos;

namespace Security.Application.Tenants.GetTenants;

public sealed class GetTenantsQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetTenantsQuery, Result<IReadOnlyCollection<TenantDto>>>
{
    public async Task<Result<IReadOnlyCollection<TenantDto>>> Handle(
        GetTenantsQuery request,
        CancellationToken cancellationToken)
    {
        var tenants = await tenantRepository.ListAsync(cancellationToken);

        var response = tenants
            .Select(TenantMapper.ToDto)
            .ToArray();

        return Result<IReadOnlyCollection<TenantDto>>.Success(response);
    }
}
