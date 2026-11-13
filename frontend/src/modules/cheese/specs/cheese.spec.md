# Quesería (panel)

## Alcance

Pantallas del módulo Quesería como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Cheese/specs/cheese.spec.md`](../../../../../backend/Modules/Cheese/specs/cheese.spec.md).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/queseria` | `CheeseOverviewView` | Existencias por producto, avisos de caducidad, tabla de existencias y registro de mermas |
| `/panel/queseria/tandas/nueva` | `CheeseBatchFormView` | Registrar una tanda de canasta y panela a partir de una ordeña |
| `/panel/queseria/tandas/:id` | `CheeseBatchFormView` | Editar o eliminar una tanda |
| `/panel/queseria/compras/nueva` | `CheesePurchaseFormView` | Registrar una compra de asadero |
| `/panel/queseria/compras/:id` | `CheesePurchaseFormView` | Editar o eliminar una compra |
| `/panel/queseria/historial` | `CheeseHistoryView` | Tandas, compras y mermas de los últimos 90 días |

## Flujos

1. **Tanda.** Se elige la ordeña (fecha, litros y cabras); se sugieren los litros del día, la elaboración ese mismo día y la caducidad a 7 días (máximo 11). Se capturan kg de canasta y piezas de panela (al menos una). Sin ordeñas disponibles, se enlaza a Ordeña.
2. **Compra de asadero.** Proveedor (lista, con alta rápida por nombre y teléfono), fecha, piezas, costo por pieza y caducidad sugerida a 15 días. Muestra el total antes de guardar. El texto aclara que el asadero es de leche de vaca y de proveedor.
3. **Merma.** Desde la fila de existencias: motivo (merma, consumo propio o regalo), cantidad (no mayor a lo disponible), fecha y notas.
4. **Editar y eliminar.** El formulario de tanda o compra muestra lo consumido; eliminar pide confirmación y el error de la API explica por qué no se puede.

## Reglas de interfaz

- Canasta en kg con hasta 3 decimales; panela y asadero en piezas.
- Caducidad: «caduca hoy», «caduca en N días», «caducó hace N días»; aviso con color y texto cuando faltan 2 días o menos, y peligro si ya caducó.
- El asadero se identifica siempre como «de proveedor» para no confundirlo con el queso propio.

## Aceptación

1. Con la API real: registrar una tanda desde una ordeña, una compra de asadero con proveedor nuevo y una merma; las existencias y totales cambian.
2. Una tanda que excede los litros de la ordeña o una merma mayor a lo disponible muestran el error en español y conservan lo capturado.
3. Eliminar una tanda con merma muestra el motivo; tras borrar la merma se elimina.
4. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
