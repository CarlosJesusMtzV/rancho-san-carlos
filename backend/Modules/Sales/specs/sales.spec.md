# Ventas

## Alcance

Clientes y pedidos de queso, ganado en pie y cabrito en canal. Un pedido es una **venta en el momento** (se registra entregada y cobrada) o un **encargo** (aparta el queso, los animales o las canales y se entrega y cobra después). Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

- Clientes fijos (restaurantes, tiendas) y compradores de ganado.
- Queso por presentación de tanda (canasta en kg, panela en piezas) o por compra de asadero, al precio del catálogo o el que se ponga.
- Ganado en pie (becerro `GAN-BEC`, cabrito `GAN-CAB`) por kilo en pie: se pesa al vender.
- Cabrito en canal (`CAB-CAN`) por kilo de canal: el peso es el registrado en la matanza.
- El pago se recibe completo al entregar; no hay cuentas por cobrar.
- Factura opcional: se anota el folio fiscal; el sistema no timbra.
- El queso se puede entregar a domicilio; el ganado lo recoge el comprador.

Fuera de alcance: cotizaciones (`quote` queda sin uso), timbrado CFDI, envíos con vehículo y chofer (`shipments`, módulo Equipo), devoluciones parciales.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| V1 | Cada pedido tiene cliente, fecha (no futura) y al menos un renglón. Un encargo puede llevar fecha estimada de entrega, no anterior al pedido. | 400 / 404 / 422 |
| V2 | Queso: solo de una entrada con existencia suficiente y sin caducar en la fecha del pedido; piezas enteras; el producto es el de la entrada. | 409 / 422 |
| V3 | Ganado en pie: animal activo de la especie del producto (becerro bovino, cabrito caprino), sin retiro de carne en la fecha del pedido y sin otro pedido vigente; kg en pie de 0.01 a 1,000. | 409 / 422 |
| V4 | Canal: de una matanza vigente, sin otro pedido vigente; los kg son los de la canal registrada. | 409 / 422 |
| V5 | Precios por unidad de 0 a 1,000,000 con dos decimales. El total es la suma de cantidad × precio (`v_order_totals`). | 422 |
| V6 | Un encargo (`confirmed`) se puede editar. Al entregarlo se registra la fecha de entrega (no anterior al pedido ni futura), el pago completo con su forma (efectivo, transferencia, tarjeta o cheque) y los animales en pie quedan `sold` con esa fecha de salida. | 409 / 422 |
| V7 | Una venta entregada no se edita. Cancelarla devuelve los animales al hato y anula el pago; con factura vigente, primero se cancela la factura. Un pedido cancelado no se reactiva. | 409 |
| V8 | Entrega a domicilio solo con dirección (por omisión la del cliente). | 400 |
| V9 | Factura: una por pedido entregado, con folio fiscal único y cliente con RFC; total = total del pedido + impuestos indicados (por omisión 0: alimentos y ganado van a tasa 0). | 409 / 422 |
| V10 | Clientes: nombre obligatorio, RFC opcional de 12 o 13 caracteres y único. | 400 / 409 |

La base garantiza V2 a V4 y V8 (`tg_order_items_check`, `ck_orders_*`); el servicio valida antes para dar mensajes claros.

## API

Base `/api/sales`. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /catalog` | 200 `SalesCatalog` | — |
| `GET /customers?search=` | 200 `Customer[]` | — |
| `POST /customers` | 201 `Customer` | 400, 404, 409 |
| `PUT /customers/{id}` | 200 `Customer` | 400, 404, 409 |
| `GET /available?date=&orderId=` | 200 `Available` | — |
| `GET /orders?status&from&to` | 200 `OrderSummary[]` | 400 |
| `GET /orders/{id}` | 200 `OrderDetail` | 404 |
| `POST /orders` | 201 `OrderDetail` | 400, 404, 409, 422 |
| `PUT /orders/{id}` | 200 `OrderDetail` | 400, 404, 409, 422 |
| `POST /orders/{id}/deliver` | 200 `OrderDetail` | 400, 404, 409, 422 |
| `POST /orders/{id}/cancel` | 200 `OrderDetail` | 404, 409 |
| `PUT /orders/{id}/invoice` | 200 `OrderDetail` | 400, 404, 409, 422 |
| `DELETE /orders/{id}/invoice` | 200 `OrderDetail` | 404 |
| `GET /summary` | 200 `SalesSummary` | — |

- `GET /available` devuelve lo que se puede vender en la fecha (hoy si no se indica); con `orderId` incluye lo que ya aparta ese encargo para poder editarlo.
- `GET /orders` sin rango: últimos 90 días; los encargos pendientes se listan siempre.

### Contratos

```ts
SalesCatalog { products: { id, sku, name, kind, saleUnit, unitPrice | null }[], customerTypes: { id, name }[] }
Customer { id, name, customerTypeId, customerTypeName, rfc | null, phone | null, email | null, address | null }
Available {
  cheese: { source: 'batch' | 'purchase', sourceId, code, productId, productName, saleUnit, available, expiresOn, unitPrice | null }[],
  animals: { animalId, visualTag, productId, productName, sex, ageMonths | null, lastWeightKg | null }[],
  carcasses: { slaughterId, visualTag, productId, carcassWeightKg, runDate }[]
}
OrderLine { id, kind: 'cheese' | 'animal' | 'carcass', productId, productName, saleUnit, source: 'batch' | 'purchase' | null, sourceId, label, quantity, unitPrice, total }
OrderSummary {
  id, customerId, customerName, orderDate, estimatedDeliveryDate | null, status: 'confirmed' | 'delivered' | 'cancelled',
  deliveryType: 'pickup' | 'delivery', deliveredOn | null, itemCount, total, invoiceFolio | null
}
OrderDetail extends OrderSummary {
  deliveryAddress | null, notes | null, items: OrderLine[],
  payment: { date, amount, method } | null,
  invoice: { fiscalFolio, issueDate, subtotal, taxes, total } | null
}
SalesSummary {
  monthTotal, monthByKind: { cheese, animal, carcass },   // ventas entregadas en el mes
  pendingOrders, dueSoon                                   // encargos; dueSoon: entrega estimada en 2 días o vencida
}
```

Peticiones:

```ts
SaveCustomer { name, customerTypeId, rfc?, phone?, email?, address? }
SaveOrder {
  customerId, orderDate, estimatedDeliveryDate?, deliveryType, deliveryAddress?, notes?,
  items: { kind, source?, sourceId, quantity?, unitPrice }[],   // quantity: kg en pie; en canal se toma de la matanza
  deliverNow?: { paymentMethod }                                   // venta en el momento: se entrega y cobra el mismo día
}
DeliverOrder { deliveredOn, paymentMethod }
SaveInvoice { fiscalFolio, issueDate, taxes? }
```

## Aceptación

1. Una venta en el momento de 2 panelas y 5.5 kg de canasta responde 201 entregada, con pago por el total y existencias descontadas.
2. Un encargo de un becerro de 152 kg a $55/kg aparta al becerro (deja de aparecer disponible) y no lo saca del hato; al entregarlo queda vendido con fecha de salida y pago de $8,360.
3. Un becerro con retiro de carne, un bovino como cabrito, una canal ya apartada o más queso del disponible responden 422 o 409 sin guardar nada.
4. Queso caducado en la fecha del pedido no se puede vender.
5. Cancelar un encargo libera lo apartado; cancelar una venta entregada devuelve el animal al hato.
6. La factura exige RFC del cliente, folio único y pedido entregado; con factura, la venta no se cancela.
7. `GET /summary` suma lo entregado en el mes por tipo y cuenta los encargos pendientes.
