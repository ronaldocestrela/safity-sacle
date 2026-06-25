using SafetyScale.Domain.Entities;

namespace SafetyScale.Application.Abstractions.Persistence;

public interface IEmailQueueRepository
{
    Task AddAsync(EmailQueueMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmailQueueMessage>> ClaimAvailableBatchAsync(
        int batchSize,
        TimeSpan staleProcessingThreshold,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task ScheduleRetryAsync(
        Guid messageId,
        int attempts,
        string error,
        DateTime availableAtUtc,
        CancellationToken cancellationToken = default);

    Task MarkFailedPermanentlyAsync(
        Guid messageId,
        int attempts,
        string error,
        CancellationToken cancellationToken = default);

    Task<EmailQueueMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);
}
