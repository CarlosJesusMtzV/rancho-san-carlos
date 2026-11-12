# Módulo Hato

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: registrar y consultar los animales del rancho con sus razas, lotes y pesajes, y saber qué becerros están listos para venta.
- Incluye: API `/api/herd`, mapeo EF Core, manejo de errores común en `application/problem+json`, pantallas bajo `/panel/hato`.
- Fuera de alcance: autenticación, instalaciones, partos, sanidad, matanza y ventas.
- Restricciones y decisiones pendientes: sin usuarios todavía, las columnas `created_by`, `updated_by` y `deleted_by` quedan en nulo.

## Requisitos y referencias

- Requisito solicitado y fuente: «empecemos a hacer lo que falte», 2026-11-11; orden de módulos en `specs/domain.spec.md`.
- Specs y contratos afectados: `backend/Modules/Herd/specs/herd.spec.md`, `frontend/src/modules/herd/specs/herd.spec.md`, `frontend/src/modules/home/specs/home.spec.md`, `specs/database.spec.md`.

## Tareas

- [x] Explorar estado actual y resolver incertidumbres.
- [x] Escribir specs de backend y frontend.
- [x] Backend: entidades, configuración EF Core, `ApiExceptionHandler`, `HerdService`, `HerdController`.
- [x] Frontend: listado, alta y edición, ficha con pesajes y baja, catálogos, becerros para venta.
- [x] Corregir el desbordamiento de tablas en móvil (`.grid > *`, `.stack > *` con `min-width: 0` en `global.css`).
- [x] Compilar el backend: `dotnet build backend/TecNM.Api.csproj` (0 errores, 0 advertencias).
- [x] Ejecutar los criterios de aceptación de la spec backend contra PostgreSQL real.
- [x] Quitar `[Produces]` del controller: forzaba `application/json` en los errores 400 de formato.
- [ ] Repetir con Docker Compose y PostgreSQL 15 en la laptop.

## Decisiones

- 2026-11-11: el `DbContext` convierte PascalCase a snake_case en todas las propiedades, en lugar de `HasColumnName` por columna o un paquete NuGet extra.
- 2026-11-11: las relaciones EF usan llaves simples; las llaves compuestas por especie se quedan en la base, que es quien las garantiza.
- 2026-11-11: borrar un pesaje es borrado lógico; volver a registrar esa fecha reactiva el registro, porque la llave única de la base incluye los eliminados.
- 2026-11-11: la salida `slaughtered` queda reservada al módulo de Matanza; aquí solo `sold`, `dead` y `culled`.
- 2026-11-11: etapas productivas con códigos en inglés validados por especie y sexo en el servicio, sin `CHECK` en la base, para poder ajustarlas sin migración.
- 2026-11-11: la fecha «de hoy» se calcula en hora de Coahuila (UTC−6).

## Verificación y evidencia

Ejecutado el 2026-11-11 con .NET SDK 10.0.404 y PostgreSQL 16 local, base creada con `database.sql`:

- `dotnet build backend/TecNM.Api.csproj`: correcto, 0 errores y 0 advertencias.
- API real: 40 comprobaciones correctas, que cubren los 10 criterios de aceptación de la spec backend y validaciones extra (genealogía, rangos de peso y fecha, paginación, búsqueda con comodines escapados, conteo por lote, 404 en español).
- Errores de PostgreSQL forzados con restricciones temporales en la base de prueba: `CHECK` → 422 genérico, llave única → 409, trigger → 422 con su mensaje en español. Todos `application/problem+json`, sin excepción cruda y sin 500.
- 12 peticiones simultáneas para crear la misma raza: 1 × 201 y 11 × 409, ningún 500.
- Pantallas contra la API real (navegador sin interfaz): las 33 comprobaciones correctas, incluidos móvil 375 px y tema oscuro. La base quedó con los datos esperados (aretes en mayúsculas, `created_at` y `updated_at` llenos, baja con `exit_date`).
- Pantallas contra API simulada: mismas 33 comprobaciones correctas.
- `npm --prefix frontend run build` y `node scripts/check-layout.mjs`: correctos.
- Sintaxis C# (tree-sitter) y nombres de columnas contra PostgreSQL (81/81): correctos.
- Un 500 observado durante las pruebas se debió a borrar y recrear la base con la API encendida (conexión del pool cerrada). Con API y base iniciadas juntas: 0 errores 500.
- **No verificado:** Docker Compose con PostgreSQL 15 y HTTPS.

## Estado y siguientes pasos

Terminado. Falta probarlo en la laptop con Docker y unir la rama a `main`.
