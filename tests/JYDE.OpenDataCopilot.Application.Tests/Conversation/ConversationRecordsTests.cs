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

    [Fact]
    public void ConversationEvent_TablaYGrafico()
    {
        TableArtifact table = new("Mortalidad", ["genero", "total"], [["M", "1"], ["F", "2"]]);
        ChartArtifact chart = new("Mortalidad", "bar", "genero", "total");

        ConversationEvent tableEvent = ConversationEvent.ForTable(table);
        tableEvent.Kind.ShouldBe(ConversationEventKind.Table);
        tableEvent.Table.ShouldNotBeNull().Columns.ShouldBe(["genero", "total"]);
        tableEvent.Table.Rows.Count.ShouldBe(2);

        ConversationEvent chartEvent = ConversationEvent.ForChart(chart);
        chartEvent.Kind.ShouldBe(ConversationEventKind.Chart);
        chartEvent.Chart.ShouldNotBeNull().Type.ShouldBe("bar");
        chartEvent.Chart.XColumn.ShouldBe("genero");
        (chart with { Type = "line" }).ShouldNotBe(chart);
        table.ToString().ShouldContain("Mortalidad");
    }

    [Fact]
    public void ConversationRecord_ConContenidoCompleto_ExponeSusCampos()
    {
        ConversationMessageRecord user = new("m1", "user", "hola");
        ConversationMessageRecord assistant = new(
            "m2", "assistant", "respuesta", "figures-agent",
            [new Citation("xpi4-vt35", "Morbilidad", "https://x", 0.9)]);
        ConversationArtifactRecord chart = new(
            "a1", "chart", "Cifras", ["x", "y"], [["a", "1"]], Type: "bar", XColumn: "x", YColumn: "y");
        ConversationAuditEntryRecord audit = new(
            "e1", "hola", [new AgentInteraction("router-agent", "req", "res")]);
        DateTimeOffset updated = new(2026, 7, 11, 9, 0, 0, TimeSpan.Zero);

        ConversationRecord record = new(
            "c1", "Título", "thread-1",
            [user, assistant], "objetivo",
            [new SelectedDataset("xpi4-vt35", "Morbilidad")],
            [chart], [audit], updated);

        record.Id.ShouldBe("c1");
        record.Title.ShouldBe("Título");
        record.ThreadId.ShouldBe("thread-1");
        record.Objective.ShouldBe("objetivo");
        record.UpdatedAtUtc.ShouldBe(updated);
        record.SelectedDatasets.ShouldHaveSingleItem().Name.ShouldBe("Morbilidad");

        record.Messages.Count.ShouldBe(2);
        record.Messages[0].Id.ShouldBe("m1");
        record.Messages[0].Role.ShouldBe("user");
        record.Messages[0].Content.ShouldBe("hola");
        record.Messages[0].Agent.ShouldBeNull();
        record.Messages[0].Sources.ShouldBeNull();
        record.Messages[1].Agent.ShouldBe("figures-agent");
        record.Messages[1].Sources.ShouldNotBeNull().ShouldHaveSingleItem().DatasetId.ShouldBe("xpi4-vt35");

        ConversationArtifactRecord artifact = record.Artifacts.ShouldHaveSingleItem();
        artifact.Id.ShouldBe("a1");
        artifact.Kind.ShouldBe("chart");
        artifact.Title.ShouldBe("Cifras");
        artifact.Columns.ShouldBe(["x", "y"]);
        artifact.Rows.ShouldHaveSingleItem().ShouldBe(["a", "1"]);
        artifact.Type.ShouldBe("bar");
        artifact.XColumn.ShouldBe("x");
        artifact.YColumn.ShouldBe("y");

        ConversationAuditEntryRecord entry = record.AuditLog.ShouldHaveSingleItem();
        entry.Id.ShouldBe("e1");
        entry.UserMessage.ShouldBe("hola");
        entry.Interactions.ShouldHaveSingleItem().Agent.ShouldBe("router-agent");
    }

    [Fact]
    public void ConversationSummary_ExponeSusCampos()
    {
        DateTimeOffset updated = new(2026, 7, 11, 9, 0, 0, TimeSpan.Zero);

        ConversationSummary summary = new("c1", "Título", updated);

        summary.Id.ShouldBe("c1");
        summary.Title.ShouldBe("Título");
        summary.UpdatedAtUtc.ShouldBe(updated);
    }
}
