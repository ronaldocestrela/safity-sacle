namespace SafetyScale.Application.Abstractions.Messaging;

public sealed record EmailMessageRequest(
    string To,
    string Subject,
    string? BodyHtml = null,
    string? BodyText = null);
