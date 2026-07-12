# Conclusiones

> Fuentes: presentación del equipo ([`resources/`](../resources/), sección "Resultados y demo"),
> código y pruebas del repositorio, [`Changelog.md`](../Changelog.md),
> [`tech-debt.md`](tech-debt.md).

## 1. Hallazgos: el prototipo ya funciona

Las validaciones completadas de extremo a extremo:

| Validación | Evidencia |
|---|---|
| **Ingesta real del catálogo** | Metadatos leídos desde la API Socrata de datos.gov.co, completos o por categorías (`POST /catalog/ingest`). |
| **Búsqueda semántica operativa** | Embeddings + índice vectorial en Atlas Vector Search; consulta en lenguaje natural devuelve los datasets relevantes (`GET /search`). |
| **El chat responde citando la fuente** | Sistema multiagente + streaming SSE de extremo a extremo; el evento `sources` acompaña cada respuesta basada en datos. |
| **Cifras con datos reales** | El agente de cifras genera SoQL, lo ejecuta en vivo sobre la fuente oficial y produce tablas y gráficos. |
| **Calidad verificada en CI** | Build con `TreatWarningsAsErrors` + umbral de cobertura ≥ 95 % por proyecto + análisis SonarCloud bloquean cualquier regresión. |

**Métricas del proyecto** (corte de la presentación, 2026-07-11): 121 tests · ≥ 95 % de
cobertura · 17 ADRs · 4 bounded contexts · bajo costo (capas gratuitas y modelos económicos).

## 2. Lo que aprendimos construyéndolo

- **La arquitectura multiagente compensa.** Especializar agentes pequeños (enrutar, recomendar,
  analizar, cifrar) redujo errores y tokens frente a un prompt monolítico, y agregar una
  capacidad nueva no toca el núcleo ([ADR-0015](adr/0015-arquitectura-multiagente.md)); la
  evolución de versiones de cada agente quedó trazada en el [`Changelog`](../Changelog.md).
- **Los guardrails deben ser código, no promesas.** El re-ranking con umbral antes de citar, el
  parseo defensivo del JSON y la degradación explícita convirtieron "no alucinar" en un
  comportamiento verificable por pruebas.
- **El costo se diseña.** Puertos + adaptadores locales ($0) + modelos mini + dimensión de
  embeddings reducida (256) mantuvieron el desarrollo en costo cercano a cero sin bloquear la
  calidad del camino de producción.
- **El gobierno documental escala al equipo.** SAD + ADRs + instrucciones por capa permitieron
  trabajar con asistentes de IA sin degradar el diseño (los límites de capa se mantienen).

## 3. Limitaciones actuales (honestas)

- **Sin autenticación/multiusuario**: las conversaciones no están particionadas por usuario
  (alcance de concurso; seguimiento en [ADR-0017](adr/0017-persistencia-conversaciones.md)).
- **Calidad dependiente del catálogo cargado**: si una categoría no se ha ingerido, el sistema lo
  detecta y sugiere cargarla, pero no responde hasta entonces (decisión de honestidad).
- **Heterogeneidad de esquemas**: SoQL sobre datasets arbitrarios puede fallar; el agente lo
  declara y pide reformular ([SAD §10](architecture/SAD.md#10-riesgos-y-mitigaciones)).
- **Índice vectorial en tier gratuito**: latencia y límites del M0 de Atlas; el índice tarda en
  quedar consultable tras crearse ([ADR-0014](adr/0014-atlas-vector-search.md)).
- Deudas técnicas registradas con su condición de disparo en [`tech-debt.md`](tech-debt.md).

## 4. Próximos pasos (roadmap)

Visión: de asistente inteligente a **plataforma agéntica autoevolutiva** — aprende del uso,
optimiza su desempeño y escala de forma segura para un mayor impacto nacional. Fases (detalle en
[`requirements/user_stories.md §Backlog`](requirements/user_stories.md#épica-6--backlog-evolución-roadmap)):

1. **Gestión de usuarios** — roles, permisos y experiencia personalizada.
2. **Gestión de sesiones** — historial y memoria conversacional por usuario.
3. **Gestión de tokens** — cuotas, métricas de consumo y caché de contexto.
4. **Agente de autoaprendizaje** — analiza la auditoría persistida para proponer insights,
   mejores prompts y nuevos agentes (ciclo autoevolutivo, habilitado por la auditoría del
   [ADR-0017](adr/0017-persistencia-conversaciones.md)).

## 5. Conclusión general

Los datos abiertos de Colombia ya existen; el problema era la distancia entre el dato y la
persona. OpenData Copilot demuestra —con un prototipo funcional, citado y auditado— que esa
distancia se puede cerrar con una pregunta: **convierte la transparencia en conversación**,
con ingeniería sólida (hexagonal + DDD, TDD, CI), IA responsable (sin fuente no hay respuesta) y
un costo estructuralmente bajo que hace viable escalarla a nivel nacional.
