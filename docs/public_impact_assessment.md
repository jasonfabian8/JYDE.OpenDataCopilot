# Evaluación de impacto público, ética y mitigación de sesgos

> Fuentes: presentación del equipo ([`resources/`](../resources/)), [SAD §1–2 y
> §10](architecture/SAD.md), [ADR-0002](adr/0002-socrata-sin-scraping.md),
> [ADR-0015](adr/0015-arquitectura-multiagente.md), [ADR-0017](adr/0017-persistencia-conversaciones.md).
> Valores del concurso Datos al Ecosistema 2026: uso responsable/ético de la IA, impacto social,
> escalabilidad.

## 1. Impacto esperado: del dato abierto a la decisión informada

| Audiencia | Impacto |
|---|---|
| **Ciudadanía** | 50M+ de colombianos pueden preguntar sobre información pública sin saber de datos, portales ni APIs. |
| **Periodismo e investigación** | Evidencia verificable y citada en segundos, en lugar de días entre portales y CSV. |
| **Entidades públicas** | Sus datos por fin se usan: transparencia con retorno. |
| **Territorio** | 32 departamentos y 1.100+ municipios con datos consultables en lenguaje natural. |

**Decisiones que habilita**: salud, educación, seguridad, presupuesto, ambiente y conectividad —
las áreas temáticas del catálogo de datos.gov.co que el usuario puede cargar y consultar.

## 2. IA responsable: principios operativos (no declarativos)

Los principios éticos del proyecto están **implementados en el código y verificados por pruebas**,
no solo enunciados:

| Principio | Implementación verificable |
|---|---|
| **Sin fuente no hay respuesta** | Toda respuesta basada en datos emite el evento `sources` con el dataset y su URL pública; los agentes sólo citan candidatos cuya relevancia recalculada supera el umbral (0.5). |
| **Nunca inventar cifras** | Los números provienen exclusivamente de ejecutar SoQL sobre la fuente oficial en el momento de la pregunta ([`models/figures-agent.md`](../models/figures-agent.md)); si la consulta falla, el agente lo declara con honestidad. |
| **Honestidad ante la incertidumbre** | Si los datos no soportan la respuesta o el catálogo no cubre la necesidad, el sistema lo dice y sugiere qué cargar — guardrail de "no sé" ([SAD §10](architecture/SAD.md#10-riesgos-y-mitigaciones)). |
| **Trazabilidad total** | Cada turno registra las interacciones crudas con cada agente (input/output) en una bitácora de auditoría persistible ([ADR-0017](adr/0017-persistencia-conversaciones.md)). |
| **Datos legítimos** | Única fuente: la API oficial de Socrata de datos.gov.co; **cero scraping** ([ADR-0002](adr/0002-socrata-sin-scraping.md)) — términos de uso respetados y procedencia trazable. |
| **Control del usuario** | La memoria (objetivo) es visible y **editable**; el guardado de conversaciones es manual y eliminar **borra todo el rastro** (transcripción, memoria, artefactos y auditoría). |

## 3. Sesgos: análisis y mitigaciones

| Riesgo de sesgo | Descripción | Mitigación actual |
|---|---|---|
| **Sesgo de recuperación** | El índice vectorial podría privilegiar datasets "cercanos" por embedding aunque no respondan la pregunta, sobre-representando ciertos temas. | Re-ranking por el LLM con relevancia recalculada por candidato + umbral: lo que no viene al caso **no se cita** ([ADR-0015](adr/0015-arquitectura-multiagente.md)). |
| **Sesgo de cobertura** | El catálogo cargado (por categorías) determina qué se puede responder; una carga parcial sesga las respuestas hacia lo cargado. | El `category-recommender-agent` detecta la brecha y **propone explícitamente** qué categorías faltan, en lugar de responder con lo que hay. |
| **Sesgo de la fuente** | Los datos abiertos mismos pueden tener calidad y cobertura desiguales entre territorios. | La cita obligatoria permite a cualquier usuario **auditar la fuente original**; el sistema no agrega ni corrige datos por su cuenta (sin cocina de cifras). |
| **Alucinación del LLM** | El modelo podría generar afirmaciones no sustentadas. | RAG estricto (el LLM sólo ve datasets recuperados del índice), respuestas estructuradas (JSON) con parseo defensivo y degradación explícita ([SAD §10](architecture/SAD.md#10-riesgos-y-mitigaciones)). |

**Pendiente (evolución):** pruebas automatizadas de equidad territorial (el equivalente a
`bias_tests/` de la guía del concurso) cuando existan modelos de decisión propios; hoy el sistema
no clasifica ni puntúa personas o territorios — solo recupera, consulta y cita datos publicados.

## 4. Privacidad y seguridad de los datos

- El sistema consume **datos ya públicos** publicados por el Estado; no recolecta datos
  personales de los datasets ni los cruza para reidentificación.
- Las conversaciones se persisten **solo por decisión del usuario** (guardado manual) y pueden
  eliminarse por completo ([ADR-0017](adr/0017-persistencia-conversaciones.md)).
- Secretos (claves de Foundry, cadena de conexión de Atlas) **fuera del repositorio** — se
  inyectan por configuración de entorno.
- Prácticas DevSecOps del pipeline: ver la tabla de capacidades (SAST/SCA) en
  [`marco_metodologico.md §Seguridad`](marco_metodologico.md#seguridad-devsecops).
- Autenticación/multiusuario: fuera del alcance del concurso, registrado como seguimiento del
  [ADR-0017](adr/0017-persistencia-conversaciones.md).

## 5. Sostenibilidad y escalabilidad del impacto

- **Bajo costo estructural**: modelos económicos (gpt-4o-mini/gpt-4.1-mini, embeddings small),
  capas gratuitas (Atlas M0) y adaptadores locales a $0 — el impacto no depende de un presupuesto
  grande ([SAD §2](architecture/SAD.md#2-drivers-y-atributos-de-calidad)).
- **Sin lock-in**: proveedores intercambiables por configuración
  ([ADR-0003](adr/0003-ports-adapters-intercambiables.md)) — el proyecto puede migrar de nube,
  LLM o base de datos sin reescritura.
- **Escalable a nivel nacional**: backend stateless + servicios gestionados; una capacidad nueva
  = un agente nuevo, sin reescribir el núcleo.
- **Abierto y auditable**: código MIT en GitHub con la arquitectura documentada — cualquiera
  puede verificar hoy cómo se construyen las respuestas.
