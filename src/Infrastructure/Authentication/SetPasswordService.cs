using Microsoft.AspNetCore.Identity;
using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Infrastructure.Identity;

namespace SafetyScale.Infrastructure.Authentication;

public sealed class SetPasswordService(UserManager<AppUser> userManager) : ISetPasswordService
{
    public async Task<SetPasswordResult> SetInitialPasswordAsync(
        string userId,
        string token,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return new SetPasswordResult(SetPasswordStatus.InvalidToken);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new SetPasswordResult(SetPasswordStatus.UserNotFound);
        }

        if (!user.EmailConfirmed)
        {
            return new SetPasswordResult(SetPasswordStatus.InvalidToken);
        }

        var result = await userManager.ResetPasswordAsync(user, token, password);
        if (result.Succeeded)
        {
            return new SetPasswordResult(SetPasswordStatus.Success);
        }

        var codes = result.Errors.Select(e => e.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Contains("InvalidToken"))
        {
            return new SetPasswordResult(SetPasswordStatus.InvalidToken);
        }

        if (codes.Contains("PasswordTooShort") ||
            codes.Contains("PasswordRequiresDigit") ||
            codes.Contains("PasswordRequiresLower") ||
            codes.Contains("PasswordRequiresUpper") ||
            codes.Contains("PasswordRequiresNonAlphanumeric") ||
            codes.Contains("InvalidPassword"))
        {
            return new SetPasswordResult(
                SetPasswordStatus.InvalidPassword,
                result.Errors.Select(e => e.Description).ToArray());
        }

        return new SetPasswordResult(SetPasswordStatus.InvalidToken);
    }
}
