# ADR 0010 — API con controladores MVC (no Minimal API)

- **Estado:** Aceptado
- **Fecha:** 2026-06-19
- **Decisores:** Equipo OpenData Copilot

## Contexto

La API expondrá varios bounded contexts (Catalog, Search, Conversation) con un número creciente de endpoints, incluido streaming (SSE) y, a futuro, autenticación, versionado y validación de contratos. Necesitamos un estilo de exposición HTTP **uniforme** en toda la solución. Criterios
técnicos:

- **Consistencia y organización:** un mismo patrón para todos los endpoints, agrupados por recurso en clases cohesivas, fácil de localizar a medida que la API crece.
- **Capacidades transversales declarativas:** *filters* (autorización, validación, manejo de errores), *model binding* y validación vía `[ApiController]`, *attribute routing* y convenciones.
- **Testabilidad:** controladores como clases con dependencias inyectadas, testeables de forma unitaria además de las pruebas de integración HTTP.
- **Separación de responsabilidades:** la capa de presentación (controladores) queda claramente delimitada respecto a la composición (`Program.cs`).

## Decisión

**Toda la exposición HTTP se implementa con controladores MVC** (`ControllerBase` + `[ApiController]` + *attribute routing*). **No se usa Minimal API** (`app.MapGet/MapPost/...`) para endpoints de la aplicación.

- `Program.cs` se limita a la composición: `AddControllers()` + `MapControllers()` y el registro de adaptadores por configuración. 
- Endpoints delgados: validan entrada, invocan un caso de uso de `Application` y mapean el resultado a HTTP. Sin lógica de negocio en el controlador.

## Consecuencias

- **Positivas:** estilo homogéneo y escalable; acceso directo a filters/binding/validación/versionado; controladores testeables; `Program.cs` enfocado sólo en composición.
- **Negativas / trade-offs:** algo más de ceremonia que Minimal API para endpoints triviales; se asume a cambio de consistencia a largo plazo.
- **Seguimiento:** mantener los controladores delgados; las capacidades transversales (errores, validación) se centralizan con filters cuando se requieran.

## Alternativas consideradas

- **Minimal API** — conciso para APIs pequeñas, pero dispersa la configuración y es menos uniforme a medida que crecen endpoints y necesidades transversales. Descartado como estándar del proyecto.
