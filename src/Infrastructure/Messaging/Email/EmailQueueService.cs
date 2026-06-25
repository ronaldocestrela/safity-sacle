using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;
using SafetyScale.Domain.Entities;

namespace SafetyScale.Infrastructure.Messaging.Email;

public sealed class EmailQueueService(
    IEmailQueueRepository emailQueueRepository,
    IUnitOfWork unitOfWork) : IEmailQueueService
{
    public async Task<Guid> EnqueueAsync(EmailMessageRequest message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.To))
        {
            throw new ArgumentException("Recipient address is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            throw new ArgumentException("Subject is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.BodyHtml) && string.IsNullOrWhiteSpace(message.BodyText))
        {
            throw new ArgumentException("Either HTML or text body is required.", nameof(message));
        }

        var now = DateTime.UtcNow;
        var queueMessage = new EmailQueueMessage
        {
            Id = Guid.NewGuid(),
            To = message.To.Trim(),
            Subject = message.Subject.Trim(),
            BodyHtml = message.BodyHtml,
            BodyText = message.BodyText,
            Status = EmailQueueStatus.Pending,
            Attempts = 0,
            AvailableAtUtc = now,
            CreatedAtUtc = now
        };

        await emailQueueRepository.AddAsync(queueMessage, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return queueMessage.Id;
    }
}
