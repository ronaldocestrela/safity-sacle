using FluentAssertions;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Messaging.Email;

namespace SafetyScale.Tests.Infrastructure.Messaging;

public class EmailQueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_ShouldPersistPendingMessage()
    {
        var repository = new CapturingEmailQueueRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new EmailQueueService(repository, unitOfWork);

        var messageId = await service.EnqueueAsync(
            new EmailMessageRequest("user@example.com", "Subject", BodyText: "Body"));

        messageId.Should().NotBeEmpty();
        repository.LastAddedMessage.Should().NotBeNull();
        repository.LastAddedMessage!.Status.Should().Be(EmailQueueStatus.Pending);
        repository.LastAddedMessage.To.Should().Be("user@example.com");
        unitOfWork.SaveChangesCount.Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldRejectMessageWithoutBody()
    {
        var repository = new CapturingEmailQueueRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new EmailQueueService(repository, unitOfWork);

        var action = () => service.EnqueueAsync(
            new EmailMessageRequest("user@example.com", "Subject"));

        await action.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class CapturingEmailQueueRepository : IEmailQueueRepository
    {
        public EmailQueueMessage? LastAddedMessage { get; private set; }

        public Task AddAsync(EmailQueueMessage message, CancellationToken cancellationToken = default)
        {
            LastAddedMessage = message;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EmailQueueMessage>> ClaimAvailableBatchAsync(
            int batchSize,
            TimeSpan staleProcessingThreshold,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmailQueueMessage>>([]);

        public Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ScheduleRetryAsync(
            Guid messageId,
            int attempts,
            string error,
            DateTime availableAtUtc,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task MarkFailedPermanentlyAsync(
            Guid messageId,
            int attempts,
            string error,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<EmailQueueMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
            => Task.FromResult<EmailQueueMessage?>(null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.FromResult(1);
        }
    }
}
