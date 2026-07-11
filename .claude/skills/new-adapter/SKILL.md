---
name: new-adapter
description: Crea una nueva implementación (adaptador) de un puerto existente en la capa Infrastructure, más su registro DI por configuración y un esqueleto de test de integración. Úsalo para añadir un proveedor nuevo (p. ej. AzureAISearch, Qdrant, MongoAtlas) a un puerto ya definido.
---

# Skill: nuevo adaptador

Añade una implementación concreta de un puerto sin tocar Domain ni Application
([ADR-0003](../../../docs/adr/0003-ports-adapters-intercambiables.md)). Lee
[la capa Infrastructure](../../../src/JYDE.OpenDataCopilot.Infrastructure/CLAUDE.md).

## Entrada

- `Puerto`: interfaz existente en Application (p. ej. `IDatasetSearchIndex`).
- `Proveedor`: nombre del proveedor/tecnología (p. ej. `AzureAISearch`, `Qdrant`, `MongoAtlas`).

## Pasos

1. Verifica que el puerto exista en `src/JYDE.OpenDataCopilot.Application/.../I{Puerto}.cs`.
2. Crea el adaptador en `src/JYDE.OpenDataCopilot.Infrastructure/{Area}/{Proveedor}{Puerto}.cs`
   (sin la `I`), implementando la interfaz. Coloca aquí TODO el detalle externo (SDK/HttpClient,
   serialización, reintentos/timeouts).
3. Añade los paquetes NuGet necesarios **solo** al proyecto Infrastructure.
4. **Registro DI por configuración** en el composition root
   (`src/JYDE.OpenDataCopilot.Api/Program.cs`): añade una rama en el `switch` de
   `Providers:{Seccion}` que registre `{Proveedor}{Puerto}` cuando el valor coincida.
5. Expón las opciones de configuración necesarias en `appsettings.json` (y `.local` para secretos).
6. Crea un test de integración en
   `tests/JYDE.OpenDataCopilot.Infrastructure.IntegrationTests/{Area}/{Proveedor}{Puerto}Tests.cs`
   (xUnit + Shouldly), saltable si la dependencia no está disponible localmente.

## Reglas

- No cambies la firma del puerto para acomodar un proveedor; si hace falta, discútelo en un ADR.
- Mapea errores del SDK a resultados/excepciones del dominio; no filtres tipos del SDK hacia arriba.
- Mantén el adaptador intercambiable: nada específico del proveedor debe escapar de esta clase.
- Compila y, si la dependencia está arriba (`docker compose up`), corre el test.
