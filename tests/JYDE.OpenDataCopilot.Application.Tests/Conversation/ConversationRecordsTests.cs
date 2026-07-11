using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas de los records del bounded context Conversation (igualdad y accesores).</summary>
public sealed class ConversationRecordsTests
{
    [Fact]
    public void Citation_IgualdadYAccesores()
    {
        Citation citation = new("aaaa-0001", "Uno", "https://x", 0.9);
        Citation copia = citation with { };

        citation.DatasetId.ShouldBe("aaaa-0001");
        citation.Name.ShouldBe("Uno");
        citation.SourceUrl.ShouldBe("https://x");
        citation.Score.ShouldBe(0.9);
        (citation == copia).ShouldBeTrue();
        citation.GetHashCode().ShouldBe(copia.GetHashCode());
        (citation with { Score = 0.1 }).ShouldNotBe(citation);
        citation.ToString().ShouldContain("aaaa-0001");
    }

    [Fact]
    public void ChatPrompt_YContext_Accesores()
    {
        ChatPrompt prompt = new("dataset-recommender-agent", "entrada");
        prompt.Agent.ShouldBe("dataset-recommender-agent");
        prompt.Input.ShouldBe("entrada");
        (prompt with { Input = "otro" }).ShouldNotBe(prompt);

        ConversationContext context = new("pregunta", 5, "resp-1");
        context.Question.ShouldBe("pregunta");
        context.TopK.ShouldBe(5);
        context.PreviousResponseId.ShouldBe("resp-1");

        ChatResult result = new("texto", "resp-2");
        result.Text.ShouldBe("texto");
        result.ResponseId.ShouldBe("resp-2");
        (result with { ResponseId = null }).ResponseId.ShouldBeNull();
    }

    [Fact]
    public void ConversationEvent_Factorias_YIgualdad()
    {
        ConversationEvent agent = ConversationEvent.ForAgent("reco");
        ConversationEvent token = ConversationEvent.ForToken("hola");
        ConversationEvent sources = ConversationEvent.ForSources([new Citation("aaaa-0001", "Uno", null, 1.0)]);
        ConversationEvent conversation = ConversationEvent.ForConversation("resp-1");
        ConversationEvent done = ConversationEvent.Completed();

        agent.Kind.ShouldBe(ConversationEventKind.Agent);
        agent.Agent.ShouldBe("reco");
        token.Token.ShouldBe("hola");
        sources.Sources.ShouldNotBeNull().ShouldHaveSingleItem();
        conversation.Kind.ShouldBe(ConversationEventKind.Conversation);
        conversation.ConversationId.ShouldBe("resp-1");
        done.Kind.ShouldBe(ConversationEventKind.Done);

        agent.ShouldBe(ConversationEvent.ForAgent("reco"));
        agent.GetHashCode().ShouldBe(ConversationEvent.ForAgent("reco").GetHashCode());
        agent.ToString().ShouldContain("Agent");
    }

    [Fact]
    public void ConversationEvent_Categorias_YCategoryRecommendation()
    {
        CategoryRecommendation recommendation = new("Transporte", 261, false, 0.9);
        ConversationEvent categories = ConversationEvent.ForCategories("accidentalidad vial", [recommendation]);

        categories.Kind.ShouldBe(ConversationEventKind.Categories);
        categories.Query.ShouldBe("accidentalidad vial");
        CategoryRecommendation single = categories.Categories.ShouldNotBeNull().ShouldHaveSingleItem();
        single.Name.ShouldBe("Transporte");
        single.Count.ShouldBe(261);
        single.Loaded.ShouldBeFalse();
        single.Relevance.ShouldBe(0.9);
        (recommendation with { Loaded = true }).ShouldNotBe(recommendation);
        recommendation.ToString().ShouldContain("Transporte");
    }
}
