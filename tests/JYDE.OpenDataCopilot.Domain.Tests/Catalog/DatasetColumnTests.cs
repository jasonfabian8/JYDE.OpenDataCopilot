using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Domain.Tests.Catalog;

/// <summary>Pruebas del value object <see cref="DatasetColumn"/>.</summary>
public sealed class DatasetColumnTests
{
    private static DatasetColumn Base() => new("Municipio", "municipio", "text", "Nombre del municipio");

    [Fact]
    public void Constructor_AsignaPropiedades()
    {
        DatasetColumn column = Base();

        column.Name.ShouldBe("Municipio");
        column.FieldName.ShouldBe("municipio");
        column.DataType.ShouldBe("text");
        column.Description.ShouldBe("Nombre del municipio");
    }

    [Fact]
    public void Description_EsOpcional()
    {
        DatasetColumn column = new("Edad", "edad", "number");

        column.Description.ShouldBeNull();
    }

    [Fact]
    public void Igualdad_PorValor_EsVerdadera()
    {
        DatasetColumn a = Base();
        DatasetColumn b = Base();

        (a == b).ShouldBeTrue();
        a.Equals(b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Theory]
    [InlineData("X", "municipio", "text", "Nombre del municipio")]
    [InlineData("Municipio", "x", "text", "Nombre del municipio")]
    [InlineData("Municipio", "municipio", "number", "Nombre del municipio")]
    [InlineData("Municipio", "municipio", "text", "otra")]
    public void Desigualdad_CuandoCambiaUnaPropiedad(string name, string field, string type, string? description)
    {
        DatasetColumn distinto = new(name, field, type, description);

        (Base() == distinto).ShouldBeFalse();
        Base().Equals(distinto).ShouldBeFalse();
    }
}
