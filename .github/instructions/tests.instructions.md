---
applyTo: "tests/**/*.cs"
---

# Pruebas — reglas (Copilot)

TDD por convención ([ADR-0006](../../docs/adr/0006-tdd-por-convencion.md)).

- Framework: **xUnit**. Aserciones fluidas con **Shouldly** (no FluentAssertions, por licencia).
- Preferir test primero (Red-Green-Refactor), sobre todo en Domain y Application.
- Domain/Application: tests unitarios con **dobles** de los puertos (sin red ni I/O real).
- Infrastructure: tests de **integración** contra dependencias reales (Socrata, Docker local).
- Nombrar tests de forma descriptiva: `Metodo_Escenario_ResultadoEsperado`.
- Un assert lógico por test cuando sea posible; arrange-act-assert claro.
- **Cobertura ≥ 95% por proyecto** (líneas/ramas/métodos), medida con coverlet
  (`dotnet test /p:CollectCoverage=true`). No inflar la métrica; excluir sólo lo razonable con
  `[ExcludeFromCodeCoverage]`.
