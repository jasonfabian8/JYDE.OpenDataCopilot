# ADR 0005 — Estrategia de datos híbrida (metadatos amplios + cache selectivo)

- **Estado:** Aceptado
- **Fecha:** 2026-06-18
- **Decisores:** Equipo OpenData Copilot

## Contexto

El catálogo de `datos.gov.co` supera los 8.000 datasets y presenta una **asimetría técnica** entre dos necesidades de la solución:

- **Descubrimiento:** indexar metadatos es homogéneo, de bajo costo y escala de forma lineal, habilitando *recall* amplio de búsqueda sobre todo el catálogo.
- **Respuesta con datos exactos:** consultar datasets arbitrarios es difícil por la **heterogeneidad de esquemas**, la variabilidad en la calidad de los datos y la **latencia/límites** de la API por consulta.

Optimizar una sola vía penaliza la otra: priorizar amplitud degrada la precisión y la latencia de las respuestas; materializar todos los datos para maximizar precisión implica una sobrecarga de infraestructura y mantenimiento desproporcionada.

## Decisión

Adoptar una estrategia **híbrida**:

1. **Amplitud:** indexar metadatos (todo el catálogo o filtrado por las 5 áreas) para búsqueda y descubrimiento.
2. **Profundidad:** consultar datos on-demand vía SoQL y **cachear selectivamente** los datasets más usados/curados para velocidad, demo offline y respuestas con visualización.
3. **Honestidad:** si los datos no soportan la respuesta, el sistema lo declara; nunca inventa.

## Consecuencias

- **Positivas:** cobertura amplia a bajo costo + respuestas precisas donde importa; buen demo.
- **Negativas / trade-offs:** mantener dos caminos (live vs. cache) y un set curado.
- **Seguimiento:** definir el set curado por área; decidir el alcance de indexación inicial según el costo/desempeño de embeddings (ver [ADR-0004](0004-azure-foundry-gpt41mini.md)).

## Alternativas consideradas

- **Sólo consulta en vivo** — más simple, pero mayor latencia por respuesta y dependencia de la disponibilidad y los límites de la API en cada consulta.
- **ETL completo a un warehouse (p. ej. BigQuery)** — descartado: sobrecarga operativa e infraestructura desproporcionadas frente al beneficio, e introduce una segunda nube.
