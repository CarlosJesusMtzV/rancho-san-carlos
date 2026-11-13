# Dominio: Rancho San Carlos y La Cremería San Carlos

## Alcance

Sistema de gestión del negocio familiar en Castaños, Coahuila. Sustituye por completo el dominio escolar de la plantilla (alumnos, materias, grupos). Este proyecto deriva de `TecNM_Project_Template` y deja de ser agnóstico.

- **Rancho San Carlos:** cría de ganado bovino y caprino.
- **La Cremería San Carlos:** quesería artesanal: queso canasta y panela con leche de cabra del propio hato; además revende queso asadero de leche de vaca comprado a un proveedor.
- **Sitio público:** catálogo, pedido y consulta de trazabilidad.
- **Panel:** operación interna (hato, sanidad, reproducción, ordeña, quesería, ventas).

## Reglas de negocio

| # | Regla | Dónde se garantiza |
| --- | --- | --- |
| R1 | Solo hay dos especies: Bovino (1) y Caprino (2). | `ck_species_catalog` |
| R2 | El bovino es únicamente para carne y se vende vivo: becerros de unos 8 meses y 150 kg. | `species.production_purpose`, `v_calf_sale_candidates` |
| R3 | El caprino da cabritos y leche. Los cabritos se venden en pie o sacrificados y pelados (en canal). | `species.production_purpose`, `kid_slaughters` |
| R4 | Solo se ordeñan cabras hembra. Canasta y panela son de leche de cabra propia; el asadero se compra hecho (leche de vaca) y no se elabora ni se presenta como propio. | `ck_milk_records_goats_only`, llave compuesta `(animal_id, species_id)`, `tg_milk_records_check` |
| R5 | La raza y el lote de un animal son de su misma especie. | Llaves compuestas en `animals` |
| R6 | Toda tanda de queso proviene de la leche de un solo día, con una tanda por día y sin usar más litros de los ordeñados. | `cheese_batches.milk_lot_id NOT NULL`, `uq_cheese_batches_milk_lot_active`, `tg_cheese_batches_check`, `tg_milk_lots_cheese_check` |
| R7 | Un animal vivo o una canal se venden una sola vez. | `uq_order_items_animal_active`, `uq_order_items_kid_slaughter_active` |
| R8 | Solo se sacrifican cabritos; nunca bovinos. Sin despiece: la canal se vende entera. | Llave compuesta y `ck_kid_slaughters_goats_only` |
| R9 | Un animal inactivo lleva fecha de salida; un cabrito sacrificado queda `slaughtered`. | `ck_animals_exit`; el estado lo actualiza el servicio de matanza |
| R10 | No se ordeña una cabra con retiro de leche vigente ni una que ya salió del hato. Una ordeña por día. | `tg_milk_records_check`, `uq_milk_lots_collection_date_active` |
| R11 | No se sacrifica un cabrito con retiro de carne vigente. | `tg_kid_slaughters_check` |
| R12 | Un becerro con retiro de carne vigente no se vende. | Servicio de ventas + `v_calf_sale_candidates.meat_allowed_from` (pendiente) |
| R13 | Una tanda solo produce quesos propios (`cheese`) y una compra solo es de queso de reventa (`resale`); las piezas van en números enteros. | `tg_cheese_batch_outputs_check`, `tg_cheese_purchases_check` |
| R15 | Las existencias de queso no quedan negativas: mermas y ventas no pasan de lo disponible, y una tanda o compra no baja de lo ya consumido. | `v_cheese_stock`, `tg_cheese_stock_adjustments_check`, `tg_cheese_batch_outputs_check`, `tg_cheese_purchases_check`; ventas: servicio de Ventas (pendiente) |
| R14 | Una canal se vende con el producto `CAB-CAN`; un animal vivo con `GAN-CAB` o `GAN-BEC`. | Servicio de ventas (pendiente) |

Las reglas R12 a R14 no están en la base de datos: se implementan y prueban en los servicios de cada módulo.

### Meta de venta de becerros

Edad promedio de 8 meses y peso aproximado de 150 kg. `v_calf_sale_candidates` marca `reached_sale_target` cuando un becerro alcanza cualquiera de las dos; las metas están escritas en la vista y se ajustan ahí.

## Productos

| SKU | Producto | Unidad | Precio | Presentación |
| --- | --- | --- | --- | --- |
| `QSO-CAN` | Queso Canasta de Cabra | kg | $120 MXN/kg | Rueda de 4.5 kg ($540) o 5 kg ($600) |
| `QSO-PAN` | Queso Panela Artesanal | pieza | $120 MXN | Pieza de 1 kg; mismo queso que el canasta, en otro molde |
| `QSO-ASA` | Queso Asadero | pieza | $80 MXN | Pieza de 500 g; leche de vaca, de proveedor (reventa) |
| `GAN-CAB` | Cabrito en pie | cabeza | Cotización | Ejemplar o lote |
| `CAB-CAN` | Cabrito en canal | pieza | Cotización | Sacrificado y pelado, canal entera |
| `GAN-BEC` | Becerro en pie | cabeza | Cotización | ~8 meses y ~150 kg |

El catálogo vive en `products` (`database.sql`) y, mientras no exista la API, se repite en `frontend/src/modules/landing/data/catalog.ts`. Cambiar un precio obliga a cambiar ambos. El sitio presenta el asadero como queso de proveedor y de leche de vaca.

## Módulos y orden de construcción

