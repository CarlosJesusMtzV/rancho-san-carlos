-- =============================================================================
-- Migración: ordeña diaria con total de litros y cabras ordeñadas (módulo Ordeña).
--
-- Se ordeña una vez al día; se mide el total de litros, no por cabra, y la leche
-- del día forma un lote para quesería. Requiere las migraciones 01 y 02.
--
-- Si ya había ordeñas registradas a mano, revísalas antes: esta migración quita
-- los litros por cabra y exige el total del día.
--
-- Uso (desde la raíz del proyecto, con Docker en marcha):
--   docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-11-03-ordena.sql
-- =============================================================================

BEGIN;

DROP VIEW v_cheese_batch_traceability;
DROP TRIGGER tg_milk_records_check ON milk_records;

-- Lote de leche = ordeña del día, con su total.
ALTER TABLE milk_lots
    ADD COLUMN total_liters numeric(8,2) NOT NULL,
    ADD CONSTRAINT ck_milk_lots_total_liters CHECK (total_liters > 0);
CREATE UNIQUE INDEX uq_milk_lots_collection_date_active ON milk_lots (collection_date)
    WHERE deleted_at IS NULL;

-- Cabras ordeñadas: sin litros ni hora; la fecha es la del lote.
ALTER TABLE milk_records
    DROP CONSTRAINT uq_milk_records_animal_time,
    DROP CONSTRAINT ck_milk_records_liters,
    DROP COLUMN milked_at,
    DROP COLUMN liters;
CREATE UNIQUE INDEX uq_milk_records_lot_animal_active ON milk_records (milk_lot_id, animal_id)
    WHERE deleted_at IS NULL;

-- Ordeña: solo cabras hembra activas y sin retiro de leche en la fecha de la ordeña.
CREATE OR REPLACE FUNCTION fn_check_milk_record()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_sex char(1);
    v_lifecycle varchar(12);
    v_tag varchar(30);
    v_date date;
    v_allowed date;
BEGIN
    SELECT sex, lifecycle_status, visual_tag INTO v_sex, v_lifecycle, v_tag FROM animals WHERE id = NEW.animal_id;
    IF v_sex IS DISTINCT FROM 'F' THEN
        RAISE EXCEPTION 'Solo se pueden ordeñar cabras hembra.' USING ERRCODE = 'check_violation';
    END IF;
    IF v_lifecycle IS DISTINCT FROM 'active' THEN
        RAISE EXCEPTION 'La cabra % ya no está activa en el hato.', v_tag USING ERRCODE = 'check_violation';
    END IF;
    SELECT collection_date INTO v_date FROM milk_lots WHERE id = NEW.milk_lot_id;
    v_allowed := fn_withdrawal_allowed_from(NEW.animal_id, 'milk', v_date);
    IF v_allowed IS NOT NULL THEN
        RAISE EXCEPTION 'La cabra % tiene retiro de leche; su leche puede usarse a partir del %.', v_tag, to_char(v_allowed, 'DD/MM/YYYY')
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER tg_milk_records_check
BEFORE INSERT OR UPDATE OF animal_id, milk_lot_id ON milk_records
FOR EACH ROW EXECUTE FUNCTION fn_check_milk_record();

CREATE VIEW v_cheese_batch_traceability AS
SELECT
    cb.id AS cheese_batch_id,
    cb.code AS batch_code,
    p.sku,
    p.name AS product_name,
    cb.made_on,
    cb.expires_on,
    cb.liters_used,
    cb.kg_produced,
    cb.kg_available,
    ml.code AS milk_lot_code,
    ml.collection_date,
    count(DISTINCT mr.animal_id) AS goats_milked,
    ml.total_liters AS liters_collected
FROM cheese_batches cb
JOIN products p ON p.id = cb.product_id
JOIN milk_lots ml ON ml.id = cb.milk_lot_id
LEFT JOIN milk_records mr ON mr.milk_lot_id = ml.id AND mr.deleted_at IS NULL
WHERE cb.deleted_at IS NULL
GROUP BY cb.id, p.sku, p.name, ml.code, ml.collection_date, ml.total_liters;

COMMIT;
