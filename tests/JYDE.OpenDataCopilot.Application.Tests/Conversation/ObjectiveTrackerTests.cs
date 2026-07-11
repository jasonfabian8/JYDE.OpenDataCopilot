using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas del <see cref="ObjectiveTracker"/> (memoria del objetivo).</summary>
public sealed class ObjectiveTrackerTests
{
    [Fact]
    public async Task UpdateAsync_ConJsonDelLlm_DevuelveObjetivoActualizado()
    {
        ObjectiveTracker tracker = new(new StubChatCompletion("{\"objetivo\":\"analizar mortalidad y su relación con la deserción\"}"));

        string result = await tracker.UpdateAsync("analizar mortalidad", "quiero cruzarla con deserción", TestContext.Current.CancellationToken);

        result.ShouldBe("analizar mortalidad y su relación con la deserción");
    }

    [Fact]
    public async Task UpdateAsync_ConMensajeVacio_DevuelveElActual()
    {
        ObjectiveTracker tracker = new(new StubChatCompletion("{\"objetivo\":\"otro\"}"));

        (await tracker.UpdateAsync("actual", "   ", TestContext.Current.CancellationToken)).ShouldBe("actual");
    }

    [Fact]
    public async Task UpdateAsync_SinJsonOSinObjetivoOMalformado_DevuelveElActual()
    {
        (await new ObjectiveTracker(new StubChatCompletion("no json"))
            .UpdateAsync("actual", "mensaje", TestContext.Current.CancellationToken)).ShouldBe("actual");
        (await new ObjectiveTracker(new StubChatCompletion("{\"otro\":\"x\"}"))
            .UpdateAsync("actual", "mensaje", TestContext.Current.CancellationToken)).ShouldBe("actual");
        (await new ObjectiveTracker(new StubChatCompletion("{malformado}"))
            .UpdateAsync("actual", "mensaje", TestContext.Current.CancellationToken)).ShouldBe("actual");
    }

    [Fact]
    public async Task UpdateAsync_SinObjetivoPrevio_YSinJson_DevuelveVacio()
    {
        (await new ObjectiveTracker(new StubChatCompletion("no json"))
            .UpdateAsync(null, "mensaje", TestContext.Current.CancellationToken)).ShouldBe(string.Empty);
    }

    [Fact]
    public async Task UpdateAsync_SiElLlmFalla_DevuelveElActual()
    {
        (await new ObjectiveTracker(new ThrowingChatCompletion())
            .UpdateAsync("actual", "mensaje", TestContext.Current.CancellationToken)).ShouldBe("actual");
        (await new ObjectiveTracker(new ThrowingChatCompletion(new InvalidOperationException("x")))
            .UpdateAsync("actual", "mensaje", TestContext.Current.CancellationToken)).ShouldBe("actual");
    }

    [Fact]
    public void Constructor_ConChatNulo_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new ObjectiveTracker(null!));
    }
}
