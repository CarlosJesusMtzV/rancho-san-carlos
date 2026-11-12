# Hato (panel)

## Alcance

Pantallas del módulo Hato dentro de `MainLayout`, como rutas hijas de `/panel`. Consumen la API descrita en [`backend/Modules/Herd/specs/herd.spec.md`](../../../../../backend/Modules/Herd/specs/herd.spec.md).

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/panel/hato` | `HerdListView` | Listado con filtros y totales |
| `/panel/hato/nuevo` | `AnimalFormView` | Alta de animal |
| `/panel/hato/:id` | `AnimalDetailView` | Ficha, pesajes y salida del hato |
| `/panel/hato/:id/editar` | `AnimalFormView` | Edición |
| `/panel/hato/catalogos` | `HerdCatalogView` | Razas y lotes |
| `/panel/hato/becerros` | `CalfSaleView` | Becerros rumbo a la venta |

## Flujos

1. **Primer uso.** Sin razas registradas, el listado y el alta indican que primero se registren razas y enlazan a catálogos.
2. **Listado.** Filtros por especie (todas, bovino, caprino), estado (activos, bajas, todos), sexo, lote y búsqueda por arete. Los filtros viven en la URL para compartir y volver atrás. Paginación de 50.
3. **Alta.** Especie primero; raza, lote, etapa, padre y madre se limitan a esa especie (padre solo machos, madre solo hembras, etapas compatibles con el sexo). Al guardar se abre la ficha.
4. **Edición.** Igual que el alta, con la especie fija.
5. **Ficha.** Datos, genealogía con enlaces, historial de pesajes con alta y eliminación, y salida del hato (vendido, muerto, desecho) con confirmación. Un animal inactivo se muestra como tal y no acepta pesajes.
6. **Catálogos.** Lista y alta de razas y lotes por especie.
7. **Becerros.** Tabla de `calf-sale-candidates` con meta alcanzada, edad, último peso y retiro sanitario.

## Estados

- **Cargando:** texto «Cargando…» con `aria-busy`; nunca una tabla vacía engañosa.
- **Vacío:** mensaje que explica qué falta y el siguiente paso.
- **Error de carga:** `UiAlert` en tono de peligro con el `detail` de la API y botón «Reintentar».
- **Error al guardar:** `UiAlert` sobre el formulario con el `detail`; el formulario conserva lo escrito.
- **Envío:** botón con `loading`; no se puede enviar dos veces.
- **Éxito:** `UiToast` con confirmación breve.

## Reglas de interfaz

- Textos en español de México; códigos internos (`calf`, `sold`, `M`) se muestran con su etiqueta.
- Color por especie solo como acompañamiento: bovino `--brand-terracota`, caprino `--brand-mezquite`, siempre con su nombre escrito.
- Tablas con desplazamiento horizontal propio en pantallas angostas; sin desbordamiento de la página a 375 px.
- Fechas `DD/MM/AAAA`; pesos con «kg».
- La salida del hato pide confirmación porque deja al animal inactivo.

## Aceptación

1. Con la API simulada, `/panel/hato` muestra animales, totales por especie y responde a cada filtro actualizando la URL.
2. Sin razas, el alta muestra el aviso y el enlace a catálogos.
3. En el alta, al elegir Caprino solo aparecen razas, lotes y etapas caprinas; al elegir hembra desaparecen las etapas solo de macho.
4. Un 409 o 422 de la API aparece como mensaje legible sin perder lo capturado.
5. La ficha muestra el último peso, agrega un pesaje y lo lista primero.
6. Dar de baja pide confirmación y la ficha queda como inactiva sin formulario de pesaje.
7. Compila con `npm --prefix frontend run build`; sin desbordamiento horizontal a 375 px en claro y oscuro.
