---
applyTo: "src/JYDE.OpenDataCopilot.Application/**/*.cs"
---

# Capa Application — reglas (Copilot)

Casos de uso y orquestación. Detalle en [SAD](../../docs/architecture/SAD.md).

- Sólo referencia a **Domain**. No referenciar Infrastructure ni Api.
- **Definir aquí los puertos** (interfaces): `ICatalogSource`, `IDatasetSearchIndex`,
  `IEmbeddingGenerator`, `IChatCompletion`, `IDataQuery`, `IDatasetCache`, `IConversationStore`.
- Implementar **casos de uso** que orquestan puertos + dominio; exponer DTOs.
- **No implementar** los puertos aquí (eso es Infrastructure); sólo consumirlos por interfaz.
- Puertos pequeños y orientados al caso de uso.
- Testear con dobles de los puertos (TDD por convención).
- Documentación XML en tipos y miembros públicos.
