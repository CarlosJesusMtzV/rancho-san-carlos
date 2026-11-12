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
- [ ] Compilar el backend: `dotnet build backend/TecNM.Api.csproj`.
- [ ] Ejecutar los criterios de aceptación de la spec backend contra PostgreSQL real (Docker).

## Decisiones

- 2026-11-11: el `DbContext` convierte PascalCase a snake_case en todas las propiedades, en lugar de `HasColumnName` por columna o un paquete NuGet extra.
- 2026-11-11: las relaciones EF usan llaves simples; las llaves compuestas por especie se quedan en la base, que es quien las garantiza.
- 2026-11-11: borrar un pesaje es borrado lógico; volver a registrar esa fecha reactiva el registro, porque la llave única de la base incluye los eliminados.
- 2026-11-11: la salida `slaughtered` queda reservada al módulo de Matanza; aquí solo `sold`, `dead` y `culled`.
- 2026-11-11: etapas productivas con códigos en inglés validados por especie y sexo en el servicio, sin `CHECK` en la base, para poder ajustarlas sin migración.
- 2026-11-11: la fecha «de hoy» se calcula en hora de Coahuila (UTC−6).

## Verificación y evidencia

- Sintaxis C#: los 17 archivos `.cs` analizados con tree-sitter, sin errores.
- Nombres: las 81 propiedades de las 6 entidades coinciden con columnas reales de PostgreSQL 16 creadas con `database.sql`, y los tipos son compatibles (`smallint`→`short`, `date`→`DateOnly`, `numeric`→`decimal`).
- `npm --prefix frontend run build`: correcto. `node scripts/check-layout.mjs`: correcto.
- Navegador sin interfaz contra una API simulada que sigue el contrato: 33 comprobaciones correctas (primer uso, catálogos, filtros por especie y sexo, 409 visibles sin perder datos, genealogía, pesajes, baja con confirmación, filtros en URL, becerros, API caída, sin desbordamiento a 375 px y en tema oscuro).
- **No verificado:** compilación de C# y ejecución real de la API. El entorno de trabajo no tiene acceso a NuGet ni al SDK de .NET. Pendiente correr `dotnet build` y los criterios de aceptación de la spec backend.

## Estado y siguientes pasos

Listo para compilar en la laptop. Si `dotnet build` marca errores, se corrigen antes de empezar Sanidad.
