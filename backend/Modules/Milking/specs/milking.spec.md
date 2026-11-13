# Ordeña

## Alcance

Registro de la ordeña diaria de las chivas: fecha, total de litros y cabras ordeñadas. Cada ordeña es el lote de leche (`milk_lots`) del que saldrán los quesos. Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

Fuera de alcance: litros por cabra, calidad de la leche, elaboración del queso (módulo Quesería).

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| O1 | Una ordeña por día; la fecha no es futura y no cambia después de registrarla. | 409 / 422 |
| O2 | Total de litros mayor que 0 y hasta 2,000, con dos decimales como máximo. | 422 |
| O3 | Al menos una cabra y sin repetidas. | 400 |
| O4 | Solo cabras hembra activas y sin retiro de leche en la fecha de la ordeña. El servicio lo valida con mensaje claro y la base lo garantiza (`tg_milk_records_check`). | 422 |
| O5 | El código del lote es `LL-AAAAMMDD` y se genera solo. | — |
| O6 | Editar una ordeña permite cambiar total, notas y cabras; la lista de cabras se reemplaza (borrado lógico de las que salen). | 400 / 404 / 422 |
| O7 | Borrar una ordeña es borrado lógico de la ordeña y sus cabras; libera la fecha. | 404 |
| O8 | Si la leche se usó en una tanda de queso, la ordeña no se borra y su total no baja de los litros usados (Quesería, regla Q9; `tg_milk_lots_cheese_check`). | 409 / 422 |

## API

Base `/api/milking`. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /goats?date=` | 200 `MilkingGoat[]` | 400 |
| `GET /days?from&to` | 200 `MilkDaySummary[]` | 400 |
| `GET /days/{id}` | 200 `MilkDayDetail` | 404 |
| `POST /days` | 201 `MilkDayDetail` | 400, 409, 422 |
| `PUT /days/{id}` | 200 `MilkDayDetail` | 400, 404, 422 |
| `DELETE /days/{id}` | 204 | 404, 409 |
| `GET /summary` | 200 `MilkSummary` | — |

`GET /goats` lista las cabras hembra activas con su situación para la fecha (hoy si no se indica). `GET /days` sin rango devuelve los últimos 60 días.

### Contratos

```ts
MilkingGoat { animalId, visualTag, productionStage | null, lotCode | null, canBeMilked, milkAllowedFrom | null }
MilkDaySummary { id, code, collectionDate, totalLiters, goatCount, litersPerGoat, notes | null }
MilkDayDetail extends MilkDaySummary {
  goats: { animalId, visualTag }[],
  cheeseBatch: { id, code, litersUsed } | null   // tanda que usó esta leche
}
MilkSummary {
  lastDay: MilkDaySummary | null,
  last7Days: { days, liters, averagePerDay },
  last30Days: { days, liters, averagePerDay },
  previous30Days: { days, liters, averagePerDay }
}
```

Peticiones:

```ts
CreateMilkDay { collectionDate, totalLiters, goatIds: number[], notes? }
UpdateMilkDay { totalLiters, goatIds: number[], notes? }
```

`litersPerGoat` es el total entre las cabras ordeñadas, redondeado a dos decimales. Los periodos de `MilkSummary` cuentan hacia atrás desde hoy (hora de Coahuila); `days` es cuántos días tuvieron ordeña.

## Aceptación

1. `GET /goats` marca `canBeMilked = false` y la fecha de liberación para una cabra con retiro de leche; no lista machos, bovinos ni bajas.
2. Registrar la ordeña de hoy con 3 cabras y 7.5 litros responde 201 con código `LL-AAAAMMDD` y 2.5 litros por cabra.
3. Una segunda ordeña el mismo día responde 409.
4. Incluir una cabra con retiro de leche responde 422 con su arete y no crea nada.
5. Editar la ordeña quitando una cabra deja 2 y recalcula los litros por cabra.
6. Borrar la ordeña permite registrar otra ese día.
7. `GET /summary` suma litros y promedia por día en los periodos.
