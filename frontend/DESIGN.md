# Sistema de Diseño: Rancho San Carlos y La Cremería San Carlos

Guía visual del sitio público y del panel. Los valores salen de `src/shared/styles/global.css`: si cambia un token, se actualiza aquí en el mismo cambio. Las vistas usan tokens, nunca colores escritos a mano (salvo las piezas impresas del sitio público, ver sección 0).

## 0. Identidad Rancho San Carlos

### Paleta de marca

| Nombre | Hex | Pantone | Token | Uso |
| --- | --- | --- | --- | --- |
| Negro Herradura | `#1A1918` | Black C | `--brand-ink`, `--color-primary` (claro) | Texto, botones primarios, encabezado y pie |
| Blanco Cuajada | `#F7F4EC` | 7527 U | `--brand-cream`, `--color-bg-main` (claro) | Fondo principal |
| Terracota Sierra | `#B05A3E` | 7523 U | `--brand-terracota`, `--color-primary` (oscuro) | División bovina, foco |
| Ocre Canasta | `#D19A36` | 7556 U | `--brand-ocre`, `--color-accent` | Quesería, acentos |
| Verde Mezquite | `#5E6B3E` | 7749 U | `--brand-mezquite` | División caprina |
| Gris Caliza | `#444B50` | 432 U | `--brand-caliza`, `--color-text-secondary` | Texto secundario, interfaz |
| Cobre Paila | `#B87333` | 876 C | `--brand-cobre` | Acabado especial |

En tema oscuro los tokens `--brand-*` de división se aclaran (`#D07A5C`, `#E0B45C`, `#9CAB74`) para mantener contraste sobre fondo `#141312`. Los valores exactos de cada token están en `src/shared/styles/global.css`.

### Color por división

| División | Token | Dónde |
| --- | --- | --- |
| Quesería | `--brand-ocre` | Tarjetas, pestañas y fichas de queso |
| Caprina | `--brand-mezquite` | Cabritos, ordeña |
| Bovina | `--brand-terracota` | Becerros |

El color nunca es la única señal: cada división lleva además su etiqueta de texto.

### Tipografía

| Rol | Fuente | Token |
| --- | --- | --- |
| Identidad (logotipo, títulos de tarjeta) | Cinzel 600/700 | `--font-identity` |
| Titulares | Playfair Display 600/700 | `--font-display` |
| Cuerpo e interfaz | Plus Jakarta Sans 300 a 700 | `--font-body` |
| Acento manuscrito | Cormorant Garamond itálica | `--font-accent` |

Se cargan desde Google Fonts en `frontend/index.html`, con Georgia y fuentes del sistema como respaldo.

### Patrones y piezas

- **Canasta:** rejilla diagonal en ocre a baja opacidad para fondos claros (`.lp-pattern-basket`).
- **Semidesierto:** punteado sobre Negro Herradura para secciones oscuras (`.lp-pattern-desert`).
- **Logotipo:** horizontal en el sitio público (`BrandLogo.vue`); isotipo «SC» en el panel (`shared/assets/rancho-san-carlos-mark.svg`).
- **Sello circular y etiqueta:** representan piezas impresas y usan colores fijos en ambos temas.

---

## 1. Carácter

Ganadería y quesería familiar del semidesierto coahuilense. La interfaz se siente cálida, sobria y de oficio: papel crema, tinta casi negra, ocre de canasta y terracota de sierra. Nada de brillos ni degradados llamativos; el color se reserva para identificar divisiones y estados.

- **Sitio público (`/`):** editorial y de marca. Titulares en Playfair Display, logotipo y títulos de tarjeta en Cinzel, patrones de canasta y semidesierto, fotografía de producto.
- **Panel (`/panel`):** herramienta de trabajo diario. Densidad media, tablas legibles, formularios claros, mismos tokens que el sitio pero sin ornamento.
- **Modo oscuro:** fondo de noche de rancho (`#141312`), tarjetas cálidas (`#22201E`) y terracota como color primario, para que siga sintiéndose de la misma marca.

---

## 2. Tokens de color

### 2.1 Primario y acento

