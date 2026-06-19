# ADR 0004 — Modelos vía Azure AI Foundry (GPT-4.1-mini inicial)

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

La solución requiere dos capacidades de IA: un **LLM** para generar consultas (SoQL) y redactar respuestas citadas, y un modelo de **embeddings** para la búsqueda semántica. Criterios técnicos de la decisión:

- **Superficie de integración única** que dé acceso a múltiples modelos y permita cambiarlos detrás de los puertos `IChatCompletion`/`IEmbeddingGenerator` sin modificar la aplicación.
- **Calidad** suficiente en generación de SoQL y en síntesis con citación.
- **Latencia** adecuada para una experiencia conversacional (con streaming).
- **Relación costo/desempeño** óptima por capacidad, ajustable según evolucione la calidad requerida.
- **Gobernanza y observabilidad** centralizadas del consumo de modelos.

Azure AI Foundry expone múltiples modelos tras una misma integración y mantiene estas capacidades dentro de la misma nube que el resto de la solución.

## Decisión

- Consumir modelos a través de **Azure AI Foundry**.
- **Chat:** iniciar con **GPT-4.1-mini** por su relación costo/desempeño; el puerto `IChatCompletion` permite cambiar de modelo sin tocar la aplicación.
- **Embeddings:** modelo económico vía Foundry (p. ej. `text-embedding-3-small`), por confirmar.

## Consecuencias

- **Positivas:** un solo punto de integración; flexibilidad de modelo; costo controlado.
- **Negativas / trade-offs:** GPT-4.1-mini puede requerir mejor prompt engineering para SoQL
  complejo → se compensa con prompts especializados y validación de SoQL.
- **Seguimiento:** confirmar el modelo de embeddings y medir costo/calidad; escalar de modelo si la solución lo justifica.

## Alternativas consideradas

- **Gemini / Google Vertex AI** — implicaría operar en una segunda nube (GCP) además de Azure, fragmentando despliegue, facturación y gobernanza; el beneficio no compensa esa complejidad.
- **OpenAI directo** — viable, pero Foundry centraliza acceso a múltiples modelos, gobernanza y facturación dentro de una sola nube, alineado con el resto de la solución.
