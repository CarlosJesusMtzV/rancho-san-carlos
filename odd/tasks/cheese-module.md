# Módulo Quesería

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar el queso que sale de la leche de cada día, las compras de asadero para reventa y lo que se pierde, y saber cuánto queda de cada tanda o compra y cuándo caduca.
- Incluye: tandas de canasta y panela por ordeña, compras de asadero con proveedor y costo, mermas, consumo propio y regalos, existencias con caducidad, historial; protección de la ordeña usada; corrección del sitio público sobre el asadero.
- Fuera de alcance: venta (Ventas descontará con `order_items`), insumos y costo de elaboración, página pública de trazabilidad con datos reales.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-13 (canasta y panela son el mismo queso y proceso; cada tanda usa la leche de un día; el asadero se compra hecho, es de leche de vaca y se sigue vendiendo sin decir que es propio; se registra cada compra). Supuestos aceptados por el usuario: kg de canasta y piezas de panela por tanda; caducidad sugerida de 7 días con máximo de 11; inventario por tanda y compra con mermas.
- Specs y contratos afectados: `backend/Modules/Cheese/specs/cheese.spec.md`, `frontend/src/modules/cheese/specs/cheese.spec.md`, `backend/Modules/Milking/specs/milking.spec.md` (regla Q9), `frontend/src/modules/landing/specs/landing.spec.md`, `specs/domain.spec.md` (R4, R6, R13, R15), `specs/database.spec.md`, skill `rancho-san-carlos`.

## Tareas

- [x] Esquema: `cheese_batches` por ordeña, `cheese_batch_outputs`, `cheese_purchases`, `cheese_stock_adjustments`, `v_cheese_stock`, 5 triggers, tipo de producto `resale`, asadero de leche de vaca; `order_items` por presentación o compra.
- [x] Migración `docs/migrations/2026-11-13-04-queseria.sql`, armada a partir de `database.sql`; se detiene si ya hay queso registrado.
- [x] Backend `/api/cheese`; Ordeña no deja eliminar ni bajar el total de una ordeña con tanda.
- [x] Pantallas `/panel/queseria` (existencias, tanda, compra, historial), enlace desde la ordeña y menú.
- [x] Sitio público: el asadero aparece como queso de proveedor y de leche de vaca.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-13: las existencias no se guardan; `v_cheese_stock` las calcula (entró − mermas − vendido en pedidos confirmados o entregados), para que no se desfasen.
- 2026-11-13: los triggers impiden existencias negativas aunque el backend falle; Ventas debe validar con `fn_cheese_consumed` o la vista antes de confirmar.
- 2026-11-13: códigos `QF-AAAAMMDD` (día de la ordeña) y `CQ-AAAAMMDD` con sufijo `-2`, `-3` si se repiten.
- 2026-11-13: una merma no se edita; se elimina y se registra de nuevo.
- 2026-11-13: `products.species_id` es la especie de origen; el asadero queda con bovino (leche de vaca), tipo `resale`.
- 2026-11-13: alta rápida de proveedores desde la compra (nombre y teléfono); el resto de sus datos queda para el módulo de compras.

## Verificación y evidencia

Ejecutado el 2026-11-13 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: 53 tablas, 7 vistas, 7 triggers; 29 comprobaciones SQL de las reglas (tanda repetida, litros de más, caducidad, asadero en tanda, piezas con decimales, merma de más, bajar o borrar con consumo, ordeña protegida); una venta confirmada descuenta existencia y una cotización no.
- Migración: sobre el esquema de `main` deja el mismo esquema que `database.sql` (`pg_dump -s`) y los mismos productos; con un lote de queso previo se detiene sin cambios.
- `dotnet build`: 0 errores, 0 advertencias. `npm run build` y `check-layout` correctos.
- API real: 54 comprobaciones de los 8 criterios de aceptación y validaciones extra, sin 500. Regresión de API: Hato, Sanidad, Reproducción y Ordeña sin fallas.
- Navegador contra la API real: Quesería 42/42 (incluidos 375 px, tema oscuro y sitio público); regresión Hato 34/34, Sanidad 35/35, Reproducción 22/22 y Ordeña 24/24, sin errores de consola salvo los 409/422 esperados.
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Siguiente módulo: Matanza de cabritos (hecho, ver `slaughter-module.md`).
