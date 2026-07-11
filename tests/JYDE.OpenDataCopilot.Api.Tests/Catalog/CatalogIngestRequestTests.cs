using JYDE.OpenDataCopilot.Api.Catalog;
using JYDE.OpenDataCopilot.Application.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Catalog;

/// <summary>Pruebas del saneamiento en la frontera de <see cref="CatalogIngestRequest"/>.</summary>
public sealed class CatalogIngestRequestTests
{
    [Fact]
    public void ToFilter_AcotaElLimiteYElNumeroDeCategorias()
    {
        string[] categories = [.. Enumerable.Range(0, 200).Select(index => $"cat-{index}")];
        CatalogIngestRequest request = new(categories, Limit: 999_999);

        CatalogFilter filter = request.ToFilter();

        filter.Limit.ShouldBe(10_000); // acotado a MaxLimit
        filter.Categories.ShouldNotBeNull();
        filter.Categories.Count.ShouldBe(50); // acotado a MaxCategories
    }

    [Fact]
    public void ToFilter_SinLimiteNiCategorias_DejaValoresNulos()
    {
        CatalogFilter filter = new CatalogIngestRequest().ToFilter();

        filter.Limit.ShouldBeNull();
        filter.Categories.ShouldBeNull();
    }

    [Fact]
    public void ToFilter_ConLimiteNegativo_LoLlevaACero()
    {
        CatalogFilter filter = new CatalogIngestRequest(Limit: -5).ToFilter();

        filter.Limit.ShouldBe(0);
    }

    [Fact]
    public void ToFilter_ConCategoriasVacias_DejaNull()
    {
        CatalogFilter filter = new CatalogIngestRequest([]).ToFilter();

        filter.Categories.ShouldBeNull();
    }
}
