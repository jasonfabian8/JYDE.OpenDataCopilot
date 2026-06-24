using JYDE.OpenDataCopilot.Application.Search;
using Shouldly;
using Xunit;

namespace JYDE.OpenDataCopilot.Application.Tests.Search;

/// <summary>Pruebas de los DTOs/records del bounded context Search (igualdad y accesores).</summary>
public sealed class SearchRecordsTests
{
    [Fact]
    public void DatasetVector_IgualdadYAccesores()
    {
        DatasetVector vector = new("aaaa-0001", "Uno", "Movilidad", "https://x/y", [1f, 2f]);
        DatasetVector copia = vector with { };
        DatasetVector distinto = vector with { Id = "bbbb-0002" };

        vector.Name.ShouldBe("Uno");
        vector.Category.ShouldBe("Movilidad");
        vector.SourceUrl.ShouldBe("https://x/y");
        vector.Embedding.Count.ShouldBe(2);

        (vector == copia).ShouldBeTrue();
        vector.GetHashCode().ShouldBe(copia.GetHashCode());
        (vector == distinto).ShouldBeFalse();
        vector.ToString().ShouldContain("aaaa-0001");
    }

    [Fact]
    public void DatasetSearchHit_IgualdadYAccesores()
    {
        DatasetSearchHit hit = new("aaaa-0001", "Uno", "Salud", "https://x/y", 0.87);
        DatasetSearchHit copia = hit with { };
        DatasetSearchHit distinto = hit with { Score = 0.1 };

        hit.Id.ShouldBe("aaaa-0001");
        hit.Name.ShouldBe("Uno");
        hit.Category.ShouldBe("Salud");
        hit.SourceUrl.ShouldBe("https://x/y");
        hit.Score.ShouldBe(0.87);

        (hit == copia).ShouldBeTrue();
        hit.GetHashCode().ShouldBe(copia.GetHashCode());
        (hit == distinto).ShouldBeFalse();
        hit.ToString().ShouldContain("Uno");
    }
}
