using FluentAssertions;
using SafetyScale.Web.Blazor.Models.Platform;

namespace SafetyScale.Tests.Web.Blazor.Platform;

public class LeadStatusLabelsTests
{
    [Theory]
    [InlineData(LeadStatusDto.New, "Novo")]
    [InlineData(LeadStatusDto.Contacted, "Contatado")]
    [InlineData(LeadStatusDto.ProposalSent, "Proposta enviada")]
    [InlineData(LeadStatusDto.Contracted, "Contratado")]
    [InlineData(LeadStatusDto.Lost, "Perdido")]
    public void GetLabel_ReturnsPortugueseLabel(LeadStatusDto status, string expected)
    {
        LeadStatusLabels.GetLabel(status).Should().Be(expected);
    }
}
