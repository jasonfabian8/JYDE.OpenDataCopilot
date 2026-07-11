using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Chat;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Chat;

/// <summary>Pruebas del adaptador <see cref="FakeChatCompletion"/>.</summary>
public sealed class FakeChatCompletionTests
{
    [Fact]
    public async Task CompleteAsync_DevuelveTextoYResponseId()
    {
        FakeChatCompletion chat = new();
        ChatPrompt prompt = new("dataset-recommender-agent", "entrada");

        ChatResult result = await chat.CompleteAsync(prompt, TestContext.Current.CancellationToken);

        result.Text.ShouldNotBeNullOrWhiteSpace();
        result.ResponseId.ShouldNotBeNull();
    }

    [Fact]
    public async Task CompleteAsync_ConPromptNulo_Lanza()
    {
        FakeChatCompletion chat = new();

        await Should.ThrowAsync<ArgumentNullException>(() => chat.CompleteAsync(null!, TestContext.Current.CancellationToken));
    }
}
