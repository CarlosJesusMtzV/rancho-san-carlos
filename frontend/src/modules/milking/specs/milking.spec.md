# Ordeña (panel)

## Alcance

Pantallas del módulo Ordeña como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/Milking/specs/milking.spec.md`](../../../../../backend/Modules/Milking/specs/milking.spec.md).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/ordena` | `MilkingView` | Totales, registro de la ordeña del día y ordeñas de los últimos 60 días |
| `/panel/ordena/:id` | `MilkDayView` | Editar total, notas y cabras de una ordeña, o eliminarla |

## Flujos

1. **Registrar.** Fecha (hoy por omisión), total de litros, notas y cabras. Al elegir la fecha se marcan todas las cabras que se pueden ordeñar ese día; las que tienen retiro de leche aparecen deshabilitadas con la fecha en que se liberan. Se muestra el promedio por cabra antes de guardar.
2. **Día ya registrado.** Si la fecha ya tiene ordeña, el formulario se sustituye por un aviso con enlace para editarla.
3. **Editar.** Total, notas y cabras; la fecha queda fija. Las cabras ya ordeñadas se muestran aunque después hayan salido del hato.
4. **Eliminar.** Con confirmación; deja libre el día.

## Reglas de interfaz

- Litros con «L» y hasta dos decimales.
- La tendencia compara el promedio diario de los últimos 30 días con los 30 anteriores, solo si ambos periodos tienen ordeñas.
- Una cabra con retiro nunca puede marcarse: lo explica el texto junto a ella, no solo el color.

## Aceptación

1. Con la API real: registrar la ordeña de hoy con todas las cabras disponibles, editarla quitando una y eliminarla.
2. Una cabra tratada con retiro aparece deshabilitada con su fecha de liberación.
3. Elegir una fecha con ordeña muestra el aviso con enlace para editar.
4. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
