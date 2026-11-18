# Seguridad de ArchiveCore

Este documento describe el modelo de seguridad real de la versión final y cómo configurarlo.

## Secretos

ArchiveCore **no contiene secretos en el repositorio** y **no tiene credenciales por defecto**.

| Secreto | Desarrollo local | Docker |
|---|---|---|
| Cadena de conexión | User Secrets (`ConnectionStrings:ArchiveCore`) o `appsettings.json` con autenticación integrada | `.env` → `ARCHIVECORE_SQL_SA_PASSWORD` |
| Clave JWT (≥ 32 caracteres) | User Secrets (`Jwt:SigningKey`) | `.env` → `ARCHIVECORE_JWT_SIGNING_KEY` |
| Primer administrador | User Secrets (`BootstrapAdmin:*`) | `.env` → `ARCHIVECORE_BOOTSTRAP_ADMIN_*` |

Los User Secrets se guardan en el perfil del usuario del sistema operativo, nunca dentro del repositorio.
`.env` está en `.gitignore`; `.env.example` solo contiene nombres de variables vacíos.

Genera valores aleatorios, por ejemplo:

```bash
openssl rand -base64 48   # clave JWT
```

```powershell
cd src/ArchiveCore.WebApi
dotnet user-secrets init
dotnet user-secrets set "Jwt:SigningKey" "<clave-aleatoria-de-al-menos-32-caracteres>"
```

## Primer administrador (bootstrap de un solo uso)

El administrador inicial solo se crea cuando se cumplen las tres condiciones:

1. `BootstrapAdmin:Enabled` es `true`.
2. `dbo.Users` está vacía.
3. El rol sembrado `Administrator` existe.

La contraseña debe cumplir la política de contraseñas (abajo). Después del primer arranque,
vuelve a poner `BootstrapAdmin:Enabled` en `false` y elimina la contraseña:

```powershell
dotnet user-secrets set "BootstrapAdmin:Enabled" "false"
dotnet user-secrets remove "BootstrapAdmin:Password"
```

Los demás usuarios los crea un Administrator desde la página **Usuarios**.

## Política de contraseñas

Una única política (`PasswordPolicy`) se aplica al bootstrap y a la creación de usuarios:
entre 12 y 128 caracteres, con mayúscula, minúscula, número y símbolo, sin espacios al inicio o
al final. Las contraseñas se guardan solo como hash con `PasswordHasher<TUser>` de ASP.NET Core.

## Sesiones

| Elemento | Dónde vive | Vida útil |
|---|---|---|
| Access token (JWT) | Solo en memoria del SPA; nunca en `localStorage` ni `sessionStorage` | 15 minutos (`Jwt:AccessTokenMinutes`) |
| Refresh token | Cookie `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth` | 7 días (`Jwt:RefreshTokenDays`) |

- En la base de datos solo se guarda el hash SHA-256 del refresh token.
- Cada uso del refresh token lo rota; un token revocado o reutilizado se rechaza.
- Al recargar la página, el SPA restaura la sesión con `POST /api/v1/auth/refresh` (cookie).
- Ante un `401`, el cliente intenta **un único** refresh compartido por todas las peticiones
  concurrentes y repite la petición una sola vez; si el refresh falla, limpia la sesión y
  redirige al login con un aviso de sesión expirada.
- Cerrar sesión llama primero a `POST /api/v1/auth/revoke` (funciona aunque el access token haya
  expirado) y después limpia el estado local.
- Los navegadores aceptan cookies `Secure` en `http://localhost`. Solo si accedes al stack por
  HTTP desde otro host distinto de localhost debes poner `ARCHIVECORE_REFRESH_COOKIE_SECURE=false`.
- Rate limiting por IP (ventana fija de 1 minuto, respuesta `429`): login 10/min
  (`RateLimiting:LoginPerMinute`), refresh y revoke 30/min (`RateLimiting:RefreshPerMinute`).
- Tokens y contraseñas no se escriben en logs.

## Roles y autorización

La autorización real se aplica en la API mediante políticas reutilizables
(`AuthorizationPolicies`). El SPA solo oculta o deshabilita acciones por comodidad.
Cualquier endpoint sin política explícita exige, como mínimo, un usuario autenticado.

| Capacidad | Administrator | RecordsManager | User |
|---|:---:|:---:|:---:|
| Dashboard, expedientes, documentos, historial de movimientos, descargas | ✓ | ✓ | ✓ (lectura) |
| Crear/editar expedientes, asignar, transferir, cerrar | ✓ | ✓ | — |
| Crear documentos, subir versiones, eliminación lógica | ✓ | ✓ | — |
| Consultar auditoría | ✓ | — | — |
| Administrar usuarios (`/api/v1/users`) | ✓ | — | — |
| SQL Lab SOF-008 | ✓ | — | — |

