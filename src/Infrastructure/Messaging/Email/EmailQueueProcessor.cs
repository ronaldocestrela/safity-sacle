using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Messaging;
using SafetyScale.Application.Abstractions.Persistence;

namespace SafetyScale.Infrastructure.Messaging.Email;

public sealed class EmailQueueProcessor(
    IEmailQueueRepository emailQueueRepository,
    IEmailSender emailSender,
    IOptions<EmailQueueWorkerOptions> workerOptions,
    ILogger<EmailQueueProcessor> logger) : IEmailQueueProcessor
{
    public async Task<int> ProcessAvailableBatchAsync(CancellationToken cancellationToken = default)
    {
        var options = workerOptions.Value;
        var staleThreshold = TimeSpan.FromMinutes(options.StaleProcessingMinutes);

        var batch = await emailQueueRepository.ClaimAvailableBatchAsync(
            options.BatchSize,
            staleThreshold,
            cancellationToken);

        if (batch.Count == 0)
        {
            return 0;
        }

        var processedCount = 0;

        foreach (var queueMessage in batch)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["EmailQueueMessageId"] = queueMessage.Id
            });

            var request = new EmailMessageRequest(
                queueMessage.To,
                queueMessage.Subject,
                queueMessage.BodyHtml,
                queueMessage.BodyText);

            try
            {
                await emailSender.SendAsync(request, cancellationToken);
                await emailQueueRepository.MarkSentAsync(queueMessage.Id, cancellationToken);
                processedCount++;

                logger.LogInformation(
                    "Email queue message {MessageId} sent successfully.",
                    queueMessage.Id);
            }
            catch (Exception ex)
            {
                var attempts = queueMessage.Attempts + 1;
                var error = TruncateError(ex);

                if (attempts >= options.MaxAttempts)
                {
                    await emailQueueRepository.MarkFailedPermanentlyAsync(
                        queueMessage.Id,
                        attempts,
                        error,
                        cancellationToken);

                    logger.LogError(
                        ex,
                        "Email queue message {MessageId} failed permanently after {Attempts} attempts.",
                        queueMessage.Id,
                        attempts);
                }
                else
                {
                    var nextAttemptAt = EmailRetryPolicy.CalculateNextAvailableUtc(
                        attempts,
                        options.InitialRetryDelaySeconds,
                        options.MaxRetryDelaySeconds,
                        DateTime.UtcNow);

                    await emailQueueRepository.ScheduleRetryAsync(
                        queueMessage.Id,
                        attempts,
                        error,
                        nextAttemptAt,
                        cancellationToken);

                    logger.LogWarning(
                        ex,
                        "Email queue message {MessageId} failed on attempt {Attempts}; next retry at {NextAttemptAt:O}.",
                        queueMessage.Id,
                        attempts,
                        nextAttemptAt);
                }
            }
        }

        return processedCount;
    }

    private static string TruncateError(Exception exception)
    {
        var message = exception.Message.Trim();
        return message.Length <= 2000 ? message : message[..2000];
    }
}
