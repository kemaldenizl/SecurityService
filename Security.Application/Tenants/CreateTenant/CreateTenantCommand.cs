using MediatR;
using Security.Application.Common.Results;
using Security.Application.Tenants.Dtos;

namespace Security.Application.Tenants.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Slug
) : IRequest<Result<TenantDto>>;
