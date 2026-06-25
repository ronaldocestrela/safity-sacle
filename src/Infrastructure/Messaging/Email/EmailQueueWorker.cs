using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SafetyScale.Application.Abstractions.Messaging;

namespace SafetyScale.Infrastructure.Messaging.Email;

public sealed class EmailQueueWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailQueueWorkerOptions> workerOptions,
    ILogger<EmailQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = workerOptions.Value;

        if (!options.Enabled)
        {
            logger.LogInformation("Email queue worker is disabled by configuration.");
            return;
        }

        logger.LogInformation("Email queue worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IEmailQueueProcessor>();
                var processedCount = await processor.ProcessAvailableBatchAsync(stoppingToken);

                if (processedCount > 0)
                {
                    logger.LogInformation("Email queue worker processed {ProcessedCount} message(s).", processedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing email queue batch.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Email queue worker stopped.");
    }
}
