# Acceso (panel)

## Alcance

Inicio de sesión, cuenta propia y administración de usuarios. Consume la API de [`backend/Modules/Access/specs/access.spec.md`](../../../../../backend/Modules/Access/specs/access.spec.md), donde vive la matriz de roles y permisos.

| Ruta | Vista | Propósito |
| --- | --- | --- |
| `/entrar` | `LoginView` | Usuario y contraseña; fuera del panel |
| `/panel/cuenta` | `AccountView` | Ver mis roles y cambiar mi contraseña |
| `/panel/usuarios` | `UsersView` | Alta, edición, baja y restablecimiento de contraseña (solo `users.manage`) |

## Flujos

1. **Entrar.** Sin sesión, cualquier ruta de `/panel` lleva a `/entrar?redirect=...` y regresa al destino después de entrar. Usuario o contraseña incorrectos y cuenta bloqueada muestran el mensaje de la API.
2. **Contraseña temporal.** Con contraseña temporal solo se abre `/panel/cuenta` hasta cambiarla.
3. **Sesión vencida.** Un 401 de la API limpia la sesión y lleva a `/entrar` con el aviso «Tu sesión venció».
4. **Menú por rol.** El menú lateral y «Tus módulos» de la portada solo muestran las zonas de `src/app/access.ts` que el usuario puede abrir. Escribir a mano la dirección de una zona sin permiso regresa a la portada con un aviso.
5. **Botones por permiso.** Guardar, eliminar, compras de asadero, honorarios, pago al rastro, resumen de ventas y factura se ocultan con `can()` cuando el rol no los puede usar. La API vuelve a comprobarlo.
6. **Usuarios.** Lista con roles y último acceso. El alta pide nombre, usuario, roles agrupados por negocio (General, Ganadería, Quesería) y contraseña temporal. «Restablecer contraseña» pone otra temporal. Una cuenta desactivada ya no entra.
7. **Salir.** «Cerrar sesión» en el menú borra la sesión y lleva a `/entrar`.

## Estados

- Carga, error con mensaje en español y lista vacía en Usuarios.
- El formulario conserva lo capturado cuando la API rechaza el guardado.

## Aceptación

1. Con la API real: el dueño entra con la contraseña temporal, la cambia, crea un vaquero y un vendedor de queso; cada uno ve solo su menú.
2. El vaquero no ve honorarios ni el pago al rastro; el vendedor no ve el resumen de ventas ni la factura.
3. Compila; sin desbordamiento horizontal a 375 px en claro y oscuro.
