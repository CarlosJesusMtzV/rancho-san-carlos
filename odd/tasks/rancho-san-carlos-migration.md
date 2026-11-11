# Migración de la plantilla al dominio Rancho San Carlos

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: convertir `TecNM_Project_Template` en la base del sistema de Rancho San Carlos y La Cremería San Carlos, con esquema ganadero y marca propia.
- Incluye: análisis de la plantilla, `database.sql`, identidad visual, sitio público, panel inicial, specs y skills.
- Fuera de alcance: módulos de backend, autenticación, despliegue.
- Restricciones y decisiones pendientes: ver «Decisiones tomadas y por confirmar» en `specs/domain.spec.md`.

## Requisitos y referencias

- Requisito solicitado y fuente: petición del 2026-11-10; `BDPRY.sql` e `index.html` originales.
- Base: `RubenTecNM/TecNM_Project_Template`, commit `a158acf`. `AngelZurita28/TecNM_Project_Template` (`4959417`) es la versión anterior con login de demostración y el dominio escolar.
- Specs y contratos afectados: `specs/domain.spec.md`, `specs/database.spec.md`, `specs/template.spec.md`, `frontend/src/modules/landing/specs/landing.spec.md`, `frontend/src/modules/home/specs/home.spec.md`, `mainLayout.spec.md`, `components.spec.md`.

## Tareas

- [x] Explorar ambos repositorios y resolver cuál es la base.
- [x] Escribir specs de dominio y base de datos.
- [x] Migrar y corregir el esquema a PostgreSQL.
- [x] Aplicar tokens, tipografía y logotipo de la marca.
- [x] Portar y corregir el sitio público; mover el inicio del sistema a `/panel`.
- [x] Instalar la skill `grilling` y crear la skill del proyecto.
- [x] Confirmar con la familia las decisiones de producto (2026-11-10).
- [x] Agregar matanza de cabritos, meta de venta de becerros y triggers de retiro sanitario.
- [x] Retirar sellos oficiales sin certificación.
- [ ] Arrancar con Docker y verificar `database.sql` en la imagen PostgreSQL 15.
- [ ] Compilar el backend (`dotnet build`).
- [ ] Colocar las fotos reales en `frontend/public/img/`.
- [ ] Reescribir las secciones 2 a 7 de `frontend/DESIGN.md` con la paleta nueva.
- [ ] Primer módulo: Hato (spec, entidades EF Core, API y vistas).

## Decisiones

- 2026-11-10: base en el repositorio de RubenTecNM por ser la versión vigente y agnóstica.
- 2026-11-10: tablas y columnas en inglés y `snake_case` por regla del template; equivalencias en `specs/database.spec.md`.
- 2026-11-10: se conservan las columnas de auditoría del diseño original en todas las tablas.
- 2026-11-10: `/` es el sitio público y `/panel` el sistema, para no mezclar la página comercial con el layout de gestión.
- 2026-11-10: confirmado con la familia: quesos solo de cabra; cabritos en pie o en canal; becerros vivos de ~8 meses y ~150 kg; sin certificaciones oficiales; sin mínimo de tablas.
- 2026-11-10: los retiros de leche y carne se garantizan con triggers en la base de datos por ser reglas de inocuidad.
- 2026-11-10: no se renombran `TecNM.Api`, la base `tecnm_template` ni las variables `TECNM_*`, para poder seguir recibiendo cambios de la plantilla.

## Verificación y evidencia

- `psql -v ON_ERROR_STOP=1 -f database.sql` en PostgreSQL 16 local: correcto; 48 tablas, 6 vistas, 2 triggers.
- Casos negativos (ordeña de bovino, de macho o con retiro; matanza de bovino o con retiro; raza y lote de otra especie; tercera especie; animal o canal vendidos dos veces): rechazados. Detalle en `specs/database.spec.md`.
- `npm --prefix frontend run build`: correcto.
- `(cd frontend && node scripts/check-layout.mjs)`: correcto.
- `python3 scripts/check-setup.py` y `python3 scripts/check-dev-ports.py`: ver resultado en el reporte de entrega.
- Revisión visual con navegador sin interfaz a 1366 px y 375 px, tema claro y oscuro: sin desbordamiento; total del pedido $620 MXN en el caso de la spec. Las fuentes de Google no cargaron en el entorno de prueba.
- No verificado: Docker Compose, PostgreSQL 15, `dotnet build`, lector de pantalla.

## Estado y siguientes pasos

Base lista para empezar el módulo Hato. Antes, confirmar las decisiones pendientes y correr `bash scripts/setup-dev.sh --project-name rancho-san-carlos`.
