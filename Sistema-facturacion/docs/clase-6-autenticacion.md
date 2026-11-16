# Clase 6: Migración de autenticación y autorización

**De Forms Authentication a ASP.NET Core Identity y OAuth/OIDC**

> Ya migramos los servicios. Ahora, ¿cómo hacemos para que los usuarios sigan entrando y tengan los mismos permisos?

Esta rama parte de la clase 5 (endpoints REST/gRPC y Worker Service) y agrega a `Facturacion.Api`:

- **ASP.NET Core Identity sobre la tabla `USUARIOS` existente.** No se crean tablas `AspNet*` ni se copian datos.
- **Migración de contraseñas al iniciar sesión.** Las cuentas con hash BCrypt del legacy siguen entrando y pasan al hash de Identity.
- **Mismos roles y permisos:** `ADMIN` y `OPERADOR`, expresados como políticas por operación.
- **Cookie de autenticación** para el login de usuarios y **validación de access tokens JWT** para la demostración de OAuth/OIDC.

## Mapa del código

| Archivo | Qué resuelve | Equivalente legacy |
|---|---|---|
| [Seguridad/UsuarioStore.cs](../Facturacion.Api/Seguridad/UsuarioStore.cs) | Permite que Identity lea y escriba `USUARIOS` (id, nombre, hash, rol). | Consultas de `AccountController` |
| [Seguridad/PasswordHasherCompatibleLegacy.cs](../Facturacion.Api/Seguridad/PasswordHasherCompatibleLegacy.cs) | Verifica BCrypt y pide el rehash. Los hashes nuevos usan el formato de Identity. | `PasswordSeguridad.cs` |
| [Seguridad/UsuarioClaimsPrincipalFactory.cs](../Facturacion.Api/Seguridad/UsuarioClaimsPrincipalFactory.cs) | Arma las claims (id, nombre, rol) y bloquea el ingreso de cuentas inactivas. | `GenericPrincipal` en `Global.asax.cs` |
| [Seguridad/SeguridadExtensions.cs](../Facturacion.Api/Seguridad/SeguridadExtensions.cs) | Configura Identity, la cookie, JWT bearer, las políticas y el límite de intentos. Revalida la cuenta en cada petición. | `Web.config` + `Application_PostAuthenticateRequest` + `IntentosLogin.cs` |
| [Seguridad/Roles.cs](../Facturacion.Api/Seguridad/Roles.cs) | Define los roles y las políticas (`Consultar`, `GestionarCatalogo`, `AdministrarUsuarios`). | `PermisoAttribute` + `[Authorize(Roles=...)]` |
| [Controllers/AuthController.cs](../Facturacion.Api/Controllers/AuthController.cs) | `POST login`, `POST logout` y `GET yo`. | `AccountController` |
| [Controllers/UsuariosController.cs](../Facturacion.Api/Controllers/UsuariosController.cs) | Administración de cuentas, solo para ADMIN. Nunca expone el hash y muestra si la cuenta ya migró. | No existía |
| [Facturacion.Api.http](../Facturacion.Api/Facturacion.Api.http) | Los escenarios de prueba de la diapositiva 15. | — |

## Migrar cuentas sin pedir una cuenta nueva

```text
login(usuario, contraseña)
  └─ UserManager busca el usuario en USUARIOS
  └─ PasswordHasherCompatibleLegacy.VerifyHashedPassword
       ├─ hash "$2a$/$2b$/$2y$" (BCrypt legacy)
       │    ├─ correcto   → SuccessRehashNeeded
       │    │                 └─ Identity guarda un hash PBKDF2 nuevo con la contraseña recién ingresada
       │    └─ incorrecto → Failed
       └─ otro formato → verificación estándar de Identity
  └─ ¿ACTIVO = 1? (IUserConfirmation) → si no, NotAllowed
  └─ se emite la cookie Facturacion.Auth con las claims id + nombre + rol
```

Puntos para remarcar en clase:

1. **Un hash no se puede convertir en otro sin la contraseña.** Por eso la migración ocurre cuando el usuario vuelve a entrar. Las cuentas que nunca vuelven a entrar quedan en BCrypt: `GET /api/usuarios` muestra `FormatoHash` para seguir el avance. Para cerrar la migración hay que decidir una fecha y forzar el restablecimiento de esas cuentas.
2. **El formato del hash identifica si la cuenta ya migró**, así que no hace falta otra columna. Si el esquema lo permitiera, una columna `HASH_MIGRADO_EN` facilitaría la auditoría.
3. **Se conserva el identificador de negocio:** `USUARIOS.ID` sigue siendo el `NameIdentifier` de la identidad.
4. **Durante la convivencia, el legacy no entiende el hash nuevo.** Si las dos aplicaciones validan contra la misma tabla, una cuenta migrada ya no puede entrar al legacy. Hay tres alternativas: que el legacy también acepte el formato de Identity; que solo la aplicación nueva administre contraseñas y el legacy delegue el login; o migrar todas las cuentas en el corte final. Esta es la discusión de la diapositiva 14.
5. **`PASSWORDHASH` tiene 100 caracteres.** Un hash v3 de Identity ocupa 84, así que entra. Antes de migrar hay que revisar siempre el largo de la columna.

## Permisos conservados

