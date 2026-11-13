# Ventas (panel)

## Alcance

Pantallas del módulo Ventas como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Sales/specs/sales.spec.md`](../../../../../backend/Modules/Sales/specs/sales.spec.md).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/ventas` | `SalesOverviewView` | Ventas del mes, encargos pendientes y lista de pedidos |
| `/panel/ventas/nueva` | `OrderFormView` | Registrar una venta en el momento o un encargo |
| `/panel/ventas/:id` | `OrderFormView` | Ver un pedido; editar un encargo; entregar y cobrar; cancelar; factura |
| `/panel/ventas/clientes` | `CustomersView` | Alta y edición de clientes |

## Flujos

1. **Venta en el momento.** Cliente (con alta rápida), fecha y renglones; forma de pago. Se guarda entregada y cobrada.
2. **Encargo.** Igual, con fecha estimada de entrega y sin pago. Aparta lo vendido. Desde el pedido: «Entregar y cobrar» (fecha y forma de pago) o «Cancelar».
3. **Renglones.** Queso: entrada con existencia (tanda o compra) y su caducidad, cantidad y precio sugerido. Ganado en pie: animal, kg en pie y precio por kg. Canal: cabrito sacrificado, kg de canal fijos y precio por kg. Se ve el total de cada renglón y del pedido.
4. **Entrega.** «Recoge el cliente» o «Entregar a domicilio» con la dirección del cliente por omisión.
5. **Factura.** En una venta entregada: folio fiscal, fecha e impuestos; se puede quitar.

## Aceptación

1. Con la API real: venta en el momento de queso, encargo de un becerro entregado después y cancelación de un encargo.
2. Más queso del disponible o un animal con retiro muestran el error en español y conservan lo capturado.
3. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
