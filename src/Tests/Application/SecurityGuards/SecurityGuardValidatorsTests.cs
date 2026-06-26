using FluentAssertions;
using SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.ActivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;
using SafetyScale.Tests.Application.Common;

namespace SafetyScale.Tests.Application.SecurityGuards;

public class SecurityGuardValidatorsTests
{
    [Fact]
    public async Task CreateValidator_ShouldFail_WhenNameIsEmpty()
    {
        var validator = new CreateSecurityGuardCommandValidator(
            new FakeSecurityGuardInviteService(),
            new FakePlanLimitEvaluator());

        var result = await validator.ValidateAsync(new CreateSecurityGuardCommand(string.Empty, "guard@example.com"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateValidator_ShouldFail_WhenEmailIsAlreadyUsed()
    {
        var validator = new CreateSecurityGuardCommandValidator(
            new FakeSecurityGuardInviteService { EmailAvailable = false },
            new FakePlanLimitEvaluator());

        var result = await validator.ValidateAsync(new CreateSecurityGuardCommand("Maria", "used@example.com"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateValidator_ShouldFail_WhenIdIsEmpty()
    {
        var validator = new UpdateSecurityGuardCommandValidator();

        var result = validator.Validate(new UpdateSecurityGuardCommand(Guid.Empty, "Nome"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void InactivateValidator_ShouldFail_WhenIdIsEmpty()
    {
        var validator = new InactivateSecurityGuardCommandValidator();

        var result = validator.Validate(new InactivateSecurityGuardCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ActivateValidator_ShouldFail_WhenIdIsEmpty()
    {
        var validator = new ActivateSecurityGuardCommandValidator();

        var result = validator.Validate(new ActivateSecurityGuardCommand(Guid.Empty));

        result.IsValid.Should().BeFalse();
    }
}
