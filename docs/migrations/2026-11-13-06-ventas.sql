-- =============================================================================
-- Migración: Ventas con encargos, precio por kilo de ganado y canal, entrega y pago.
--
-- Los pedidos son ventas en el momento o encargos que apartan lo vendido; el
-- ganado en pie y la canal se venden por kilo; el pago se registra por pedido al
-- entregar; la factura es opcional. Requiere las migraciones 01 a 05.
--
-- Si ya había pedidos, facturas o pagos registrados, la migración se detiene
-- sin cambiar nada.
--
-- Uso (desde la raíz del proyecto, con Docker en marcha):
--   docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-13-06-ventas.sql
-- =============================================================================

BEGIN;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM orders) OR EXISTS (SELECT 1 FROM invoices) OR EXISTS (SELECT 1 FROM payments) THEN
        RAISE EXCEPTION 'Hay pedidos, facturas o pagos registrados: revísalos antes de migrar.';
    END IF;
END
$$;

-- Pedido: venta en el momento o encargo, con entrega.
ALTER TABLE orders
    ALTER COLUMN order_status SET DEFAULT 'confirmed',
    ADD COLUMN delivery_type varchar(10) NOT NULL DEFAULT 'pickup',
    ADD COLUMN delivery_address varchar(255),
    ADD COLUMN delivered_on date,
    ADD COLUMN notes varchar(255),
    ADD CONSTRAINT ck_orders_delivery_type CHECK (delivery_type IN ('pickup', 'delivery')),
    ADD CONSTRAINT ck_orders_delivery_address CHECK (delivery_type = 'pickup' OR delivery_address IS NOT NULL),
    ADD CONSTRAINT ck_orders_delivered_on CHECK (CASE order_status WHEN 'delivered' THEN delivered_on IS NOT NULL WHEN 'cancelled' THEN true ELSE delivered_on IS NULL END AND (delivered_on IS NULL OR delivered_on >= order_date));

-- Ganado y canal por kilo: la cantidad del renglón son los kg. La venta única la cuida tg_order_items_check.
ALTER TABLE order_items DROP CONSTRAINT ck_order_items_unit;
DROP INDEX uq_order_items_animal_active;
DROP INDEX uq_order_items_kid_slaughter_active;

-- Factura opcional sin estado de pago; el pago es del pedido.
ALTER TABLE invoices
    DROP CONSTRAINT ck_invoices_payment_status,
    DROP COLUMN payment_status;
ALTER TABLE payments
    DROP COLUMN invoice_id,
    ADD COLUMN order_id integer NOT NULL,
    ADD CONSTRAINT fk_payments_order_id FOREIGN KEY (order_id) REFERENCES orders (id);
CREATE INDEX ix_payments_order_id ON payments (order_id);

-- Una factura y un pago vigentes por pedido.
CREATE UNIQUE INDEX uq_invoices_order_active ON invoices (order_id)
    WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX uq_payments_order_active ON payments (order_id)
    WHERE deleted_at IS NULL;

-- Renglón de pedido: queso de una entrada vigente, sin caducar y con existencia; ganado activo de la especie
-- del producto y sin retiro de carne; canal de una matanza vigente. Animal o canal en un solo pedido vigente.
-- Los renglones no se editan: se eliminan (borrado lógico) y se registran de nuevo.
CREATE FUNCTION fn_check_order_item()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_kind varchar(10);
    v_unit varchar(10);
    v_species smallint;
    v_name varchar(120);
    v_date date;
    v_status varchar(10);
    v_source_product integer;
    v_in numeric;
    v_expires date;
    v_code varchar(30);
    v_available numeric;
    v_tag varchar(30);
    v_animal_species smallint;
    v_lifecycle varchar(12);
    v_allowed date;
    v_other integer;
