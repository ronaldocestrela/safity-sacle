using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SafetyScale.Application.Abstractions.Messaging;

namespace SafetyScale.Infrastructure.Messaging.Email;

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> smtpOptions,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessageRequest message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var options = smtpOptions.Value;
        ValidateOptions(options);

        var mimeMessage = BuildMimeMessage(message, options);

        using var client = new SmtpClient();
        await client.ConnectAsync(
            options.Host,
            options.Port,
            options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(options.Username))
        {
            await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Email sent to {Recipient} with subject {Subject}.", message.To, message.Subject);
    }

    public static void ValidateOptions(SmtpOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Host))
        {
            throw new InvalidOperationException("SMTP host is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException("SMTP from address is not configured.");
        }

        if (options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException("SMTP port is invalid.");
        }
    }

    private static MimeMessage BuildMimeMessage(EmailMessageRequest message, SmtpOptions options)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(options.FromDisplayName ?? options.FromAddress, options.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));
        mimeMessage.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder();
        if (!string.IsNullOrWhiteSpace(message.BodyHtml))
        {
            bodyBuilder.HtmlBody = message.BodyHtml;
        }

        if (!string.IsNullOrWhiteSpace(message.BodyText))
        {
            bodyBuilder.TextBody = message.BodyText;
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();
        return mimeMessage;
    }
}
