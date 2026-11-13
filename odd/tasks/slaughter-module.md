# Módulo Matanza de cabritos

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar qué cabritos se llevaron a sacrificar cada día, a qué rastro o matancero y cuánto se pagó, con el peso vivo y en canal de cada uno, y saber qué canales quedan por vender.
- Incluye: jornadas de matanza con varios cabritos, pesos y rendimiento, pago total y por cabrito, salida del hato y regreso si se corrige, canales sin vender, resumen.
- Fuera de alcance: despiece, venta de la canal (Ventas), caducidad de la canal.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-13 (rastro o matancero externo; varios cabritos el mismo día; peso vivo y peso en canal).
- Specs y contratos afectados: `backend/Modules/Slaughter/specs/slaughter.spec.md`, `frontend/src/modules/slaughter/specs/slaughter.spec.md`, `specs/domain.spec.md` (R11), `specs/database.spec.md`.

## Tareas

- [x] Esquema: `slaughter_runs`; `kid_slaughters` ligada a la jornada con peso vivo obligatorio y un cabrito por matanza vigente; triggers de retiro, nacimiento, fecha fija y canal en pedido.
- [x] Migración `docs/migrations/2026-11-13-05-matanza.sql`; se detiene si ya hay matanzas.
- [x] Backend `/api/slaughter`; mapeo mínimo de `orders` y `order_items` para saber si una canal ya está en un pedido.
- [x] Pantallas `/panel/matanza`, menú y portada.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-13: el nombre del rastro o matancero se escribe en cada jornada, sin catálogo, como el veterinario en Sanidad.
- 2026-11-13: el pago es el total de la jornada y es opcional; el pago por cabrito se calcula.
- 2026-11-13: una canal está disponible mientras no esté en un renglón de un pedido vigente (no cancelado).
- 2026-11-13: la fecha de la jornada no cambia; al quitar un cabrito o eliminar la jornada vuelve al hato como activo.

## Verificación y evidencia

Ejecutado el 2026-11-13 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: 54 tablas, 7 vistas, 8 triggers; 16 comprobaciones SQL (cabrito repetido, bovino, retiro de carne, animal muerto, canal más pesada, sin peso vivo, pago negativo, fecha fija, antes de nacer, canal en pedido vigente o cancelado).
- Migración: sobre el esquema de `main` deja el mismo esquema que `database.sql` (`pg_dump -s`); con una matanza previa se detiene sin cambios.
- `dotnet build`: 0 errores, 0 advertencias. `npm run build` y `check-layout` correctos.
- API real: 33 comprobaciones de los 7 criterios de aceptación, sin 500. Regresión de API: Hato 40, Sanidad 37, Reproducción 34, Ordeña 19 y Quesería 54, sin fallas.
- Navegador contra la API real: Matanza 17/17 (incluidos 375 px y tema oscuro); regresión Hato 34/34, Sanidad 35/35, Reproducción 22/22, Ordeña 24/24 y Quesería 42/42, sin errores de consola.
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Siguiente módulo: Ventas.
