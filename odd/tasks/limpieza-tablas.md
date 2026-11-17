# Limpieza: quitar tablas de equipo, vehículos e instalaciones

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: quitar del esquema las tablas heredadas de la plantilla que el negocio no va a usar, para que el proyecto quede más claro antes de crecer.
- Incluye: eliminar 7 tablas (`facility_types`, `facilities`, `equipment_categories`, `equipment`, `maintenance_records`, `vehicles`, `shipments`) y la columna `facility_id` de `animal_lots` y `supply_stock`.
- Fuera de alcance: recursos humanos, compras a proveedores y finanzas, que se conservan documentados como futuro; alimentación (`feeding_records`, `diets`) también se queda.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisión del usuario del 2026-11-16, al analizar el proyecto y elegir limpiar solo el grupo de equipo, vehículos e instalaciones.
- Specs y contratos afectados: `specs/database.spec.md`, `specs/domain.spec.md`, `README.md`, `docs/primeros-pasos.md`, entidad `AnimalLot`.

## Tareas

- [x] Quitar las 7 tablas y las 2 columnas `facility_id` del generador; regenerar `database.sql` (49 tablas).
- [x] Quitar la propiedad `FacilityId` de la entidad `AnimalLot`.
- [x] Migración `docs/migrations/2026-11-16-08-limpieza-tablas.sql`; se detiene si alguna tabla tiene datos.
- [x] Actualizar specs, README y guía de arranque.
- [ ] Aplicar la migración 08 en la laptop o el servidor con los datos reales.

## Decisiones

- 2026-11-16: `shipments` (envíos con chofer) se quita junto con vehículos; ya estaba fuera del alcance de Ventas.
- 2026-11-16: las columnas `facility_id` de `animal_lots` y `supply_stock` nunca se llenaron; se quitan con las tablas.
- 2026-11-16: recursos humanos, compras y finanzas se conservan en el esquema como posibles módulos futuros.

## Verificación y evidencia

Ejecutado el 2026-11-16 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: `database.sql` pasa de 56 a 49 tablas; 7 vistas y 10 triggers sin cambio. La migración 08 sobre el esquema anterior deja el mismo esquema que `database.sql` (`pg_dump -s`, idénticos salvo los tokens de sesión de pg_dump). Con un registro en `facility_types` la migración se detiene sin borrar nada.
- `dotnet build`: 0 errores, 0 advertencias.
- API real del módulo Hato (que usa `animal_lots`): 40/40 comprobaciones sin fallas contra el esquema de 49 tablas.
- **No verificado:** el resto de las suites de API y navegador (el cambio solo quita tablas y una columna sin uso) ni Docker Compose con PostgreSQL 15.
