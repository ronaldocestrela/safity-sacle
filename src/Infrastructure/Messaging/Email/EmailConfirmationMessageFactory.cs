using System.Net;
using SafetyScale.Application.Abstractions.Messaging;

namespace SafetyScale.Infrastructure.Messaging.Email;

public static class EmailConfirmationMessageFactory
{
    public static EmailMessageRequest Create(string to, string displayName, string confirmationLink)
    {
        var safeName = WebUtility.HtmlEncode(displayName);
        var safeLink = WebUtility.HtmlEncode(confirmationLink);

        return new EmailMessageRequest(
            To: to,
            Subject: "Confirme seu e-mail — SafetyScale",
            BodyHtml: $"""
                <p>Olá, {safeName}!</p>
                <p>Confirme seu e-mail para ativar o acesso à plataforma SafetyScale:</p>
                <p><a href="{safeLink}">Confirmar e-mail</a></p>
                <p>Se você não solicitou este cadastro, ignore esta mensagem.</p>
                """,
            BodyText: $"""
                Olá, {displayName}!

                Confirme seu e-mail para ativar o acesso à plataforma SafetyScale:
                {confirmationLink}

                Se você não solicitou este cadastro, ignore esta mensagem.
                """);
    }
}
