using JYDE.OpenDataCopilot.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace JYDE.OpenDataCopilot.Api.Tests.Controllers;

/// <summary>Pruebas del <see cref="HomeController"/>.</summary>
public sealed class HomeControllerTests
{
    [Fact]
    public void Get_DevuelveMensajeDeBienvenida()
    {
        HomeController controller = new();

        IActionResult result = controller.Get();

        OkObjectResult ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBe("OpenData Copilot API");
    }
}
