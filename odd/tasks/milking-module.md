# Módulo Ordeña

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar cada día cuánta leche de cabra se juntó y qué cabras se ordeñaron, sin usar leche de cabras en retiro, y dejar el lote de leche listo para Quesería.
- Incluye: ordeña diaria (total de litros, cabras, notas), edición y borrado, cabras disponibles con su retiro, resumen de 7 y 30 días.
- Fuera de alcance: litros por cabra, varias ordeñas al día, lotes de queso (Quesería).
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-11 (total por ordeña y cabras ordeñadas aparte; una ordeña al día; todo lo del día es un lote).
- Specs y contratos afectados: `backend/Modules/Milking/specs/milking.spec.md`, `frontend/src/modules/milking/specs/milking.spec.md`, `specs/domain.spec.md` (R10), `specs/database.spec.md`.

## Tareas

- [x] Esquema: `milk_lots.total_liters` y un lote activo por fecha; `milk_records` solo liga cabra y lote; trigger revisa cabra hembra activa sin retiro en la fecha del lote.
- [x] Migración `docs/migrations/2026-11-11-03-ordena.sql`.
- [x] Backend `/api/milking`.
- [x] Pantallas `/panel/ordena` y `/panel/ordena/:id`, menú y portada del panel.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-11: el código del lote es `LL-AAAAMMDD`; si un día se borra y se vuelve a registrar, lleva sufijo `-2`, porque el código es único aun entre registros borrados.
- 2026-11-11: la fecha de una ordeña no se edita; para corregirla se elimina y se registra de nuevo.
- 2026-11-11: el backend replica `fn_withdrawal_allowed_from` para mostrar el retiro antes de guardar; el trigger sigue siendo la última defensa.
- 2026-11-11: al editar una ordeña se conservan sus cabras aunque después hayan entrado en retiro o salido del hato.

## Verificación y evidencia

Ejecutado el 2026-11-11 con .NET 10.0.404 y PostgreSQL 16 local:

- Migración: aplicada sobre el esquema de `main` deja el mismo esquema que `database.sql` (`pg_dump -s`).
- `dotnet build`: 0 errores, 0 advertencias. `npm run build` y `check-layout` correctos.
- API real: 19 comprobaciones (retiro, macho, vaca, día repetido, fecha futura, litros inválidos, edición, borrado y nuevo registro con sufijo, resumen).
- Navegador contra la API real: Ordeña 24/24 (incluidos 375 px y tema oscuro), regresión Hato 34/34, Sanidad 35/35 y Reproducción 22/22, sin errores de consola ni 500.
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Siguiente módulo: Quesería (hecho, ver `cheese-module.md`).
