# CLAUDE.md — Capa Infrastructure

Adaptadores: implementaciones concretas de los puertos. Ver gobierno global en
[`/CLAUDE.md`](../../CLAUDE.md) y [SAD](../../docs/architecture/SAD.md).

## Reglas

- Referencia a **Application** (y Domain transitivo). Implementa los **puertos** definidos allí.
- **Aquí viven TODOS los detalles externos:** `HttpClient`, SDKs de Azure/Foundry, clientes de
  Socrata, pgvector/Qdrant/Azure AI Search, DuckDB/Postgres/MongoDB, serialización, reintentos.
- Organiza por proveedor/tecnología en subcarpetas: `Socrata/`, `Foundry/`, `Search/`, `Cache/`.
- **Adaptadores intercambiables por configuración** ([ADR-0003](../../docs/adr/0003-ports-adapters-intercambiables.md)):
  cada implementación se registra en el composition root (Api) según `appsettings → Providers`.
- Añadir un proveedor = **nuevo adaptador** + rama de registro DI. No cambiar dominio/aplicación.
- Tests de integración en `tests/...Infrastructure.IntegrationTests`.
- **Documentación XML** en tipos y miembros públicos.

## Convenciones de adaptadores

- Un adaptador por clase, nombre `=> {Proveedor}{Puerto}` sin la `I`
  (p. ej. `SocrataCatalogClient`, `PgVectorSearchIndex`, `FoundryChatCompletion`).
- Resiliencia donde toque APIs externas: timeouts y reintentos.
- Mapear errores externos a excepciones/resultados del dominio; no filtrar tipos de SDK hacia arriba.
