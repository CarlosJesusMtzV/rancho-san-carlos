# Módulo Sanidad

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar la sanidad del hato y saber en todo momento qué animales tienen retiro de leche o carne.
- Incluye: visitas del veterinario con pago, eventos individuales o por lote, tratamientos con retiros, catálogo de medicamentos sin existencias, pruebas de laboratorio, enlaces desde la ficha de Hato.
- Fuera de alcance: registrar el pago como gasto contable (Finanzas), inventario de insumos, autenticación.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-11 (veterinario externo que surte y cobra medicamentos; nombre libre; honorarios y medicamentos por separado; visita opcional; aplicación por lote; sin inventario de medicamentos).
- Specs y contratos afectados: `backend/Modules/AnimalHealth/specs/animal-health.spec.md`, `frontend/src/modules/animal-health/specs/animal-health.spec.md`, `specs/domain.spec.md`, `specs/database.spec.md`, `components.spec.md`.

## Tareas

- [x] Esquema: `vet_visits`, `health_events.vet_visit_id`, retiros sugeridos en `supplies`, tipos de insumo iniciales, zona horaria de la base.
- [x] Migración `docs/migrations/2026-11-11-01-sanidad.sql` para bases existentes.
- [x] Backend `/api/animal-health`.
- [x] Pantallas `/panel/sanidad` y enlaces desde la ficha de Hato.
- [x] Patrones de página compartidos en `shared/styles/panel.css`; `UiLoadError` y `shared/utils/format.ts` compartidos.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-11: ruta `/api/animal-health` porque `/api/health` es el health check.
- 2026-11-11: `health_events.vet_employee_id` se sustituye por `vet_visit_id`; el veterinario es externo y no es empleado.
- 2026-11-11: la base usa la zona `America/Monterrey`. Con UTC, una ordeña a las 20:00 contaba como el día siguiente y movía un día los retiros.
- 2026-11-11: un evento duplicado (mismo animal, tipo y día) responde 409, para evitar el doble envío al vacunar un lote.
- 2026-11-11: las visitas no se borran porque conservan lo pagado; los eventos sí, con borrado lógico.
- 2026-11-11: `SpeciesTag` lleva su estilo dentro del componente porque lo usan dos módulos.

## Verificación y evidencia

Ejecutado el 2026-11-11 con .NET 10.0.404 y PostgreSQL 16 local:

- `dotnet build`: 0 errores, 0 advertencias.
- API real: 38 comprobaciones de los 10 criterios de aceptación y validaciones extra. Incluye que la base rechace la ordeña de la cabra tratada y la permita al borrar el evento.
- Migración: aplicada sobre una base con el esquema anterior deja el mismo esquema que `database.sql` (`pg_dump -s`).
- Zona horaria: `2026-11-12 01:30 UTC` se convierte en el día 11.
- Navegador contra la API real: Sanidad 35/35 y Hato 33/33 (regresión tras mover estilos), incluidos 375 px y tema oscuro, sin errores de consola ni 500.
- Se encontró y corrigió un desbordamiento a 375 px en Laboratorio (etiqueta oculta con posición absoluta fuera de la tabla).
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Siguiente módulo: Reproducción.
