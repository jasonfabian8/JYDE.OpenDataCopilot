# CLAUDE.md — Capa Domain

Núcleo del negocio. Ver gobierno global en [`/CLAUDE.md`](../../CLAUDE.md) y
[SAD](../../docs/architecture/SAD.md).

## Reglas

- **Cero dependencias externas.** Nada de paquetes NuGet de infraestructura (HTTP, Azure, Mongo,
  EF, Socrata SDK...). Sólo BCL de .NET.
- **No referencia a otras capas.** Domain no conoce Application, Infrastructure ni Api.
- Aquí viven: **entidades**, **value objects**, **agregados**, reglas de negocio puras y, si
  corresponde, interfaces de **repositorio del dominio**.
- Modela con el **lenguaje ubicuo** (glosario en el SAD): `Dataset`, `Column`, `Category`,
  `DatasetMetadata`, etc.
- Inmutabilidad y validación en el constructor cuando sea razonable; evita estados inválidos.
- **Documentación XML** en tipos y miembros públicos.

## Qué NO hacer aquí

- Lógica de acceso a datos, serialización JSON, llamadas a APIs o detalles de proveedor.
- Esos puertos (interfaces de servicios técnicos) se definen en **Application**.
