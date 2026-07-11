using JYDE.OpenDataCopilot.Application.Catalog;
using JYDE.OpenDataCopilot.Domain.Catalog;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Socrata;

/// <summary>Pruebas del adaptador <see cref="SocrataCatalogClient"/> con un handler HTTP falso.</summary>
public sealed class SocrataCatalogClientTests
{
    private static SocrataCatalogClient CreateClient(FakeHttpMessageHandler handler, int pageSize = 1000)
    {
        HttpClient httpClient = new(handler);
        SocrataCatalogOptions options = new() { PageSize = pageSize };
        return new SocrataCatalogClient(httpClient, options);
    }

    private static async Task<List<Dataset>> CollectAsync(SocrataCatalogClient client, CatalogFilter filter)
    {
        List<Dataset> datasets = [];
        await foreach (Dataset dataset in client.FetchAsync(filter))
        {
            datasets.Add(dataset);
        }

        return datasets;
    }

    private static string ResultJson(string id, string name) =>
        $$$"""{"resource":{"id":"{{{id}}}","name":"{{{name}}}"}}""";

    private static string ResponseJson(int resultSetSize, params string[] results) =>
        $$"""{"results":[{{string.Join(',', results)}}],"resultSetSize":{{resultSetSize}}}""";

    [Fact]
    public async Task FetchAsync_MapeaTodosLosCamposDelResultado()
    {
        string body = """
        {
          "results": [
            {
              "resource": {
                "id": "ddau-8cy9",
                "name": "Accidentalidad vial",
                "description": "Accidentes de tránsito",
                "updatedAt": "2024-01-02T03:04:05.000Z",
                "columns_name": ["Municipio", "Edad"],
                "columns_field_name": ["municipio", "edad"],
                "columns_datatype": ["text", "number"],
                "columns_description": ["Nombre del municipio", ""]
              },
              "classification": { "domain_category": "Movilidad", "domain_tags": ["movilidad", "vias"] },
              "permalink": "https://www.datos.gov.co/d/ddau-8cy9",
              "link": "https://www.datos.gov.co/resource/ddau-8cy9.json"
            }
          ],
          "resultSetSize": 1
        }
        """;
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler(body));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        datasets.ShouldHaveSingleItem();
        Dataset dataset = datasets[0];
        dataset.Id.Value.ShouldBe("ddau-8cy9");
        dataset.Name.ShouldBe("Accidentalidad vial");
        dataset.Description.ShouldBe("Accidentes de tránsito");
        dataset.Category.ShouldBe("Movilidad");
        dataset.Tags.ShouldBe(["movilidad", "vias"]);
        dataset.Columns.Count.ShouldBe(2);
        dataset.Columns[0].ShouldBe(new DatasetColumn("Municipio", "municipio", "text", "Nombre del municipio"));
        dataset.SourceUrl.ShouldBe(new Uri("https://www.datos.gov.co/d/ddau-8cy9"));
        dataset.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task FetchAsync_PaginaSobreVariasPaginas()
    {
        string page1 = ResponseJson(3, ResultJson("aaaa-0001", "Uno"), ResultJson("aaaa-0002", "Dos"));
        string page2 = ResponseJson(3, ResultJson("aaaa-0003", "Tres"));
        FakeHttpMessageHandler handler = new(page1, page2);
        SocrataCatalogClient client = CreateClient(handler, pageSize: 2);

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        datasets.Select(d => d.Id.Value).ShouldBe(["aaaa-0001", "aaaa-0002", "aaaa-0003"]);
        handler.Requests.Count.ShouldBe(2);
        handler.Requests[0].Query.ShouldContain("offset=0");
        handler.Requests[1].Query.ShouldContain("offset=2");
    }

    [Fact]
    public async Task FetchAsync_RespetaElLimite()
    {
        string page = ResponseJson(5, ResultJson("aaaa-0001", "Uno"), ResultJson("aaaa-0002", "Dos"));
        FakeHttpMessageHandler handler = new(page);
        SocrataCatalogClient client = CreateClient(handler, pageSize: 10);

        List<Dataset> datasets = await CollectAsync(client, new CatalogFilter(Limit: 1));

        datasets.ShouldHaveSingleItem();
        handler.Requests.ShouldHaveSingleItem();
        handler.Requests[0].Query.ShouldContain("limit=1");
    }

