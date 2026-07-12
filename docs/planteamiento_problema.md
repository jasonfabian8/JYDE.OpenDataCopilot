# Planteamiento del problema

> Fuentes: [`README.md`](../README.md), presentación del equipo
> ([`resources/`](../resources/)), [SAD §1](architecture/SAD.md#1-visión-y-contexto).

## El punto de partida: miles de datos públicos que casi nadie puede usar

Colombia publica en [datos.gov.co](https://www.datos.gov.co) más de **8.000 conjuntos de datos
abiertos** sobre salud, educación, seguridad, economía y medio ambiente. La información existe y es
valiosa, pero para la mayoría de las personas sigue siendo inaccesible en la práctica:

1. **Barrera técnica real.** Encontrar el dataset correcto exige conocer el portal, descargar CSV,
   entender estructuras de columnas y —para consultas serias— dominar APIs y SoQL.
2. **Valor público dormido.** La información existe, pero no llega ni a quien decide ni a quien
   pregunta: transparencia sin retorno.
3. **Fricción de descubrimiento.** Un buscador tradicional entrega archivos; el ciudadano necesita
   **respuestas**.

## ¿A quiénes afecta y por qué?

| Audiencia | Dolor |
|---|---|
| **Ciudadanos** | Deciden a diario sin poder verificar la información pública. |
| **Periodistas** | Verificar una cifra les toma días entre portales y CSV. |
| **Investigadores** | Gastan el tiempo buscando y limpiando datos, no analizándolos. |
| **Emprendedores** | Sin datos usables no detectan oportunidades ni mercados. |
| **Entidades públicas** | Publican datos que nadie usa: transparencia sin retorno. |

## Objetivo

Que cualquier persona consulte los datos abiertos de Colombia **con una pregunta en lenguaje
natural** y reciba una respuesta **clara, verificable y siempre citando su fuente**.

## Alcance de la solución

**OpenData Copilot** es un asistente conversacional (copiloto multiagente con RAG) que:

- Descubre e indexa los metadatos del catálogo de datos.gov.co (por categorías, bajo demanda).
- Comprende preguntas en lenguaje natural y las enruta al agente especializado adecuado.
- Consulta datos reales en vivo vía SoQL sobre la API oficial de Socrata.
- Responde citando la fuente (dataset + enlace); **si los datos no soportan la respuesta, lo
  declara — nunca inventa cifras**.
- Genera artefactos (tablas y gráficos) y conserva la memoria de la conversación.

### Restricciones de contexto

- **Costo como restricción dura**: recursos propios del equipo; adaptadores locales gratuitos en
  desarrollo, capas gratuitas y modelos económicos en producción (ver
  [SAD §2](architecture/SAD.md#2-drivers-y-atributos-de-calidad)).
- **Solo API oficial** de Socrata, sin web scraping ([ADR-0002](adr/0002-socrata-sin-scraping.md)).
- **Time-to-market**: demo funcional dentro del calendario del concurso Datos al Ecosistema 2026.
