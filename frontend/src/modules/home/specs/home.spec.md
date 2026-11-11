# Panel

## Alcance

Página de entrada del sistema de gestión en `/panel`, dentro de `MainLayout`. Sin datos ni operaciones de negocio todavía.

- Usa `UiBasePage` con `fluid=true` y componentes compartidos.
- Muestra las reglas del negocio y la lista de módulos planeados con la especie a la que aplican.
- No realiza peticiones a la API.
- La navegación lateral solo enlaza rutas existentes: Panel y Sitio público.
- Cada módulo nuevo exporta sus rutas por `index.ts` y se agrega como hija de `/panel`.
- Textos en español de México; identidad Rancho San Carlos.

## Aceptación

Compilación TypeScript/Vue correcta. `/panel` muestra título, reglas, módulos planeados y selector de tema. El layout funciona con header, sidebar, ambos o ninguno.
