using JYDE.OpenDataCopilot.Api.Controllers;
using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Conversation;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas unitarias del <see cref="ConversationsController"/> (sin pipeline HTTP).</summary>
public sealed class ConversationsControllerTests
{
    private static (ConversationsController Controller, InMemoryConversationStore Store) Build()
    {
        InMemoryConversationStore store = new();
        ConversationArchiveService archive = new(store);
        return (new ConversationsController(archive), store);
    }

    private static ConversationRecord Sample(string id = "c1", string title = "Título") =>
        new(id, title, "thread", [], "objetivo", [], [], []);

    [Fact]
    public async Task Save_DevuelveNoContent_YPersiste()
    {
        (ConversationsController controller, InMemoryConversationStore store) = Build();

        IActionResult result = await controller.Save("c1", Sample(), CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        (await store.GetAsync("c1", TestContext.Current.CancellationToken)).ShouldNotBeNull();
    }

    [Fact]
    public async Task Save_UsaElIdDeLaRuta_ComoAutoritativo()
    {
        (ConversationsController controller, InMemoryConversationStore store) = Build();

        await controller.Save("ruta-1", Sample(id: "cuerpo-distinto"), CancellationToken.None);

        (await store.GetAsync("ruta-1", TestContext.Current.CancellationToken)).ShouldNotBeNull();
        (await store.GetAsync("cuerpo-distinto", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Save_SinCuerpo_O_IdVacio_DevuelveBadRequest()
    {
        (ConversationsController controller, _) = Build();

        (await controller.Save("c1", null, CancellationToken.None)).ShouldBeOfType<BadRequestObjectResult>();
        (await controller.Save(" ", Sample(), CancellationToken.None)).ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Get_Existente_DevuelveOk()
    {
        (ConversationsController controller, _) = Build();
        await controller.Save("c1", Sample(title: "Hola"), CancellationToken.None);

        IActionResult result = await controller.Get("c1", CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<ConversationRecord>().Title.ShouldBe("Hola");
    }

    [Fact]
    public async Task Get_Inexistente_DevuelveNotFound()
    {
        (ConversationsController controller, _) = Build();

        (await controller.Get("nope", CancellationToken.None)).ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_IdVacio_DevuelveBadRequest()
    {
        (ConversationsController controller, _) = Build();

        (await controller.Get(" ", CancellationToken.None)).ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task List_DevuelveOkConResumenes()
    {
        (ConversationsController controller, _) = Build();
        await controller.Save("c1", Sample(title: "Uno"), CancellationToken.None);
        await controller.Save("c2", Sample("c2", "Dos"), CancellationToken.None);

        IActionResult result = await controller.List(CancellationToken.None);

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeAssignableTo<IReadOnlyList<ConversationSummary>>()!.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Delete_DevuelveNoContent_YElimina()
    {
        (ConversationsController controller, InMemoryConversationStore store) = Build();
        await controller.Save("c1", Sample(), CancellationToken.None);

        IActionResult result = await controller.Delete("c1", CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        (await store.GetAsync("c1", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task Delete_IdVacio_DevuelveBadRequest()
    {
        (ConversationsController controller, _) = Build();

        (await controller.Delete("", CancellationToken.None)).ShouldBeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Constructor_ConServicioNulo_Lanza() =>
        Should.Throw<ArgumentNullException>(() => new ConversationsController(null!));
}
