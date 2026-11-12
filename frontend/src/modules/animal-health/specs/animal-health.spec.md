# Sanidad (panel)

## Alcance

Pantallas del módulo Sanidad como rutas hijas de `/panel`. Consumen la API de [`backend/Modules/AnimalHealth/specs/animal-health.spec.md`](../../../../../backend/Modules/AnimalHealth/specs/animal-health.spec.md) y, para elegir animales y lotes, la API pública del módulo Hato.

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/sanidad` | `HealthOverviewView` | Retiros vigentes y últimas visitas |
| `/panel/sanidad/eventos/nuevo` | `HealthEventFormView` | Registrar un evento para animales o un lote. Acepta `?visita=` y `?animal=` |
| `/panel/sanidad/historial` | `HealthHistoryView` | Eventos con filtros; acepta `?animal=` |
| `/panel/sanidad/visitas` | `VetVisitListView` | Visitas del veterinario y alta |
| `/panel/sanidad/visitas/:id` | `VetVisitDetailView` | Datos y pago de la visita, eventos registrados |
| `/panel/sanidad/medicamentos` | `MedicationsView` | Catálogo con retiros sugeridos |
| `/panel/sanidad/laboratorio` | `LabTestsView` | Pruebas y resultados |

La ficha del animal en Hato enlaza a su historial sanitario y a registrar un evento para ese animal.

## Flujos

1. **Visita del veterinario.** Se crea con fecha, nombre, honorarios y medicamentos (el total se muestra al capturar). Desde su detalle se registran los eventos, que toman la fecha de la visita.
2. **Evento.** Destino: animales elegidos (con filtro por especie y búsqueda por arete) o un lote completo. Tipo, diagnóstico, visita opcional y medicamentos. Al elegir un medicamento se llenan sus días de retiro sugeridos, que se pueden cambiar. Vacunación y desparasitación piden al menos un medicamento.
3. **Retiros.** El resumen muestra cada animal con retiro vigente y el primer día permitido para leche (solo cabras) y para carne.
4. **Corrección.** Un evento mal capturado se elimina desde el historial, con confirmación; deja de contar para los retiros.
5. **Medicamentos.** Alta y edición en la misma pantalla.
6. **Laboratorio.** Alta de muestra pendiente y cambio de resultado en la tabla.

## Estados

Mismos criterios que Hato: «Cargando…», vacío con siguiente paso, error de carga con «Reintentar», error al guardar sin perder lo capturado, botones con `loading`, confirmación con `UiToast`.

## Reglas de interfaz

- Tipos de evento en español: Vacunación, Desparasitación, Enfermedad, Lesión, Revisión, Otro.
- El retiro de leche de un bovino se muestra como «No aplica»: los bovinos no se ordeñan.
- Importes en pesos con dos decimales.
- Retiros vigentes con badge de advertencia; el color siempre acompaña a la fecha.

## Aceptación

1. Con la API real, crear medicamento, visita y un evento de lote desde las pantallas deja los eventos en el detalle de la visita y en el historial.
2. Elegir un medicamento llena sus días de retiro; cambiarlos se respeta al guardar.
3. Una vacunación sin medicamentos no se envía y explica por qué.
4. La cabra tratada aparece en retiros vigentes con su fecha de leche; al eliminar el evento desaparece.
5. Desde la ficha de Hato se llega al historial del animal y al registro con el animal preseleccionado.
6. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