| Token | Claro | Oscuro | Uso |
| --- | --- | --- | --- |
| `--color-primary` | `#1A1918` | `#B05A3E` | Botones principales, enlaces activos |
| `--color-primary-hover` | `#33302D` | `#C46A4C` | Hover del primario |
| `--color-primary-dark` | `#0F0E0D` | `#2A1A14` | Superficies de máximo contraste |
| `--color-primary-subtle` | `#EFECE3` | `#2E211B` | Fondo de elementos seleccionados |
| `--color-primary-tint` | `#E9E4D6` | `#33261F` | Tinte de filas o chips activos |
| `--color-accent` | `#D19A36` | `#E0B45C` | Ocre Canasta: acentos, barra superior de métricas |
| `--color-accent-light` | `#F8EFD9` | `#2B2416` | Fondo suave de acento |

En claro el primario es Negro Herradura; en oscuro pasa a Terracota Sierra, porque un primario negro desaparece sobre fondo oscuro.

### 2.2 Superficies y bordes

| Token | Claro | Oscuro | Uso |
| --- | --- | --- | --- |
| `--color-bg-main` | `#F7F4EC` | `#141312` | Lienzo de la página |
| `--color-bg-light` | `#EFECE3` | `#1A1918` | Barra lateral, encabezados de tabla |
| `--color-surface-white` | `#FFFFFF` | `#22201E` | Tarjetas |
| `--color-border` | `#E3DDCF` | `#33302C` | Borde de tarjetas y divisores |
| `--color-border-light` | `#EEE9DD` | `#2A2825` | Divisores internos |
| `--color-border-medium` | `#CFC7B5` | `#45413B` | Bordes de controles |
| `--color-border-focus` | `#B05A3E` | `#E0B45C` | Contorno de foco |
| `--color-header-bg` | `#1A1918` | `#0F0E0D` | Encabezado del panel |
| `--color-footer-bg` | `#1A1918` | `#0B0A0A` | Pie del panel |

### 2.3 Texto

| Token | Claro | Oscuro | Uso |
| --- | --- | --- | --- |
| `--color-text-primary` | `#1A1918` | `#F7F4EC` | Títulos y datos |
| `--color-text-secondary` | `#444B50` | `#B9B3A6` | Texto de apoyo, etiquetas |
| `--color-text-muted` | `#77756F` | `#8A857B` | Pistas y marcadores de posición |
| `--color-text-inverse` | `#FFFFFF` | `#141312` | Texto sobre primario |

### 2.4 Estados

Los estados usan la escala semántica de la plantilla, ajustada para contraste en ambos temas. El color siempre acompaña a un texto («Activo», «Lista para venta»), nunca lo sustituye.

| Estado | Uso en el rancho | Claro (fondo / texto) | Oscuro (fondo / texto) |
| --- | --- | --- | --- |
| Éxito | Activo, meta alcanzada, guardado | `#D1FAE5` / `#065F46` | `#0C3825` / `#6EE7B7` |
| Advertencia | Baja, retiro sanitario, pendiente | `#FEF3C7` / `#92400E` | `#3D2509` / `#FCD34D` |
| Peligro | Muerto, error al guardar, acción destructiva | `#FEE2E2` / `#991B1B` | `#401418` / `#FCA5A5` |
| Información | En crecimiento, avisos neutros | `#E0F2FE` / `#075985` | `#132E52` / `#93C5FD` |

### 2.5 Marca y división

| Token | Claro | Oscuro | Uso |
| --- | --- | --- | --- |
| `--brand-ink` | `#1A1918` | `#1A1918` | Negro Herradura |
| `--brand-cream` | `#F7F4EC` | `#F7F4EC` | Blanco Cuajada |
| `--brand-terracota` | `#B05A3E` | `#D07A5C` | División bovina |
| `--brand-ocre` | `#D19A36` | `#E0B45C` | Quesería |
| `--brand-mezquite` | `#5E6B3E` | `#9CAB74` | División caprina |
| `--brand-caliza` | `#444B50` | `#B9B3A6` | Gris Caliza |
| `--brand-cobre` | `#B87333` | `#D4935A` | Cobre Paila (acabado especial) |

---

## 3. Tipografía

