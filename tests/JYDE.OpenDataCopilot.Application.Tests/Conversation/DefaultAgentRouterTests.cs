using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas de <see cref="DefaultAgentRouter"/>.</summary>
public sealed class DefaultAgentRouterTests
{
    [Fact]
    public void Route_EligeElPrimerAgenteQuePuedeAtender()
    {
        DefaultAgentRouter router = new();
        StubAgent noPuede = new("a", canHandle: false);
        StubAgent siPuede = new("b", canHandle: true);

        IConversationAgent selected = router.Route("hola", [noPuede, siPuede]);

        selected.Name.ShouldBe("b");
    }

    [Fact]
    public void Route_SiNingunoPuede_UsaElPrimeroComoReserva()
    {
        DefaultAgentRouter router = new();
        StubAgent a = new("a", canHandle: false);
        StubAgent b = new("b", canHandle: false);

        router.Route("hola", [a, b]).Name.ShouldBe("a");
    }

    [Fact]
    public void Route_SinAgentes_Lanza()
    {
        DefaultAgentRouter router = new();

        Should.Throw<InvalidOperationException>(() => router.Route("hola", []));
        Should.Throw<ArgumentNullException>(() => router.Route("hola", null!));
    }
}
