# Forms Authentication y permisos

Forms Authentication utiliza `System.Web` para emitir y verificar un ticket protegido en una cookie. El formulario permite identificar al usuario; el ticket se envía en las siguientes peticiones y la autorización decide sus permisos. Autenticación y autorización resuelven preguntas diferentes. Contexto de ASP.NET clásico en [Security Basics and ASP.NET Support](https://learn.microsoft.com/en-us/aspnet/web-forms/overview/older-versions-security/introduction/security-basics-and-asp-net-support-cs).

## Implementación del repositorio

[`AccountController.cs`](../../legacyGestionDeFacturacion.web/Controllers/AccountController.cs) expone Login GET/POST y Logout exclusivamente POST. Los POST llevan antiforgery. El login consulta Usuario y verifica el hash; si falla presenta un mensaje genérico y no distingue usuario inexistente de contraseña incorrecta. Para un usuario inexistente se verifica contra un hash ficticio. Solo se aceptan redirecciones locales de `returnUrl`.

[`PasswordSeguridad.cs`](../../FacturacionLegacy.Datos/PasswordSeguridad.cs) delega en BCrypt.Net-Next: salt generado por la biblioteca y factor 12. No implementa un algoritmo criptográfico propio. El ingreso de contraseñas locales exige al menos 12 caracteres y limita a 72 bytes UTF-8 para evitar truncamiento de bcrypt. La [guía de almacenamiento de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html) describe estas consideraciones para bcrypt en sistemas legacy; no se presentan como la única opción para sistemas nuevos.

[`Web.config`](../../legacyGestionDeFacturacion.web/Web.config) habilita Forms Authentication, cookies Secure/HttpOnly/SameSite=Lax, ticket de 20 minutos sin renovación deslizante y cookies exclusivamente de navegador. El filtro global `RequireHttps` impone HTTPS; `Authorize` protege las acciones, salvo Login con `AllowAnonymous`. Los datos de sesión se abandonan al entrar/salir.

[`Global.asax.cs`](../../legacyGestionDeFacturacion.web/Global.asax.cs), en `Application_PostAuthenticateRequest`, vuelve a consultar la cuenta activa y asigna un GenericPrincipal con el rol actual de SQL Server. No confía en roles enviados por formularios ni en un rol persistido por el cliente. Desactivar un usuario elimina su autenticación al siguiente acceso. [`PermisoAttribute`](../../legacyGestionDeFacturacion.web/Seguridad/PermisoAttribute.cs) deriva de `AuthorizeAttribute`: anónimo obtiene el flujo de login; un autenticado sin el rol requerido obtiene 403.

## Cuentas y permisos

No existe registro público. Ejecutar `FacturacionLegacy.Tools.exe usuarios-demo` después de `migrar`; elegir contraseñas de desarrollo al ser solicitadas, sin eco. Las cuentas `admin`/ADMIN y `operador`/OPERADOR se insertan solo si faltan. Los hashes se guardan en Usuarios; el repositorio no incluye contraseñas ni hashes de cuentas reales.

| Operación | ADMIN | OPERADOR |
| --- | --- | --- |
| Consultar clientes/productos | Sí | Sí |
| Crear/editar/desactivar clientes/productos | Sí | No |
| Crear/consultar facturas | Sí | Sí |
| Emitir/anular facturas | Sí | No |
| Reportes por estado | Sí | No |

Los controllers aplican roles y los botones siguen esa misma intención. Ocultar un botón no sustituye los controles del servidor. Todas las escrituras requieren antiforgery. Logout borra la cookie en el navegador y abandona la sesión.

## Limitaciones y migración conceptual

[`IntentosLogin.cs`](../../legacyGestionDeFacturacion.web/Seguridad/IntentosLogin.cs) permite cinco intentos por cuenta cada 15 minutos y limita el tamaño de su colección. Es un control básico por proceso: se reinicia y no se comparte entre varios servidores; no reemplaza controles distribuidos contra abuso. No hay MFA, recuperación de contraseña, administración de cuentas, auditoría ni revocación central por sesión. Una copia de un ticket sigue siendo válida hasta expirar si la cuenta continúa activa; cerrar sesión borra la cookie local, no revoca todas las copias. La lectura del usuario en cada petición permite revocar la cuenta y actualizar roles, pero añade una consulta SQL.

Para el proyecto nuevo, ASP.NET Core Identity ofrece gestión de usuarios y funcionalidades de cuentas sobre otro stack. OAuth delega autorización; para identidad federada suele intervenir OpenID Connect. Identity y OAuth no son nombres alternativos para la cookie Forms. Ver [introducción a ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0).

Los alumnos deberán definir cómo migrar usuarios/hashes, emitir nuevas cookies o tokens y preservar permisos, errores de login, HTTPS y antiforgery. Las cookies Forms no se reutilizan automáticamente en el proyecto paralelo. No se implementó Identity, OAuth ni un proveedor externo en esta rama.
