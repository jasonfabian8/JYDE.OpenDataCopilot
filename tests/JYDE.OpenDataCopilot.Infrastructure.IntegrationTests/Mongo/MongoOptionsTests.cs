using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Mongo;

/// <summary>Pruebas de <see cref="MongoOptions"/>.</summary>
public sealed class MongoOptionsTests
{
    [Fact]
    public void Valores_PorDefecto_SonRazonables()
    {
        MongoOptions options = new();

        options.ConnectionString.ShouldBeEmpty();
        options.Database.ShouldBe("odc_BD");
        options.CatalogCollection.ShouldBe("datasets");
        MongoOptions.SectionName.ShouldBe("Mongo");
    }

    [Fact]
    public void Propiedades_SePuedenAsignar()
    {
        MongoOptions options = new()
        {
            ConnectionString = "mongodb://localhost",
            Database = "otra",
            CatalogCollection = "coleccion",
        };

        options.ConnectionString.ShouldBe("mongodb://localhost");
        options.Database.ShouldBe("otra");
        options.CatalogCollection.ShouldBe("coleccion");
    }
}
