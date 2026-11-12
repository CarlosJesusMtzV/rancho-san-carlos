# Reproducción

## Alcance

Montas naturales, diagnóstico de preñez en las visitas del veterinario y partos que dan de alta a las crías en el hato. Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

Fuera de alcance: inseminación artificial, sincronización de celos, destete (se registra cambiando la etapa en Hato).

## Ciclo de una monta

```
pending ──diagnóstico──▶ pregnant ──parto──▶ delivered
   │                        │
   │                        └─diagnóstico aborted─▶ aborted
   └──diagnóstico open──▶ open
```

`outcome` refleja el último diagnóstico. Un parto deja la monta en `delivered`. Desde `open` o `aborted` la hembra puede volver a montarse.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| R1 | La monta es entre una hembra y un macho activos de la misma especie. | 422 / 409 |
| R2 | La fecha de monta no es futura ni anterior al nacimiento de ninguno. | 422 |
| R3 | Una hembra no tiene dos montas abiertas: con una `pending` o `pregnant` no se registra otra. | 409 |
| R4 | Fecha probable de parto = fecha de monta + gestación (bovino 283 días, caprino 150). | — |
| R5 | El diagnóstico va en una visita del veterinario, con fecha de visita posterior a la monta y no posterior al parto. Una visita da un diagnóstico por monta. | 404 / 409 / 422 |
| R6 | No se diagnostica una monta ya `delivered`. | 409 |
| R7 | El parto es de una hembra activa, con fecha no futura. Si tiene monta `pending` o `pregnant` se liga a la más reciente (fecha posterior a la monta, padre de la monta); si no (por ejemplo, comprada cargada), se registra sin monta y con padre desconocido. | 409 / 422 |
| R8 | Cada cría viva necesita arete visual único, sexo y peso al nacer opcional (0.1 a 100 kg); se da de alta con especie de la madre, raza y lote por omisión de la madre, padre de la monta (si la hay), origen `born_on_ranch` y etapa `calf` o `kid`. | 400 / 409 / 422 |
| R9 | Un parto tiene al menos una cría (viva o muerta) y como máximo 6. | 400 |
| R10 | Todo el parto se guarda o nada: si una cría falla, no se crea ni el parto ni las demás. | — |
| R11 | Una monta se puede borrar solo si sigue `pending` y no tiene diagnósticos. | 409 |

## API

Base `/api/reproduction`. Un parto sin monta deja `breedingId` y `sireTag` en nulo. Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /females?speciesId&status` | 200 `FemaleStatus[]` | 400 |
| `GET /breedings?femaleId&outcome&from&to` | 200 `BreedingItem[]` | 400 |
| `POST /breedings` | 201 `BreedingItem` | 400, 404, 409, 422 |
| `DELETE /breedings/{id}` | 204 | 404, 409 |
| `POST /breedings/{id}/checks` | 201 `BreedingItem` | 400, 404, 409, 422 |
| `GET /births?damId&from&to` | 200 `BirthItem[]` | 400 |
| `POST /births` | 201 `BirthItem` | 400, 404, 409, 422 |

`status` de `/females`: `open` (sin monta abierta), `pending` (montada sin diagnóstico), `pregnant`, `due-soon` (cargada con parto probable en 30 días o vencido) o `all` (predeterminado). Solo hembras activas.

### Contratos

```ts
FemaleStatus {
  animalId, visualTag, speciesId, breedName, lotCode | null, ageMonths | null,
  breeding: BreedingItem | null,     // monta más reciente
  lastBirthDate | null, birthCount,
  daysToDue | null                   // si está cargada o pendiente; negativo si ya pasó
}
BreedingItem {
  id, speciesId, femaleId, femaleTag, maleId, maleTag, matingDate, expectedDueDate,
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
CreateBreeding { femaleId, maleId, matingDate, notes? }
CreateCheck { vetVisitId, result: 'pregnant' | 'open' | 'aborted', notes? }
CreateBirth {
  damId, birthDate, stillbornCount?, complications?,
  offspring: { visualTag, officialTag?, sex, birthWeightKg?, breedId?, lotId? }[]
}
```

## Aceptación

1. Montar una vaca con un toro responde 201 con parto probable a 283 días; con una cabra, a 150.
2. Montar una vaca con un macho caprino o con otra hembra responde 422; montar de nuevo una vaca `pending` responde 409.
3. Un diagnóstico `pregnant` en una visita deja la monta `pregnant`; otro diagnóstico en la misma visita responde 409; con una visita anterior a la monta responde 422.
4. `GET /females?status=pregnant` lista la vaca con `daysToDue`.
5. Un parto con dos crías vivas y una muerta crea 2 animales con madre, padre, especie, raza y lote de la madre, `birth_event_id` y etapa `calf`; la monta queda `delivered`.
6. Un parto con un arete repetido responde 409 y no crea nada.
7. Un parto de una hembra sin monta abierta se registra sin monta y con padre desconocido; de una hembra inactiva responde 409.
8. Una monta `open` permite registrar una nueva monta.
9. Borrar una monta con diagnóstico responde 409.
