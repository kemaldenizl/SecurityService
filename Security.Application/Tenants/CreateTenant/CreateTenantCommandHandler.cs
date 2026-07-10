using MediatR;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Application.Tenants.Dtos;
using Security.Domain.Auditing;
using Security.Domain.Tenancy;

namespace Security.Application.Tenants.CreateTenant;

public sealed class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    public async Task<Result<TenantDto>> Handle(
        CreateTenantCommand request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = request.Slug.Trim().ToLowerInvariant();

        var slugExists = await tenantRepository.SlugExistsAsync(slug, cancellationToken);
        if (slugExists)
            return Result<TenantDto>.Failure(TenantErrors.SlugAlreadyExists);

        var tenant = new Tenant(Guid.NewGuid(), name, slug, dateTimeProvider.UtcNow);

        await tenantRepository.AddAsync(tenant, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.TenantCreated,
            AuditPayloadBuilder.Build(new
            {
                @event = "tenant_created",
                tenantId = tenant.Id,
                slug = tenant.Slug
            }));

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TenantDto>.Success(TenantMapper.ToDto(tenant));
    }
}
