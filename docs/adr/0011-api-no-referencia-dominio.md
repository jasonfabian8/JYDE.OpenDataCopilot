# ADR 0011 — La capa API no referencia el Domain; consume DTOs de Application

- **Estado:** Aceptado
- **Fecha:** 2026-06-19
- **Decisores:** Equipo OpenData Copilot

## Contexto

En la arquitectura hexagonal, la API es un **adaptador conductor** (driving). Debe invocar **casos de uso de Application** (puertos de entrada) y no conocer el modelo de dominio ni invocar los puertos de salida (p. ej. repositorios) directamente. No se pude permitir que un controlador referencie el dominio. Ejemplo de una mala práctica a evitar: el controlador del catálogo referenciaba el dominio (`using ...Domain`, construía `DatasetId`, recibía la entidad `Dataset`) y llamaba al repositorio directamente, con el mapeo dominio→DTO ubicado en la capa de presentación.
Esto acopla la presentación al dominio y dispersa responsabilidades.

## Decisión

- **La capa API no referencia el `Domain`.** Sus controladores dependen únicamente de **casos de uso de `Application`** y de **DTOs de `Application`** (o de sus propios modelos de *request* HTTP).
- El **mapeo dominio → DTO** vive en `Application`. Los casos de uso de lectura exponen DTOs (p. ej. `DatasetDto`), nunca entidades de dominio.
- Los controladores **no invocan puertos de salida** (repositorios, etc.) directamente; lo hacen a  través de los casos de uso.
- La validación que requiere conocimiento del dominio (p. ej. formato de `DatasetId`) ocurre en  `Application`; el controlador traduce el error a HTTP (400).

## Consecuencias

- **Positivas:** límites de capa nítidos; el contrato HTTP no expone el modelo de dominio; cambios en el dominio no rompen la API; controladores delgados y testeables; mapeo centralizado.
- **Negativas / trade-offs:** algunos DTOs y casos de uso de lectura adicionales; se acepta a cambio de un acoplamiento correcto.
- **Seguimiento:** aplicar el mismo patrón a los siguientes bounded contexts (Search, Conversation).

## Alternativas consideradas

- **Mapear dominio→DTO en la API** y/o llamar repositorios desde el controlador — más directo, pero acopla la presentación al dominio y rompe la dirección de dependencias. Descartado.
- **Exponer entidades de dominio en el contrato HTTP** — descartado: filtra el modelo interno y fragiliza el contrato público.