| Operación | Endpoint | ADMIN | OPERADOR |
|---|---|---|---|
| Consultar clientes y productos | `GET /api/clientes`, `GET /api/productos`, gRPC | Sí | Sí |
| Crear, editar y borrar clientes y productos | `POST/PUT/DELETE` | Sí | No (403) |
| Administrar usuarios | `/api/usuarios` | Sí | No (403) |
| Sin sesión | cualquier endpoint protegido | 401 | 401 |

Hay una **política de respaldo** (`FallbackPolicy`): los endpoints sin `[Authorize]` ni `[AllowAnonymous]` exigen un usuario autenticado. Solo `/openapi`, Swagger, `/weatherforecast` y el login son anónimos.

El legacy volvía a consultar la cuenta en cada petición. Acá se mantiene ese comportamiento con `OnValidatePrincipal`: una cuenta desactivada pierde el acceso en su próxima petición y, si cambia su rol, se reemite la cookie.

## Ejecutar

### 1. Base de datos (Oracle Free en Docker)

Desde `Sistema-facturacion/`:

```bash
docker compose up -d
docker compose logs -f oracle      # esperar "DATABASE IS READY TO USE!" (1-3 min la primera vez)
```

La primera vez que arranca, el contenedor ejecuta los scripts de [db/](../db):

- [01-esquema.sql](../db/01-esquema.sql) crea el usuario `facturacion_app` y las tablas que mapea `FacturacionDbContext`.
- [02-datos-demo.sql](../db/02-datos-demo.sql) carga clientes, productos y dos cuentas con hash BCrypt, como las del legacy:

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `admin-clase-6!` | ADMIN |
| `operador` | `operador-clase-6!` | OPERADOR |

Para empezar de cero: `docker compose down -v && docker compose up -d`.

Para repetir la demostración de migración sin recrear la base, se puede restaurar el hash BCrypt con el `UPDATE` correspondiente. Los valores están en `02-datos-demo.sql`.

### 2. API

Requiere el SDK de .NET 10 (`brew install --cask dotnet-sdk`).

```bash
cd Sistema-facturacion/Facturacion.Api
dotnet run --launch-profile http
```

La API queda en `http://localhost:5246`, y Swagger en `/swagger`.

### 3. Probar

Ejecutar los pasos de [Facturacion.Api.http](../Facturacion.Api/Facturacion.Api.http) en orden. Por ejemplo, en VS Code con la extensión REST Client. Las contraseñas de la base demo ya están cargadas en el archivo.

Con `curl`:

```bash
B=http://localhost:5246
curl -i $B/api/clientes                                              # 401
curl -i -c op.txt -H 'Content-Type: application/json' \
  -d '{"nombreUsuario":"operador","password":"operador-clase-6!"}' $B/api/auth/login   # 204
curl -i -b op.txt $B/api/auth/yo                                     # rol OPERADOR
curl -i -b op.txt $B/api/clientes                                    # 200
curl -i -b op.txt -X DELETE $B/api/productos/1                       # 403
```

En la consola de la API aparece `Cuenta N migrada de BCrypt al hash de Identity.` en el primer login de cada cuenta.

## Demostración OAuth/OIDC (bearer token)

La API **no emite tokens**: los emite el proveedor de identidad y la API solo los valida (firma, emisor, audiencia y vencimiento). Para activarlo, completar `appsettings.Development.json`:

```json
"Autenticacion": {
  "Oidc": {
    "Authority": "http://localhost:8080/realms/facturacion",
    "Audience": "facturacion-api",
    "RoleClaimType": "roles"
  }
}
```

Si `Authority` queda vacío, la API solo acepta la cookie. Cuando está configurado, una petición con `Authorization: Bearer …` se autentica con JWT y el resto con la cookie (ver `EsquemaCookieOBearer`).

Para que las políticas funcionen con tokens, el proveedor tiene que emitir los roles `ADMIN` u `OPERADOR` en la claim configurada en `RoleClaimType`. Un token sin esos roles recibe 403. Esto también sirve para mostrar en clase que autenticar no es autorizar.

Recorrido sugerido: el cliente inicia sesión con **Authorization Code + PKCE**, obtiene un access token y llama a `GET /api/auth/yo` y `GET /api/clientes`. En jwt.io se puede ver el contenido del token, con una aclaración: **decodificar no es validar**.

Para procesos sin usuario, como el `FacturacionWorker` de la clase 5, el flujo es **Client Credentials**.

## Limitaciones y ejercicios para los alumnos

- **No hay bloqueo por cuenta.** El esquema no tiene columnas de lockout, así que el límite de intentos es por IP y en memoria (10 cada 5 minutos en `/api/auth/login`). *Ejercicio:* agregar `INTENTOS_FALLIDOS` y `BLOQUEADO_HASTA` e implementar `IUserLockoutStore`.
- **No hay security stamp.** Cerrar sesión borra la cookie del navegador, pero una copia de la cookie sigue valiendo hasta vencer, igual que en el legacy. *Ejercicio:* agregar la columna e implementar `IUserSecurityStampStore`.
- **No hay cambio ni recuperación de contraseña.** *Ejercicio:* `POST /api/auth/cambiar-password` con `UserManager.ChangePasswordAsync`.
- **No hay antiforgery.** La cookie es `SameSite=Strict`. Si se agregan vistas MVC o Razor Pages, hay que volver a usar antiforgery en los POST.
- **Producción:** usar HTTPS y `CookieSecurePolicy.Always`. Hoy queda en `SameAsRequest` para que el perfil `http` funcione en clase.
