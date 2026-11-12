# Dominio: Rancho San Carlos y La Cremería San Carlos

## Alcance

Sistema de gestión del negocio familiar en Castaños, Coahuila. Sustituye por completo el dominio escolar de la plantilla (alumnos, materias, grupos). Este proyecto deriva de `TecNM_Project_Template` y deja de ser agnóstico.

- **Rancho San Carlos:** cría de ganado bovino y caprino.
- **La Cremería San Carlos:** quesería artesanal con leche de cabra del propio hato.
- **Sitio público:** catálogo, pedido y consulta de trazabilidad.
- **Panel:** operación interna (hato, sanidad, reproducción, ordeña, quesería, ventas).

## Reglas de negocio

| # | Regla | Dónde se garantiza |
| --- | --- | --- |
| R1 | Solo hay dos especies: Bovino (1) y Caprino (2). | `ck_species_catalog` |
| R2 | El bovino es únicamente para carne y se vende vivo: becerros de unos 8 meses y 150 kg. | `species.production_purpose`, `v_calf_sale_candidates` |
| R3 | El caprino da cabritos y leche. Los cabritos se venden en pie o sacrificados y pelados (en canal). | `species.production_purpose`, `kid_slaughters` |
| R4 | Solo se ordeñan cabras hembra; los tres quesos son exclusivamente de leche de cabra. | `ck_milk_records_goats_only`, llave compuesta `(animal_id, species_id)`, `tg_milk_records_check` |
| R5 | La raza y el lote de un animal son de su misma especie. | Llaves compuestas en `animals` |
| R6 | Todo lote de queso proviene de un lote de leche. | `cheese_batches.milk_lot_id NOT NULL` |
| R7 | Un animal vivo o una canal se venden una sola vez. | `uq_order_items_animal_active`, `uq_order_items_kid_slaughter_active` |
| R8 | Solo se sacrifican cabritos; nunca bovinos. Sin despiece: la canal se vende entera. | Llave compuesta y `ck_kid_slaughters_goats_only` |
| R9 | Un animal inactivo lleva fecha de salida; un cabrito sacrificado queda `slaughtered`. | `ck_animals_exit`; el estado lo actualiza el servicio de matanza |
| R10 | No se ordeña una cabra con retiro de leche vigente. | `tg_milk_records_check` |
| R11 | No se sacrifica un cabrito con retiro de carne vigente. | `tg_kid_slaughters_check` |
| R12 | Un becerro con retiro de carne vigente no se vende. | Servicio de ventas + `v_calf_sale_candidates.meat_allowed_from` (pendiente) |
| R13 | Un lote de queso solo es de un producto tipo queso. | Servicio de quesería (pendiente) |
| R14 | Una canal se vende con el producto `CAB-CAN`; un animal vivo con `GAN-CAB` o `GAN-BEC`. | Servicio de ventas (pendiente) |

Las reglas R12 a R14 no están en la base de datos: se implementan y prueban en los servicios de cada módulo.

### Meta de venta de becerros

Edad promedio de 8 meses y peso aproximado de 150 kg. `v_calf_sale_candidates` marca `reached_sale_target` cuando un becerro alcanza cualquiera de las dos; las metas están escritas en la vista y se ajustan ahí.

## Productos

| SKU | Producto | Unidad | Precio | Presentación |
| --- | --- | --- | --- | --- |
| `QSO-CAN` | Queso Canasta de Cabra | kg | $120 MXN/kg | Rueda de 4.5 kg ($540) o 5 kg ($600) |
| `QSO-PAN` | Queso Panela Artesanal | pieza | $120 MXN | Pieza de 1 kg |
| `QSO-ASA` | Queso Asadero Artesanal | pieza | $80 MXN | Pieza de 500 g |
| `GAN-CAB` | Cabrito en pie | cabeza | Cotización | Ejemplar o lote |
| `CAB-CAN` | Cabrito en canal | pieza | Cotización | Sacrificado y pelado, canal entera |
| `GAN-BEC` | Becerro en pie | cabeza | Cotización | ~8 meses y ~150 kg |

El catálogo vive en `products` (`database.sql`) y, mientras no exista la API, se repite en `frontend/src/modules/landing/data/catalog.ts`. Cambiar un precio obliga a cambiar ambos.

## Módulos y orden de construcción

| Orden | Módulo | Tablas principales | Especie |
| --- | --- | --- | --- |
| 1 | Hato | `species`, `breeds`, `animal_lots`, `animals`, `weight_records` | Ambas |
| 2 | Sanidad | `vet_visits`, `health_events`, `treatments`, `lab_tests`, medicamentos en `supplies` | Ambas |
| 3 | Reproducción | `breeding_events`, `birth_events` | Ambas |
| 4 | Ordeña | `milk_lots`, `milk_records` | Caprino |
| 5 | Quesería | `products`, `cheese_batches` | Caprino |
| 6 | Matanza de cabritos | `kid_slaughters` | Caprino |
| 7 | Ventas | `customers`, `orders`, `order_items`, `invoices`, `payments`, `shipments` | Ambas |
| Después | Insumos, compras, RH, equipo, finanzas | Ver `specs/database.spec.md` | — |

Cada módulo se construye como vertical slice con su propia spec en `backend/Modules/<Feature>/specs/` y `frontend/src/modules/<feature>/specs/`.

## Decisiones

Confirmadas con la familia el 2026-11-10:

| Decisión | Resultado |
| --- | --- |
| Origen de la leche | Los tres quesos son solo de leche de cabra. |
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

Pendientes:

| Decisión | Estado |
| --- | --- |
| Precios: $120/kg canasta, $120 panela, $80 asadero | Tomados del sitio; confirmar |
| Precio de referencia de cabrito en pie, en canal y becerro | Sin definir; hoy se cotizan |
| Autenticación y roles del panel | Sin definir; la plantilla ya no trae login |

## Aceptación

1. `database.sql` se ejecuta sin errores en un PostgreSQL 15 vacío y crea 48 tablas, 6 vistas y 2 triggers en `snake_case`.
2. Registrar ordeña de un bovino, de un macho o de una cabra con retiro de leche falla en la base de datos.
3. Registrar la matanza de un bovino o de un cabrito con retiro de carne falla.
4. Asignar a un animal una raza o lote de otra especie falla.
5. Vender dos veces el mismo animal o la misma canal falla.
6. El sitio público muestra los productos y presentaciones de `products` con los mismos precios.
7. Ningún texto del sitio o del panel menciona alumnos, materias ni control escolar.