BEGIN
    IF TG_OP = 'UPDATE' THEN
        IF NEW.deleted_at IS NOT NULL OR (NEW.product_id = OLD.product_id
            AND NEW.cheese_batch_output_id IS NOT DISTINCT FROM OLD.cheese_batch_output_id
            AND NEW.cheese_purchase_id IS NOT DISTINCT FROM OLD.cheese_purchase_id
            AND NEW.animal_id IS NOT DISTINCT FROM OLD.animal_id
            AND NEW.kid_slaughter_id IS NOT DISTINCT FROM OLD.kid_slaughter_id
            AND NEW.quantity = OLD.quantity) THEN
            RETURN NEW;
        END IF;
        RAISE EXCEPTION 'Los renglones de un pedido no se editan; se reemplazan.' USING ERRCODE = 'check_violation';
    END IF;

    SELECT order_date, order_status INTO v_date, v_status FROM orders WHERE id = NEW.order_id;
    IF v_status = 'cancelled' THEN
        RAISE EXCEPTION 'El pedido está cancelado.' USING ERRCODE = 'check_violation';
    END IF;
    SELECT pt.kind, p.sale_unit, p.species_id, p.name INTO v_kind, v_unit, v_species, v_name
    FROM products p JOIN product_types pt ON pt.id = p.product_type_id
    WHERE p.id = NEW.product_id;

    IF v_kind IN ('cheese', 'resale') THEN
        IF NEW.animal_id IS NOT NULL OR NEW.kid_slaughter_id IS NOT NULL
            OR num_nonnulls(NEW.cheese_batch_output_id, NEW.cheese_purchase_id) <> 1 THEN
            RAISE EXCEPTION '% se vende de una tanda o de una compra.', v_name USING ERRCODE = 'check_violation';
        END IF;
        IF NEW.cheese_batch_output_id IS NOT NULL THEN
            SELECT o.product_id, o.quantity, cb.expires_on, cb.code INTO v_source_product, v_in, v_expires, v_code
            FROM cheese_batch_outputs o
            JOIN cheese_batches cb ON cb.id = o.cheese_batch_id
            WHERE o.id = NEW.cheese_batch_output_id AND o.deleted_at IS NULL AND cb.deleted_at IS NULL
            FOR UPDATE OF o;
        ELSE
            SELECT product_id, quantity, expires_on, code INTO v_source_product, v_in, v_expires, v_code
            FROM cheese_purchases
            WHERE id = NEW.cheese_purchase_id AND deleted_at IS NULL
            FOR UPDATE;
        END IF;
        IF v_source_product IS DISTINCT FROM NEW.product_id THEN
            RAISE EXCEPTION 'La tanda o compra elegida no es de %.', v_name USING ERRCODE = 'check_violation';
        END IF;
        IF v_expires < v_date THEN
            RAISE EXCEPTION '% (%) caducó el %; no se vende.', v_name, v_code, to_char(v_expires, 'DD/MM/YYYY') USING ERRCODE = 'check_violation';
        END IF;
        IF v_unit = 'piece' AND NEW.quantity <> trunc(NEW.quantity) THEN
            RAISE EXCEPTION '% se vende en piezas enteras.', v_name USING ERRCODE = 'check_violation';
        END IF;
        IF v_status IN ('confirmed', 'delivered') THEN
            v_available := v_in - fn_cheese_consumed(NEW.cheese_batch_output_id, NEW.cheese_purchase_id);
            IF NEW.quantity > v_available THEN
                RAISE EXCEPTION 'Solo hay % disponibles de % (%).', trim_scale(v_available), v_name, v_code USING ERRCODE = 'check_violation';
            END IF;
        END IF;
    ELSIF v_kind = 'livestock' THEN
        IF NEW.animal_id IS NULL OR num_nonnulls(NEW.cheese_batch_output_id, NEW.cheese_purchase_id, NEW.kid_slaughter_id) > 0 THEN
            RAISE EXCEPTION '% se vende por animal.', v_name USING ERRCODE = 'check_violation';
        END IF;
        SELECT visual_tag, species_id, lifecycle_status INTO v_tag, v_animal_species, v_lifecycle FROM animals WHERE id = NEW.animal_id;
        IF v_animal_species IS DISTINCT FROM v_species THEN
            RAISE EXCEPTION '% no corresponde a %.', v_tag, v_name USING ERRCODE = 'check_violation';
        END IF;
        IF v_lifecycle IS DISTINCT FROM 'active' THEN
            RAISE EXCEPTION '% ya salió del hato.', v_tag USING ERRCODE = 'check_violation';
        END IF;
        v_allowed := fn_withdrawal_allowed_from(NEW.animal_id, 'meat', v_date);
        IF v_allowed IS NOT NULL THEN
            RAISE EXCEPTION '% tiene retiro de carne; puede venderse a partir del %.', v_tag, to_char(v_allowed, 'DD/MM/YYYY')
                USING ERRCODE = 'check_violation';
        END IF;
        SELECT oi.order_id INTO v_other
        FROM order_items oi JOIN orders od ON od.id = oi.order_id
        WHERE oi.animal_id = NEW.animal_id AND oi.deleted_at IS NULL AND od.deleted_at IS NULL AND od.order_status <> 'cancelled'
        LIMIT 1;
        IF v_other IS NOT NULL THEN
            RAISE EXCEPTION '% ya está en el pedido %.', v_tag, v_other USING ERRCODE = 'check_violation';
        END IF;
    ELSIF v_kind = 'carcass' THEN
        IF NEW.kid_slaughter_id IS NULL OR num_nonnulls(NEW.cheese_batch_output_id, NEW.cheese_purchase_id, NEW.animal_id) > 0 THEN
            RAISE EXCEPTION '% se vende por matanza.', v_name USING ERRCODE = 'check_violation';
        END IF;
        SELECT a.visual_tag INTO v_tag
        FROM kid_slaughters ks JOIN animals a ON a.id = ks.animal_id
        WHERE ks.id = NEW.kid_slaughter_id AND ks.deleted_at IS NULL;
        IF v_tag IS NULL THEN
            RAISE EXCEPTION 'La matanza elegida ya no está vigente.' USING ERRCODE = 'check_violation';
        END IF;
        SELECT oi.order_id INTO v_other
        FROM order_items oi JOIN orders od ON od.id = oi.order_id
        WHERE oi.kid_slaughter_id = NEW.kid_slaughter_id AND oi.deleted_at IS NULL AND od.deleted_at IS NULL AND od.order_status <> 'cancelled'
        LIMIT 1;
        IF v_other IS NOT NULL THEN
            RAISE EXCEPTION 'La canal de % ya está en el pedido %.', v_tag, v_other USING ERRCODE = 'check_violation';
        END IF;
    ELSE
        RAISE EXCEPTION 'El producto no se puede vender.' USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER tg_order_items_check
