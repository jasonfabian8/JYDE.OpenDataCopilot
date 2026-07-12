using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Conversation;

/// <summary>Pruebas del adaptador <see cref="InMemoryConversationStore"/>.</summary>
public sealed class InMemoryConversationStoreTests
{
    private static ConversationRecord Sample(string id, string title, DateTimeOffset updatedAt) =>
        new(id, title, "thread", [], "objetivo", [], [], [], updatedAt);

    [Fact]
    public async Task SaveAsync_LuegoGetAsync_DevuelveLaConversacion()
    {
        InMemoryConversationStore store = new();
        ConversationRecord conversation = Sample("c1", "Hola", DateTimeOffset.UtcNow);

        await store.SaveAsync(conversation, TestContext.Current.CancellationToken);

        ConversationRecord? found = await store.GetAsync("c1", TestContext.Current.CancellationToken);
        found.ShouldNotBeNull();
        found.Title.ShouldBe("Hola");
        found.Objective.ShouldBe("objetivo");
    }

    [Fact]
    public async Task SaveAsync_ConMismoId_Reemplaza()
    {
        InMemoryConversationStore store = new();

        await store.SaveAsync(Sample("c1", "Viejo", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);
        await store.SaveAsync(Sample("c1", "Nuevo", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        (await store.GetAsync("c1", TestContext.Current.CancellationToken))!.Title.ShouldBe("Nuevo");
        (await store.ListAsync(TestContext.Current.CancellationToken)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAsync_Inexistente_DevuelveNull()
    {
        InMemoryConversationStore store = new();

        (await store.GetAsync("nope", TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    [Fact]
    public async Task ListAsync_OrdenaPorFechaDescendente_YProyectaResumen()
    {
        InMemoryConversationStore store = new();
        DateTimeOffset baseTime = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);
        await store.SaveAsync(Sample("c1", "Antigua", baseTime), TestContext.Current.CancellationToken);
        await store.SaveAsync(Sample("c2", "Reciente", baseTime.AddHours(1)), TestContext.Current.CancellationToken);

        IReadOnlyList<ConversationSummary> summaries = await store.ListAsync(TestContext.Current.CancellationToken);

        summaries.Select(summary => summary.Id).ShouldBe(["c2", "c1"]);
        summaries[0].Title.ShouldBe("Reciente");
    }

    [Fact]
    public async Task DeleteAsync_QuitaLaConversacion_YEsIdempotente()
    {
        InMemoryConversationStore store = new();
        await store.SaveAsync(Sample("c1", "Hola", DateTimeOffset.UtcNow), TestContext.Current.CancellationToken);

        await store.DeleteAsync("c1", TestContext.Current.CancellationToken);
        await store.DeleteAsync("c1", TestContext.Current.CancellationToken); // idempotente: no lanza

        (await store.GetAsync("c1", TestContext.Current.CancellationToken)).ShouldBeNull();
        (await store.ListAsync(TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Validaciones_DeArgumentos()
    {
        InMemoryConversationStore store = new();

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await store.SaveAsync(null!, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentException>(
            async () => await store.GetAsync(" ", TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentException>(
            async () => await store.DeleteAsync("", TestContext.Current.CancellationToken));
    }
}
