using MediatR;
using Security.API.Common;
using Security.API.Common.ErrorMapping;
using Security.API.Contracts.Tenants;
using Security.Application.Tenants.CreateTenant;
using Security.Application.Tenants.Dtos;
using Security.Application.Tenants.GetTenants;
using Security.Domain.Authorization;
using Security.Infrastructure.RateLimiting;

namespace Security.API.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags(ApiTags.Admin)
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicyNames.Admin);

        group.MapGet("/", GetTenantsAsync)
            .RequireAuthorization(PermissionCodes.TenantsRead)
            .WithName("GetTenants")
            .WithSummary("Gets all tenants.")
            .WithDescription("Returns all tenants registered in the system.")
            .Produces<IReadOnlyCollection<TenantResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        group.MapPost("/", CreateTenantAsync)
            .RequireAuthorization(PermissionCodes.TenantsManage)
            .WithName("CreateTenant")
            .WithSummary("Creates a new tenant.")
            .WithDescription("Registers a new tenant with a unique slug.")
            .Accepts<CreateTenantRequest>("application/json")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> GetTenantsAsync(
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantsQuery(), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = result.Value
            .Select(ToTenantResponse)
            .ToArray();

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateTenantAsync(
        CreateTenantRequest request,
        HttpContext httpContext,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTenantCommand(request.Name, request.Slug), cancellationToken);

        if (result.IsFailure)
            return httpContext.ToApiResult(result);

        var response = ToTenantResponse(result.Value);

        return Results.Created($"/api/tenants/{response.Id}", response);
    }

    private static TenantResponse ToTenantResponse(TenantDto tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, tenant.CreatedAtUtc);
}