| Endpoint | Política |
|---|---|
| `POST /api/v1/auth/login`, `/refresh`, `/revoke` | Anónimo + rate limiting |
| `GET /api/v1/account/me`, `/dashboard`, `/catalogs/*` | Lectores (cualquier rol) |
| `GET /api/v1/records…`, `GET …/workflow/history`, `GET /api/v1/documents…` | Lectores |
| `POST/PUT /api/v1/records…`, `POST …/workflow/transfer`, `POST …/workflow/close` | RecordsManager o Administrator |
| `POST /api/v1/documents…`, `POST …/versions`, `DELETE /api/v1/documents/{id}` | RecordsManager o Administrator |
| `GET /api/v1/audit` | Administrator |
| `/api/v1/users…` | Administrator |
| `/api/v1/academic/sql-lab…` | Administrator |

Las pruebas de integración verifican `401` sin token y `403` para cada rol sin permiso.

## Errores

Todas las respuestas de error usan `ProblemDetails` (`400` validación, `401`, `403`, `404`,
`409` conflicto/duplicado/concurrencia con `rowversion`, `429`, `500`). En producción no se
exponen stack traces ni mensajes internos de SQL Server; cada respuesta lleva un
`X-Correlation-Id` que también se guarda en la auditoría.

## Documentos

- Tamaño máximo configurable: `DocumentStorage:MaxFileSizeBytes` (20 MB por defecto).
  Nginx (`client_max_body_size 25m`) y Kestrel aceptan un límite compatible.
- Allow-list de extensiones: PDF, PNG, JPG/JPEG, TIF/TIFF, DOCX, XLSX, PPTX, ODT, ODS, DOC, XLS,
  PPT, TXT y CSV. Se valida extensión **y** firma binaria del archivo; el `Content-Type` del
  cliente no se usa para decidir.
- Los archivos se guardan con nombres generados por el servidor fuera del directorio web; la
  descarga resuelve la ruta en el servidor, impide path traversal, envía un nombre de archivo
  saneado y el `Content-Type` de la allow-list, y nunca expone rutas físicas.
- Si falla la transacción de base de datos, el archivo físico recién escrito se elimina.
- Cada versión guarda su hash SHA-256.

## Red, cabeceras y CORS

- Docker sirve SPA y API en el **mismo origen** a través de Nginx, por lo que no se necesita CORS.
  En desarrollo, el proxy de Vite también es same-origin. CORS solo acepta `localhost`/`127.0.0.1`
  en Development y los orígenes listados explícitamente en `Cors:AllowedOrigins` (nunca `*`).
- `AllowedHosts` por defecto: `localhost;127.0.0.1` (configurable con `ARCHIVECORE_ALLOWED_HOSTS`).
- `X-Forwarded-For`/`X-Forwarded-Proto` solo se aceptan desde las redes configuradas en
  `ReverseProxy:KnownNetworks` (la red privada de Docker).
- HTTPS/HSTS se activan con `Security:UseHttpsRedirection` (por defecto fuera de Development). El
  stack Docker local no termina TLS; para un despliegue real coloca un proxy TLS delante.
- Cabeceras: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
  `Referrer-Policy: no-referrer`, `Permissions-Policy`, y una CSP estricta para el SPA
  (`default-src 'self'`, sin scripts ni estilos inline) y para la API (`default-src 'none'`).

## Auditoría

Los triggers de SQL Server leen `SESSION_CONTEXT('UserId')`, `IpAddress` y `CorrelationId`, que
un interceptor de EF Core establece en cada apertura de conexión (también con `NULL`), de modo que
nunca quedan valores residuales en conexiones reutilizadas del pool. Detalle en
[`docs/architecture/data-access-strategy.md`](../architecture/data-access-strategy.md).

## Dependencias

CI falla ante cualquier vulnerabilidad High/Critical en:

- paquetes NuGet (`dotnet list package --vulnerable --include-transitive`);
- dependencias npm de runtime (`npm audit --omit=dev --audit-level=high`);
- el árbol npm completo (`npm run audit:gate`), salvo excepciones documentadas aquí.

### Excepción vigente: GHSA-fc5q-39xc-vjpf (picomatch)

| Campo | Valor |
|---|---|
| Paquete | `picomatch` ≤ 4.0.7 (todas las versiones publicadas) |
| Severidad | High — ReDoS con patrones extglob suministrados por un atacante |
| Versión corregida | No existe a 18 nov 2026 |
| Ruta | Solo herramientas de desarrollo/build: `vite`, `vitest`, `typescript-eslint` |
| Exposición en ArchiveCore | Ninguna en runtime: el bundle publicado no incluye `picomatch` y los patrones glob los define el propio repositorio, no usuarios. |
| Acción | `frontend/scripts/audit-gate.mjs` acepta solo este identificador; cualquier otra High/Critical rompe CI. Retirar la excepción en cuanto exista una versión corregida (`npm update` + `npm run audit:gate`). |

## Privacidad del repositorio público

- `docs/legacy/DatabaseProject-original.sql` es una copia **sanitizada**: conserva tablas,
  columnas, tipos y consultas, con placeholders en los valores de los `INSERT` históricos.
  Ver [`docs/legacy/legacy-analysis.md`](../legacy/legacy-analysis.md).
- El SQL Lab solo ejecuta consultas predefinidas de solo lectura, exclusivamente para
  Administrator; no existe consola SQL libre y nunca muestra los `INSERT` históricos.
