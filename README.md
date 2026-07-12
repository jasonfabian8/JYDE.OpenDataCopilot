# 🚀 OpenData Copilot

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=jasonfabian8_JYDE.OpenDataCopilot&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=jasonfabian8_JYDE.OpenDataCopilot)

## Conversa con los datos abiertos de Colombia

¿Qué pasaría si cualquier ciudadano pudiera hacer preguntas sobre información pública de la misma forma en que conversa con un asistente de IA?

**OpenData Copilot** transforma miles de conjuntos de datos publicados en datos.gov.co en respuestas claras, útiles y fáciles de entender.

Ya no es necesario buscar archivos, descargar hojas de cálculo o interpretar estructuras complejas. Solo pregunta.

![Landing de OpenData Copilot: titular editorial "Pregúntale a los datos abiertos de Colombia" junto a una consola que muestra el flujo pregunta → consulta SoQL → respuesta citando la fuente.](docs/images/landing-preview.png)

--------------------------

## 💡 El problema

Colombia cuenta con miles de conjuntos de datos abiertos que contienen información valiosa sobre salud, educación, seguridad, economía, medio ambiente y muchos otros temas.

Sin embargo, para la mayoría de los ciudadanos estos datos siguen siendo difíciles de encontrar, comprender y utilizar.

---

## ✅ Nuestra solución

OpenData Copilot actúa como un puente entre los ciudadanos y los datos abiertos.

La plataforma:

* Descubre e indexa automáticamente los conjuntos de datos publicados.
* Comprende preguntas en lenguaje natural.
* Identifica las fuentes más relevantes.
* Analiza la información disponible.
* Genera respuestas claras y fáciles de interpretar.
* Muestra la fuente de los datos para garantizar transparencia.

---

## ⚙️ Funcionalidades

Lo que OpenData Copilot hace, de la pregunta a la respuesta citada:

* **Descubrimiento e ingesta por categorías.** Ingiere los metadatos del catálogo de datos.gov.co
  desde la API oficial de Socrata —completo o filtrado por las áreas que elija el usuario— sin
  copiar los datos ni hacer scraping.
* **Búsqueda semántica (RAG).** Vectoriza los metadatos e indexa los datasets para encontrarlos
  por significado, no por palabras clave exactas.
* **Copiloto multiagente.** Seis agentes de IA especializados colaboran en cada conversación: un
  **enrutador** decide quién atiende, y los agentes de **recomendación**, **análisis de esquemas**,
  **cifras** y **categorías** resuelven la necesidad concreta.
* **Respuestas con datos reales.** El agente de cifras genera consultas **SoQL**, las ejecuta en
  vivo sobre la fuente oficial y devuelve **tablas y gráficos** — nunca cifras inventadas.
* **Respuesta citada, siempre.** Toda respuesta basada en datos muestra su fuente (dataset +
  enlace); si los datos no la soportan, el sistema lo declara. *Sin fuente no hay respuesta.*
* **Conversación en tiempo real.** La respuesta se transmite token a token (streaming SSE) para
  una experiencia fluida.
* **Memoria conversacional.** Recuerda el objetivo de la conversación (editable) y los datasets
  que el usuario fija, para no perder el hilo.
* **Persistencia y auditoría.** Guarda, recupera y elimina conversaciones completas —transcripción,
  memoria, artefactos y bitácora de auditoría de cada agente— para trazabilidad y continuidad.
* **Proveedores intercambiables por configuración.** Cambiar de LLM, base de datos o índice es un
  cambio de configuración: sin tocar el núcleo y con opciones locales gratuitas para desarrollo.

> Detalle funcional: [requerimientos](docs/requirements/requerimientos_iniciales.md) ·
> [historias de usuario](docs/requirements/user_stories.md) · [API](docs/api_spec.md) ·
> [agentes](models/README.md).

---

## 🗣️ Ejemplos de preguntas

* ¿Cuáles son los municipios con mayor índice de desempleo?
* ¿Cómo ha evolucionado la accidentalidad vial en los últimos años?
* ¿Qué departamentos tienen mayor cobertura de internet?
* ¿Cuántas instituciones educativas existen en mi ciudad?
* ¿Cómo se distribuye el presupuesto público en determinada región?

---

## 🌎 Impacto esperado

