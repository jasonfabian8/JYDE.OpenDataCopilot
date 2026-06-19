---
applyTo: "src/JYDE.OpenDataCopilot.Api/**/*.cs"
---

# Capa Api — reglas (Copilot)

Composition root + presentación HTTP. Detalle en [SAD](../../docs/architecture/SAD.md).

- Referencia a Infrastructure y Application. Único lugar que conoce a todas las capas.
- **Composición DI por configuración**: registrar el adaptador de cada puerto según
  `appsettings → Providers` (`SearchIndex`, `DatasetCache`, `Chat`, `Embeddings`).
- Endpoints delgados: validar entrada → llamar caso de uso → mapear a HTTP. Sin lógica de negocio.
- Soportar streaming (SSE) para chat en vivo.
- No acceder directamente a SDKs externos; siempre vía puertos/casos de uso.
- Documentación XML en tipos y miembros públicos.
