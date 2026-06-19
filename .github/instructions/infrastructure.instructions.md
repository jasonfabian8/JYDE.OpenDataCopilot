---
applyTo: "src/JYDE.OpenDataCopilot.Infrastructure/**/*.cs"
---

# Capa Infrastructure — reglas (Copilot)

Adaptadores: implementaciones concretas de los puertos. Detalle en
[SAD](../../docs/architecture/SAD.md).

- Implementa los puertos definidos en Application. Referencia a Application (Domain transitivo).
- **Aquí van TODOS los detalles externos**: HttpClient, SDKs de Azure/Foundry, Socrata,
  pgvector/Qdrant/Azure AI Search, DuckDB/Postgres/MongoDB, serialización, reintentos.
- Organizar por proveedor en subcarpetas: `Socrata/`, `Foundry/`, `Search/`, `Cache/`.
- Adaptadores intercambiables por configuración (registro DI en Api según `Providers`).
- Nombrar `{Proveedor}{Puerto}` sin la `I` (p. ej. `PgVectorSearchIndex`, `FoundryEmbeddings`).
- Resiliencia (timeouts, reintentos) al tocar APIs externas; no filtrar tipos de SDK hacia arriba.
- Tests de integración en `tests/...Infrastructure.IntegrationTests`.
- Documentación XML en tipos y miembros públicos.
