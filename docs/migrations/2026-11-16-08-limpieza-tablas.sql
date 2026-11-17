-- =============================================================================
-- Migración: quitar las tablas de equipo, vehículos e instalaciones.
--
-- Son 7 tablas heredadas de la plantilla que nunca se usaron: facility_types,
-- facilities, equipment_categories, equipment, maintenance_records, vehicles y
-- shipments. También se quita la columna facility_id de animal_lots y de
-- supply_stock, que apuntaba a facilities y nunca se llenó. Requiere las
-- migraciones 01 a 07.
--
-- Si alguna de esas 7 tablas tiene datos, la migración se detiene sin cambiar
-- nada, para que los revises antes.
--
-- Uso (desde la raíz del proyecto, con Docker en marcha):
--   docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-16-08-limpieza-tablas.sql
-- =============================================================================

BEGIN;

DO $$
DECLARE
    tabla text;
    total bigint;
BEGIN
    FOREACH tabla IN ARRAY ARRAY[
        'facility_types', 'facilities', 'equipment_categories', 'equipment',
        'maintenance_records', 'vehicles', 'shipments'
    ] LOOP
        IF to_regclass(tabla) IS NOT NULL THEN
            EXECUTE format('SELECT count(*) FROM %I', tabla) INTO total;
            IF total > 0 THEN
                RAISE EXCEPTION 'La tabla % tiene % registros: revísalos antes de quitarla.', tabla, total;
            END IF;
        END IF;
    END LOOP;
END
$$;

-- Columnas que apuntaban a facilities en tablas que sí se conservan.
ALTER TABLE animal_lots DROP COLUMN IF EXISTS facility_id;
ALTER TABLE supply_stock DROP COLUMN IF EXISTS facility_id;

-- Tablas en orden de dependencias (primero las que referencian a otras).
DROP TABLE IF EXISTS shipments;
DROP TABLE IF EXISTS maintenance_records;
DROP TABLE IF EXISTS equipment;
DROP TABLE IF EXISTS equipment_categories;
DROP TABLE IF EXISTS vehicles;
DROP TABLE IF EXISTS facilities;
DROP TABLE IF EXISTS facility_types;

COMMIT;
