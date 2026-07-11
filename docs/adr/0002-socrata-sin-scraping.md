# ADR 0002 — Ingesta vía API de Socrata (sin web scraping)

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

Necesitamos ingerir tanto los metadatos del catálogo (para descubrimiento) como los datos de cada dataset (para responder). `datos.gov.co` corre sobre la plataforma **Socrata**, que expone una **API de catálogo** (`/api/catalog/v1`) con metadatos de todos los datasets (nombre, descripción,
columnas, tipos, categoría, tags) y una **API de datos por dataset** consultable con **SoQL** (SQL sobre HTTP) y descargable como CSV/JSON.

## Decisión

Obtener tanto los metadatos (descubrimiento) como los datos (respuestas) **exclusivamente a través de las APIs oficiales de Socrata**. No se implementa web scraping.

## Consecuencias

- **Positivas:** mucho más rápido y robusto; sin fragilidad ante cambios de HTML; datos estructurados directos; menos componentes que operar (no Airflow/Selenium).
- **Negativas / trade-offs:** dependemos de la disponibilidad y los límites de la API de Socrata → mitigado con reintentos, timeouts y cache selectivo.
- **Seguimiento:** considerar un App Token de Socrata si aparecen límites de tasa.

## Alternativas consideradas

- **Web scraping (Selenium/Puppeteer)** — descartado: innecesario, frágil y lento de mantener.
