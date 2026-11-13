# Quesería

## Alcance

Elaboración de queso fresco de cabra (canasta y panela), compra de queso asadero para reventa y existencias de queso. Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

- **Canasta y panela** son el mismo queso y salen del mismo proceso; solo cambia el molde y la presentación. Una **tanda** (`cheese_batches`) usa la leche de un solo día (`milk_lots`) y registra lo que salió por presentación (`cheese_batch_outputs`): kg de canasta y piezas de panela.
- **Asadero** no se elabora: es queso de leche de vaca que se compra a un proveedor y se revende (`cheese_purchases`). No tiene trazabilidad a la ordeña.
- **Existencias**: lo que entró (tanda o compra) menos mermas, consumo propio o regalos (`cheese_stock_adjustments`) y menos lo vendido en pedidos confirmados o entregados (`order_items`, módulo Ventas). Se consultan en `v_cheese_stock`; no hay columnas de existencia que mantener.

Fuera de alcance: insumos de quesería (cuajo, sal), costos de elaboración, venta (módulo Ventas), página pública de trazabilidad con datos reales.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| Q1 | Una tanda por lote de leche (un día). Los litros usados van de 0.01 al total de la ordeña de ese día. | 409 / 422 |
| Q2 | La tanda se elabora el día de la ordeña o después, nunca en fecha futura. Su lote de leche no cambia después de registrarla. | 422 |
| Q3 | La tanda produce al menos una presentación, sin repetir: canasta en kg (hasta 3 decimales) y panela en piezas enteras. Solo productos de tipo `cheese`. | 400 / 422 |
| Q4 | Caducidad de la tanda: después de la elaboración y a no más de 11 días. Sugerida: 7 días. | 422 |
| Q5 | Una compra es solo de queso de reventa (`resale`, hoy `QSO-ASA`), en piezas enteras, con proveedor, costo unitario y caducidad posterior a la compra (sugerida: 15 días). La fecha de compra no es futura. | 400 / 404 / 422 |
| Q6 | Una merma, consumo propio o regalo sale de una tanda (una presentación) o de una compra, y no puede pasar de lo disponible. No se edita: se elimina y se registra de nuevo. | 400 / 404 / 422 |
| Q7 | Editar una tanda o compra no puede dejar la cantidad por debajo de lo ya consumido (mermas y ventas). Quitar una presentación solo si no se ha consumido nada de ella. | 422 |
| Q8 | Eliminar una tanda o compra (borrado lógico) solo si no se ha consumido nada; si hubo mermas, se eliminan primero. | 409 |
| Q9 | En Ordeña, una ordeña cuya leche se usó en una tanda no se elimina y su total no baja de los litros usados. | 409 / 422 |
| Q10 | Códigos automáticos: tanda `QF-AAAAMMDD` (fecha de la ordeña) y compra `CQ-AAAAMMDD`; si ya existe, se agrega `-2`, `-3`… | — |

La base de datos garantiza Q1 a Q7 y Q9 (`uq_cheese_batches_milk_lot_active`, `ck_*`, `tg_cheese_batches_check`, `tg_cheese_batch_outputs_check`, `tg_cheese_purchases_check`, `tg_cheese_stock_adjustments_check`, `tg_milk_lots_cheese_check`); el servicio valida antes para dar mensajes claros.

## API