Queremos que los datos abiertos dejen de ser archivos difíciles de interpretar y se conviertan en información accesible para todos.

Con OpenData Copilot buscamos:

* Fortalecer la transparencia.
* Promover decisiones informadas.
* Facilitar la investigación y el periodismo de datos.
* Acercar el gobierno abierto a la ciudadanía.
* Democratizar el acceso a la información pública.

---

## 🎯 Nuestra visión

Imaginamos un país donde cualquier persona pueda acceder al conocimiento contenido en los datos públicos simplemente haciendo una pregunta.

**Porque los datos abiertos solo generan valor cuando las personas pueden entenderlos y utilizarlos.**

---

## 🧪 Resultados validados

El prototipo ya funciona de extremo a extremo:

* ✔ **Ingesta real del catálogo** — metadatos leídos desde la API Socrata de datos.gov.co.
* ✔ **Búsqueda semántica operativa** — embeddings + índice vectorial (Atlas Vector Search).
* ✔ **Chat que responde citando la fuente** — multiagente + streaming SSE de extremo a extremo.
* ✔ **Calidad verificada en CI** — build y umbral de cobertura bloquean cualquier regresión.

| 121 tests | ≥ 95 % cobertura | 17 ADRs | 4 bounded contexts | Bajo costo |
|:---:|:---:|:---:|:---:|:---:|

---

## 🏗️ Arquitectura y stack

Arquitectura **hexagonal + DDD**: dependencias sólo hacia adentro
(`Api → Infrastructure → Application → Domain`), con toda dependencia externa detrás de un puerto
intercambiable por configuración.

| Capa | Tecnologías |
|------|-------------|
| **Backend** | .NET 10 · ASP.NET Core (controladores MVC) · xUnit + Shouldly |
| **Frontend** | React + Vite · Zustand · Tailwind CSS · Vitest |
| **Infraestructura** | MongoDB Atlas + Atlas Vector Search · Azure AI Foundry (LLM + embeddings) · Docker |

> Fuente de verdad de la arquitectura: [SAD](docs/architecture/SAD.md) ·
> [diagramas](docs/architecture/diagramas.md) · [decisiones (ADR)](docs/adr/README.md).

---

## 🚀 Puesta en marcha

Todo el flujo funciona **sin credenciales y a costo cero** con los adaptadores locales por defecto.

```bash
dotnet build                                        # compilar la solución
dotnet test                                         # ejecutar las pruebas
dotnet run --project src/JYDE.OpenDataCopilot.Api   # levantar la API
cd web && npm install && npm run dev                # levantar el frontend
```

Guía detallada de validación paso a paso: [docs/validacion_guide.md](docs/validacion_guide.md).

---

## 📚 Documentación

| Documento | Contenido |
|-----------|-----------|
| [Estructura del repositorio](docs/estructura_repositorio.md) | Guía de navegación y mapeo con la estructura del concurso |
| [Planteamiento del problema](docs/planteamiento_problema.md) | Problema, audiencias y objetivo |
| [Marco metodológico](docs/marco_metodologico.md) | Metodología del equipo (hexagonal + DDD + TDD + CI) |
| [Fuentes de datos](docs/fuentes_datos.md) | datos.gov.co vía API Socrata; estrategia híbrida |
| [Estructuras de datos](data/README.md) | Catálogo, embeddings (RAG) y conversaciones |
| [Agentes de IA](models/README.md) | Los 6 agentes: modelo, versión y rol |
| [Especificación de la API](docs/api_spec.md) | Endpoints REST + eventos SSE |
| [Requerimientos](docs/requirements/requerimientos_iniciales.md) e [historias de usuario](docs/requirements/user_stories.md) | Alcance funcional y no funcional |
| [Evaluación de impacto](docs/public_impact_assessment.md) | Impacto público, ética y sesgos |
| [Conclusiones](docs/conclusiones.md) · [Changelog](Changelog.md) | Hallazgos, roadmap e historial de versiones |

---

## 👥 Equipo

Proyecto **ID 241** · Nivel Avanzado · **Datos al Ecosistema 2026 (MinTIC)**.

Yehison Fabián Becerra · Edwin Giovanni Villamizar Aldana · Yenny Alarcón · Helen Daniela Benítez Hipólito.

Código abierto bajo licencia [MIT](LICENSE).
