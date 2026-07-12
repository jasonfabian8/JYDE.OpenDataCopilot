# Historias de usuario

Historias derivadas de los [requerimientos iniciales](requerimientos_iniciales.md), agrupadas por
épica (alineadas con los bounded contexts del [SAD §5](../architecture/SAD.md#5-bounded-contexts)).
Los roles provienen de las audiencias del [planteamiento del problema](../planteamiento_problema.md):
ciudadano, periodista, investigador, emprendedor, entidad pública y equipo de desarrollo.

Estado: ✅ implementada · 🔜 backlog (roadmap).

---

## Épica 1 — Catálogo (descubrir qué existe)

### US-01 · Ingesta del catálogo ✅
**Como** operador del sistema, **quiero** ingerir los metadatos del catálogo de datos.gov.co
(completo o por categorías, con límite) **para** que el Copilot conozca qué datasets existen sin
copiar sus datos.

*Criterios de aceptación*
- `POST /catalog/ingest` con cuerpo vacío ingiere todo el catálogo; con `categories`/`limit`, la
  porción indicada (límite ≤ 10.000, categorías ≤ 50).
- Los metadatos guardados incluyen id 4x4, nombre, descripción, categoría, tags, columnas
  (`FieldName`, `DataType`) y `sourceUrl` ([`data/catalogo_datasets.md`](../../data/catalogo_datasets.md)).
- La ingesta usa exclusivamente la API oficial de Socrata (RF-01, RNF-04).

### US-02 · Categorías disponibles ✅
**Como** usuario, **quiero** ver las categorías temáticas del catálogo con su conteo **para**
decidir qué áreas cargar sin ingerir 8.000 datasets.

*Criterios*: `GET /catalog/categories` devuelve nombre + conteo por categoría (RF-02).

---

## Épica 2 — Búsqueda semántica (encontrar el dataset correcto)

### US-03 · Índice semántico ✅
**Como** operador, **quiero** vectorizar los metadatos ingeridos y construir el índice **para**
habilitar la búsqueda por significado (RAG).

*Criterios*
- `POST /search/index` genera embeddings de todos los datasets del catálogo y reporta cuántos indexó.
- Índice y consulta usan el mismo generador de embeddings (RF-03; [`data/rag_embeddings.md`](../../data/rag_embeddings.md)).

### US-04 · Búsqueda en lenguaje natural ✅
**Como** investigador, **quiero** buscar datasets escribiendo mi necesidad en lenguaje natural
**para** no depender de conocer palabras clave exactas del portal.

*Criterios*: `GET /search?q=…&top=n` devuelve los top-k con id, nombre, categoría, fuente y score (RF-04).

---

## Épica 3 — Conversación multiagente (preguntar y entender)

### US-05 · Preguntar en lenguaje natural ✅
**Como** ciudadano, **quiero** hacer preguntas en mi idioma y ver la respuesta aparecer en tiempo
real **para** consultar datos públicos sin conocimientos técnicos.

*Criterios*
- `POST /chat` responde por SSE: `agent` → contenido → `done` (RF-05).
- El mensaje se enruta al agente adecuado; si el enrutador LLM falla, degrada a reglas (RF-06).

### US-06 · Respuestas siempre citadas ✅
**Como** periodista, **quiero** que cada respuesta basada en datos incluya su fuente (dataset +
enlace a datos.gov.co) **para** poder verificar y publicar con evidencia.

*Criterios*
- Evento `sources` con las citas cuya relevancia recalculada supera el umbral (0.5).
- Si ningún dataset es relevante o los datos no soportan la respuesta, el Copilot lo declara;
  **nunca inventa cifras** (RF-07, RNF-03).

### US-07 · Recomendación de datasets ✅
**Como** emprendedor, **quiero** que el Copilot me recomiende los datasets útiles para mi
necesidad **para** descubrir oportunidades sin explorar el portal a mano.

*Criterios*: el [`dataset-recommender-agent`](../../models/dataset-recommender-agent.md) responde con
texto claro + citas re-rankeadas (RF-07).

### US-08 · Entender un dataset y sus cruces ✅
**Como** investigador, **quiero** que me expliquen las columnas de un dataset y si dos datasets
pueden cruzarse (municipio, año…) **para** planear un análisis sin descargar nada.

*Criterios*: el [`dataset-analyst-agent`](../../models/dataset-analyst-agent.md) explica esquemas y
evalúa correlaciones desde metadatos reales, citando los datasets usados (RF-10).

### US-09 · Cargar lo que falta con un clic ✅
**Como** usuario, **quiero** que si el catálogo cargado no cubre mi pregunta me sugieran qué
categorías cargar **para** ampliarlo y reintentar sin empezar de nuevo.

*Criterios*: evento `categories` con botones; al clic se ingiere la categoría, se reindexa y se
re-pregunta automáticamente (RF-09).

---

## Épica 4 — Cifras y artefactos (respuestas con datos reales)

### US-10 · Cifras con datos en vivo ✅
**Como** ciudadano, **quiero** preguntar "¿cuántos…?" o "¿cuál es el total de…?" y recibir la
cifra real **para** decidir con datos y no con impresiones.

*Criterios*
- El [`figures-agent`](../../models/figures-agent.md) genera SoQL, el backend lo ejecuta sobre la API
  oficial y la cifra proviene de esa ejecución (RF-08).
- Si la consulta no es posible o falla, el agente lo explica con honestidad (RNF-03).

### US-11 · Tablas y gráficos ✅
**Como** periodista, **quiero** ver el resultado tabulado y graficado **para** usarlo directamente
en mi trabajo.

*Criterios*: eventos `table` y `chart` (`bar`/`line`, ejes X/Y) con los datos incrustados; los
artefactos se redibujan al recuperar la conversación sin reconsultar la fuente (RF-08, RF-12).

---

## Épica 5 — Memoria y persistencia (no perder el trabajo)

### US-12 · Memoria de la conversación ✅
**Como** usuario, **quiero** que el Copilot recuerde mi objetivo y los datasets que fijé **para**
no repetir contexto en cada pregunta.

*Criterios*
- El objetivo se actualiza por turno (evento `objective`) y **es editable** (RF-11).
- Los datasets fijados se anteponen al contexto de analista y cifras.

### US-13 · Guardar y recuperar conversaciones ✅
**Como** usuario, **quiero** guardar una conversación (con sus artefactos y memoria) y recuperarla
después **para** continuar mi análisis en otro momento.

*Criterios*
- Guardado **manual** (`PUT /conversations/{id}`), listado por reciente (`GET /conversations`),
  recuperación completa (`GET /conversations/{id}`) (RF-12).
- Eliminar borra todo el rastro: transcripción, memoria, artefactos y auditoría
  (`DELETE /conversations/{id}`).

### US-14 · Auditoría de la IA ✅
**Como** equipo de desarrollo, **quiero** registrar las interacciones crudas de cada turno con
los agentes **para** trazar, depurar y mejorar prompts con uso real.

*Criterios*: evento `audit` por turno + `auditLog` persistido (RF-13, RNF-08;
[`data/conversaciones.md`](../../data/conversaciones.md)).

---

## Épica 6 — Backlog: evolución (roadmap)

Fases de la visión del equipo (presentación, [`resources/`](../../resources/)); se abordarán con su
propio ADR al concertarse.

### US-15 · Gestión de usuarios 🔜
**Como** administrador, **quiero** roles y permisos **para** personalizar la experiencia y
particionar los datos por usuario. *(Fase 1; prerequisito del multiusuario del ADR-0017.)*

### US-16 · Sesiones por usuario 🔜
**Como** usuario registrado, **quiero** mi historial y memoria conversacional asociados a mi
cuenta **para** continuar en cualquier dispositivo. *(Fase 2.)*

### US-17 · Gestión de tokens 🔜
**Como** operador, **quiero** cuotas, métricas de consumo y caché de contexto **para** proteger
los recursos y el costo del modelo. *(Fase 3.)*

### US-18 · Agente de autoaprendizaje 🔜
**Como** equipo, **quiero** un agente que analice la auditoría persistida **para** proponer
mejoras de prompts, nuevos agentes y optimizaciones del RAG. *(Fase 4 — ciclo autoevolutivo.)*
