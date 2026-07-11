using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using JYDE.OpenDataCopilot.Infrastructure.Foundry;
using JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Foundry;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Chat;

/// <summary>Pruebas del adaptador <see cref="FoundryChatCompletion"/> con un handler HTTP falso.</summary>
public sealed class FoundryChatCompletionTests
{
    private static FoundryOptions Options(string apiKey = "secret-key") => new()
    {
        Endpoint = "https://recurso.services.ai.azure.com/api/projects/p",
        ApiKey = apiKey,
        Chat = new FoundryChatSettings
        {
            Model = "gpt-4o-mini",
            Agents =
            {
                ["dataset-recommender-agent"] = new FoundryAgentSettings { Name = "dataset-recommender-agent", Version = "1" },
            },
        },
    };

    private static FoundryChatCompletion Create(FakeFoundryHandler handler, FoundryOptions options) =>
        new(new HttpClient(handler), options);

    private static Task<ChatResult> CompleteAsync(FoundryChatCompletion chat, ChatPrompt prompt) =>
        chat.CompleteAsync(prompt, TestContext.Current.CancellationToken);

    [Fact]
    public async Task CompleteAsync_DevuelveTextoEId_YReferenciaElAgente()
    {
        FakeFoundryHandler handler = new(
            """{"id":"resp-1","output":[{"type":"message","content":[{"type":"output_text","text":"Hola mundo"}]}]}""");
        FoundryChatCompletion chat = Create(handler, Options());

        ChatResult result = await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "consulta"));

        result.Text.ShouldBe("Hola mundo");
        result.ResponseId.ShouldBe("resp-1");
        handler.LastApiKey.ShouldBe("secret-key");
        handler.LastUri!.AbsolutePath.ShouldEndWith("/openai/v1/responses");
        handler.LastBody.ShouldNotBeNull();
        handler.LastBody.ShouldContain("agent_reference");
        handler.LastBody.ShouldContain("dataset-recommender-agent");
        handler.LastBody.ShouldContain("\"version\"");
    }

    [Fact]
    public async Task CompleteAsync_ConHilo_EnviaPreviousResponseId()
    {
        FakeFoundryHandler handler = new("""{"id":"resp-2","output":[{"content":[{"type":"output_text","text":"ok"}]}]}""");
        FoundryChatCompletion chat = Create(handler, Options());

        await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "sigue", "resp-1"));

        handler.LastBody.ShouldNotBeNull();
        handler.LastBody.ShouldContain("previous_response_id");
        handler.LastBody.ShouldContain("resp-1");
    }

    [Fact]
    public async Task CompleteAsync_SinSalida_DevuelveTextoVacio()
    {
        FakeFoundryHandler handler = new("""{"id":"r","output":[]}""");
        FoundryChatCompletion chat = Create(handler, Options());

        ChatResult result = await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "x"));

        result.Text.ShouldBeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_ConAgenteNoCatalogado_UsaElCodigoComoNombre()
    {
        FakeFoundryHandler handler = new("""{"output":[{"content":[{"type":"output_text","text":"ok"}]}]}""");
        FoundryChatCompletion chat = Create(handler, Options());

        await CompleteAsync(chat, new ChatPrompt("agente-desconocido", "x"));

        handler.LastBody.ShouldNotBeNull();
        handler.LastBody.ShouldContain("agente-desconocido");
    }

    [Fact]
    public async Task CompleteAsync_SinApiKey_NoEnviaCabecera()
    {
        FakeFoundryHandler handler = new("""{"output":[{"content":[{"type":"output_text","text":"ok"}]}]}""");
        FoundryChatCompletion chat = Create(handler, Options(apiKey: string.Empty));

        await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "x"));

        handler.LastApiKey.ShouldBeNull();
    }

    [Fact]
    public async Task CompleteAsync_SinPropiedadOutput_DevuelveVacio()
    {
        FakeFoundryHandler handler = new("{}");
        FoundryChatCompletion chat = Create(handler, Options());

        (await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "x"))).Text.ShouldBeEmpty();
    }

    [Fact]
    public async Task CompleteAsync_IgnoraItemsSinContenidoYPartesNoTexto()
    {
        FakeFoundryHandler handler = new(
            """{"output":[{"type":"message"},{"type":"message","content":[{"type":"reasoning"},{"type":"output_text","text":"hi"}]}]}""");
        FoundryChatCompletion chat = Create(handler, Options());

        (await CompleteAsync(chat, new ChatPrompt("dataset-recommender-agent", "x"))).Text.ShouldBe("hi");
    }

    [Fact]
    public void Constructor_ConArgumentosNulos_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new FoundryChatCompletion(null!, Options()));
        Should.Throw<ArgumentNullException>(() => new FoundryChatCompletion(new HttpClient(new FakeFoundryHandler("{}")), null!));
    }
}
