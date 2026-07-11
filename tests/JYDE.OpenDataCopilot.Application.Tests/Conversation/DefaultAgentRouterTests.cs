using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas de <see cref="DefaultAgentRouter"/> (enrutado por reglas).</summary>
public sealed class DefaultAgentRouterTests
{
    [Fact]
    public async Task RouteAsync_EligeElPrimerAgenteQuePuedeAtender()
    {
        DefaultAgentRouter router = new();
        StubAgent noPuede = new("a", canHandle: false);
        StubAgent siPuede = new("b", canHandle: true);

        IConversationAgent selected =
            await router.RouteAsync("hola", [noPuede, siPuede], TestContext.Current.CancellationToken);

        selected.Name.ShouldBe("b");
    }

    [Fact]
    public async Task RouteAsync_SiNingunoPuede_UsaElPrimeroComoReserva()
    {
        DefaultAgentRouter router = new();
        StubAgent a = new("a", canHandle: false);
        StubAgent b = new("b", canHandle: false);

        (await router.RouteAsync("hola", [a, b], TestContext.Current.CancellationToken)).Name.ShouldBe("a");
    }

    [Fact]
    public async Task RouteAsync_SinAgentes_Lanza()
    {
        DefaultAgentRouter router = new();

        await Should.ThrowAsync<InvalidOperationException>(() => router.RouteAsync("hola", []));
        await Should.ThrowAsync<ArgumentNullException>(() => router.RouteAsync("hola", null!));
    }
}
