using System.Net;

namespace SafetyScale.Tests.Web.Blazor.TestHelpers;

public sealed class StubHttpMessageHandler(HttpStatusCode statusCode, HttpContent? content = null)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = content,
            RequestMessage = request,
        });
}
