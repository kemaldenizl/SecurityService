using MediatR;
using Microsoft.Extensions.Logging;
using Security.Application.Abstractions.Auditing;
using Security.Application.Abstractions.Email;
using Security.Application.Abstractions.Persistence;
using Security.Application.Abstractions.Security;
using Security.Application.Abstractions.Time;
using Security.Application.Abstractions.UnitOfWork;
using Security.Application.Auth.PasswordChange.Dtos;
using Security.Application.Common.Auditing;
using Security.Application.Common.Errors;
using Security.Application.Common.Results;
using Security.Domain.Auditing;
using Security.Domain.Tokens;

namespace Security.Application.Auth.PasswordChange.RequestPasswordChange;

public sealed class RequestPasswordChangeCommandHandler(
    IUserRepository userRepository,
    IPasswordChangeTokenRepository passwordChangeTokenRepository,
    IAuditLogRepository auditLogRepository,
    IAuditLogFactory auditLogFactory,
    IPasswordChangeTokenGenerator passwordChangeTokenGenerator,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    ILogger<RequestPasswordChangeCommandHandler> logger)
    : IRequestHandler<RequestPasswordChangeCommand, Result<RequestPasswordChangeResponse>>
{
    private static readonly TimeSpan PasswordChangeTokenLifetime = TimeSpan.FromMinutes(30);

    public async Task<Result<RequestPasswordChangeResponse>> Handle(RequestPasswordChangeCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<RequestPasswordChangeResponse>.Failure(AuthErrors.InvalidCurrentPassword);
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return Result<RequestPasswordChangeResponse>.Failure(AuthErrors.InvalidCurrentPassword);
        }

        var utcNow = dateTimeProvider.UtcNow;
        var tokenPair = passwordChangeTokenGenerator.Generate();

        var changeToken = new PasswordChangeToken(
            Guid.NewGuid(),
            user.Id,
            tokenPair.HashedToken,
            utcNow.Add(PasswordChangeTokenLifetime),
            utcNow);

        await passwordChangeTokenRepository.AddAsync(changeToken, cancellationToken);

        var auditLog = auditLogFactory.Create(
            AuditActionType.PasswordChangeRequested,
            AuditPayloadBuilder.Build(new
            {
                @event = "password_change_requested",
                userId = user.Id,
                email = user.Email
            }),
            user.Id);

        await auditLogRepository.AddAsync(auditLog, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendPasswordChangeAsync(
                user.Email,
                tokenPair.PlainTextToken,
                cancellationToken
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Password change email could not be sent. UserId: {UserId}",
                user.Id
            );
        }

        return Result<RequestPasswordChangeResponse>.Success(new RequestPasswordChangeResponse("A confirmation link has been sent to your email address."));
    }
}
