using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Domain.Tests.Catalog;

/// <summary>Pruebas del agregado <see cref="Dataset"/>.</summary>
public sealed class DatasetTests
{
    private static DatasetId AnyId() => new("ddau-8cy9");

    [Fact]
    public void Constructor_ConDatosMinimos_AsignaValoresYColeccionesVacias()
    {
        Dataset dataset = new(AnyId(), "Accidentalidad vial");

        dataset.Id.ShouldBe(AnyId());
        dataset.Name.ShouldBe("Accidentalidad vial");
        dataset.Tags.ShouldBeEmpty();
        dataset.Columns.ShouldBeEmpty();
        dataset.Description.ShouldBeNull();
        dataset.SourceUrl.ShouldBeNull();
    }

    [Fact]
    public void Constructor_RecortaEspaciosDelNombre()
    {
        Dataset dataset = new(AnyId(), "  Cobertura de internet  ");

        dataset.Name.ShouldBe("Cobertura de internet");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ConNombreVacio_LanzaArgumentException(string name)
    {
        Should.Throw<ArgumentException>(() => new Dataset(AnyId(), name));
    }

    [Fact]
    public void Constructor_SinId_LanzaArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new Dataset(null!, "x"));
    }

    [Fact]
    public void Constructor_ConMetadatosCompletos_LosConserva()
    {
        DatasetColumn[] columns = [new("Municipio", "municipio", "text", "Nombre del municipio")];
        string[] tags = ["movilidad", "transporte"];
        Uri url = new("https://www.datos.gov.co/d/ddau-8cy9");
        DateTimeOffset updated = DateTimeOffset.UtcNow;

        Dataset dataset = new(
            AnyId(),
            "Accidentalidad",
            description: "Accidentes de tránsito",
            category: "Movilidad",
            tags: tags,
            columns: columns,
            sourceUrl: url,
            updatedAt: updated);

        dataset.Description.ShouldBe("Accidentes de tránsito");
        dataset.Category.ShouldBe("Movilidad");
        dataset.Tags.ShouldBe(tags);
        dataset.Columns.ShouldBe(columns);
        dataset.SourceUrl.ShouldBe(url);
        dataset.UpdatedAt.ShouldBe(updated);
    }
}
