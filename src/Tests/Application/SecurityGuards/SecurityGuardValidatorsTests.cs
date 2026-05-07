using FluentAssertions;
using SafetyScale.Application.SecurityGuards.Commands.CreateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.InactivateSecurityGuard;
using SafetyScale.Application.SecurityGuards.Commands.UpdateSecurityGuard;

namespace SafetyScale.Tests.Application.SecurityGuards;

public class SecurityGuardValidatorsTests
{
    [Fact]
    public void CreateValidator_ShouldFail_WhenNameIsEmpty()
    {
        var validator = new CreateSecurityGuardCommandValidator();

        var result = validator.Validate(new CreateSecurityGuardCommand(string.Empty));

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
}
