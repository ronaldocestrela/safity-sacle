using System.Text.Json;
using FluentAssertions;
using SafetyScale.Web.Blazor.Models.Schedules;
using SafetyScale.Web.Blazor.Services.Api;

namespace SafetyScale.Tests.Web.Blazor.Api;

public sealed class SchedulesDtoDeserializationTests
{
    [Fact]
    public void SampleScheduleJson_DeserializesToMonthlyScheduleDto()
    {
        var dto = JsonSerializer.Deserialize<MonthlyScheduleDto>(
            SchedulesSampleJson,
            AppJsonSerializerOptions.Create());

        dto.Should().NotBeNull();
        dto!.Items.Should().HaveCount(2);
        dto.Items[0].SecurityGuardName.Should().Be("Pat Smith");
    }

    private const string SchedulesSampleJson =
        """
        {
          "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
          "month": 5,
          "year": 2026,
          "generatedAt": "2026-05-01T10:00:00.000Z",
          "items": [{
            "id": "11111111-1111-1111-1111-111111111111",
            "securityGuardId": "22222222-2222-2222-2222-222222222222",
            "securityGuardName": "Pat Smith",
            "securityGuardIsActive": true,
            "sectorId": "33333333-3333-3333-3333-333333333333",
            "sectorName": "Primary",
            "date": "2026-05-07",
            "isWeekend": false
          }, {
            "id": "44444444-4444-4444-4444-444444444444",
            "securityGuardId": "55555555-5555-5555-5555-555555555555",
            "securityGuardName": "Alex Inactive",
            "securityGuardIsActive": false,
            "sectorId": "33333333-3333-3333-3333-333333333333",
            "sectorName": "Primary",
            "date": "2026-05-10",
            "isWeekend": true
          }]
        }
        """;
}