    [Fact]
    public async Task FetchAsync_OmiteResultadosInvalidos()
    {
        string body = """
        {
          "results": [
            { },
            { "resource": { "name": "Sin id" } },
            { "resource": { "id": "no-valido", "name": "Id con formato inválido" } },
            { "resource": { "id": "aaaa-0009", "name": "Válido" } }
          ],
          "resultSetSize": 4
        }
        """;
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler(body));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        datasets.ShouldHaveSingleItem();
        datasets[0].Id.Value.ShouldBe("aaaa-0009");
    }

    [Fact]
    public async Task FetchAsync_ConLimiteCero_NoConsultaNiEmite()
    {
        FakeHttpMessageHandler handler = new(ResponseJson(5, ResultJson("aaaa-0001", "Uno")));
        SocrataCatalogClient client = CreateClient(handler);

        List<Dataset> datasets = await CollectAsync(client, new CatalogFilter(Limit: 0));

        datasets.ShouldBeEmpty();
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchAsync_SinResultados_NoEmiteNada()
    {
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler(ResponseJson(0)));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        datasets.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchAsync_IncluyeCategoriasEnLaUrl()
    {
        FakeHttpMessageHandler handler = new(ResponseJson(1, ResultJson("aaaa-0001", "Uno")));
        SocrataCatalogClient client = CreateClient(handler);

        await CollectAsync(client, new CatalogFilter(["Medio Ambiente"]));

        handler.Requests[0].Query.ShouldContain("categories=Medio%20Ambiente");
    }

    [Fact]
    public async Task FetchAsync_AcotaAlDominioConfigurado()
    {
        FakeHttpMessageHandler handler = new(ResponseJson(1, ResultJson("aaaa-0001", "Uno")));
        SocrataCatalogClient client = CreateClient(handler);

        await CollectAsync(client, CatalogFilter.All);

        handler.Requests[0].Query.ShouldContain("domains=www.datos.gov.co");
        handler.Requests[0].Query.ShouldContain("search_context=www.datos.gov.co");
    }

    [Fact]
    public async Task FetchAsync_MapeaColumnasConArreglosFaltantes_UsaValoresPorDefecto()
    {
        string body = """
        {
          "results": [
            {
              "resource": {
                "id": "aaaa-0010",
                "name": "Solo nombres de columna",
                "columns_name": ["Departamento"]
              }
            }
          ],
          "resultSetSize": 1
        }
        """;
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler(body));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        DatasetColumn column = datasets[0].Columns.ShouldHaveSingleItem();
        column.Name.ShouldBe("Departamento");
        column.FieldName.ShouldBe("Departamento");
        column.DataType.ShouldBe("unknown");
        column.Description.ShouldBeNull();
    }

    [Fact]
    public async Task FetchAsync_ConAppToken_EnviaCabecera()
    {
        FakeHttpMessageHandler handler = new(ResponseJson(1, ResultJson("aaaa-0001", "Uno")));
        HttpClient httpClient = new(handler);
        SocrataCatalogClient client = new(httpClient, new SocrataCatalogOptions { AppToken = "tok-123" });

        await CollectAsync(client, CatalogFilter.All);

        handler.LastAppToken.ShouldBe("tok-123");
    }

    [Fact]
    public async Task FetchAsync_ConRespuestaNula_NoEmiteNada()
    {
        // La API responde con un cuerpo JSON nulo: el deserializado es null y no debe romper.
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler("null"));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        datasets.ShouldBeEmpty();
    }

    [Fact]
    public async Task FetchAsync_ConArreglosDeColumnasMasCortos_UsaValoresPorDefecto()
    {
        string body = """
        {
          "results": [
            {
              "resource": {
                "id": "aaaa-0011",
                "name": "Columnas desparejas",
                "columns_name": ["Municipio", "Edad"],
                "columns_field_name": ["municipio"]
              }
            }
          ],
          "resultSetSize": 1
        }
        """;
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler(body));

        List<Dataset> datasets = await CollectAsync(client, CatalogFilter.All);

        IReadOnlyList<DatasetColumn> columns = datasets[0].Columns;
        columns.Count.ShouldBe(2);
        columns[0].FieldName.ShouldBe("municipio");
        columns[1].FieldName.ShouldBe("Edad"); // sin field_name propio, cae al nombre
        columns[1].DataType.ShouldBe("unknown");
    }

    [Fact]
    public async Task FetchAsync_ConLimiteMayorAlMaximo_SeAcotaAlMaximo()
    {
        // Defensa CWE-834: aunque el usuario pida más, el bucle no supera MaxResults.
        string page = ResponseJson(100,
            ResultJson("aaaa-0001", "Uno"), ResultJson("aaaa-0002", "Dos"), ResultJson("aaaa-0003", "Tres"));
        HttpClient httpClient = new(new FakeHttpMessageHandler(page));
        SocrataCatalogClient client = new(httpClient, new SocrataCatalogOptions { PageSize = 10, MaxResults = 2 });

        List<Dataset> datasets = await CollectAsync(client, new CatalogFilter(Limit: 1000));

        datasets.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetCategoriesAsync_MapeaOrdenaPorConteo_YOmiteVacias()
    {
        string body = """
        {
          "results": [
            { "domain_category": "Educación", "count": 1372 },
            { "domain_category": "Transporte", "count": 261 },
            { "domain_category": "", "count": 9 },
            { "domain_category": "Salud", "count": 1312 }
          ],
          "resultSetSize": 4
        }
        """;
        FakeHttpMessageHandler handler = new(body);
        SocrataCatalogClient client = CreateClient(handler);

        IReadOnlyList<CatalogCategory> categories = await client.GetCategoriesAsync(TestContext.Current.CancellationToken);

        categories.Select(category => category.Name).ShouldBe(["Educación", "Salud", "Transporte"]);
        categories[0].Count.ShouldBe(1372);
        handler.Requests[0].AbsolutePath.ShouldContain("domain_categories");
        handler.Requests[0].Query.ShouldContain("domains=www.datos.gov.co");
    }

    [Fact]
    public async Task GetCategoriesAsync_ConRespuestaNula_DevuelveVacio()
    {
        SocrataCatalogClient client = CreateClient(new FakeHttpMessageHandler("null"));

        IReadOnlyList<CatalogCategory> categories = await client.GetCategoriesAsync(TestContext.Current.CancellationToken);

        categories.ShouldBeEmpty();
    }

    [Fact]
    public void Constructor_ConArgumentosInvalidos_Lanza()
    {
        HttpClient httpClient = new(new FakeHttpMessageHandler());

        Should.Throw<ArgumentNullException>(() => new SocrataCatalogClient(null!, new SocrataCatalogOptions()));
        Should.Throw<ArgumentNullException>(() => new SocrataCatalogClient(httpClient, null!));
        Should.Throw<ArgumentOutOfRangeException>(
            () => new SocrataCatalogClient(httpClient, new SocrataCatalogOptions { PageSize = 0 }));
        Should.Throw<ArgumentOutOfRangeException>(
            () => new SocrataCatalogClient(httpClient, new SocrataCatalogOptions { MaxResults = 0 }));
    }
}
