# Sanidad

## Alcance

Registro sanitario del hato: visitas del veterinario, eventos (vacunas, desparasitaciones, enfermedades, lesiones, revisiones), tratamientos con sus retiros de leche y carne, catálogo de medicamentos y pruebas de laboratorio.

Decisiones de producto en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones): el veterinario es externo y cobra por visita (honorarios y medicamentos por separado); no hay inventario de medicamentos; la familia puede registrar eventos sin visita; se puede aplicar a un lote completo.

Fuera de alcance: pago de la visita como gasto contable (módulo Finanzas), inventario de insumos, autenticación.

La ruta base es `/api/animal-health` porque `/api/health` es el health check del backend.

## Reglas

| # | Regla | Error |
| --- | --- | --- |
| S1 | Un evento se registra para uno o varios animales (`animalIds`) o para todos los activos de un lote (`lotId`), nunca ambos. Máximo 500 animales por registro. | 400 |
| S2 | Solo animales activos reciben eventos. Un lote sin animales activos no genera nada. | 409 / 422 |
| S3 | La fecha del evento no es futura ni anterior al nacimiento de ningún animal incluido. | 422 |
| S4 | Si el evento pertenece a una visita, su fecha es la de la visita. | 422 |
| S5 | Vacunación y desparasitación llevan al menos un tratamiento. Máximo 10 tratamientos por evento. | 422 / 400 |
| S6 | Cada tratamiento usa un medicamento del catálogo (tipo `medicine` o `vaccine`) y una dosis de hasta 60 caracteres. | 422 / 400 |
| S7 | Los días de retiro de leche y carne son de 0 a 365; si no se envían, se toman los sugeridos del medicamento. | 400 |
| S8 | No se duplica un evento del mismo tipo, para el mismo animal y el mismo día. Evita el doble envío. | 409 |
| S9 | Borrar un evento es borrado lógico del evento y sus tratamientos; deja de contar para los retiros. | — |
| S10 | La visita lleva fecha no futura y nombre del veterinario (hasta 120). Honorarios y medicamentos de 0 a 1,000,000 con dos decimales. | 400 / 422 |
| S11 | No se cambia la fecha de una visita que ya tiene eventos. Las visitas no se borran: conservan lo pagado. | 409 |
| S12 | Medicamento con nombre comercial único (sin distinguir mayúsculas), tipo de sanidad y retiros de 0 a 365. | 409 / 422 / 400 |
| S13 | Prueba de laboratorio para un animal registrado, con fecha de muestra no futura ni anterior al nacimiento y tipo de prueba (hasta 40). El resultado inicia `pending` y puede cambiar a `negative`, `positive` o `suspect`. | 400 / 404 / 422 |

Los triggers de la base (`tg_milk_records_check`, `tg_kid_slaughters_check`) consumen estos retiros: una cabra con retiro de leche no se puede ordeñar y un cabrito con retiro de carne no se puede sacrificar.

## API

| Método y ruta | Éxito | Errores |
| --- | --- | --- |
| `GET /catalog` | 200 `HealthCatalog` | — |
| `POST /medications` | 201 `MedicationItem` | 400, 409, 422 |
| `PUT /medications/{id}` | 200 `MedicationItem` | 400, 404, 409, 422 |
| `GET /visits?from&to` | 200 `VetVisitSummary[]` | 400 |
| `GET /visits/{id}` | 200 `VetVisitDetail` | 404 |
| `POST /visits` | 201 `VetVisitDetail` | 400, 422 |
| `PUT /visits/{id}` | 200 `VetVisitDetail` | 400, 404, 409, 422 |
| `GET /events?animalId&lotId&visitId&type&from&to&page&pageSize` | 200 `HealthEventPage` | 400 |
| `POST /events` | 201 `CreatedEvents` | 400, 404, 409, 422 |
| `DELETE /events/{id}` | 204 | 404 |
| `GET /withdrawals` | 200 `ActiveWithdrawal[]` | — |
| `GET /lab-tests?animalId&result` | 200 `LabTestItem[]` | 400 |
| `POST /lab-tests` | 201 `LabTestItem` | 400, 404, 409, 422 |
| `PUT /lab-tests/{id}/result` | 200 `LabTestItem` | 400, 404 |

