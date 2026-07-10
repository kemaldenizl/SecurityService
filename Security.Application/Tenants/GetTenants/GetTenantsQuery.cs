using MediatR;
using Security.Application.Common.Results;
using Security.Application.Tenants.Dtos;

namespace Security.Application.Tenants.GetTenants;

public sealed record GetTenantsQuery : IRequest<Result<IReadOnlyCollection<TenantDto>>>;
