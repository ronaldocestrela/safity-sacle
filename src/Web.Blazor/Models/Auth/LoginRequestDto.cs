namespace SafetyScale.Web.Blazor.Models.Auth;

/// <summary>Parity with <c>SafetyScale.Api.Contracts.Auth.LoginRequest</c> and React login body.</summary>
public sealed record LoginRequestDto(string Email, string Password);
