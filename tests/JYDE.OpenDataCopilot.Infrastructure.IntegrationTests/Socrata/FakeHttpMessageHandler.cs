using System.Net;
using System.Text;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Socrata;

/// <summary>Handler HTTP de prueba que responde con cuerpos JSON encolados y registra las URIs pedidas.</summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<string> _bodies;

    public FakeHttpMessageHandler(params string[] jsonBodies) => _bodies = new Queue<string>(jsonBodies);

    public List<Uri> Requests { get; } = [];

    public string? LastAppToken { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request.RequestUri!);
        LastAppToken = request.Headers.TryGetValues("X-App-Token", out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;

        string body = _bodies.Count > 0 ? _bodies.Dequeue() : """{"results":[],"resultSetSize":0}""";
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        return Task.FromResult(response);
    }
}
