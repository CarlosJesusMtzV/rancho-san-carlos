# Matanza de cabritos

## Alcance

Registro de las jornadas de matanza: el día en que se llevan uno o varios cabritos a un rastro o matancero externo, cuánto se le pagó y, por cabrito, su peso vivo y su peso en canal. Cada cabrito sacrificado sale del hato (`slaughtered`) y su canal queda disponible para venderse entera (`CAB-CAN`, módulo Ventas). Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

Fuera de alcance: despiece (la canal se vende entera), venta de la canal (Ventas), caducidad de la canal.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| M1 | Solo cabritos: animales caprinos activos. Un animal se sacrifica una sola vez. | 404 / 409 / 422 |
| M2 | Sin retiro de carne vigente en la fecha de la jornada; el servicio lo explica y la base lo garantiza (`tg_kid_slaughters_check`). | 422 |
| M3 | La jornada lleva fecha (no futura, no anterior al nacimiento de ningún cabrito), nombre del rastro o matancero y al menos un cabrito. El pago total es opcional y no negativo. La fecha no cambia después. | 400 / 422 |
| M4 | Por cabrito: peso vivo y peso en canal mayores que 0, hasta 100 kg, con dos decimales; la canal no pesa más que el animal vivo. | 400 / 422 |
| M5 | Al registrar, el cabrito queda `slaughtered` con fecha de salida = fecha de la jornada. Al quitarlo de la jornada o eliminarla, vuelve a `active` sin fecha de salida. | — |
| M6 | Un cabrito cuya canal ya se vendió no se quita ni se elimina la jornada. | 409 / 422 |

## API

Base `/api/slaughter`. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /kids?date=` | 200 `SlaughterCandidate[]` | — |
| `GET /runs?from&to` | 200 `SlaughterRunSummary[]` | 400 |
| `GET /runs/{id}` | 200 `SlaughterRunDetail` | 404 |
| `POST /runs` | 201 `SlaughterRunDetail` | 400, 404, 409, 422 |
| `PUT /runs/{id}` | 200 `SlaughterRunDetail` | 400, 404, 409, 422 |
| `DELETE /runs/{id}` | 204 | 404, 409 |
| `GET /carcasses` | 200 `Carcass[]` | — |
| `GET /summary` | 200 `SlaughterSummary` | — |

- `GET /kids`: caprinos activos, del más joven al más viejo, con su retiro de carne para la fecha (hoy si no se indica).
- `GET /runs` sin rango: últimos 180 días. `GET /carcasses`: canales sin vender, la más antigua primero.

### Contratos

```ts
SlaughterCandidate { animalId, visualTag, sex, ageMonths | null, productionStage | null, lastWeightKg | null, canBeSlaughtered, meatAllowedFrom | null }
SlaughteredKid { slaughterId, animalId, visualTag, sex, ageMonths | null, liveWeightKg, carcassWeightKg, yieldPercent, sold }
SlaughterRunSummary {
  id, runDate, slaughterhouse, cost | null, kidCount, liveWeightKg, carcassWeightKg,
  yieldPercent, costPerKid | null, notes | null
}
SlaughterRunDetail extends SlaughterRunSummary { kids: SlaughteredKid[] }
Carcass { slaughterId, animalId, visualTag, runId, runDate, slaughterhouse, carcassWeightKg, daysSinceSlaughter }
SlaughterSummary {
  availableCarcasses, availableCarcassKg,
  kidsLast30Days, averageYieldPercent | null,   // últimos 180 días
  lastRun: SlaughterRunSummary | null
}
```

Peticiones:

```ts
SaveRun { runDate (solo al crear), slaughterhouse, cost?, notes?, kids: { animalId, liveWeightKg, carcassWeightKg }[] }
```

`yieldPercent` = canal ÷ peso vivo × 100, con un decimal. En `PUT` la lista de cabritos reemplaza la anterior: los que salen vuelven al hato, los nuevos se validan como en el alta.

## Aceptación

1. `GET /kids` marca `canBeSlaughtered = false` y la fecha de liberación para un cabrito con retiro de carne; no lista bovinos ni bajas.
2. Registrar una jornada con dos cabritos (18 kg vivo / 9.5 kg canal y 20 / 10.4) y $300 responde 201 con 2 cabritos, rendimiento por cabrito y $150 por cabrito; ambos quedan `slaughtered` con fecha de salida.
3. Incluir un bovino, un cabrito ya sacrificado o uno con retiro de carne responde 422 (o 409) y no guarda nada.
4. Canal más pesada que el animal vivo responde 422.
5. Editar la jornada quitando un cabrito lo devuelve al hato como activo y ajusta los totales.
6. Eliminar la jornada devuelve sus cabritos al hato; con una canal vendida responde 409.
7. `GET /carcasses` y `GET /summary` muestran las canales sin vender y el rendimiento promedio.
