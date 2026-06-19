---
name: new-context
description: Genera un nuevo bounded context completo (Domain + Application + Infrastructure + Tests) siguiendo el patrón hexagonal/DDD del proyecto OpenData Copilot. Úsalo al añadir un área de negocio nueva (p. ej. Catalog, Search, Conversation).
---

# Skill: nuevo bounded context

Crea la estructura de carpetas y clases base de un bounded context respetando la arquitectura
hexagonal del proyecto. Lee primero [SAD](../../../docs/architecture/SAD.md) y el
[`/CLAUDE.md`](../../../CLAUDE.md).

## Entrada

- `Nombre` del contexto en PascalCase (p. ej. `Catalog`, `Search`, `Conversation`).
- Opcional: nombre de la **entidad** principal y del **puerto** principal.

## Qué genera (respetando la dirección de dependencias)

1. **Domain** — `src/JYDE.OpenDataCopilot.Domain/{Nombre}/`
   - Entidad/agregado principal (`{Entidad}.cs`) con lenguaje ubicuo, validación en constructor.
   - Value objects necesarios.
2. **Application** — `src/JYDE.OpenDataCopilot.Application/{Nombre}/`
   - Puerto(s) `I{Algo}.cs` (interfaces) que el contexto necesita.
   - Caso(s) de uso `{Accion}{Nombre}Service.cs` que orquestan puerto(s) + dominio.
   - DTOs de entrada/salida.
3. **Infrastructure** — `src/JYDE.OpenDataCopilot.Infrastructure/{Nombre}/`
   - Esqueleto de adaptador(es) que implementan el/los puerto(s) (ver skill `new-adapter`).
4. **Tests**
   - `tests/...Domain.Tests/{Nombre}/` y `tests/...Application.Tests/{Nombre}/` con casos base
     usando xUnit + Shouldly y dobles de los puertos.

## Reglas

- Domain sin dependencias externas. Application define puertos, no los implementa.
- `namespace JYDE.OpenDataCopilot.{Capa}.{Nombre}` con file-scope.
- Documentación XML en miembros públicos.
- Tras generar, deja TODOs claros donde falte lógica y compila (`dotnet build`).
