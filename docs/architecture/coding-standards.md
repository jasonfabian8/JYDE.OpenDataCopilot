# Estándar de Codificación — OpenData Copilot

> **Fuente única de verdad** de prácticas de código. `CLAUDE.md` y
> `.github/copilot-instructions.md` referencian este documento; no se duplican las reglas.
> Complementa al [SAD](SAD.md) (arquitectura) y a los [ADRs](../adr/) (decisiones).

El objetivo: código **limpio, legible y mantenible**, que cualquier integrante del equipo pueda leer y entender rápido. Aplica a todo el código C# de la solución.

---

## 1. Principios SOLID

- **S — Responsabilidad Única (SRP):** cada clase/módulo tiene **una sola razón para cambiar**.
  Un caso de uso hace una cosa; un adaptador habla con un solo proveedor; una entidad encapsula un solo concepto del dominio.
- **O — Abierto/Cerrado (OCP):** extender sin modificar. Añadir un proveedor = **nuevo adaptador**
  de un puerto, no editar el código existente (ver [ADR-0003](../adr/0003-ports-adapters-intercambiables.md)).
- **L — Sustitución de Liskov (LSP):** toda implementación de un puerto debe ser intercambiable sin romper al consumidor. Respetar el contrato (pre/post-condiciones); no lanzar `NotSupportedException` en métodos del contrato.
- **I — Segregación de Interfaces (ISP):** puertos **pequeños y específicos** del caso de uso.
  Mejor varias interfaces cohesivas que una "catch-all".
- **D — Inversión de Dependencias (DIP):** el dominio y la aplicación dependen de **abstracciones** (puertos), no de implementaciones. Las dependencias concretas se inyectan desde el composition root. Es la base de la arquitectura hexagonal.

---

## 2. Clean Code

### Nombres
- Nombres **reveladores de intención**, en el lenguaje ubicuo del dominio. Evitar abreviaturas
  crípticas y nombres genéricos (`data`, `info`, `manager`, `helper`).
- Métodos = verbos (`IndexarCatalogo`, `BuscarDatasets`); clases/propiedades = sustantivos.
- Booleanos como afirmaciones (`IsCached`, `HasResults`).

### Funciones / métodos
- **Pequeños y con un solo nivel de abstracción.** Idealmente < 20 líneas; si crece, extraer.
- **Pocos parámetros** (0–6). Si son muchos, agrupar en un objeto/DTO.
- **Sin efectos secundarios ocultos**: el nombre debe reflejar todo lo que hace.
- **Evitar banderas booleanas** como parámetro que cambian el comportamiento: separar en métodos.
- **Fail-fast**: validar entradas al inicio y usar *guard clauses* en vez de anidar `if`.

### Comentarios y documentación
- El código se explica por sí mismo; el comentario justifica el **por qué**, no el **qué**.
- **Documentación XML** (`/// <summary>`) obligatoria en tipos y miembros públicos (ver `Directory.Build.props`).
- Nada de código comentado "por si acaso": el historial de git ya lo guarda.

### Estructura y formato
- **Tipos explícitos por defecto.** Usar `var` **sólo en excepciones reales** (p. ej. tipos anónimos o cuando el tipo explícito es imposible/redundante de forma evidente). Mejora la legibilidad al ver el tipo de un vistazo.
- **Un solo tipo por archivo** (clase/record/interfaz/enum/struct) y nombre de archivo = tipo.
- DRY: extraer duplicación a un único lugar. KISS: la solución más simple que funcione. YAGNI: no construir lo que no se necesita hoy.
- Inmutabilidad por defecto donde sea razonable (`record`, propiedades `init`, colecciones de solo lectura).
- Profundidad de anidación baja; preferir retornos tempranos.

### Manejo de errores
- Excepciones para lo excepcional; resultados explícitos para flujos esperables.
- No tragar excepciones (`catch {}` vacío). No exponer tipos de SDK externos hacia capas internas (mapear en el adaptador).
- Mensajes de error accionables, con contexto.

---

## 3. Async, recursos y rendimiento
- `async`/`await` de extremo a extremo en I/O (HTTP, BD); propagar `CancellationToken`.
- Liberar recursos con `using`/`await using`. No bloquear con `.Result`/`.Wait()`.
- Medir antes de optimizar; legibilidad primero, micro-optimización solo con evidencia.

---

## 4. Pruebas (ver [ADR-0006](../adr/0006-tdd-por-convencion.md))
- TDD por convención. xUnit + Shouldly. Dobles para los puertos en Domain/Application.
- Tests legibles (Arrange-Act-Assert) y nombrados `Metodo_Escenario_ResultadoEsperado`.
- Cada test prueba **una** conducta; sin dependencias entre tests.
- **Cobertura ≥ 95% por proyecto** (líneas, ramas y métodos). Umbral configurado con coverlet en `Directory.Build.props`; se mide con `dotnet test /p:CollectCoverage=true` y se exige en CI. Excluir de la cobertura sólo lo razonable (código generado, `Program.cs` de arranque) con ` [ExcludeFromCodeCoverage]` cuando aplique, no para inflar la métrica.

---

## 5. Checklist de revisión (PR)
- [ ] ¿Una sola responsabilidad por clase/método? (SRP)
- [ ] ¿Se respeta la dirección de dependencias y se programa contra puertos? (DIP/hexagonal)
- [ ] ¿Nombres claros, métodos cortos, sin duplicación?
- [ ] ¿Un tipo por archivo, nombre de archivo = tipo?
- [ ] ¿Documentación XML en lo público y comentarios que explican el *por qué*?
- [ ] ¿Manejo de errores correcto y sin filtrar tipos externos?
- [ ] ¿Tests que cubren la conducta y pasan? **Cobertura ≥ 95%** y build sin warnings.
