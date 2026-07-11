using Microsoft.AspNetCore.Mvc;

namespace JYDE.OpenDataCopilot.Api.Controllers;

/// <summary>Endpoint raíz informativo de la API.</summary>
[ApiController]
[Route("/")]
public sealed class HomeController : ControllerBase
{
    /// <summary>Mensaje de bienvenida / verificación rápida de que la API responde.</summary>
    [HttpGet]
    public IActionResult Get() => Ok("OpenData Copilot API");
}
