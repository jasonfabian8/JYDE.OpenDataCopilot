using JYDE.OpenDataCopilot.Application.Conversation;
using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Mongo;

/// <summary>Pruebas del mapeo JSON de <see cref="ConversationDocument"/> (ida y vuelta).</summary>
public sealed class ConversationDocumentTests
{
    private static ConversationRecord RichRecord()
    {
        ConversationMessageRecord user = new("m1", "user", "cuántas muertes por género");
        ConversationMessageRecord assistant = new(
            "m2", "assistant", "Aquí está la tabla.", "figures-agent",
            [new Citation("xpi4-vt35", "Morbilidad", "https://datos.gov.co/d/xpi4-vt35", 0.95)]);
        ConversationArtifactRecord chart = new(
            "a1", "chart", "Morbilidad", ["genero", "total"], [["M", "10"], ["F", "12"]],
            Type: "bar", XColumn: "genero", YColumn: "total");
        ConversationAuditEntryRecord audit = new(
            "e1", "cuántas muertes", [new AgentInteraction("router-agent", "req", "figures-agent")]);

        return new ConversationRecord(
            "c1", "Morbilidad por género", "thread-9",
            [user, assistant], "analizar morbilidad",
            [new SelectedDataset("xpi4-vt35", "Morbilidad")],
            [chart], [audit],
            new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void FromRecord_LuegoToRecord_PreservaTodo()
    {
        ConversationRecord original = RichRecord();

        ConversationRecord restored = ConversationDocument.FromRecord(original).ToRecord();

        restored.Id.ShouldBe("c1");
        restored.Title.ShouldBe("Morbilidad por género");
        restored.ThreadId.ShouldBe("thread-9");
        restored.Objective.ShouldBe("analizar morbilidad");
        restored.SelectedDatasets.ShouldHaveSingleItem().Id.ShouldBe("xpi4-vt35");
        restored.Messages.Count.ShouldBe(2);
        restored.Messages[1].Agent.ShouldBe("figures-agent");
        restored.Messages[1].Sources.ShouldNotBeNull().ShouldHaveSingleItem().Score.ShouldBe(0.95);
        ConversationArtifactRecord chart = restored.Artifacts.ShouldHaveSingleItem();
        chart.Kind.ShouldBe("chart");
        chart.XColumn.ShouldBe("genero");
        chart.Rows.Count.ShouldBe(2);
        restored.AuditLog.ShouldHaveSingleItem().Interactions.ShouldHaveSingleItem().Agent.ShouldBe("router-agent");
        restored.UpdatedAtUtc.ShouldBe(original.UpdatedAtUtc);
    }

    [Fact]
    public void ToSummary_ProyectaIdTituloYFechaUtc()
    {
        ConversationDocument document = ConversationDocument.FromRecord(RichRecord());

        ConversationSummary summary = document.ToSummary();

        summary.Id.ShouldBe("c1");
        summary.Title.ShouldBe("Morbilidad por género");
        summary.UpdatedAtUtc.ShouldBe(new DateTimeOffset(2026, 7, 11, 10, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void FromRecord_Nulo_Lanza() =>
        Should.Throw<ArgumentNullException>(() => ConversationDocument.FromRecord(null!));

    [Fact]
    public void ToRecord_PayloadInvalido_Lanza()
    {
        ConversationDocument document = new() { Id = "c1", Payload = "null" };

        Should.Throw<InvalidOperationException>(() => document.ToRecord());
    }
}
