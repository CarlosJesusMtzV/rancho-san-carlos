# Reproducción (panel)

## Alcance

Pantallas del módulo Reproducción como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Reproduction/specs/reproduction.spec.md`](../../../../../backend/Modules/Reproduction/specs/reproduction.spec.md), la API pública de Hato (animales, razas, lotes) y la de Sanidad (visitas del veterinario).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/reproduccion` | `ReproductionOverviewView` | Estado de cada hembra activa, totales y diagnóstico de preñez |
| `/panel/reproduccion/montas/nueva` | `BreedingFormView` | Registrar monta; acepta `?hembra=` |
| `/panel/reproduccion/partos/nuevo` | `BirthFormView` | Registrar parto y dar de alta a las crías; acepta `?madre=` |
| `/panel/reproduccion/historial` | `ReproductionHistoryView` | Montas y partos; acepta `?hembra=` |

La ficha de una hembra en Hato enlaza a su historial reproductivo.

## Flujos

1. **Monta.** Especie, hembra (solo vacías), semental (solo machos activos), fecha. Se muestra el parto probable antes de guardar.
2. **Diagnóstico.** Desde la fila de una hembra montada o cargada: visita del veterinario (solo posteriores a la monta), resultado y notas. Si no hay visitas, enlaza a registrarlas en Sanidad.
3. **Parto.** Madre, fecha, crías muertas y, por cría viva, arete, sexo, peso y, si difieren de la madre, raza y lote. Avisa si el parto se liga a una monta (padre conocido) o no. Al guardar abre el historial de la madre con las crías enlazadas a su ficha.
4. **Historial.** Partos y montas con diagnósticos; una monta sin diagnóstico ni parto se puede eliminar.

## Reglas de interfaz

- Estados: «Montada, sin diagnóstico», «Cargada», «Vacía», «Aborto», «Parió», siempre con texto además del color.
- Parto probable con «en N días» o «vencido hace N días»; resaltado si faltan 30 días o menos.
- Los totales y filtros por especie se calculan sobre la lista de hembras activas.

## Aceptación

1. Con la API real: registrar monta, diagnosticarla cargada en una visita y registrar el parto con dos crías deja las crías en Hato con madre y padre, y la monta como «Parió».
2. Una hembra sin monta abierta muestra el aviso de padre desconocido al registrar su parto.
3. Un arete repetido en el parto muestra el error sin perder lo capturado.
4. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
