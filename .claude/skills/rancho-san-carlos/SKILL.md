---
name: rancho-san-carlos
description: Reglas de dominio y flujo de trabajo del sistema de Rancho San Carlos y La Cremería San Carlos (ganado bovino y caprino, ordeña, quesería). Usar al crear o modificar módulos, specs, tablas, entidades EF Core o vistas Vue de este repositorio.
---

# Rancho San Carlos: dominio y forma de trabajo

Antes de editar, leer `AGENTS.md` (raíz), el `AGENTS.md` del área afectada y `specs/domain.spec.md`.

## Reglas de negocio que no se negocian

1. Solo existen dos especies: `species.id = 1` Bovino y `species.id = 2` Caprino. No agregar otras.
2. Los bovinos son únicamente para carne y se venden vivos: becerros de unos 8 meses y 150 kg (`v_calf_sale_candidates`). No se ordeñan ni se sacrifican.
3. Los caprinos dan cabritos y leche. Los cabritos se venden en pie (`GAN-CAB`) o sacrificados y pelados, en canal (`CAB-CAN`, tabla `kid_slaughters`).
4. Los tres quesos son exclusivamente de leche de cabra. Solo se ordeñan cabras hembra sin retiro de leche (lo garantiza `tg_milk_records_check`).
5. Cada lote de queso (`cheese_batches`) proviene de un lote de leche (`milk_lots`); esa cadena es la trazabilidad pública.
6. No se sacrifica un cabrito con retiro de carne (lo garantiza `tg_kid_slaughters_check`). No hay despiece: la canal se vende entera.
7. Un animal vendido, sacrificado, muerto o desechado deja de estar `active` y lleva `exit_date`.
8. Los triggers lanzan SQLSTATE `23514` con mensaje en español: el backend lo devuelve como 422.
9. No mostrar sellos ni nombres de autoridades (SINIIGA, SENASICA, NOM) sin certificación real.

Si una petición contradice estas reglas, detenerse y preguntar: es una decisión de producto.

## Flujo para un módulo nuevo (vertical slice)

1. Aclarar decisiones pendientes. Si la skill `grilling` está disponible y faltan decisiones relevantes, usarla; si el cambio es simple, omitirla.
2. Escribir primero la spec:
   - Backend: `backend/Modules/<Feature>/specs/<feature>.spec.md` con endpoints, DTOs, errores y criterios de aceptación.
   - Frontend: `frontend/src/modules/<feature>/specs/<feature>.spec.md` con flujos y estados.
3. Backend (`backend/Modules/<Feature>/`): `<Feature>Controller.cs` delgado, `<Feature>Service.cs` con reglas y LINQ asíncrono, `<Feature>DTOs.cs`. Entidades compartidas en `backend/Core/Entities/`, mapeo en `backend/Data/` con nombres `snake_case` idénticos a `database.sql`. Nada de SQL crudo: las consultas complejas usan las vistas `v_*`.
4. Frontend (`frontend/src/modules/<feature>/`): `routes.ts`, `index.ts`, `views/`, `api/`, `types/`. Registrar las rutas como hijas de `/panel` en `frontend/src/app/router/index.ts` y agregar el enlace en `mainLayout.config.ts` solo cuando la ruta exista.
5. Reutilizar `frontend/src/shared/components/` y los tokens de `global.css` (`--color-*`, `--brand-*`, `--font-*`). No escribir colores sueltos en vistas del panel.
6. Si cambia el esquema: editar `database.sql`, actualizar `specs/database.spec.md` y recordar que un volumen existente no se migra solo.
7. Verificar según el alcance (tabla de `CONTRIBUTING.md`) y reportar qué se ejecutó y qué no.

## Convenciones

- Código, rutas, archivos, tablas y columnas en inglés; PostgreSQL en `snake_case`.
- Textos para el usuario en español de México.
- Todas las tablas llevan `status`, `created_by/at`, `updated_by/at`, `deleted_by/at`. Borrar es marcar `deleted_at`, no `DELETE`.
- Color por división: quesería ocre (`--brand-ocre`), caprino verde mezquite (`--brand-mezquite`), bovino terracota (`--brand-terracota`).

## Orden sugerido de módulos

Hato → Sanidad → Reproducción → Ordeña → Quesería → Matanza de cabritos → Ventas. El resto (RH, compras, finanzas, equipo) ya tiene tablas pero va después.