Listados de visitas y eventos: del más reciente al más antiguo. `from` y `to` son fechas inclusivas. Eventos paginados de 50 en 50 (máximo 200).

### Contratos

```ts
HealthCatalog { supplyTypes: { id, name, category }[]; medications: MedicationItem[] }
MedicationItem { id, supplyTypeId, supplyTypeName, commercialName, activeIngredient | null,
  defaultMilkWithdrawalDays, defaultMeatWithdrawalDays }

VetVisitSummary { id, visitDate, vetName, serviceFee, medicationCost, total, notes | null, eventCount, animalCount }
VetVisitDetail extends VetVisitSummary { events: HealthEventItem[] }

HealthEventItem {
  id, animalId, visualTag, speciesId, eventDate, eventType, diagnosis | null,
  vetVisitId | null, vetName | null,
  treatments: { id, supplyId, medicationName, dose, milkWithdrawalDays, meatWithdrawalDays }[],
  milkAllowedFrom | null, meatAllowedFrom | null      // fecha del evento + mayor retiro; null si es 0
}
HealthEventPage { items: HealthEventItem[]; total, page, pageSize }
CreatedEvents { created, events: HealthEventItem[] }

ActiveWithdrawal { animalId, visualTag, speciesId, lotCode | null, milkAllowedFrom | null, meatAllowedFrom | null }

LabTestItem { id, animalId, visualTag, sampleDate, testType, labName | null,
  result: 'pending' | 'negative' | 'positive' | 'suspect' }
```

Peticiones:

```ts
MedicationInput { supplyTypeId, commercialName, activeIngredient?, defaultMilkWithdrawalDays, defaultMeatWithdrawalDays }
VetVisitInput { visitDate, vetName, serviceFee, medicationCost, notes? }
CreateEvents {
  animalIds?: number[]; lotId?: number;
  eventDate, eventType: 'vaccination' | 'deworming' | 'illness' | 'injury' | 'checkup' | 'other',
  diagnosis?, vetVisitId?,
  treatments: { supplyId, dose, milkWithdrawalDays?, meatWithdrawalDays? }[]
}
CreateLabTest { animalId, sampleDate, testType, labName? }
LabTestResult { result }
```

`*AllowedFrom` es el primer día en que la leche puede ir a quesería o el animal puede sacrificarse; coincide con la vista `v_active_withdrawals` y con los triggers. Errores con el formato común descrito en [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores).

## Aceptación

1. `GET /catalog` devuelve los tipos Medicamento, Vacuna y Desparasitante sin medicamentos; crear «Oxitetraciclina» con retiros 4 (leche) y 28 (carne) responde 201; repetirla en minúsculas responde 409.
2. Crear una visita con honorarios 500 y medicamentos 320.50 devuelve total 820.50.
3. Una vacunación sin tratamientos responde 422.
4. Un evento con visita y fecha distinta a la visita responde 422.
5. Aplicar un desparasitante a un lote con 3 activos y 1 baja crea 3 eventos; repetirlo responde 409 sin crear nada.
6. Un tratamiento sin días de retiro usa los del medicamento; con días enviados usa esos.
7. Tras tratar una cabra con 4 días de retiro de leche, aparece en `GET /withdrawals` y la base rechaza su ordeña hasta la fecha indicada.
8. Borrar ese evento la quita de `GET /withdrawals`.
9. Cambiar la fecha de una visita con eventos responde 409; cambiar el veterinario o los montos responde 200.
10. Registrar una prueba de laboratorio queda `pending`; cambiarla a `negative` responde 200; un resultado inválido responde 400.
