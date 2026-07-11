using JYDE.OpenDataCopilot.Domain.Catalog;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Domain.Tests.Catalog;

/// <summary>Pruebas del value object <see cref="DatasetId"/>.</summary>
public sealed class DatasetIdTests
{
    [Fact]
    public void Constructor_ConFormato4x4_CreaIdentificador()
    {
        DatasetId id = new("ddau-8cy9");

        id.Value.ShouldBe("ddau-8cy9");
        id.ToString().ShouldBe("ddau-8cy9");
    }

    [Fact]
    public void Constructor_NormalizaMayusculasYEspacios()
    {
        DatasetId id = new("  DDAU-8CY9 ");

        id.Value.ShouldBe("ddau-8cy9");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ddau8cy9")]
    [InlineData("ddau-8cy")]
    [InlineData("ddau-8cy99")]
    [InlineData("dd@u-8cy9")]
    public void Constructor_ConValorInvalido_LanzaArgumentException(string value)
    {
        Should.Throw<ArgumentException>(() => new DatasetId(value));
    }

    [Fact]
    public void Igualdad_PorValor_EsVerdadera()
    {
        DatasetId a = new("ddau-8cy9");
        DatasetId b = new("DDAU-8CY9");

        a.ShouldBe(b);
    }
}
