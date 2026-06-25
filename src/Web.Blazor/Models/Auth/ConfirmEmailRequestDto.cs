namespace SafetyScale.Web.Blazor.Models.Auth;

public sealed record ConfirmEmailRequestDto(string UserId, string Token);

public sealed record ConfirmEmailResponseDto(string? Message);
