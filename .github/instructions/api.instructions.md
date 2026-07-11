---
applyTo: "src/JYDE.OpenDataCopilot.Api/**/*.cs"
---

# Capa Api — reglas (Copilot)

Composition root + presentación HTTP. Detalle en [SAD](../../docs/architecture/SAD.md).

- Referencia a Infrastructure y Application. Único lugar que conoce a todas las capas.
- **Controladores MVC, NO Minimal API** ([ADR-0010](../../docs/adr/0010-api-con-controladores.md)):
  todo endpoint en `ControllerBase` + `[ApiController]` con attribute routing. `Program.cs` sólo compone (`AddControllers`/`MapControllers`); nada de `app.MapGet/MapPost/...`.
- **Composición DI por configuración**: registrar el adaptador de cada puerto según
  `appsettings → Providers` (`SearchIndex`, `DatasetCache`, `Chat`, `Embeddings`).
- Controladores delgados: validar entrada → llamar caso de uso → mapear a HTTP. Sin lógica de negocio.
- **No referenciar el `Domain`** ([ADR-0011](../../docs/adr/0011-api-no-referencia-dominio.md)):
  sólo casos de uso y **DTOs de Application** (o modelos de request propios); nada de entidades/VOs
  de dominio ni de llamar puertos de salida (repositorios) directamente.
- Soportar streaming (SSE) para chat en vivo.
- No acceder directamente a SDKs externos; siempre vía puertos/casos de uso.
- Documentación XML en tipos y miembros públicos.
