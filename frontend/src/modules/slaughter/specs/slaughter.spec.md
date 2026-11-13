# Matanza de cabritos (panel)

## Alcance

Pantallas del módulo Matanza como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Slaughter/specs/slaughter.spec.md`](../../../../../backend/Modules/Slaughter/specs/slaughter.spec.md).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/matanza` | `SlaughterOverviewView` | Canales disponibles, rendimiento y jornadas recientes |
| `/panel/matanza/nueva` | `SlaughterRunFormView` | Registrar una jornada con varios cabritos |
| `/panel/matanza/:id` | `SlaughterRunFormView` | Editar o eliminar una jornada |

## Flujos

1. **Registrar jornada.** Fecha, rastro o matancero y pago total. Se marcan los cabritos (del más joven al más viejo, con edad y último peso); los que tienen retiro de carne aparecen deshabilitados con su fecha de liberación. Por cada cabrito marcado se capturan peso vivo y peso en canal; se ve el rendimiento por cabrito, los totales y el pago por cabrito antes de guardar.
2. **Editar.** Rastro, pago, notas, pesos y cabritos; la fecha queda fija. Un cabrito con canal vendida no se puede quitar.
3. **Eliminar.** Con confirmación; los cabritos vuelven al hato.

## Reglas de interfaz

- Pesos en kg con hasta dos decimales; rendimiento con un decimal y «%».
- Un cabrito con retiro nunca puede marcarse: lo explica el texto junto a él, no solo el color.
- Desde la ficha de un cabrito sacrificado se llega a su jornada (enlace en Hato cuando exista; fuera de alcance aquí).

## Aceptación

1. Con la API real: registrar una jornada con dos cabritos, ver sus canales disponibles, editarla quitando uno y eliminarla.
2. Canal más pesada que el peso vivo muestra el error y conserva lo capturado.
3. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
