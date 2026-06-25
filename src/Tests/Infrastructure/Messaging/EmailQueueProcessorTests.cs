using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class EmailQueueProcessorTests
{
    [Fact]
    public async Task ProcessAvailableBatchAsync_ShouldMarkSent_WhenSenderSucceeds()
    {
        var messageId = Guid.NewGuid();
        var repository = new FakeEmailQueueRepository([
            new EmailQueueMessage
            {
                Id = messageId,
                To = "user@example.com",
                Subject = "Hello",
                BodyText = "Body",
                Status = EmailQueueStatus.Processing,
                Attempts = 0
            }
        ]);

        var sender = new FakeEmailSender();
        var processor = CreateProcessor(repository, sender, maxAttempts: 3);

        var processedCount = await processor.ProcessAvailableBatchAsync();

        processedCount.Should().Be(1);
        sender.SentCount.Should().Be(1);
        repository.SentMessageIds.Should().Contain(messageId);
    }

    [Fact]
    public async Task ProcessAvailableBatchAsync_ShouldScheduleRetry_WhenSenderFailsBelowMaxAttempts()
    {
        var messageId = Guid.NewGuid();
        var repository = new FakeEmailQueueRepository([
            new EmailQueueMessage
            {
                Id = messageId,
                To = "user@example.com",
                Subject = "Hello",
                BodyText = "Body",
                Status = EmailQueueStatus.Processing,
                Attempts = 1
            }
        ]);

        var sender = new FakeEmailSender { ShouldFail = true };
        var processor = CreateProcessor(repository, sender, maxAttempts: 3);

        var processedCount = await processor.ProcessAvailableBatchAsync();

        processedCount.Should().Be(0);
        repository.RetriedMessages.Should().ContainSingle()
            .Which.Should().Be((messageId, 2));
    }

    [Fact]
    public async Task ProcessAvailableBatchAsync_ShouldMarkFailedPermanently_WhenMaxAttemptsReached()
    {
        var messageId = Guid.NewGuid();
        var repository = new FakeEmailQueueRepository([
            new EmailQueueMessage
            {
                Id = messageId,
                To = "user@example.com",
                Subject = "Hello",
                BodyText = "Body",
                Status = EmailQueueStatus.Processing,
                Attempts = 2
            }
        ]);

        var sender = new FakeEmailSender { ShouldFail = true };
        var processor = CreateProcessor(repository, sender, maxAttempts: 3);

        await processor.ProcessAvailableBatchAsync();

        repository.FailedMessages.Should().ContainSingle()
            .Which.Should().Be((messageId, 3));
    }

    private static EmailQueueProcessor CreateProcessor(
        IEmailQueueRepository repository,
        IEmailSender sender,
        int maxAttempts)
    {
        var options = Options.Create(new EmailQueueWorkerOptions
        {
            BatchSize = 5,
            MaxAttempts = maxAttempts,
            InitialRetryDelaySeconds = 30,
            MaxRetryDelaySeconds = 3600,
            StaleProcessingMinutes = 10
        });

        return new EmailQueueProcessor(
            repository,
            sender,
            options,
            NullLogger<EmailQueueProcessor>.Instance);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool ShouldFail { get; init; }

        public int SentCount { get; private set; }

        public Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("smtp down");
            }

            SentCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmailQueueRepository(IReadOnlyList<EmailQueueMessage> batch) : IEmailQueueRepository
    {
        public List<Guid> SentMessageIds { get; } = [];

        public List<(Guid MessageId, int Attempts)> RetriedMessages { get; } = [];

        public List<(Guid MessageId, int Attempts)> FailedMessages { get; } = [];

        public Task AddAsync(EmailQueueMessage message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<EmailQueueMessage>> ClaimAvailableBatchAsync(
            int batchSize,
            TimeSpan staleProcessingThreshold,
            CancellationToken cancellationToken = default)
            => Task.FromResult(batch);

        public Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            SentMessageIds.Add(messageId);
            return Task.CompletedTask;
        }

        public Task ScheduleRetryAsync(
            Guid messageId,
            int attempts,
            string error,
            DateTime availableAtUtc,
            CancellationToken cancellationToken = default)
        {
            RetriedMessages.Add((messageId, attempts));
            return Task.CompletedTask;
        }

        public Task MarkFailedPermanentlyAsync(
            Guid messageId,
            int attempts,
            string error,
            CancellationToken cancellationToken = default)
        {
            FailedMessages.Add((messageId, attempts));
            return Task.CompletedTask;
        }

        public Task<EmailQueueMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
            => Task.FromResult<EmailQueueMessage?>(batch.FirstOrDefault(message => message.Id == messageId));
    }
}
