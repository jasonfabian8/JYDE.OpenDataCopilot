using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Mongo;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Mongo;

/// <summary>Pruebas del mapeo dominio↔documento de persistencia (sin tocar MongoDB).</summary>
public sealed class DatasetDocumentMappingTests
{
    [Fact]
    public void RoundTrip_ConDatosCompletos_ConservaLosValores()
    {
        Dataset original = new(
            new DatasetId("ddau-8cy9"),
            "Accidentalidad",
            description: "Accidentes de tránsito",
            category: "Movilidad",
            tags: ["movilidad", "vias"],
            columns: [new DatasetColumn("Municipio", "municipio", "text", "Nombre")],
            sourceUrl: new Uri("https://www.datos.gov.co/d/ddau-8cy9"),
            updatedAt: DateTimeOffset.UnixEpoch);

        Dataset restored = DatasetDocument.FromDomain(original).ToDomain();

        restored.Id.ShouldBe(original.Id);
        restored.Name.ShouldBe("Accidentalidad");
        restored.Description.ShouldBe("Accidentes de tránsito");
        restored.Category.ShouldBe("Movilidad");
        restored.Tags.ShouldBe(["movilidad", "vias"]);
        restored.Columns.ShouldHaveSingleItem().ShouldBe(new DatasetColumn("Municipio", "municipio", "text", "Nombre"));
        restored.SourceUrl.ShouldBe(original.SourceUrl);
        restored.UpdatedAt.ShouldBe(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void RoundTrip_ConDatosMinimos_MantieneNulosYColeccionesVacias()
    {
        Dataset original = new(new DatasetId("aaaa-0001"), "Mínimo");

        Dataset restored = DatasetDocument.FromDomain(original).ToDomain();

        restored.Id.Value.ShouldBe("aaaa-0001");
        restored.SourceUrl.ShouldBeNull();
        restored.Description.ShouldBeNull();
        restored.Tags.ShouldBeEmpty();
        restored.Columns.ShouldBeEmpty();
        restored.UpdatedAt.ShouldBeNull();
    }
}
