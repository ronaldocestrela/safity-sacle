using System.Net;
using SafetyScale.Application.Abstractions.Messaging;

namespace SafetyScale.Infrastructure.Messaging.Email;

public static class SecurityGuardInviteMessageFactory
{
    public static EmailMessageRequest Create(string to, string displayName, string setPasswordLink)
    {
        var safeName = WebUtility.HtmlEncode(displayName);
        var safeLink = WebUtility.HtmlEncode(setPasswordLink);

        return new EmailMessageRequest(
            To: to,
            Subject: "Defina sua senha — SafetyScale",
            BodyHtml: $"""
                <p>Olá, {safeName}!</p>
                <p>Você foi convidado(a) para acessar a plataforma SafetyScale.</p>
                <p>Clique no link abaixo para definir sua senha e entrar no sistema:</p>
                <p><a href="{safeLink}">Definir senha</a></p>
                <p>Se você não esperava este convite, ignore esta mensagem.</p>
                """,
            BodyText: $"""
                Olá, {displayName}!

                Você foi convidado(a) para acessar a plataforma SafetyScale.
                Defina sua senha pelo link:
                {setPasswordLink}

                Se você não esperava este convite, ignore esta mensagem.
                """);
    }
}
