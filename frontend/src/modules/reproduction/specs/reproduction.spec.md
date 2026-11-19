# Reproducción (panel)

## Alcance

Pantallas del módulo Reproducción como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Reproduction/specs/reproduction.spec.md`](../../../../../backend/Modules/Reproduction/specs/reproduction.spec.md), la API pública de Hato (animales, razas, lotes) y la de Sanidad (visitas del veterinario).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/reproduccion` | `ReproductionOverviewView` | Estado de cada hembra activa, totales y diagnóstico de preñez |
| `/panel/reproduccion/empadres/nuevo` | `EmpadreFormView` | Registrar empadre (un semental con varias hembras); acepta `?hembra=` |
| `/panel/reproduccion/partos/nuevo` | `BirthFormView` | Registrar parto y dar de alta a las crías; acepta `?madre=` |
| `/panel/reproduccion/historial` | `ReproductionHistoryView` | Empadres y partos; acepta `?hembra=` |

La ficha de una hembra en Hato enlaza a su historial reproductivo.

## Flujos

1. **Empadre.** Especie, semental (solo machos activos), fecha de entrada del semental y una o varias hembras vacías marcadas con casillas. Se muestra el parto probable estimado antes de guardar.
2. **Diagnóstico.** Desde la fila de una hembra en empadre o cargada: visita del veterinario (solo posteriores a la entrada del semental), resultado y notas. Si no hay visitas, enlaza a registrarlas en Sanidad.
3. **Parto.** Madre, fecha, crías muertas y, por cría viva, arete, sexo, peso y, si difieren de la madre, raza y situación. Avisa si el parto se liga a un empadre (padre conocido) o no. Al guardar abre el historial de la madre con las crías enlazadas a su ficha.
4. **Historial.** Partos y empadres con diagnósticos; se puede quitar a una hembra de un empadre si sigue sin diagnóstico ni parto.

## Reglas de interfaz

- Estados: «En empadre, sin diagnóstico», «Cargada», «Vacía», «Aborto», «Parió», siempre con texto además del color.
- Parto probable con «en N días» o «vencido hace N días»; resaltado si faltan 30 días o menos.
- Los totales y filtros por especie se calculan sobre la lista de hembras activas.

## Aceptación

1. Con la API real: registrar un empadre, diagnosticar cargada una hembra en una visita y registrar su parto con dos crías deja las crías en Hato con madre y padre, y la hembra como «Parió».
2. Una hembra sin empadre abierto muestra el aviso de padre desconocido al registrar su parto.
3. Un arete repetido en el parto muestra el error sin perder lo capturado.
4. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
