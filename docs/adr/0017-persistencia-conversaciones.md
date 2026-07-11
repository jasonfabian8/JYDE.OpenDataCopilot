# ADR 0017 — Persistencia de conversaciones (transcripción + memoria + artefactos + auditoría)

- **Estado:** Aceptado
- **Fecha:** 2026-07-11
- **Decisores:** Equipo OpenData Copilot

## Contexto

El Copilot mantiene, por conversación, una **transcripción** (mensajes), **memoria** (objetivo +
datasets fijados), **artefactos** (tablas/gráficos) y **auditoría** (interacciones crudas con los
agentes). Hasta ahora todo esto vivía solo en el estado del navegador y se perdía al refrescar o al
cambiar de dispositivo. Se necesita **persistirlo en BD** para no perder el trabajo y poder
**eliminar** una conversación por completo (incluida su memoria/artefactos/auditoría).

Restricciones: costo cero por defecto (local), reutilizar la infraestructura ya adoptada
(MongoDB Atlas, [ADR-0012](0012-persistencia-mongodb-atlas.md)) y respetar la regla de puertos y
adaptadores intercambiables por configuración ([ADR-0003](0003-ports-adapters-intercambiables.md)).

## Decisión

- Definir el puerto **`IConversationStore`** en **Application** (Save/Get/List/Delete) sobre un DTO
  `ConversationRecord` (con `ConversationMessageRecord`, `ConversationArtifactRecord`,
  `ConversationAuditEntryRecord`, `SelectedDataset`) y un `ConversationSummary` para listar. El caso
  de uso `ConversationArchiveService` orquesta el puerto y **sella la fecha de actualización** en el
  servidor (ordena la lista por reciente).
- Dos adaptadores en **Infrastructure**, **seleccionables por configuración**
  (`Providers:ConversationStore = InMemory | Mongo`):
  - `InMemoryConversationStore` (por defecto, **$0**): diccionario en memoria; dev/pruebas.
  - `MongoConversationStore` (producción): colección `conversations` en Atlas, reutilizando el
    `MongoContext` y el `MongoDB.Driver` ya adoptados (sin dependencia nueva).
- **Formato de almacenamiento en Mongo:** documento con campos consultables (`_id`, `title`,
  `updatedAtUtc`) + la conversación completa como **JSON** (`payload`). Evita mapear en BSON
  estructuras inmutables anidadas y con variantes (tabla/gráfico); el listado proyecta sin abrir el
  payload. El mapeo (ida/vuelta) se prueba en `ConversationDocument`.
- **Exposición HTTP** en `ConversationsController`: `GET /conversations` (lista),
  `GET /conversations/{id}` (completa), `PUT /conversations/{id}` (upsert), `DELETE /conversations/{id}`.
- **Guardado manual** (decisión de producto): el guardado lo dispara el usuario; el front carga la
  lista al abrir, trae el contenido al seleccionar, guarda bajo demanda y elimina por acción explícita.

## Consecuencias

- **Positivas:** las conversaciones (con su memoria/artefactos/auditoría) persisten y son
  compartibles; eliminar borra todo el rastro; sin dependencia nueva (reutiliza Atlas + driver);
  adaptador intercambiable sin tocar dominio/aplicación; local sigue a costo cero.
- **Negativas / trade-offs:** el `payload` JSON **no es consultable** dentro de Mongo (no se
  necesita: solo se lista por id/título/fecha y se recupera completo). El guardado manual puede
  perder cambios no guardados (aceptado por el usuario; el estado por conversación vive en el front
  hasta guardar). La API no aplica autenticación aún (alcance de concurso; ver seguimiento).
- **Seguimiento:** si se requiere multiusuario, añadir identidad y particionar por usuario; evaluar
  autoguardado con *debounce* si el guardado manual resulta incómodo.

## Alternativas consideradas

- **LocalStorage del navegador** — $0 e inmediato, pero no es BD compartida ni multi-dispositivo;
  descartado por el requisito explícito de "en BD".
- **Mapear la conversación a BSON tipado** (sin payload JSON) — consultable dentro del documento,
  pero frágil con records inmutables anidados y variantes (tabla/gráfico) y sin necesidad real de
  consultar el interior; se prefirió el payload JSON con campos de cabecera consultables.
- **Guardado automático por turno** — menos fricción, pero el usuario pidió control explícito del
  guardado; se deja como posible mejora (seguimiento).
