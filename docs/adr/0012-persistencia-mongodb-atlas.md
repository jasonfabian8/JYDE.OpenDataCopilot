# ADR 0012 — Persistencia con MongoDB Atlas (driver y almacén del catálogo)

- **Estado:** Aceptado
- **Fecha:** 2026-06-19
- **Decisores:** Equipo OpenData Copilot

## Contexto

El catálogo necesita un almacén **persistente** (el adaptador en memoria pierde los datos al
reiniciar) que además sea **compartible** por el equipo y para el demo, y **de costo cero**. Más
adelante el mismo motor puede cubrir la búsqueda (Atlas Search keyword + Atlas Vector Search),
reduciendo piezas. Criterios técnicos: modelo documental afín a los metadatos (JSON), capa gratuita
suficiente para el alcance, driver maduro en .NET, y selección por configuración sin acoplar el
dominio.

Se evaluó el impacto del **free tier M0** (512 MB, CPU/RAM compartida, límites de índices de
Search): el almacenamiento alcanza con holgura para ~8.000 datasets (metadatos + vectores en
binario) y la latencia/escala, aunque limitadas, son aceptables para el concurso.

## Decisión

- Adoptar **MongoDB Atlas** como almacén persistente y **`MongoDB.Driver`** como dependencia en la
  capa **Infrastructure** (gobierno de librerías: registrado aquí + en el SAD).
- Implementar `MongoCatalogRepository` como adaptador de `ICatalogRepository`, **seleccionable por
  configuración** (`Providers:CatalogRepository = InMemory | Mongo`).
- El mapeo dominio↔persistencia usa un **modelo de documento** propio (`DatasetDocument`); no se
  serializan entidades de dominio directamente.
- **Secretos fuera del repo**: la cadena de conexión vive en `appsettings.Development.json`
  (gitignored) o equivalente; el repositorio público solo contiene valores no sensibles y un
  `.example`.

## Consecuencias

- **Positivas:** persistencia real, entorno compartido/demo, costo cero; base para unificar Search
  (vector + keyword) en el mismo motor; adaptador intercambiable sin tocar dominio/aplicación.
- **Negativas / trade-offs:** dependencia nueva (`MongoDB.Driver`); límites del M0 (latencia/escala,
  nº de índices, 512 MB); escalar a futuro implica plan pago. La generación de embeddings sigue
  teniendo costo aparte (Foundry).
- **Seguimiento:** evaluar `MongoDatasetSearchIndex` (Atlas Vector + Search) para el bounded context
  Search; rotar credenciales si se exponen.

## Alternativas consideradas

- **Solo adaptador en memoria** — cero costo y simple, pero sin persistencia ni entorno compartido.
  Se conserva como opción local/dev, no como almacén real.
- **PostgreSQL + pgvector / Qdrant (Docker)** — buenos para vector, pero requieren operar
  infraestructura local o pagar hosting; Atlas M0 da un almacén gestionado gratuito ya disponible.
