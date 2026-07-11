# CLAUDE.md — Capa Application

Casos de uso y orquestación. Ver gobierno global en [`/CLAUDE.md`](../../CLAUDE.md) y
[SAD](../../docs/architecture/SAD.md).

## Reglas

- Sólo referencia a **Domain**. **No** referencia Infrastructure ni Api.
- **Aquí se DEFINEN los puertos** (interfaces) de servicios técnicos que la app necesita:
  `ICatalogSource`, `IDatasetSearchIndex`, `IEmbeddingGenerator`, `IChatCompletion`,
  `IDataQuery`, `IDatasetCache`, `IConversationStore`.
- Contiene **casos de uso / servicios de aplicación** que orquestan puertos + dominio, y **DTOs**.
- **No implementa** los puertos (eso es Infrastructure). Aquí sólo se consumen vía interfaz.
- Puertos pequeños y orientados al caso de uso (evitar interfaces "catch-all").
- TDD por convención: estos casos de uso se testean con **dobles** de los puertos.
- **Documentación XML** en tipos y miembros públicos.

## Patrón de un caso de uso

1. Recibe un DTO de entrada.
2. Usa puertos (inyectados) + entidades de dominio.
3. Devuelve un DTO de salida. Sin detalles de proveedor ni de transporte (HTTP/JSON).
