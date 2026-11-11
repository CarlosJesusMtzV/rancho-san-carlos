# Sitio público

## Alcance

Página pública en `/`, fuera de `MainLayout`: tiene encabezado y pie propios. Porta a Vue el `index.html` original de la marca y corrige sus errores.

- Secciones: portada, propósito, productos, oficio artesanal, pilares, trazabilidad y pie.
- Catálogo con filtro por división: quesería, caprina, bovina.
- Pedido en memoria con cantidades y total; se abre en `UiDialog`.
- Consulta de trazabilidad con registros de muestra.
- Selector de tema claro/oscuro y enlace al panel (`/panel`).
- No hace peticiones a la API. Catálogo y registros viven en `data/`.

## Reglas

- Precios y SKU de `data/catalog.ts` coinciden con `products` en `database.sql` y con `specs/domain.spec.md`.
- El queso canasta se agrega por rueda (4.5 kg o 5 kg) a $120 MXN/kg. El ganado se agrega como cotización, sin precio y una sola vez por presentación.
- Cabritos: dos presentaciones, en pie (`GAN-CAB`) y en canal (`CAB-CAN`). Becerros: solo en pie (`GAN-BEC`), con edad promedio de 8 meses y peso aproximado de 150 kg.
- Los tres quesos se describen como exclusivamente de leche de cabra. Los becerros, como ganado para carne vendido vivo.
- Sin sellos ni nombres de autoridades o normas (SINIIGA, SENASICA, NOM) mientras el rancho no tenga la certificación correspondiente; el pie muestra compromisos propios.
- El pedido no se envía: «Copiar resumen» deja el texto en el portapapeles. La pantalla lo dice explícitamente.
- La trazabilidad se anuncia como demostración mientras no exista la API.
- Fotos locales en `frontend/public/img/`: `queso-canasta.jpg`, `queso-panela.jpg`, `queso-asadero.jpg`, `cabritos.jpg`, `becerros.jpg` y `hero.jpg` (opcional). Si falta una foto de producto se muestra el emblema «SC»; sin `hero.jpg` queda el patrón de semidesierto. No se enlazan imágenes de terceros.
- Clases CSS con prefijo `lp-` en `styles/landing.css`; colores y fuentes desde los tokens `--color-*`, `--brand-*` y `--font-*`. Las piezas impresas (sello y etiqueta) usan colores fijos.
- Textos públicos sin referencias internas al manual de marca (bloques, secciones, códigos hexadecimales, porcentajes de escala).

## Correcciones respecto al `index.html` original

| Problema | Corrección |
| --- | --- |
| Precio mostrado distinto al que sumaba el carrito ($120 vs 145, $120 vs 135, $80 vs 150) | Una sola fuente de precios |
| Respaldo de imágenes con URL inválida (`//https://…`) | Emblema local; sin imágenes externas |
| Código buscado insertado con `innerHTML` | Interpolación de Vue; el texto se escapa |
| Etiqueta decía 1.00 kg y el pie hablaba de ruedas de 500 g y 1 kg; el producto es de 4.5 y 5 kg | Presentaciones unificadas |
| Enlace `javascript:void(0)` para el pedido | Botón |
| El carrito no permitía cantidades y vaciaba el pedido con un `alert` | Cantidades, total y resumen copiable |
| Filtros del pie dejaban las pestañas sin estado activo | Filtro compartido con `aria-pressed` |
| Textos públicos con «Bloque 1.4», «Sección 7.1», `#5E6B3E`, «(65%)», sustrato BOPP | Retirados o reescritos |
| Sellos SINIIGA, SENASICA, NOM-051 y «Hato libre» sin certificación | Sustituidos por compromisos propios |
| Rejilla de 340 px desbordaba en móviles de 375 px | Rejilla fluida |
| Sin modo oscuro, sin `prefers-reduced-motion`, sin enlace de salto | Agregados |

## Aceptación

1. `/` muestra cinco tarjetas (tres quesos, cabritos y becerros); cada filtro deja solo las de su división.
2. Cotizar cabrito en canal dos veces deja un solo renglón «Cabrito en canal · Por cotizar»; el total no cambia.
3. Agregar rueda de 4.5 kg y pieza de asadero de 500 g da un total de $620 MXN.
4. Buscar un código inexistente muestra «Registro no encontrado» y el texto escrito aparece tal cual, sin ejecutarse.
5. Sin desbordamiento horizontal a 375 px y a 1366 px, en tema claro y oscuro.
6. Compila con `npm --prefix frontend run build`.
