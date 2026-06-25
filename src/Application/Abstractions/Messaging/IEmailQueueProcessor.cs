namespace SafetyScale.Application.Abstractions.Messaging;

public interface IEmailQueueProcessor
{
    Task<int> ProcessAvailableBatchAsync(CancellationToken cancellationToken = default);
}