BEFORE INSERT OR UPDATE ON order_items
FOR EACH ROW EXECUTE FUNCTION fn_check_order_item();

-- Pedido: uno cancelado no se reactiva (lo que apartaba ya pudo venderse a otro cliente).
CREATE FUNCTION fn_check_order()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    IF OLD.order_status = 'cancelled' AND NEW.order_status <> 'cancelled' THEN
        RAISE EXCEPTION 'Un pedido cancelado no se reactiva; registra uno nuevo.' USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER tg_orders_check
BEFORE UPDATE OF order_status ON orders
FOR EACH ROW EXECUTE FUNCTION fn_check_order();

-- Matanza: un cabrito apartado para venta en pie no se sacrifica.
-- Matanza de cabritos: cabrito vigente en el hato, no anterior a su nacimiento y sin retiro de carne
-- en la fecha de la jornada; una canal en un pedido vigente no se elimina.
CREATE OR REPLACE FUNCTION fn_check_kid_slaughter()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_date date;
    v_lifecycle varchar(12);
    v_tag varchar(30);
    v_birth date;
    v_allowed date;
BEGIN
    IF TG_OP = 'UPDATE' AND NEW.deleted_at IS NOT NULL THEN
        IF OLD.deleted_at IS NULL AND EXISTS (
            SELECT 1 FROM order_items oi
            JOIN orders od ON od.id = oi.order_id
            WHERE oi.kid_slaughter_id = NEW.id AND oi.deleted_at IS NULL AND od.deleted_at IS NULL
              AND od.order_status <> 'cancelled'
        ) THEN
            RAISE EXCEPTION 'La canal ya está en un pedido; no se puede quitar de la matanza.' USING ERRCODE = 'check_violation';
        END IF;
        RETURN NEW;
    END IF;
    SELECT run_date INTO v_date FROM slaughter_runs WHERE id = NEW.slaughter_run_id;
    SELECT lifecycle_status, visual_tag, birth_date INTO v_lifecycle, v_tag, v_birth FROM animals WHERE id = NEW.animal_id;
    IF (TG_OP = 'INSERT' OR NEW.animal_id <> OLD.animal_id) AND v_lifecycle NOT IN ('active', 'slaughtered') THEN
        RAISE EXCEPTION 'El cabrito % ya salió del hato.', v_tag USING ERRCODE = 'check_violation';
    END IF;
    IF TG_OP = 'INSERT' AND EXISTS (
        SELECT 1 FROM order_items oi
        JOIN orders od ON od.id = oi.order_id
        WHERE oi.animal_id = NEW.animal_id AND oi.deleted_at IS NULL AND od.deleted_at IS NULL AND od.order_status <> 'cancelled'
    ) THEN
        RAISE EXCEPTION 'El cabrito % está apartado en un pedido de venta en pie.', v_tag USING ERRCODE = 'check_violation';
    END IF;
    IF v_birth IS NOT NULL AND v_date < v_birth THEN
        RAISE EXCEPTION 'La matanza no puede ser anterior al nacimiento de % (%).', v_tag, to_char(v_birth, 'DD/MM/YYYY')
            USING ERRCODE = 'check_violation';
    END IF;
    v_allowed := fn_withdrawal_allowed_from(NEW.animal_id, 'meat', v_date);
    IF v_allowed IS NOT NULL THEN
        RAISE EXCEPTION 'El cabrito % tiene retiro de carne; puede sacrificarse a partir del %.', v_tag, to_char(v_allowed, 'DD/MM/YYYY')
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NEW;
END;
$$;

UPDATE products SET sale_unit = 'kg', description = 'Por kilo en pie; se pesa al vender.' WHERE sku = 'GAN-CAB';
UPDATE products SET sale_unit = 'kg', description = 'Sacrificado y pelado; canal entera, por kilo de canal.' WHERE sku = 'CAB-CAN';
UPDATE products SET sale_unit = 'kg', description = 'Edad promedio de 8 meses y peso aproximado de 150 kg; por kilo en pie.' WHERE sku = 'GAN-BEC';

INSERT INTO customer_types (id, name)
SELECT v.id, v.name
FROM (VALUES (1, 'Restaurante o tienda'), (2, 'Comprador de ganado'), (3, 'Particular')) AS v (id, name)
WHERE NOT EXISTS (SELECT 1 FROM customer_types);
SELECT setval(pg_get_serial_sequence('customer_types', 'id'), greatest(3, (SELECT max(id) FROM customer_types)));

COMMIT;
