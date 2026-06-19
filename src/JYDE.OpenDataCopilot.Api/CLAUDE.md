# CLAUDE.md — Capa Api

Composition root + capa de presentación HTTP. Ver gobierno global en
[`/CLAUDE.md`](../../CLAUDE.md) y [SAD](../../docs/architecture/SAD.md).

## Reglas

- Referencia a **Infrastructure** y **Application**. Es el único lugar que conoce a todos.
- **Composición DI por configuración:** registra el adaptador de cada puerto según
  `appsettings → Providers` (`SearchIndex`, `DatasetCache`, `Chat`, `Embeddings`).
  Ver [ADR-0003](../../docs/adr/0003-ports-adapters-intercambiables.md).
- Endpoints delgados: validan entrada, llaman a un caso de uso de Application y mapean el
  resultado a HTTP. **Sin lógica de negocio aquí.**
- Soporta streaming (SSE) para respuestas de chat en vivo.
- **No** acceder directamente a SDKs externos: siempre a través de los puertos/casos de uso.
- **Documentación XML** en tipos y miembros públicos.

## appsettings → Providers (local por defecto = gratis)

```jsonc
{ "Providers": { "SearchIndex": "PgVector", "DatasetCache": "DuckDb", "Chat": "Foundry", "Embeddings": "Foundry" } }
```
