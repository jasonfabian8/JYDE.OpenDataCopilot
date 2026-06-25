using JYDE.OpenDataCopilot.Infrastructure.Foundry;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Foundry;

/// <summary>Pruebas de <see cref="FoundryOptions"/> y sus sub-configuraciones.</summary>
public sealed class FoundryOptionsTests
{
    [Fact]
    public void Valores_PorDefecto_SonRazonables()
    {
        FoundryOptions options = new();

        options.Endpoint.ShouldBeEmpty();
        options.ApiKey.ShouldBeEmpty();
        options.Chat.Model.ShouldBe("gpt-4o-mini");
        options.Chat.Agents.ShouldBeEmpty();
        options.Embeddings.Deployment.ShouldBe("text-embedding-3-small");
        options.Embeddings.ApiVersion.ShouldBe("2024-02-01");
        options.Embeddings.Dimensions.ShouldBe(1536);
        FoundryOptions.SectionName.ShouldBe("Foundry");
    }

    [Fact]
    public void Propiedades_SePuedenAsignar()
    {
        FoundryOptions options = new()
        {
            Endpoint = "https://x.services.ai.azure.com/api/projects/p",
            ApiKey = "k",
            Chat = new FoundryChatSettings
            {
                Model = "gpt-4o",
                Agents = { ["dataset-recommender-agent"] = new FoundryAgentSettings { Name = "reco", Version = "2", Model = "gpt-4o-mini" } },
            },
            Embeddings = new FoundryEmbeddingSettings { Deployment = "otro", ApiVersion = "2025-01-01", Dimensions = 256 },
        };

        options.Endpoint.ShouldBe("https://x.services.ai.azure.com/api/projects/p");
        options.ApiKey.ShouldBe("k");
        options.Chat.Model.ShouldBe("gpt-4o");
        options.Chat.Agents["dataset-recommender-agent"].Name.ShouldBe("reco");
        options.Chat.Agents["dataset-recommender-agent"].Version.ShouldBe("2");
        options.Chat.Agents["dataset-recommender-agent"].Model.ShouldBe("gpt-4o-mini");
        options.Embeddings.Deployment.ShouldBe("otro");
        options.Embeddings.Dimensions.ShouldBe(256);
    }
}
