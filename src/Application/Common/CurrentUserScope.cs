using SafetyScale.Application.Abstractions.Authentication;
using SafetyScale.Application.Common;

namespace SafetyScale.Application.Common;

public static class CurrentUserScope
{
    public static bool IsSecurityGuardOperator(ICurrentUserContext currentUser) =>
        currentUser.IsInRole(TenantRoles.SecurityGuard);

    public static bool CanAccessSecurityGuard(ICurrentUserContext currentUser, Guid securityGuardId) =>
        !IsSecurityGuardOperator(currentUser) ||
        currentUser.SecurityGuardId == securityGuardId;

    public static Guid? RequireOwnSecurityGuardId(ICurrentUserContext currentUser)
    {
        if (!IsSecurityGuardOperator(currentUser))
        {
            return null;
        }

        return currentUser.SecurityGuardId
            ?? throw new UnauthorizedAccessException("Security guard profile is not linked to this account.");
    }
}
