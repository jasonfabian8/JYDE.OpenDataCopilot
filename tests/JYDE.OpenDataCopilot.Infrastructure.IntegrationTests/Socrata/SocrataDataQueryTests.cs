using JYDE.OpenDataCopilot.Application.Figures;
using JYDE.OpenDataCopilot.Infrastructure.Socrata;
using Shouldly;

namespace JYDE.OpenDataCopilot.Infrastructure.IntegrationTests.Socrata;

/// <summary>Pruebas del adaptador <see cref="SocrataDataQuery"/> (SODA) con un handler HTTP falso.</summary>
public sealed class SocrataDataQueryTests
{
    private static SocrataDataQuery Create(FakeHttpMessageHandler handler) =>
        new(new HttpClient(handler), new SocrataCatalogOptions());

    [Fact]
    public async Task QueryAsync_ParseaColumnasYFilas_YLlamaElResourceCorrecto()
    {
        FakeHttpMessageHandler handler = new("""[{"genero":"Masculino","total":"120"},{"genero":"Femenino","total":"98"}]""");
        SocrataDataQuery client = Create(handler);

        DataQueryResult result = await client.QueryAsync(
            "aaaa-0001", "SELECT genero, count(*) AS total GROUP BY genero", TestContext.Current.CancellationToken);

        result.Columns.ShouldBe(["genero", "total"]);
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].ShouldBe(["Masculino", "120"]);
        handler.Requests[0].AbsolutePath.ShouldContain("resource/aaaa-0001.json");
        handler.Requests[0].Query.ShouldContain("query=");
    }

    [Fact]
    public async Task QueryAsync_ConNumerosBooleanosYNulos_LosConvierteATexto()
    {
        FakeHttpMessageHandler handler = new("""[{"a":5,"b":null,"c":true}]""");

        DataQueryResult result = await Create(handler).QueryAsync("aaaa-0001", "SELECT a", TestContext.Current.CancellationToken);

        result.Columns.ShouldBe(["a", "b", "c"]);
        result.Rows[0].ShouldBe(["5", "", "true"]);
    }

    [Fact]
    public async Task QueryAsync_ConFalseArraysEItemsNoObjeto_LosManeja()
    {
        FakeHttpMessageHandler handler = new("""[{"a":false,"b":[1,2]},"suelto",{"a":true,"b":"x"}]""");

        DataQueryResult result = await Create(handler).QueryAsync("aaaa-0001", "x", TestContext.Current.CancellationToken);

        result.Columns.ShouldBe(["a", "b"]);
        result.Rows.Count.ShouldBe(2);
        result.Rows[0].ShouldBe(["false", "[1,2]"]);
        result.Rows[1].ShouldBe(["true", "x"]);
    }

    [Fact]
    public async Task QueryAsync_ConRespuestaNoArreglo_DevuelveVacio()
    {
        FakeHttpMessageHandler handler = new("""{"error":true}""");

        DataQueryResult result = await Create(handler).QueryAsync("aaaa-0001", "x", TestContext.Current.CancellationToken);

        result.Columns.ShouldBeEmpty();
        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueryAsync_AcotaElNumeroDeFilas()
    {
        string items = string.Join(",", Enumerable.Range(0, SocrataDataQuery.MaxRows + 5).Select(i => $"{{\"a\":\"{i}\"}}"));
        FakeHttpMessageHandler handler = new($"[{items}]");

        DataQueryResult result = await Create(handler).QueryAsync("aaaa-0001", "x", TestContext.Current.CancellationToken);

        result.Rows.Count.ShouldBe(SocrataDataQuery.MaxRows);
    }

    [Fact]
    public async Task QueryAsync_ConAppToken_EnviaCabecera()
    {
        FakeHttpMessageHandler handler = new("[]");
        SocrataDataQuery client = new(new HttpClient(handler), new SocrataCatalogOptions { AppToken = "tok-123" });

        await client.QueryAsync("aaaa-0001", "x", TestContext.Current.CancellationToken);

        handler.LastAppToken.ShouldBe("tok-123");
    }

    [Fact]
    public async Task Constructor_YArgumentosInvalidos_Lanzan()
    {
        Should.Throw<ArgumentNullException>(() => new SocrataDataQuery(null!, new SocrataCatalogOptions()));
        Should.Throw<ArgumentNullException>(() => new SocrataDataQuery(new HttpClient(new FakeHttpMessageHandler("[]")), null!));

        SocrataDataQuery client = Create(new FakeHttpMessageHandler("[]"));
        await Should.ThrowAsync<ArgumentException>(() => client.QueryAsync("", "soql"));
        await Should.ThrowAsync<ArgumentException>(() => client.QueryAsync("aaaa-0001", "   "));
    }
}
