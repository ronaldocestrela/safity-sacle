namespace SafetyScale.Application.Abstractions.Messaging;

public interface IEmailQueueService
{
    Task<Guid> EnqueueAsync(EmailMessageRequest message, CancellationToken cancellationToken = default);
}
