using JYDE.OpenDataCopilot.Application.Conversation;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Conversation;

/// <summary>Pruebas de la auditoría de interacciones (<see cref="InteractionRecorder"/> + <see cref="AuditingChatCompletion"/>).</summary>
public sealed class AuditTests
{
    [Fact]
    public void Recorder_FrescoEstaVacio_YRecordAgrega()
    {
        InteractionRecorder recorder = new();

        recorder.Interactions.ShouldBeEmpty();
        recorder.Record("a", "req", "res");

        recorder.Interactions.ShouldHaveSingleItem().Agent.ShouldBe("a");
    }

    [Fact]
    public void Recorder_BeginYRecord_Acumulan_YBeginLimpia()
    {
        InteractionRecorder recorder = new();
        recorder.Begin();

        recorder.Record("router-agent", "req1", "res1");
        recorder.Record("dataset-recommender-agent", "req2", "res2");

        recorder.Interactions.Count.ShouldBe(2);
        recorder.Interactions[0].Agent.ShouldBe("router-agent");
        recorder.Interactions[1].Request.ShouldBe("req2");

        recorder.Begin();
        recorder.Interactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AuditingChatCompletion_RegistraLaInteraccion_YDevuelveElResultado()
    {
        InteractionRecorder recorder = new();
        recorder.Begin();
        StubChatCompletion inner = new("respuesta del modelo", responseId: "r1");
        AuditingChatCompletion audited = new(inner, recorder);

        ChatResult result = await audited.CompleteAsync(
            new ChatPrompt("figures-agent", "entrada"), TestContext.Current.CancellationToken);

        result.Text.ShouldBe("respuesta del modelo");
        AgentInteraction interaction = recorder.Interactions.ShouldHaveSingleItem();
        interaction.Agent.ShouldBe("figures-agent");
        interaction.Request.ShouldBe("entrada");
        interaction.Response.ShouldBe("respuesta del modelo");
    }

    [Fact]
    public void AuditingChatCompletion_ConArgumentosNulos_Lanza()
    {
        InteractionRecorder recorder = new();
        StubChatCompletion inner = new();

        Should.Throw<ArgumentNullException>(() => new AuditingChatCompletion(null!, recorder));
        Should.Throw<ArgumentNullException>(() => new AuditingChatCompletion(inner, null!));
    }

    [Fact]
    public void AgentInteraction_Accesores()
    {
        AgentInteraction interaction = new("router-agent", "req", "res");

        interaction.Agent.ShouldBe("router-agent");
        interaction.Request.ShouldBe("req");
        interaction.Response.ShouldBe("res");
        interaction.ToString().ShouldContain("router-agent");
    }
}
