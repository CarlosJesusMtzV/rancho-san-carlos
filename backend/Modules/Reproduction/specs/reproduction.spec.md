# Reproducción

## Alcance

Empadres por temporada, diagnóstico de preñez en las visitas del veterinario y partos que dan de alta a las crías en el hato. Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

El ganado anda en agostadero y no se ve la monta exacta, así que se registra un **empadre**: un semental con un grupo de hembras y la fecha en que entró el semental. Cada hembra del empadre (una fila de `breeding_events`) se diagnostica y pare por separado; el parto probable es estimado desde la entrada del semental.

Fuera de alcance: inseminación artificial, sincronización de celos, destete (se registra cambiando la etapa en Hato).

## Ciclo de cada hembra del empadre

```
pending ──diagnóstico──▶ pregnant ──parto──▶ delivered
   │                        │
   │                        └─diagnóstico aborted─▶ aborted
   └──diagnóstico open──▶ open
```

`outcome` refleja el último diagnóstico de esa hembra. Un parto la deja en `delivered`. Desde `open` o `aborted` la hembra puede entrar a un nuevo empadre.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| R1 | Un empadre es un semental activo con una o más hembras activas de su misma especie; el semental no va en la lista de hembras. | 400 / 404 / 422 |
| R2 | La fecha de entrada del semental no es futura ni anterior al nacimiento del semental o de alguna hembra. | 422 |
| R3 | Una hembra no está en dos empadres abiertos: con una participación `pending` o `pregnant` no entra a otro. | 409 |
| R4 | Fecha probable de parto = entrada del semental + gestación (bovino 283 días, caprino 150), igual para todo el empadre. | — |
| R5 | El diagnóstico va en una visita del veterinario, con fecha posterior a la entrada del semental y no posterior al parto. Una visita da un diagnóstico por hembra. | 404 / 409 / 422 |
| R6 | No se diagnostica una hembra ya `delivered`. | 409 |
| R7 | El parto es de una hembra activa, con fecha no futura. Si tiene una participación `pending` o `pregnant` se liga a la más reciente (fecha posterior a la entrada del semental, padre del empadre); si no (por ejemplo, comprada cargada), se registra sin empadre y con padre desconocido. | 409 / 422 |
| R8 | Cada cría viva necesita arete visual único, sexo y peso al nacer opcional (0.1 a 100 kg); se da de alta con especie de la madre, raza y situación por omisión de la madre, padre del empadre (si lo hay), origen `born_on_ranch` y etapa `calf` o `kid`. | 400 / 409 / 422 |
| R9 | Un parto tiene al menos una cría (viva o muerta) y como máximo 6. | 400 |
| R10 | Todo el parto se guarda o nada: si una cría falla, no se crea ni el parto ni las demás. | — |
| R11 | Se puede quitar a una hembra de un empadre solo si sigue `pending` y no tiene diagnósticos. | 409 |

## API

Base `/api/reproduction`. Un parto sin empadre deja `breedingId` y `sireTag` en nulo. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /females?speciesId&status` | 200 `FemaleStatus[]` | 400 |
| `GET /empadres?speciesId&from&to` | 200 `EmpadreItem[]` | 400 |
| `GET /breedings?femaleId&outcome&from&to` | 200 `BreedingItem[]` | 400 |
| `POST /empadres` | 201 `EmpadreItem` | 400, 404, 409, 422 |
| `DELETE /breedings/{id}` | 204 | 404, 409 |
| `POST /breedings/{id}/checks` | 201 `BreedingItem` | 400, 404, 409, 422 |
| `GET /births?damId&from&to` | 200 `BirthItem[]` | 400 |
| `POST /births` | 201 `BirthItem` | 400, 404, 409, 422 |

`status` de `/females`: `open` (sin participación abierta), `pending` (en empadre sin diagnóstico), `pregnant`, `due-soon` (cargada con parto probable en 30 días o vencido) o `all` (predeterminado). Solo hembras activas. `GET /empadres` agrupa las participaciones por temporada.

### Contratos

```ts
FemaleStatus {
  animalId, visualTag, speciesId, breedName, situation | null, ageMonths | null,
  breeding: BreedingItem | null,     // participación más reciente
  lastBirthDate | null, birthCount,
  daysToDue | null                   // si está cargada o pendiente; negativo si ya pasó
}
EmpadreItem {
  seasonId, speciesId, maleId, maleTag, startDate, notes | null,
  females: BreedingItem[]
}
BreedingItem {                       // participación de una hembra en un empadre
  id, seasonId, speciesId, femaleId, femaleTag, maleId, maleTag, startDate, expectedDueDate,
  outcome: 'pending' | 'pregnant' | 'open' | 'aborted' | 'delivered', notes | null,
  checks: { id, vetVisitId, vetName, checkDate, result, notes | null }[],
  birthId | null
}
BirthItem {
  id, damId, damTag, speciesId, breedingId | null, sireTag | null, birthDate,
  offspringCount, stillbornCount, complications | null,
  offspring: { animalId, visualTag, sex, birthWeightKg | null }[]
}
```

Peticiones:

```ts
CreateEmpadre { maleId, femaleIds: number[], startDate, notes? }
CreateCheck { vetVisitId, result: 'pregnant' | 'open' | 'aborted', notes? }
CreateBirth {
  damId, birthDate, stillbornCount?, complications?,
  offspring: { visualTag, officialTag?, sex, birthWeightKg?, breedId?, lotId? }[]
}
```

## Aceptación

1. Un empadre de un toro con dos vacas responde 201 con las dos ligadas a la temporada y parto probable a 283 días; con un macho caprino y una cabra, a 150.
2. Empadrar una vaca con un macho caprino, con otra hembra como semental, o con el semental en la lista de hembras responde 422; empadrar de nuevo una hembra `pending` responde 409; un empadre sin hembras responde 400.
3. Un diagnóstico `pregnant` en una visita deja la participación `pregnant`; otro en la misma visita responde 409; con una visita anterior a la entrada del semental responde 422.
4. `GET /females?status=pregnant` lista la vaca con su situación y `daysToDue`; `GET /empadres` la agrupa.
5. Un parto con dos crías vivas y una muerta crea 2 animales con madre, padre, especie, raza y situación de la madre, `birth_event_id` y etapa `calf`; la participación queda `delivered`.
6. Un parto con un arete repetido responde 409 y no crea nada.
7. Un parto de una hembra sin empadre abierto se registra con padre desconocido; de una hembra inactiva responde 409.
8. Tras un diagnóstico `open`, la hembra puede entrar a un nuevo empadre.
9. Quitar a una hembra con diagnóstico responde 409.
