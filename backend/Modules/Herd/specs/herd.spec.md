# Hato

## Alcance

Registro de los animales del rancho, sus razas, lotes de manejo y pesajes. Es la base de todos los módulos siguientes: sanidad, reproducción, ordeña, matanza y ventas parten de un animal registrado aquí.

- Catálogos: especies (fijas), razas y lotes por especie.
- Animales: alta, edición, consulta, filtros y salida del hato (vendido, muerto o desechado).
- Pesajes: historial por animal.
- Becerros para venta: consulta de `v_calf_sale_candidates`.

Fuera de alcance: autenticación (las columnas `created_by`, `updated_by` y `deleted_by` quedan en nulo hasta que exista), instalaciones, partos (los registra Reproducción) y matanza (`slaughtered` lo asigna el módulo de Matanza).

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| H1 | El arete visual es obligatorio, se guarda en mayúsculas sin espacios sobrantes y es único en todo el hato, incluidos animales que ya salieron. | 409 |
| H2 | El arete oficial es opcional y único. | 409 |
| H3 | La especie no cambia después del alta. | No editable |
| H4 | La raza y el lote son de la misma especie que el animal. | 422 |
| H5 | El sexo es `M` o `F`. | 400 |
| H6 | La fecha de nacimiento no es futura. | 422 |
| H7 | La etapa productiva pertenece al catálogo de su especie y es compatible con el sexo (tabla de etapas). | 422 |
| H8 | El padre es un macho y la madre una hembra de la misma especie; ninguno es el propio animal. | 422 |
| H9 | Solo un animal activo recibe pesajes o se da de baja. | 409 |
| H10 | La fecha de salida y la de pesaje no son futuras ni anteriores al nacimiento. | 422 |
| H11 | Un pesaje por animal y día. Registrar de nuevo una fecha eliminada la reactiva con los datos nuevos. | 409 |
| H12 | El peso está entre 0.1 y 1500 kg. | 422 |
| H13 | Las salidas permitidas aquí son `sold`, `dead` y `culled`. | 400 |
| H14 | Raza única por especie; código de lote único. | 409 |

Las reglas de base de datos (llaves compuestas, triggers) siguen aplicando; si una falla, la API responde con el mismo formato de error.

### Etapas productivas

| Especie | Código | Etiqueta | Sexo |
| --- | --- | --- | --- |
| Bovino | `calf` | Becerro / becerra | M, F |
| Bovino | `steer` | Novillo | M |
| Bovino | `heifer` | Vaquilla | F |
| Bovino | `cow` | Vaca | F |
| Bovino | `bull` | Toro | M |
| Caprino | `kid` | Cabrito / cabrita | M, F |
| Caprino | `doeling` | Primala | F |
| Caprino | `milking_doe` | Cabra en ordeña | F |
| Caprino | `dry_doe` | Cabra seca | F |
| Caprino | `buck` | Semental | M |

## API

Base: `/api/herd`. JSON en camelCase; fechas `YYYY-MM-DD`; pesos en kg con hasta 2 decimales.

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /catalog` | 200 `HerdCatalog` | — |
| `POST /breeds` | 201 `BreedItem` | 400, 409, 422 |
| `POST /lots` | 201 `LotItem` | 400, 409, 422 |
| `GET /animals` | 200 `AnimalPage` | 400 |
| `GET /animals/{id}` | 200 `AnimalDetail` | 404 |
| `POST /animals` | 201 `AnimalDetail` + `Location` | 400, 409, 422 |
| `PUT /animals/{id}` | 200 `AnimalDetail` | 400, 404, 409, 422 |
| `POST /animals/{id}/exit` | 200 `AnimalDetail` | 400, 404, 409, 422 |
| `POST /animals/{id}/weights` | 201 `WeightItem` | 400, 404, 409, 422 |
| `DELETE /animals/{id}/weights/{weightId}` | 204 | 404 |
| `GET /calf-sale-candidates` | 200 `CalfSaleCandidate[]` | — |

### Filtros de `GET /animals`

| Parámetro | Valores | Predeterminado |
| --- | --- | --- |
| `speciesId` | 1, 2 | Todas |
| `lotId` | id de lote | Todos |
| `sex` | `M`, `F` | Ambos |
| `status` | `active`, `inactive`, `all` | `active` |
| `search` | Texto en arete visual u oficial | — |
| `page` | ≥ 1 | 1 |
| `pageSize` | 1 a 200 | 50 |

Orden: especie y arete visual.

### Contratos

```ts
HerdCatalog { species: { id, name, productionPurpose }[]; breeds: BreedItem[]; lots: LotItem[] }
BreedItem { id, speciesId, name, purpose | null }
LotItem { id, speciesId, code, description | null, activeAnimals }

