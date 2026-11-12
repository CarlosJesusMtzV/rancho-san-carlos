# Módulo Reproducción

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: saber qué hembras están vacías, montadas, cargadas o por parir, y registrar partos que dan de alta a las crías.
- Incluye: montas naturales, diagnóstico de preñez en visitas del veterinario, partos con crías vivas y muertas, historial y enlaces desde Hato.
- Fuera de alcance: inseminación artificial, destete, celo.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-11 (solo monta natural; crías dadas de alta al parto; preñez diagnosticada por el veterinario en sus visitas).
- Specs y contratos afectados: `backend/Modules/Reproduction/specs/reproduction.spec.md`, `frontend/src/modules/reproduction/specs/reproduction.spec.md`, `specs/domain.spec.md`, `specs/database.spec.md`.

## Tareas

- [x] Esquema: `breeding_events` solo monta natural con especie y llaves compuestas, `pregnancy_checks`, `birth_events.stillborn_count`, un parto por monta.
- [x] Migración `docs/migrations/2026-11-11-02-reproduccion.sql`; migraciones numeradas para respetar el orden.
- [x] Backend `/api/reproduction`.
- [x] Pantallas `/panel/reproduccion` y enlace desde la ficha de una hembra.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-11: gestación de 283 días (bovino) y 150 (caprino) para la fecha probable de parto; constantes en el servicio y en `data/labels.ts`.
- 2026-11-11: un parto de una hembra sin monta abierta se registra sin monta y con padre desconocido, para hembras compradas cargadas.
- 2026-11-11: las crías muertas al nacer solo se cuentan; las vivas toman raza y lote de la madre salvo que se indiquen.
- 2026-11-11: el estado de la monta es el del diagnóstico más reciente por fecha de visita.
- 2026-11-11: `AnimalAge` y `TextInput` en `Core` para no duplicar lógica entre módulos.

## Verificación y evidencia

Ejecutado el 2026-11-11 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: restricciones nuevas probadas en SQL (monta entre especies, consigo misma, parto sin crías, segundo parto de la misma monta, diagnóstico repetido en una visita). Pruebas previas del esquema sin cambios.
- Migración: aplicada sobre el esquema de `main` deja el mismo esquema que `database.sql` (`pg_dump -s`).
- `dotnet build`: 0 errores, 0 advertencias.
- API real: 34 comprobaciones de los 9 criterios de aceptación y validaciones extra; el parto con un arete repetido no deja nada guardado.
- Navegador contra la API real: Reproducción 22/22, Hato 33/33 y Sanidad 35/35, incluidos 375 px y tema oscuro, sin errores de consola ni 500.
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Siguiente módulo: Ordeña.
