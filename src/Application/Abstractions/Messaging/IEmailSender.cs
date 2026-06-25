namespace SafetyScale.Application.Abstractions.Messaging;

public interface IEmailSender
{
    Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken = default);
}