| Orden | Módulo | Tablas principales | Especie |
| --- | --- | --- | --- |
| 1 | Hato | `species`, `breeds`, `animal_lots`, `animals`, `weight_records` | Ambas |
| 2 | Sanidad | `vet_visits`, `health_events`, `treatments`, `lab_tests`, medicamentos en `supplies` | Ambas |
| 3 | Reproducción | `breeding_events`, `pregnancy_checks`, `birth_events`, crías en `animals` | Ambas |
| 4 | Ordeña | `milk_lots`, `milk_records` | Caprino |
| 5 | Quesería | `products`, `cheese_batches`, `cheese_batch_outputs`, `cheese_purchases`, `cheese_stock_adjustments` | Caprino; asadero de proveedor |
| 6 | Matanza de cabritos | `kid_slaughters` | Caprino |
| 7 | Ventas | `customers`, `orders`, `order_items`, `invoices`, `payments`, `shipments` | Ambas |
| Después | Insumos, compras, RH, equipo, finanzas | Ver `specs/database.spec.md` | — |

Cada módulo se construye como vertical slice con su propia spec en `backend/Modules/<Feature>/specs/` y `frontend/src/modules/<feature>/specs/`.

## Decisiones

Confirmadas con la familia el 2026-11-10:

| Decisión | Resultado |
| --- | --- |
| Origen de la leche | Canasta y panela, solo de leche de cabra propia. El asadero se compra (ver Quesería, 2026-11-13). |
| Venta de cabritos | En pie o sacrificados y pelados (en canal). |
| Venta de becerros | Vivos, con edad promedio de 8 meses y peso aproximado de 150 kg. |
| Sellos del sitio | No había certificaciones oficiales. Se retiraron SINIIGA, SENASICA, NOM-051 y «Hato libre» porque el público los leería como certificaciones; se sustituyeron por compromisos propios. Si alguno se obtiene, se agrega de nuevo. |
| Número de tablas | La materia no fija mínimo ni máximo. Se conservan RH, compras, finanzas y equipo por ahora; pueden retirarse si no se usan. |

Sanidad, confirmado el 2026-11-11:

| Decisión | Resultado |
| --- | --- |
| Quién atiende | Un veterinario externo que surte y cobra los medicamentos; su nombre se escribe en cada visita (sin catálogo). |
| Pago | Por visita: honorarios y costo de medicamentos por separado. |
| Inventario de medicamentos | No se lleva. `supply_stock` queda para alimento, cuajo y empaque. |
| Retiros | Cada medicamento guarda días sugeridos de retiro de leche y carne; se pueden cambiar al aplicarlo. |
| Aplicación | Individual o a todo un lote en un solo registro. |
| Sin veterinario | La familia puede registrar eventos sin visita (por ejemplo, desparasitar). |

Reproducción, confirmado el 2026-11-11:

| Decisión | Resultado |
| --- | --- |
| Método | Solo monta natural; sin inseminación. |
| Crías | Se aretean y se dan de alta en el hato al registrar el parto. |
| Preñez | La diagnostica el veterinario en sus visitas. |

Ordeña, confirmado el 2026-11-11:

| Decisión | Resultado |
| --- | --- |
| Frecuencia | Una ordeña al día. |
| Medición | Total de litros de la ordeña; aparte, qué cabras se ordeñaron. Sin litros por cabra. |
| Lote de leche | Toda la leche del día es un lote para quesería. |

Quesería, confirmado el 2026-11-13:

| Decisión | Resultado |
| --- | --- |
| Canasta y panela | Son el mismo queso y el mismo proceso; solo cambia el molde y la presentación. Una tanda da canasta (kg) y panela (piezas). |
| Leche por tanda | Cada tanda usa la leche de un solo día; toda la leche se usa en canasta y panela. |
| Asadero | Se compra hecho a un proveedor; es de leche de vaca. Se registra cada compra (proveedor, piezas, costo) y se revende. En el sitio sigue a la venta, sin decir que es de cabra ni de elaboración propia. |
| Caducidad | Canasta y panela duran de 7 a 11 días: se sugieren 7 y se permite hasta 11. Asadero: 15 días sugeridos. |
| Inventario | Cada tanda y compra lleva su existencia; se registran mermas, consumo propio y regalos; Ventas descontará lo vendido. |

Tomadas al diseñar (cambiables sin tocar la base):

| Decisión | Valor |
| --- | --- |
| Gestación para la fecha probable de parto | Bovino 283 días; caprino 150 días |
| Crías muertas al nacer | Solo se cuentan en el parto; no se dan de alta |
| Raza y lote de la cría | Por omisión los de la madre; se pueden cambiar en el parto |
| Venta que descuenta existencias | Renglones de pedidos confirmados o entregados |
| Códigos de quesería | Tanda `QF-AAAAMMDD` (día de la ordeña); compra `CQ-AAAAMMDD` |

Pendientes:

| Decisión | Estado |
| --- | --- |
| Precios: $120/kg canasta, $120 panela, $80 asadero | Tomados del sitio; confirmar. El precio de compra del asadero se registra en cada compra. |
| Precio de referencia de cabrito en pie, en canal y becerro | Sin definir; hoy se cotizan |
| Autenticación y roles del panel | Sin definir; la plantilla ya no trae login |

## Aceptación

1. `database.sql` se ejecuta sin errores en un PostgreSQL 15 vacío y crea 53 tablas, 7 vistas y 7 triggers en `snake_case`.
2. Registrar ordeña de un bovino, de un macho o de una cabra con retiro de leche falla en la base de datos.
3. Registrar la matanza de un bovino o de un cabrito con retiro de carne falla.
4. Asignar a un animal una raza o lote de otra especie falla.
5. Vender dos veces el mismo animal o la misma canal falla.
6. El sitio público muestra los productos y presentaciones de `products` con los mismos precios.
7. Ningún texto del sitio o del panel menciona alumnos, materias ni control escolar.
