using System.Net;
using System.Text;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Foundry;

/// <summary>Handler HTTP de prueba para el adaptador de embeddings de Foundry.</summary>
internal sealed class FakeFoundryHandler : HttpMessageHandler
{
    private readonly string _body;
    private readonly HttpStatusCode _status;

    public FakeFoundryHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _body = body;
        _status = status;
    }

    public Uri? LastUri { get; private set; }

    public string? LastApiKey { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastUri = request.RequestUri;
        LastApiKey = request.Headers.TryGetValues("api-key", out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;

        HttpResponseMessage response = new(_status)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
