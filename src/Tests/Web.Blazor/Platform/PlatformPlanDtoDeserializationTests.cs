using FluentAssertions;
using System.Text.Json;
using SafetyScale.Web.Blazor.Models.Platform;
using SafetyScale.Web.Blazor.Services.Api;

namespace SafetyScale.Tests.Web.Blazor.Platform;

public class PlatformPlanDtoDeserializationTests
{
    [Fact]
    public void PlatformPlanDto_DeserializesLimits()
    {
        const string json = """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "name": "Business",
              "code": "business",
              "description": "Plano business",
              "priceMonthly": 199.90,
              "maxSecurityGuards": 25,
              "maxSectors": 8,
              "isActive": true,
              "createdAt": "2026-06-26T20:00:00Z"
            }
            """;

        var dto = JsonSerializer.Deserialize<PlatformPlanDto>(json, AppJsonSerializerOptions.Create());

        dto.Should().NotBeNull();
        dto!.MaxSecurityGuards.Should().Be(25);
        dto.MaxSectors.Should().Be(8);
    }
}
