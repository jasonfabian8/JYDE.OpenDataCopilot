# Deuda técnica

Registro vivo de deuda técnica: diferimientos conscientes que respetan el gobierno (SAD/ADR) hoy,
pero que deben resolverse cuando se cumpla su condición de disparo. Una deuda = una fila. Al saldarla,
mueve la fila a **Resueltas** con la fecha y el commit/PR.

## Abiertas

| ID | Deuda | Contexto y razón del diferimiento | Condición de disparo (cuándo resolver) | Referencias |
|----|-------|-----------------------------------|----------------------------------------|-------------|
| TD-001 | Selección de adaptador del contexto **Catalog** por configuración (`Providers`). | El composition root (`Program.cs`) registra hoy `SocrataCatalogClient` (fuente) e `InMemoryCatalogRepository` (repositorio) de forma directa. Hay **un único adaptador por puerto** y `Catalog` **no** está en la matriz `Providers` del ADR-0003. Añadir un `switch` de una sola rama sería YAGNI; el ADR contempla implementación **incremental** de los contratos. | Al incorporar un **segundo adaptador** de `ICatalogSource` o `ICatalogRepository` (p. ej. un repositorio persistente): añadir claves `Providers:CatalogSource` / `Providers:CatalogRepository`, la rama de registro DI (lanzando en valor desconocido) y su test; actualizar SAD/ADR-0003. | [ADR-0003](adr/0003-ports-adapters-intercambiables.md); `src/JYDE.OpenDataCopilot.Api/Program.cs` |

## Resueltas

_(ninguna aún)_