AnimalPage { items: AnimalSummary[]; total, page, pageSize }
AnimalSummary {
  id, visualTag, officialTag | null, speciesId, speciesName, breedName, lotId | null, lotCode | null,
  sex, birthDate | null, ageMonths | null, productionStage | null, lifecycleStatus,
  lastWeightKg | null, lastWeighedOn | null
}
AnimalDetail extends AnimalSummary {
  breedId, origin, birthWeightKg | null, exitDate | null, geneticsNotes | null,
  sire: { id, visualTag } | null, dam: { id, visualTag } | null, weights: WeightItem[]
}
WeightItem { id, weighedOn, weightKg, notes | null }
CalfSaleCandidate {
  animalId, visualTag, officialTag | null, sex, lotCode | null, birthDate, ageMonths,
  lastWeightKg | null, lastWeighedOn | null, meatAllowedFrom | null, reachedSaleTarget
}
```

Peticiones:

```ts
CreateAnimal { visualTag, officialTag?, speciesId, breedId, lotId?, sex, birthDate?, productionStage?,
  origin: 'born_on_ranch' | 'purchased', birthWeightKg?, sireId?, damId?, geneticsNotes? }
UpdateAnimal  // igual que CreateAnimal sin speciesId
AnimalExit { status: 'sold' | 'dead' | 'culled', exitDate }
CreateWeight { weighedOn, weightKg, notes? }
CreateBreed { speciesId, name, purpose? }
CreateLot { speciesId, code, description? }
```

`ageMonths` son meses cumplidos a la fecha de consulta. `weights` se ordena del más reciente al más antiguo.

### Errores

`application/problem+json` con `status`, `title` y `detail` en español; el frontend muestra `detail`.

| Origen | Estado |
| --- | --- |
| Datos con formato inválido | 400 |
| Recurso inexistente | 404 |
| Duplicado (H1, H2, H11, H14) o animal inactivo (H9) | 409 |
| Regla de negocio (H4, H6 a H8, H10, H12) | 422 |
| Llave única de PostgreSQL (`23505`) | 409 |
| `CHECK` o trigger de PostgreSQL (`23514`); el mensaje del trigger se devuelve tal cual | 422 |
| Llave foránea de PostgreSQL (`23503`) | 422 |
| Cualquier otro | 500 con mensaje genérico; el detalle solo va al log |

## Aceptación

1. Con la base recién creada, `GET /catalog` devuelve las 2 especies y listas vacías de razas y lotes.
2. Crear raza «Charolais» para Bovino responde 201; repetirla responde 409.
3. Crear un bovino con raza caprina responde 422 y no crea nada.
4. Crear `sc-bec-214` guarda `SC-BEC-214`; crear otro con el mismo arete responde 409.
5. Etapa `milking_doe` en un bovino o `bull` en una hembra responde 422.
6. Registrar dos pesajes en fechas distintas: la ficha y el listado muestran el más reciente.
7. Un segundo pesaje en la misma fecha responde 409; tras eliminar el primero, registrar esa fecha responde 201.
8. Dar de baja como `dead` deja el animal fuera de `status=active`, visible con `status=inactive`; un pesaje posterior responde 409.
9. `GET /calf-sale-candidates` incluye un becerro de 8 meses o de 150 kg con `reachedSaleTarget = true`.
10. Un error de PostgreSQL no se expone como excepción cruda.
