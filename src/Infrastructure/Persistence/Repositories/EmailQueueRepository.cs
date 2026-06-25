using Microsoft.EntityFrameworkCore;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;
using SafetyScale.Infrastructure.Persistence;

namespace SafetyScale.Infrastructure.Persistence.Repositories;

public sealed class EmailQueueRepository(ApplicationDbContext dbContext) : IEmailQueueRepository
{
    public async Task AddAsync(EmailQueueMessage message, CancellationToken cancellationToken = default)
        => await dbContext.EmailQueueMessages.AddAsync(message, cancellationToken);

    public async Task<IReadOnlyList<EmailQueueMessage>> ClaimAvailableBatchAsync(
        int batchSize,
        TimeSpan staleProcessingThreshold,
        CancellationToken cancellationToken = default)
    {
        if (batchSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        var now = DateTime.UtcNow;
        var staleBefore = now - staleProcessingThreshold;

        await dbContext.EmailQueueMessages
            .Where(message =>
                message.Status == EmailQueueStatus.Processing &&
                message.ProcessingStartedAtUtc != null &&
                message.ProcessingStartedAtUtc < staleBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, EmailQueueStatus.Pending)
                    .SetProperty(message => message.ProcessingStartedAtUtc, (DateTime?)null),
                cancellationToken);

        var candidateIds = await dbContext.EmailQueueMessages
            .Where(message =>
                message.Status == EmailQueueStatus.Pending &&
                message.AvailableAtUtc <= now)
            .OrderBy(message => message.CreatedAtUtc)
            .Take(batchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);

        var claimed = new List<EmailQueueMessage>(candidateIds.Count);

        foreach (var messageId in candidateIds)
        {
            var updatedRows = await dbContext.EmailQueueMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.Status == EmailQueueStatus.Pending)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.Status, EmailQueueStatus.Processing)
                        .SetProperty(message => message.ProcessingStartedAtUtc, now),
                    cancellationToken);

            if (updatedRows != 1)
            {
                continue;
            }

            var claimedMessage = await dbContext.EmailQueueMessages
                .AsNoTracking()
                .FirstAsync(message => message.Id == messageId, cancellationToken);

            claimed.Add(claimedMessage);
        }

        return claimed;
    }

    public async Task MarkSentAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var updatedRows = await dbContext.EmailQueueMessages
            .Where(message => message.Id == messageId && message.Status == EmailQueueStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, EmailQueueStatus.Sent)
                    .SetProperty(message => message.ProcessedAtUtc, DateTime.UtcNow)
                    .SetProperty(message => message.ProcessingStartedAtUtc, (DateTime?)null)
                    .SetProperty(message => message.LastError, (string?)null),
                cancellationToken);

        if (updatedRows != 1)
        {
            throw new InvalidOperationException($"Email queue message '{messageId}' is not in processing state.");
        }
    }

    public async Task ScheduleRetryAsync(
        Guid messageId,
        int attempts,
        string error,
        DateTime availableAtUtc,
        CancellationToken cancellationToken = default)
    {
        var updatedRows = await dbContext.EmailQueueMessages
            .Where(message => message.Id == messageId && message.Status == EmailQueueStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, EmailQueueStatus.Pending)
                    .SetProperty(message => message.Attempts, attempts)
                    .SetProperty(message => message.AvailableAtUtc, availableAtUtc)
                    .SetProperty(message => message.LastError, error)
                    .SetProperty(message => message.ProcessingStartedAtUtc, (DateTime?)null),
                cancellationToken);

        if (updatedRows != 1)
        {
            throw new InvalidOperationException($"Email queue message '{messageId}' is not in processing state.");
        }
    }

    public async Task MarkFailedPermanentlyAsync(
        Guid messageId,
        int attempts,
        string error,
        CancellationToken cancellationToken = default)
    {
        var updatedRows = await dbContext.EmailQueueMessages
            .Where(message => message.Id == messageId && message.Status == EmailQueueStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(message => message.Status, EmailQueueStatus.Failed)
                    .SetProperty(message => message.Attempts, attempts)
                    .SetProperty(message => message.LastError, error)
                    .SetProperty(message => message.ProcessedAtUtc, DateTime.UtcNow)
                    .SetProperty(message => message.ProcessingStartedAtUtc, (DateTime?)null),
                cancellationToken);

        if (updatedRows != 1)
        {
            throw new InvalidOperationException($"Email queue message '{messageId}' is not in processing state.");
        }
    }

    public async Task<EmailQueueMessage?> GetByIdAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
        => await dbContext.EmailQueueMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);
}