Base `/api/cheese`. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /catalog` | 200 `CheeseCatalog` | — |
| `POST /suppliers` | 201 `CheeseSupplier` | 400, 409 |
| `GET /milk-lots` | 200 `AvailableMilkLot[]` | — |
| `GET /batches?from&to` | 200 `CheeseBatch[]` | 400 |
| `GET /batches/{id}` | 200 `CheeseBatch` | 404 |
| `POST /batches` | 201 `CheeseBatch` | 400, 404, 409, 422 |
| `PUT /batches/{id}` | 200 `CheeseBatch` | 400, 404, 422 |
| `DELETE /batches/{id}` | 204 | 404, 409 |
| `GET /purchases?from&to` | 200 `CheesePurchase[]` | 400 |
| `GET /purchases/{id}` | 200 `CheesePurchase` | 404 |
| `POST /purchases` | 201 `CheesePurchase` | 400, 404, 422 |
| `PUT /purchases/{id}` | 200 `CheesePurchase` | 400, 404, 422 |
| `DELETE /purchases/{id}` | 204 | 404, 409 |
| `GET /stock?all=` | 200 `StockItem[]` | — |
| `GET /summary` | 200 `CheeseSummary` | — |
| `GET /adjustments?from&to` | 200 `StockAdjustment[]` | 400 |
| `POST /adjustments` | 201 `StockAdjustment` | 400, 404, 422 |
| `DELETE /adjustments/{id}` | 204 | 404 |

- `GET /milk-lots`: ordeñas de los últimos 30 días sin tanda, de la más reciente a la más antigua.
- `GET /batches` y `GET /purchases` sin rango: últimos 90 días. `GET /adjustments` sin rango: últimos 90 días.
- `GET /stock`: por omisión solo lo que tiene existencia; `all=true` incluye lo agotado de los últimos 90 días. Orden: caducidad más próxima primero.

### Contratos

```ts
CheeseProduct { id, sku, name, saleUnit: 'kg' | 'piece', unitPrice | null }
CheeseSupplier { id, name, phone | null }
CheeseCatalog { ownCheeses: CheeseProduct[], resaleCheeses: CheeseProduct[], suppliers: CheeseSupplier[] }
AvailableMilkLot { id, code, collectionDate, totalLiters, goatCount }
BatchOutput { outputId, productId, sku, productName, saleUnit, quantity, consumed, available }
CheeseBatch {
  id, code, milkLotId, milkLotCode, collectionDate, milkLotLiters,
  madeOn, expiresOn, litersUsed, notes | null, outputs: BatchOutput[]
}
CheesePurchase {
  id, code, supplierId, supplierName, productId, sku, productName,
  purchaseDate, expiresOn, quantity, consumed, available, unitCost, total, notes | null
}
StockItem {
  source: 'batch' | 'purchase', sourceId, parentId, code, productId, sku, productName, saleUnit,
  entryDate, expiresOn, daysToExpire, quantityIn, adjusted, sold, available
}
CheeseSummary {
  products: { productId, sku, productName, saleUnit, available }[],
  expiringSoon: number,   // con existencia y caducidad en 2 días o menos
  expired: number,        // con existencia y ya caducado
  lastBatch: CheeseBatch | null
}
StockAdjustment {
  id, adjustmentDate, reason: 'waste' | 'own_use' | 'gift', quantity, notes | null,
  source: 'batch' | 'purchase', sourceId, code, productName, saleUnit
}
```

Peticiones:

```ts
CreateSupplier { name, phone? }
CreateBatch { milkLotId, madeOn?, expiresOn?, litersUsed?, outputs: { productId, quantity }[], notes? }
UpdateBatch { madeOn, expiresOn, litersUsed, outputs: { productId, quantity }[], notes? }
SavePurchase { supplierId, productId?, purchaseDate, expiresOn?, quantity, unitCost, notes? }
CreateAdjustment { source: 'batch' | 'purchase', sourceId, adjustmentDate?, reason, quantity, notes? }
```

Valores por omisión: `madeOn` = fecha de la ordeña; `litersUsed` = total de la ordeña; `expiresOn` = elaboración + 7 días (tanda) o compra + 15 días (compra); `productId` de la compra = el único queso de reventa; `adjustmentDate` = hoy. En `sourceId` de una merma, `batch` se refiere a la presentación (`outputId`), no a la tanda. `consumed` = mermas + vendido.

## Aceptación

1. `GET /milk-lots` lista las ordeñas sin tanda; registrar la tanda de una de ellas con 9.5 kg de canasta y 12 panelas responde 201 con código `QF-AAAAMMDD`, caducidad a 7 días y la ordeña deja de aparecer.
2. Una segunda tanda de la misma ordeña responde 409; litros mayores al total, caducidad a 12 días o panela con decimales responden 422.
3. Una tanda con asadero o sin presentaciones responde 422 o 400.
4. Registrar una compra de 20 asaderos a $55 da total $1,100, caducidad a 15 días y código `CQ-AAAAMMDD`; una compra de canasta responde 422.
5. Una merma de 3 panelas deja 9 disponibles; una merma mayor a lo disponible responde 422 y no crea nada.
6. Editar la tanda a 2 panelas (con 3 en merma) responde 422; eliminar la tanda con merma responde 409; tras eliminar la merma, sí se elimina y la ordeña vuelve a estar disponible.
7. `GET /stock` y `GET /summary` reflejan entradas, mermas y caducidad.
8. En Ordeña, eliminar una ordeña con tanda responde 409 y bajar su total por debajo de lo usado responde 422.
