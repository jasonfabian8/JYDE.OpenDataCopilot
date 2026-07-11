# CLAUDE.md — Capa Api

Composition root + capa de presentación HTTP. Ver gobierno global en
[`/CLAUDE.md`](../../CLAUDE.md) y [SAD](../../docs/architecture/SAD.md).

## Reglas

- Referencia a **Infrastructure** y **Application**. Es el único lugar que conoce a todos.
- **Composición DI por configuración:** registra el adaptador de cada puerto según
  `appsettings → Providers` (`SearchIndex`, `DatasetCache`, `Chat`, `Embeddings`).
  Ver [ADR-0003](../../docs/adr/0003-ports-adapters-intercambiables.md).
- **Controladores MVC, no Minimal API** ([ADR-0010](../../docs/adr/0010-api-con-controladores.md)): todo endpoint va en un `ControllerBase` con `[ApiController]` y *attribute routing*. `Program.cs` sólo compone (`AddControllers`/`MapControllers`); nada de `app.MapGet/MapPost/...`.
- Controladores delgados: validan entrada, llaman a un caso de uso de Application y mapean el resultado a HTTP. **Sin lógica de negocio aquí.**
- **No referenciar el `Domain`** ([ADR-0011](../../docs/adr/0011-api-no-referencia-dominio.md)): depender sólo de casos de uso y **DTOs de Application** (o modelos de request propios). No construir entidades/VOs de dominio ni invocar puertos de salida (repositorios) directamente; el mapeo dominio→DTO vive en Application.
- Soporta streaming (SSE) para respuestas de chat en vivo.
- **No** acceder directamente a SDKs externos: siempre a través de los puertos/casos de uso.
- **Documentación XML** en tipos y miembros públicos.

## appsettings → Providers (local por defecto = gratis)

```jsonc
{ "Providers": { "SearchIndex": "PgVector", "DatasetCache": "DuckDb", "Chat": "Foundry", "Embeddings": "Foundry" } }
```
