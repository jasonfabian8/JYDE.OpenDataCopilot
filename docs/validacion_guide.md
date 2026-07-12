# Guía de validación

Guía reproducible para que pares y evaluadores validen el proyecto de extremo a extremo. Todo el
flujo funciona **sin credenciales y a costo cero** con los adaptadores locales por defecto
(`Providers` en `appsettings.json`: chat `Fake`, embeddings `Local`, índice y repositorio
`InMemory`); con credenciales de Azure AI Foundry y MongoDB Atlas se valida el camino de
producción.

> Equivalente al `validación_guide.md` de la guía del concurso (nombre sin tilde para evitar
> problemas de encoding). Mapa completo del repo: [`estructura_repositorio.md`](estructura_repositorio.md).

## 1. Prerrequisitos

| Herramienta | Uso |
|---|---|
| SDK de .NET 10 | backend (`src/`, `tests/`) |
| Node.js 20+ (npm) | frontend (`web/`) |
| Docker (opcional) | dependencias locales (`docker-compose.yml`) |

## 2. Compilar

```bash
dotnet build          # la solución compila con TreatWarningsAsErrors: debe salir limpia
```

## 3. Ejecutar las pruebas (backend)

```bash
dotnet test                              # todas las pruebas por capa
dotnet test /p:CollectCoverage=true      # pruebas + umbral de cobertura >= 95% por proyecto
```

Criterio de aceptación: **todas las pruebas pasan** y ningún proyecto baja del **95 %** de
cobertura (líneas/ramas/métodos) — el mismo umbral que bloquea el CI
(`.github/workflows/sonarcloud.yml`).

## 4. Ejecutar las pruebas (frontend)

```bash
cd web
npm install
npm test               # Vitest + React Testing Library
npm run test:coverage  # cobertura (LCOV, se sube a SonarCloud en CI)
```

## 5. Levantar la solución

```bash
# (opcional) dependencias locales si se quieren probar adaptadores Docker
docker compose up -d

# API (por defecto usa adaptadores locales: sin credenciales ni costo)
dotnet run --project src/JYDE.OpenDataCopilot.Api

# Frontend
cd web && npm run dev
```

## 6. Validar el flujo funcional (API directa)

Con la API corriendo (los ejemplos usan `curl`):

```bash
# 1) La API responde
curl http://localhost:5000/

# 2) Ver categorías disponibles del catálogo real de datos.gov.co
curl http://localhost:5000/catalog/categories

# 3) Ingerir una muestra del catálogo (metadatos reales vía API Socrata)
curl -X POST http://localhost:5000/catalog/ingest \
  -H "Content-Type: application/json" \
  -d '{"limit": 200}'

# 4) Construir el índice de búsqueda
curl -X POST http://localhost:5000/search/index

# 5) Búsqueda semántica
curl "http://localhost:5000/search?q=educacion%20municipios&top=5"

# 6) Conversar con el Copilot (respuesta en streaming SSE)
curl -N -X POST http://localhost:5000/chat \
  -H "Content-Type: application/json" \
  -d '{"question": "¿Qué datasets hay sobre desempleo?"}'
```

> El puerto puede variar según `launchSettings.json`; usa el que muestre `dotnet run`.

Detalle de endpoints y eventos SSE: [`api_spec.md`](api_spec.md).

## 7. Validar desde la interfaz web

Preguntas de ejemplo (las mismas del [README](../README.md)):

- ¿Cuáles son los municipios con mayor índice de desempleo?
- ¿Cómo ha evolucionado la accidentalidad vial en los últimos años?
- ¿Qué departamentos tienen mayor cobertura de internet?

Qué verificar:

1. **Respuesta citada**: toda respuesta basada en datos muestra su fuente (dataset + enlace a
   datos.gov.co). Si no hay soporte en los datos, el Copilot lo declara — no inventa cifras.
2. **Multiagente**: el evento `agent` indica qué agente atendió (recomendador, analista, cifras,
   categorías); ver [`models/README.md`](../models/README.md).
3. **Artefactos**: preguntas de cifras ("¿cuántos…?", "grafica…") generan tabla y/o gráfico con
   datos reales consultados vía SoQL.
4. **Memoria**: el objetivo de la conversación se actualiza por turno y es editable; los datasets
   fijados se conservan.
5. **Persistencia**: guardar, listar, recuperar y eliminar conversaciones
   (`/conversations`, [ADR-0017](adr/0017-persistencia-conversaciones.md)).

## 8. Validar la calidad continua

- CI en GitHub Actions: build + tests + cobertura + análisis SonarCloud en cada push/PR.
- Estado público del quality gate: badge de SonarCloud en el [README](../README.md).
