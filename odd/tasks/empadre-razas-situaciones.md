# Empadre, razas y situaciones

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: que la reproducción refleje cómo trabaja el rancho (empadre en agostadero, no monta vista), y que raza, propósito y situación del animal sean listas claras.
- Incluye: empadre por temporada (un semental con un grupo de hembras), propósito de raza como lista fija (carne/leche/carne-leche), razas sembradas y situación del animal con opciones fijas (antes "lote").
- Fuera de alcance: inseminación, sincronización de celos.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: mensaje del usuario del 2026-11-19 y sus respuestas (empadre por grupo con fecha de entrada del semental; situaciones bovino libre/encerrada con becerro/cuidado especial y cabra con crías/sin crías/cuidado especial; razas Limousin, Charbray, Simmental, Saanen, Alpina, Toggenburg, Boer, Nubia; propósito solo opciones).
- Specs afectadas: `specs/domain.spec.md`, `specs/database.spec.md`, `backend/Modules/Reproduction/specs`, `frontend/src/modules/reproduction/specs`, specs de Hato.

## Tareas

- [x] Esquema: `breeding_seasons`, `breeding_events.start_date` + `breeding_season_id`, `breeds.purpose` como lista fija; seeds de razas y situaciones.
- [x] Backend: empadre por grupo (`POST /empadres`, `GET /empadres`), propósito validado.
- [x] Frontend: formulario de empadre con varias hembras; situación como lista fija; propósito como select; relabel monta→empadre y lote→situación.
- [x] Migración `docs/migrations/2026-11-19-09-empadre-razas-situaciones.sql`; se detiene si ya hay razas, situaciones o empadres.
- [ ] Aplicar la migración 09 en la laptop o el servidor, o empezar con base nueva.

## Decisiones

- 2026-11-19: el empadre es un encabezado (`breeding_seasons`) y cada hembra una participación (`breeding_events`), reutilizando el diagnóstico y el parto por hembra ya probados.
- 2026-11-19: la fecha probable de parto se estima desde la entrada del semental; no se pretende saber la fecha de monta.
- 2026-11-19: la situación se queda en la tabla `animal_lots` con filas sembradas; el panel solo deja elegirlas, no crear nuevas.

## Verificación y evidencia

Ejecutado el 2026-11-19 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: 50 tablas. La migración 09 sobre el esquema anterior deja el mismo esquema que `database.sql` (`pg_dump -s`, idénticos). Con razas, situaciones o empadres previos la migración se detiene sin cambiar nada.
- API real: Reproducción 37/37 (empadre por grupo, propósito obligatorio, situaciones). Regresión con sesión: Hato 40, Sanidad, Ordeña, Quesería, Matanza, Ventas y Acceso sin fallas.
- Frontend: `npm run build` (incluye `vue-tsc`) y `check-layout` correctos. Humo de navegador del formulario de empadre y del catálogo sin errores de consola.
- **No verificado:** suites de navegador completas de los demás módulos ni Docker Compose con PostgreSQL 15.
