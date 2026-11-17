# Acceso, roles y permisos

## Objetivo y alcance autorizado

- Objetivo y resultado esperado: que cada trabajador entre al panel con su cuenta y vea y haga solo lo de su puesto, separando la ganadería de la quesería.
- Incluye: inicio de sesión, siete roles, permisos en todos los endpoints, administración de usuarios, contraseña temporal, bloqueo por intentos, auditoría de quién creó y cambió cada registro, menú y botones por permiso.
- Fuera de alcance: puestos, nómina y asistencia; recuperación de contraseña por correo; pedidos de clientes desde el sitio público (parte 2).
- Restricciones y decisiones pendientes: pedidos de clientes sin cuenta, por definir con el usuario.

## Requisitos y referencias

- Requisito solicitado y fuente: respuestas del usuario del 2026-11-16 (trabajadores con cuenta; vaqueros con animales y ordeña; un supervisor de ordeña; queseros que solo anotan cuánto queso salió; precios a la vista de todos y pagos solo de administradores; el vaquero captura la matanza sin ver el pago; clientes sin cuenta).
- Specs y contratos afectados: `backend/Modules/Access/specs/access.spec.md`, `frontend/src/modules/access/specs/access.spec.md`, `specs/domain.spec.md`, `specs/database.spec.md`.

## Tareas

- [x] Esquema: `users` sin `role`, con contraseña temporal y bloqueo; `roles`, `user_roles`; 7 roles.
- [x] Migración `docs/migrations/2026-11-16-07-acceso.sql`; se detiene si ya hay cuentas.
- [x] Backend: sesión por cookie, `/api/auth`, `/api/users`, `[RequirePermission]` en los 7 módulos, pagos ocultos sin `payments.view`, auditoría.
- [x] Panel: `/entrar`, `/panel/cuenta`, `/panel/usuarios`, menú y botones por permiso.
- [x] `setup-dev.py` genera el dueño inicial con contraseña temporal en `.env`.
- [ ] Probar en la laptop con Docker y PostgreSQL 15.
- [ ] Parte 2: pedidos de clientes desde el sitio público.

## Decisiones

- 2026-11-16: los roles se guardan en la base; qué permite cada rol se define en `Core/Security/Permissions.cs`, para que cambiar la matriz no requiera migración.
- 2026-11-16: una cuenta puede tener varios roles; sus permisos se suman.
- 2026-11-16: la cookie solo lleva el id de la cuenta; roles y estado se leen en cada petición, así una baja o un cambio de rol aplica de inmediato.
- 2026-11-16: «pagos solo administradores» cubre honorarios del veterinario, pago al rastro, costo del asadero, resumen de ventas, facturas y cancelar ventas entregadas. Supuesto por confirmar: quien vende registra el cobro al entregar.
- 2026-11-16: no hay recuperación de contraseña sin otro dueño; se recomienda tener dos cuentas de dueño.

## Verificación y evidencia

Ejecutado el 2026-11-16 con .NET 10.0.404 y PostgreSQL 16 local:

- Esquema: 56 tablas, 7 vistas, 10 triggers. La migración 07 sobre el esquema anterior deja el mismo esquema que `database.sql` (`pg_dump -s`).
- `dotnet build`: 0 errores. `npm run build` y `check-layout` correctos.
- API real: Acceso 70 comprobaciones (sesión, bloqueo, contraseña temporal, matriz por rol, pagos ocultos, auditoría). Regresión con sesión: Hato 40, Sanidad 37, Reproducción 34, Ordeña 19, Quesería 54, Matanza 30, Ventas 54, sin fallas.
- Navegador contra la API real: Acceso 37/37 (incluidos 375 px y tema oscuro); regresión Hato 34, Sanidad 35, Reproducción 22, Ordeña 24, Quesería 42, Matanza 17, Ventas 32, sin fallas.
- **No verificado:** Docker Compose con PostgreSQL 15, ni la migración 07 sobre la base de la laptop.
