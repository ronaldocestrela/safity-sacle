using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;
using SafetyScale.Tests.Api.Integration;

namespace SafetyScale.Tests.Api.Integration.Messaging;

public sealed class EmailQueueIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public EmailQueueIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task EnqueueAndProcess_ShouldMarkMessageAsSent()
    {
        var fakeSender = new RecordingEmailSender();

        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("EmailQueue:Enabled", "false");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(fakeSender);
            });
        });

        using var scope = factory.Services.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
        var processor = scope.ServiceProvider.GetRequiredService<IEmailQueueProcessor>();
        var repository = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();

        var messageId = await queueService.EnqueueAsync(
            new EmailMessageRequest(
                "user@example.com",
                "Integration test",
                BodyText: "Hello from queue"));

        var processedCount = await processor.ProcessAvailableBatchAsync();

        processedCount.Should().Be(1);
        fakeSender.SentMessages.Should().ContainSingle()
            .Which.To.Should().Be("user@example.com");

        var storedMessage = await repository.GetByIdAsync(messageId);
        storedMessage.Should().NotBeNull();
        storedMessage!.Status.Should().Be(EmailQueueStatus.Sent);
        storedMessage.ProcessedAtUtc.Should().NotBeNull();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessageRequest> SentMessages { get; } = [];

        public Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken = default)
        {
            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }
}
