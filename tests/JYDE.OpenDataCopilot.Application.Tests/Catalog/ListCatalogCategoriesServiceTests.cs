using JYDE.OpenDataCopilot.Application.Catalog;
using Shouldly;

namespace JYDE.OpenDataCopilot.Application.Tests.Catalog;

/// <summary>Pruebas del caso de uso <see cref="ListCatalogCategoriesService"/>.</summary>
public sealed class ListCatalogCategoriesServiceTests
{
    [Fact]
    public async Task ExecuteAsync_DevuelveLasCategoriasDeLaFuente()
    {
        FakeCatalogSource source = new([])
        {
            Categories = [new CatalogCategory("Transporte", 261), new CatalogCategory("Educación", 1372)],
        };
        ListCatalogCategoriesService service = new(source);

        IReadOnlyList<CatalogCategory> categories = await service.ExecuteAsync(TestContext.Current.CancellationToken);

        categories.Count.ShouldBe(2);
        categories[0].Name.ShouldBe("Transporte");
    }

    [Fact]
    public void Constructor_ConFuenteNula_Lanza()
    {
        Should.Throw<ArgumentNullException>(() => new ListCatalogCategoriesService(null!));
    }
}
