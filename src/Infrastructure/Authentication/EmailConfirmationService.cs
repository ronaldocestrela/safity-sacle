using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Infrastructure.Identity;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Infrastructure.Authentication;

public sealed class EmailConfirmationService(
    UserManager<AppUser> userManager,
    IEmailQueueService emailQueueService,
    IOptions<PublicUrlsOptions> publicUrlsOptions) : IEmailConfirmationService
{
    public async Task EnqueueConfirmationEmailAsync(
        string userId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{userId}' was not found for email confirmation.");
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmationLink = EmailConfirmationLinkBuilder.Build(
            publicUrlsOptions.Value.WebBaseUrl,
            userId,
            token);

        var message = EmailConfirmationMessageFactory.Create(email, displayName, confirmationLink);
        await emailQueueService.EnqueueAsync(message, cancellationToken);
    }

    public async Task<ConfirmEmailResult> ConfirmAsync(
        string userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return new ConfirmEmailResult(ConfirmEmailStatus.InvalidToken);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new ConfirmEmailResult(ConfirmEmailStatus.UserNotFound);
        }

        if (user.EmailConfirmed)
        {
            return new ConfirmEmailResult(ConfirmEmailStatus.AlreadyConfirmed);
        }

        var result = await userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded
            ? new ConfirmEmailResult(ConfirmEmailStatus.Success)
            : new ConfirmEmailResult(ConfirmEmailStatus.InvalidToken);
    }
}
