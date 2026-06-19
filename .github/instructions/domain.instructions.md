---
applyTo: "src/JYDE.OpenDataCopilot.Domain/**/*.cs"
---

# Capa Domain — reglas (Copilot)

Núcleo de negocio puro. Detalle en [SAD](../../docs/architecture/SAD.md).

- **Cero dependencias externas**: sólo BCL de .NET. Sin NuGet de infraestructura.
- No referenciar Application, Infrastructure ni Api.
- Sólo entidades, value objects, agregados y reglas de negocio puras.
- Modelar con el lenguaje ubicuo (`Dataset`, `Column`, `Category`, `DatasetMetadata`).
- Evitar estados inválidos (validación en constructor; preferir inmutabilidad).
- **No** poner aquí acceso a datos, serialización ni llamadas a APIs.
- Documentación XML en tipos y miembros públicos.
