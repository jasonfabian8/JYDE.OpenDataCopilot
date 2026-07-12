using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="ConversationArchiveService"/> (guardar/recuperar/listar/eliminar).</summary>
public sealed class ConversationArchiveServiceTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeStore : IConversationStore
    {
        public ConversationRecord? Saved { get; private set; }
        public string? RequestedId { get; private set; }
        public string? DeletedId { get; private set; }
        public ConversationRecord? ToReturn { get; init; }
        public IReadOnlyList<ConversationSummary> Summaries { get; init; } = [];

        public Task SaveAsync(ConversationRecord conversation, CancellationToken cancellationToken = default)
        {
            Saved = conversation;
            return Task.CompletedTask;
        }

        public Task<ConversationRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            RequestedId = id;
            return Task.FromResult(ToReturn);
        }

        public Task<IReadOnlyList<ConversationSummary>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Summaries);

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }
    }

    private static ConversationRecord Sample(string id = "c1") =>
        new(id, "Título", "thread", [], "objetivo", [], [], []);

    [Fact]
    public async Task SaveAsync_SellaLaFechaConElReloj_YDelegaAlPuerto()
    {
        FakeStore store = new();
        DateTimeOffset now = new(2026, 7, 11, 12, 0, 0, TimeSpan.Zero);
        ConversationArchiveService service = new(store, new FixedClock(now));

        await service.SaveAsync(Sample(), TestContext.Current.CancellationToken);

        store.Saved.ShouldNotBeNull();
        store.Saved.UpdatedAtUtc.ShouldBe(now);
        store.Saved.Id.ShouldBe("c1");
    }

    [Fact]
    public async Task SaveAsync_SinId_Lanza()
    {
        ConversationArchiveService service = new(new FakeStore());

        await Should.ThrowAsync<ArgumentException>(async () =>
            await service.SaveAsync(Sample(" "), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SaveAsync_Nulo_Lanza()
    {
        ConversationArchiveService service = new(new FakeStore());

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await service.SaveAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetAsync_DelegaYDevuelve()
    {
        ConversationRecord stored = Sample();
        FakeStore store = new() { ToReturn = stored };
        ConversationArchiveService service = new(store);

        ConversationRecord? found = await service.GetAsync("c1", TestContext.Current.CancellationToken);

        found.ShouldBe(stored);
        store.RequestedId.ShouldBe("c1");
    }

    [Fact]
    public async Task ListAsync_Delega()
    {
        FakeStore store = new() { Summaries = [new ConversationSummary("c1", "T", DateTimeOffset.UtcNow)] };
        ConversationArchiveService service = new(store);

        IReadOnlyList<ConversationSummary> summaries = await service.ListAsync(TestContext.Current.CancellationToken);

        summaries.ShouldHaveSingleItem().Id.ShouldBe("c1");
    }

    [Fact]
    public async Task DeleteAsync_Delega()
    {
        FakeStore store = new();
        ConversationArchiveService service = new(store);

        await service.DeleteAsync("c1", TestContext.Current.CancellationToken);

        store.DeletedId.ShouldBe("c1");
    }

    [Fact]
    public async Task Get_Y_Delete_ConIdVacio_Lanzan()
    {
        ConversationArchiveService service = new(new FakeStore());

        await Should.ThrowAsync<ArgumentException>(async () =>
            await service.GetAsync(" ", TestContext.Current.CancellationToken));
        await Should.ThrowAsync<ArgumentException>(async () =>
            await service.DeleteAsync("", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Constructor_ConPuertoNulo_Lanza() =>
        Should.Throw<ArgumentNullException>(() => new ConversationArchiveService(null!));
}
