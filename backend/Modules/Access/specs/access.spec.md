# Acceso: inicio de sesión, roles y usuarios

## Alcance

Cuentas para entrar al panel, roles por negocio (ganadería y quesería) y permisos sobre los módulos. Decisiones en [`specs/domain.spec.md`](../../../../specs/domain.spec.md#decisiones).

- El sitio público sigue abierto; todo `/api` (salvo `/api/health` y `/api/auth/login`) exige sesión.
- Una persona puede tener varios roles; sus permisos son la suma.
- Los precios los ve cualquiera. Los **pagos y costos** (honorarios y medicamentos del veterinario, pago al rastro, costo del asadero, resumen de ventas, facturas) solo los administradores (`payments.view`).
- El veterinario y el matancero son externos: no tienen cuenta.

Fuera de alcance: puestos, nómina y asistencia; cuentas de clientes; recuperación de contraseña por correo (la restablece el dueño).

## Roles

| Código | Rol | Negocio |
| --- | --- | --- |
| `owner` | Dueño | General |
| `livestock_admin` | Administrador ganadero | Ganadería |
| `cowboy` | Vaquero | Ganadería |
| `milking_supervisor` | Supervisor de ordeña | Ganadería |
| `cheese_admin` | Administrador de quesería | Quesería |
| `cheesemaker` | Quesero | Quesería |
| `cheese_seller` | Vendedor de queso | Quesería |

## Permisos por rol

`owner` tiene todos. ✔ = tiene el permiso.

| Permiso | Qué permite | Adm. ganadero | Vaquero | Sup. ordeña | Adm. quesería | Quesero | Vendedor |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `herd.read` | Ver hato, razas, lotes, pesos | ✔ | ✔ | ✔ | | | |
| `herd.write` | Registrar y editar animales, pesos, bajas, razas, lotes | ✔ | ✔ | | | | |
| `herd.delete` | Eliminar un pesaje | ✔ | | | | | |
| `health.read` | Ver sanidad y retiros | ✔ | ✔ | ✔ | | | |
| `health.write` | Medicamentos, visitas, eventos, pruebas | ✔ | ✔ | | | | |
| `health.delete` | Eliminar un evento | ✔ | | | | | |
| `repro.read` | Ver reproducción | ✔ | ✔ | | | | |
| `repro.write` | Montas, diagnósticos, partos | ✔ | ✔ | | | | |
| `repro.delete` | Eliminar una monta | ✔ | | | | | |
| `milking.read` | Ver ordeñas | ✔ | ✔ | ✔ | ✔ | | |
| `milking.write` | Registrar y corregir ordeñas | ✔ | ✔ | ✔ | | | |
| `milking.delete` | Eliminar una ordeña | ✔ | | ✔ | | | |
| `slaughter.read` | Ver matanzas y canales | ✔ | ✔ | | | | |
| `slaughter.write` | Registrar y corregir matanzas | ✔ | ✔ | | | | |
| `slaughter.delete` | Eliminar una matanza | ✔ | | | | | |
| `cheese.read` | Ver tandas y existencias | | | | ✔ | ✔ | ✔ |
| `cheese.produce` | Tandas y mermas | | | | ✔ | ✔ | |
| `cheese.delete` | Eliminar tandas y mermas | | | | ✔ | | |
| `cheese.purchases` | Compras de asadero y proveedores | | | | ✔ | | |
| `sales.cheese` | Vender y entregar queso | | | | ✔ | | ✔ |
| `sales.livestock` | Vender y entregar ganado y canal | ✔ | | | | | |
| `sales.customers` | Alta y edición de clientes | ✔ | | | ✔ | | ✔ |
| `sales.cancel` | Cancelar una venta ya entregada | ✔ | | | ✔ | | |
| `payments.view` | Ver y capturar pagos y costos; facturas; resumen de ventas | ✔ | | | ✔ | | |
| `users.manage` | Usuarios y roles | | | | | | |

Reglas derivadas:

- Sin `payments.view`, las visitas del veterinario y las matanzas se responden sin importes (`null`) y los importes enviados se ignoran: al crear quedan en 0 o vacíos, al editar se conservan.
- Ver pedidos requiere `sales.cheese` o `sales.livestock`. Crear, editar, entregar o cancelar un pedido exige el permiso de **cada** tipo de renglón que lleva: queso → `sales.cheese`; ganado en pie o canal → `sales.livestock`. `GET /sales/available` solo lista lo que el usuario puede vender.
- Quien vende registra el cobro al entregar; eso no requiere `payments.view`.

## Reglas de cuentas

| # | Regla | Error |
| --- | --- | --- |
| A1 | Usuario: 3 a 32 caracteres, minúsculas, números, punto, guion o guion bajo; único. Nombre completo obligatorio. Correo opcional y único. | 400 / 409 |
| A2 | Contraseña: 8 a 128 caracteres con al menos una letra y un número. Se guarda solo su hash (PBKDF2, `PasswordHasher`). | 400 |
| A3 | Cinco intentos fallidos seguidos bloquean la cuenta 15 minutos. El mensaje de error no distingue usuario inexistente de contraseña incorrecta. | 401 / 423 |
| A4 | Una contraseña puesta por el dueño es temporal: hasta cambiarla, la cuenta solo puede usar `/api/auth`. | 403 |
| A5 | Cada usuario tiene al menos un rol. Siempre queda al menos un dueño activo; nadie se desactiva ni se quita el rol de dueño a sí mismo. | 400 / 409 |
| A6 | Desactivar una cuenta o cambiar sus roles surte efecto en su siguiente petición. | — |
| A7 | La sesión va en una cookie `HttpOnly`, `Secure`, `SameSite=Strict`, con 12 horas de vigencia deslizante. Las peticiones que modifican datos con un `Origin` ajeno se rechazan. | 403 |
| A8 | Todo registro creado, modificado o eliminado guarda quién lo hizo (`created_by`, `updated_by`, `deleted_by`). | — |
| A9 | Primer dueño: si no hay usuarios y existen `Bootstrap__OwnerUsername` y `Bootstrap__OwnerPassword`, la API lo crea al arrancar con contraseña temporal. | — |

## API

Errores con el formato común de [`herd.spec.md`](../../Herd/specs/herd.spec.md#errores). Sin sesión: 401. Sin permiso: 403.

| Método y ruta | Permiso | Éxito | Errores |
| --- | --- | --- | --- |
| `POST /api/auth/login` | — | 200 `Session` | 400, 401, 423 |
| `POST /api/auth/logout` | sesión | 204 | — |
| `GET /api/auth/me` | sesión | 200 `Session` | 401 |
| `POST /api/auth/change-password` | sesión | 200 `Session` | 400, 401 |
| `GET /api/users` | `users.manage` | 200 `UserItem[]` | — |
| `GET /api/users/roles` | `users.manage` | 200 `RoleItem[]` | — |
| `POST /api/users` | `users.manage` | 201 `UserItem` | 400, 409 |
| `PUT /api/users/{id}` | `users.manage` | 200 `UserItem` | 400, 404, 409 |
| `POST /api/users/{id}/reset-password` | `users.manage` | 200 `UserItem` | 400, 404 |

```ts
RoleItem { code, name, division: 'general' | 'livestock' | 'cheese', description }
Session { id, username, fullName, roles: RoleItem[], permissions: string[], mustChangePassword }
UserItem { id, username, fullName, email | null, roles: RoleItem[], active, mustChangePassword, lastLoginAt | null, lockedUntil | null }
Login { username, password }
ChangePassword { currentPassword, newPassword }
SaveUser { username (solo al crear), fullName, email?, roleCodes: string[], active?, password (solo al crear) }
ResetPassword { password }
```

## Aceptación

1. Sin sesión, cualquier ruta de módulo responde 401; `/api/health` responde 200.
2. El dueño inicial entra, debe cambiar su contraseña y después puede crear usuarios.
3. Un vaquero registra un animal, una ordeña y una matanza; no puede eliminar, no ve importes de visitas ni de matanzas, y recibe 403 en quesería, ventas y usuarios.
4. Un quesero registra una tanda y una merma; recibe 403 en compras de asadero, ventas y hato.
5. Un vendedor de queso vende queso y recibe 403 al vender un becerro; el administrador ganadero al revés.
6. Cinco contraseñas incorrectas bloquean la cuenta; desactivar una cuenta corta su sesión.
7. Los registros nuevos guardan `created_by` con el usuario que los creó.