| Rol | Familia | Dónde |
| --- | --- | --- |
| Identidad | `var(--font-identity)` (Cinzel) | Logotipo, marca del panel, títulos de tarjeta del sitio |
| Titulares | `var(--font-display)` (Playfair Display) | `h1` del panel, titulares del sitio |
| Cuerpo e interfaz | `var(--font-body)` (Plus Jakarta Sans) | Todo lo demás |
| Acento | `var(--font-accent)` (Cormorant Garamond itálica) | Frases de marca del sitio; no se usa en el panel |

- Títulos de página: `clamp(28px, 3.2vw, 40px)`. Cuerpo: 16 px, interlineado 1.6. Tablas: 14 px; encabezados de tabla 11 px en mayúsculas.
- Pesos: 400 cuerpo, 600 etiquetas y datos, 700 títulos y botones.
- Cifras de métricas en 28 a 36 px, peso 700.

---

## 4. Forma y elevación

| Elemento | Radio | Elevación |
| --- | --- | --- |
| Tarjetas | 12 px | `--card-shadow` en claro; en oscuro solo borde |
| Botones, campos, tablas | 8 px | Sin sombra; foco con anillo de 3 px de `--color-border-focus` |
| Chips | 99 px | Sin sombra |
| Diálogos y elementos flotantes | 12 px | `--floating-shadow` / `--dialog-shadow` |
| Botones del sitio público | 2 px | Esquina casi recta, de etiqueta impresa |

---

## 5. Componentes

Usar siempre los de `src/shared/components/` (índice en `components/specs/components.spec.md`).

- **Botones (`UiButton`, clase `.btn`).** `primary` para la acción principal de la pantalla, una por vista. `secondary` para navegar o acciones alternativas. `danger` para bajas y eliminaciones, siempre con confirmación si no se puede deshacer. `accent` (ocre) solo en llamados de marca.
- **Tarjetas (`UiCard`).** Agrupan una tarea: datos generales, pesajes, filtros. Con `metric` muestran una cifra con barra superior ocre.
- **Tablas (`UiTable`).** Primera columna con el identificador como enlace (arete, código). Desplazamiento horizontal propio en pantallas angostas. Códigos internos se muestran con su etiqueta en español.
- **Formularios (`UiField`, `UiRadio`).** Etiqueta visible siempre; obligatorios con «*»; error de la API en un `UiAlert` arriba del formulario sin borrar lo capturado.
- **Estados (`UiBadge`, `UiAlert`, `UiToast`).** Badge para el estado de un registro, alerta para errores o avisos de la pantalla, toast para confirmar una acción terminada.
- **Especie.** Punto de color de la división seguido del nombre (`SpeciesTag` en el módulo Hato).

---

## 6. Maquetación

- Espaciado en múltiplos de 4 px: 8, 12, 16, 24, 32, 48. Separación entre tarjetas: 24 px. Relleno interno de tarjetas: 24 px.
- Panel: barra lateral con marca, grupos por módulo y selector de tema; contenido en `UiBasePage` con `fluid`.
- Rejillas con `.grid` y `.stack`; sus hijos llevan `min-width: 0` para que una tabla ancha desplace dentro de su tarjeta y no ensanche la página.
- Ancho mínimo soportado: 375 px sin desplazamiento horizontal de la página.
- Respetar `prefers-reduced-motion`: sin transiciones decorativas cuando el usuario las desactiva.

---

## 7. Indicaciones para generar pantallas

Para herramientas de generación de interfaces:

> *"Panel de trabajo para una ganadería y quesería familiar del norte de México. Fondo crema cálido (#F7F4EC), tarjetas blancas con radio de 12 px y sombra mínima, texto casi negro (#1A1918) y secundario gris caliza (#444B50). Botón principal negro (#1A1918), acentos ocre (#D19A36). Bovinos se marcan con terracota (#B05A3E) y caprinos con verde mezquite (#5E6B3E), siempre junto a su nombre. Tipografía Plus Jakarta Sans; títulos de página en Playfair Display. Tablas densas con encabezado en mayúsculas pequeñas. Textos en español de México."*

> *"Misma interfaz en modo oscuro: fondo #141312, tarjetas #22201E con borde #33302C sin sombra, texto crema #F7F4EC, botón principal terracota #B05A3E y acentos ocre #E0B45C."*
