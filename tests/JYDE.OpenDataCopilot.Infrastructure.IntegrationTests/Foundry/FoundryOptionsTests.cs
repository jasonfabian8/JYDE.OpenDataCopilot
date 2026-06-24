using JYDE.OpenDataCopilot.Infrastructure.Foundry;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Foundry;

/// <summary>Pruebas de <see cref="FoundryOptions"/>.</summary>
public sealed class FoundryOptionsTests
{
    [Fact]
    public void Valores_PorDefecto_SonRazonables()
    {
        FoundryOptions options = new();

        options.Endpoint.ShouldBeEmpty();
        options.ApiKey.ShouldBeEmpty();
        options.EmbeddingDeployment.ShouldBe("text-embedding-3-small");
        options.ApiVersion.ShouldBe("2024-02-01");
        options.Dimensions.ShouldBe(1536);
        FoundryOptions.SectionName.ShouldBe("Foundry");
    }

    [Fact]
    public void Propiedades_SePuedenAsignar()
    {
        FoundryOptions options = new()
        {
            Endpoint = "https://x.openai.azure.com",
            ApiKey = "k",
            EmbeddingDeployment = "otro",
            ApiVersion = "2025-01-01",
            Dimensions = 256,
        };

        options.Endpoint.ShouldBe("https://x.openai.azure.com");
        options.ApiKey.ShouldBe("k");
        options.EmbeddingDeployment.ShouldBe("otro");
        options.ApiVersion.ShouldBe("2025-01-01");
        options.Dimensions.ShouldBe(256);
    }
}
