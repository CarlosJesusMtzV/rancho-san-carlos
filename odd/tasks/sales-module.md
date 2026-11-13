# Módulo Ventas

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar a quién se vende queso, ganado en pie y cabrito en canal, cobrar al entregar, apartar lo encargado y anotar la factura cuando se emite.
- Incluye: clientes, ventas en el momento, encargos que apartan, entrega a domicilio del queso, entregar y cobrar, cancelar, factura por folio, resumen del mes.
- Fuera de alcance: cotizaciones, timbrado CFDI, envíos con vehículo y chofer (`shipments`), devoluciones parciales, cuentas por cobrar.
- Restricciones y decisiones pendientes: ninguna abierta.

## Requisitos y referencias

- Requisito solicitado y fuente: decisiones del usuario del 2026-11-13 (clientes fijos y compradores de ganado; pago siempre al momento; factura a veces; ganado en pie por kilo en pie; canal por kilo de canal; a veces encargan; el queso se entrega, el ganado lo recogen).
- Specs y contratos afectados: `backend/Modules/Sales/specs/sales.spec.md`, `frontend/src/modules/sales/specs/sales.spec.md`, `specs/domain.spec.md` (R7, R12, R14), `specs/database.spec.md`.

## Tareas

- [x] Esquema: entrega, notas y fecha de entrega en `orders`; ganado y canal por kilo; pago por pedido; factura sin estado de pago; `tg_order_items_check`, `tg_orders_check`; un cabrito apartado no se sacrifica; tipos de cliente.
- [x] Migración `docs/migrations/2026-11-13-06-ventas.sql`; se detiene si ya hay ventas.
- [x] Backend `/api/sales`.
- [x] Pantallas `/panel/ventas`, `/panel/ventas/nueva`, `/panel/ventas/:id`, `/panel/ventas/clientes`, menú y portada.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.

## Decisiones

- 2026-11-13: un encargo es `confirmed` y aparta queso, animales y canales; `quote` queda sin uso.
- 2026-11-13: los renglones no se editan en la base; editar un encargo reemplaza sus renglones dentro de una transacción.
- 2026-11-13: el queso caducado no se vende ni se entrega; se registra como merma en Quesería.
- 2026-11-13: un animal en pie sale del hato (`sold`) al entregarse, no al encargarse; cancelar una venta entregada lo regresa.
- 2026-11-13: la factura toma el total del pedido y los impuestos que se indiquen (0 por omisión); con factura, la venta no se cancela.

## Verificación y evidencia

Ejecutado el 2026-11-13 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: 54 tablas, 7 vistas, 10 triggers; 29 comprobaciones SQL de ventas y las de Quesería y Matanza sin fallas.
- Migración: sobre el esquema de `main` deja el mismo esquema que `database.sql` (`pg_dump -s`), con los mismos productos y tipos de cliente; con un pedido previo se detiene sin cambios.
- `dotnet build`: 0 errores, 0 advertencias. `npm run build` y `check-layout` correctos.
- API real: 54 comprobaciones de los 7 criterios de aceptación, sin 500. Regresión de API: Hato 40, Sanidad 37, Reproducción 34, Ordeña 19, Quesería 54 y Matanza 30, sin fallas.
- Navegador contra la API real: Ventas 32/32 (incluidos 375 px y tema oscuro); regresión Hato 34/34, Sanidad 35/35, Reproducción 22/22, Ordeña 24/24, Quesería 42/42 y Matanza 17/17, sin errores de consola salvo los 409/422 esperados.
- **No verificado:** Docker Compose con PostgreSQL 15.

## Estado y siguientes pasos

Terminado. Los siete módulos del orden sugerido están hechos. Siguiente: página pública de trazabilidad con datos reales y, después, insumos, compras, RH, equipo y finanzas (o retirar las tablas que no se usen).
