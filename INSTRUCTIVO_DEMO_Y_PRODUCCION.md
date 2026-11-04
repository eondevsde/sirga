# INSTRUCTIVO — Entorno de DEMO (entrenamiento) y puesta en producción

> **Para la próxima sesión.** Este documento cubre dos cosas:
> **Parte A**: armar un entorno de **entrenamiento** con copias de las bases, para usar SIRGA y SIGESWEB
> **sin ningún riesgo** sobre los datos reales.
> **Parte B**: la **puesta en producción**, con los artefactos que ya están compilados y los datos que
> faltan completar (hay **bloqueantes**, ver B.3).
>
> Fecha: **2026-11-04** · Reglas que no se negocian: ver `AGENTS.md` y `CONTEXTO_SIRGA.md`.

---

# Parte A — Entorno de DEMO / ENTRENAMIENTO

## A.1. Crear las copias de las bases

Con `BACKUP` + `RESTORE` **a otro nombre** (no adjuntar los `.mdf` originales: están en uso y se puede
corromper el archivo vivo).

```sql
-- 1) Respaldo (COPY_ONLY no altera la cadena de respaldos existente)
BACKUP DATABASE SIRGA_DB  TO DISK = 'C:\Proyectos\_RESPALDOS\demo\SIRGA_DB.bak'  WITH COPY_ONLY, INIT;
BACKUP DATABASE Sindicato TO DISK = 'C:\Proyectos\_RESPALDOS\demo\Sindicato.bak' WITH COPY_ONLY, INIT;

-- 2) Ver los nombres lógicos (hacen falta para el MOVE)
RESTORE FILELISTONLY FROM DISK = 'C:\Proyectos\_RESPALDOS\demo\SIRGA_DB.bak';

-- 3) Restaurar con nombre nuevo
RESTORE DATABASE SIRGA_DB_DEMO
  FROM DISK = 'C:\Proyectos\_RESPALDOS\demo\SIRGA_DB.bak'
  WITH MOVE 'SIRGA_DB'     TO 'C:\...\SIRGA_DB_DEMO.mdf',
       MOVE 'SIRGA_DB_log' TO 'C:\...\SIRGA_DB_DEMO_log.ldf',
       REPLACE;

RESTORE DATABASE SINDICATO_DEMO
  FROM DISK = 'C:\Proyectos\_RESPALDOS\demo\Sindicato.bak'
  WITH MOVE 'Sindicato'     TO 'C:\...\SINDICATO_DEMO.mdf',
       MOVE 'Sindicato_log' TO 'C:\...\SINDICATO_DEMO_log.ldf',
       REPLACE;
```

> **Espacio**: `Sindicato` ~627 MB, `SIRGA_DB` menos. Entran sin problema.

## A.2. Apuntar las aplicaciones a las copias

> ⚠️ **SIRGA TIENE DOS CADENAS Y HAY QUE CAMBIAR LAS DOS.** La segunda (`Legacy`) es la que **escribe** en
> el padrón del sindicato y en las tablas de DDJJ. Si se cambia sólo la primera, la app funciona contra la
> demo pero **sigue escribiendo en la base real del sindicato**: es el riesgo más grave de todo el
> instructivo.

| Aplicación | Clave de configuración | Tiene que apuntar a |
|---|---|---|
| **SIRGA** | `ConnectionStrings:DefaultConnection` | **`SIRGA_DB_DEMO`** |
| **SIRGA** | `ConnectionStrings:LegacyConnection` **y** `ConnectionStrings:Legacy` | **`SINDICATO_DEMO`** |
| **SIGESWEB** | `ConnectionStrings:Sindicato` | **`SINDICATO_DEMO`** |

**Cómo cambiarlas (opción recomendada: variables de entorno, no se toca ningún archivo del repo):**

```powershell
# SIRGA
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=SIRGA_DB_DEMO;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ConnectionStrings__LegacyConnection  = "Server=localhost;Database=SINDICATO_DEMO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:ConnectionStrings__Legacy            = $env:ConnectionStrings__LegacyConnection

# SIGESWEB
$env:ConnectionStrings__Sindicato = "Server=localhost;Database=SINDICATO_DEMO;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

*(En SIGESWEB las cadenas usan autenticación SQL; para la demo alcanza con las mismas credenciales
apuntando a `SINDICATO_DEMO`. **No escribir la contraseña en ningún archivo versionado ni en este
documento**: se toma de `.dsh-tools\.sqlpwd`, ver `AGENTS.md`.)*

**Alternativa**: un `appsettings.Demo.json` en cada app y levantar con
`ASPNETCORE_ENVIRONMENT=Demo`. **Nunca** dejar las cadenas de demo en `appsettings.json` (es el archivo
que se despliega).

## A.3. Verificar ANTES de usarlo

1. **Que la app esté leyendo la demo**: entrar a `/companies` y comparar el contador de empresas con el de
   la base real (si la demo es una copia, el número es el mismo; sirve el chequeo de escritura).
2. **Chequeo de escritura controlado**: guardar un cambio mínimo (por ejemplo el teléfono de un empleado
   de una empresa de prueba) y confirmar con SQL **en qué base** quedó:

   ```sql
   -- En la REAL no tiene que aparecer el cambio
   USE SIRGA_DB;      SELECT Mobile FROM Employees WHERE Dni = '<cuil de prueba>';
   USE SIRGA_DB_DEMO; SELECT Mobile FROM Employees WHERE Dni = '<cuil de prueba>';
   ```
3. **Chequeo del legado** (el que más importa): después de presentar una DDJJ de prueba, contar en las dos
   bases:
   ```sql
   SELECT COUNT(*) FROM ddjjt WHERE usuario = 'SIRGA';  -- en SINDICATO_DEMO sube; en Sindicato REAL no cambia
   ```
4. **Contar antes y después** en las bases reales (`Employees`, `Declarations`, `fotos_.maesoc`) y confirmar
   que no se movieron.

## A.4. Qué tener en cuenta

- **Trae datos personales reales** (nombre, CUIL, sueldo). Sirve para entrenar; para mostrar a alguien de
  afuera conviene decidirlo explícitamente.
- **Los usuarios y contraseñas son los mismos** que en la real (la copia incluye Identity): eso es una
  ventaja para el entrenamiento, y también significa que **las credenciales de producción funcionan en la
  demo**.
- **Las dos apps pueden compartir `SINDICATO_DEMO` al mismo tiempo**: es justo lo que se quiere para
  practicar el circuito (SIRGA escribe la DDJJ, SIGESWEB la lee).
- **Revisar los servicios en segundo plano de SIGESWEB** (si alguno llama a AFIP, manda mails o procesa
  archivos) antes de dejarla corriendo contra la demo.
- Para “resetear” la demo: volver a restaurar las copias desde los mismos `.bak`.

---

# Parte B — Puesta en producción

## B.1. Artefactos ya compilados (2026-11-04)

| Aplicación | Carpeta | Tamaño | Nota |
|---|---|---|---|
| **SIRGA** | `C:\Proyectos\_PUBLISH\2026-11-04\SIRGA` | 38,8 MB (62 archivos) | `SIRGA.WebUI.dll` 942 KB |
| **SIGESWEB.Web** | `C:\Proyectos\_PUBLISH\2026-11-04\SIGESWEB.Web` | 26,8 MB (45 archivos) | `SIGESWEB.Web.dll` 1,46 MB |
| **SIGESWEB.Carnet.Api** | `C:\Proyectos\_PUBLISH\2026-11-04\SIGESWEB.Carnet.Api` | 25 MB (45 archivos) | app aparte (carnet) |
| **Script de esquema SIRGA** | `...\SQL\SIRGA_esquema_completo_idempotente.sql` | 101 KB | `dotnet ef migrations script --idempotent` |

- Los tres publicados son **framework-dependent** (`net10.0`): el servidor necesita el
  **.NET 10 ASP.NET Core Runtime** (o reinstalar como self-contained).
- Incluyen `web.config` (IIS) y, en SIRGA, **`wwwroot/_content` y `SIRGA.WebUI.styles.css`**: por eso en
  Production **el CSS no se rompe** (la trampa de `Development` es de correr desde `bin`, no de un publish).
- **No** se copiaron `bin`/`obj` ni archivos de desarrollo. Sí viajan `appsettings*.json` (ver B.3).

## B.2. Requisitos del servidor

1. **.NET 10 ASP.NET Core Runtime** (x64). Sin esto la app no arranca.
2. **SQL Server** accesible y las bases de producción.
3. **IIS** con el *ASP.NET Core Module v2* (si se publica en IIS) o un servicio de Windows que corra
   `dotnet SIRGA.WebUI.dll`.

## B.3. BLOQUEANTES a resolver antes de salir a producción

> Estos puntos **no son código**: son configuración que falta y que puede dejar el sistema a medias.

1. **`SIRGA/appsettings.Production.json` NO tiene cadenas de conexión.** Hoy tiene sólo logging. Como
   `appsettings.json` (el que se publica) apunta a **`localhost`**, en el servidor de producción SIRGA
   intentaría conectarse a un SQL que está en otra máquina y **no arrancaría**. Hay que definirlas —por
   `appsettings.Production.json` o por variables de entorno— **con las dos**: `DefaultConnection` y
   `LegacyConnection`.
2. **`ApiBaseUrl` sigue en `http://localhost:5122/`** (está en `appsettings.json`). Se usa para armar links
   (pago/QR): tiene que ser la URL pública.
3. **SIGESWEB ya tiene su cadena de producción** (`appsettings.Production.json`), pero **las contraseñas
   están en texto plano en el repositorio**: rotarlas y sacarlas del archivo antes de publicar (pendiente
   conocido, ver `CONTEXTO_SIRGA.md` §10.6).
4. **Correo y pagos**: el SMTP de SIGESWEB está **vacío** y SIRGA **no tiene token de MercadoPago** en este
   entorno. Sin eso, los mails no salen y el link de pago da **403**. No es un bug del código: es
   configuración.
5. **Esquema de SIRGA**: correr **`SIRGA_esquema_completo_idempotente.sql`** contra la base de producción
   **antes** de levantar la versión nueva. Es idempotente (se puede correr más de una vez) pero **igual hay
   que hacer respaldo antes**. Los scripts viejos del repo (`script_sirga_db_produccion.sql`,
   `script_sirga_db_upgrade_produccion.sql`) están **desactualizados**: no crean las tablas ni columnas
   nuevas (ramas, actividad, obra social, bitácora de empleado, propuesta de escala).
6. **Rutas de archivos de SIGESWEB**: su `appsettings.Production.json` apunta a
   `C:\SIGESWEB\App_Data\fotos` y `...\documentos` (carnet). Verificar que existan y tengan permisos en el
   servidor.

## B.4. Cómo levantar cada una

```powershell
# SIRGA (en el servidor, carpeta del publish)
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet .\SIRGA.WebUI.dll --urls "http://0.0.0.0:5122"

# SIGESWEB
$env:ASPNETCORE_ENVIRONMENT = "Production"
dotnet .\SIGESWEB.Web.dll --urls "http://0.0.0.0:8080"
```

En IIS: *Application Pool* con **“No Managed Code”** y el **hosting bundle** de .NET 10 instalado.

## B.5. Checklist de despliegue

- [ ] Respaldo de las bases de producción (y verificado que se puede restaurar).
- [ ] Cadenas de conexión de SIRGA completas (Default + Legacy) y `ApiBaseUrl`.
- [ ] Contraseñas de SIGESWEB fuera del repo y rotadas.
- [ ] `SIRGA_esquema_completo_idempotente.sql` corrido (y releído: `EmployeeChangeLogs`, `Activities`,
      `HealthInsurances`, `ScaleSalary`).
- [ ] Publish copiado al servidor y **sin `appsettings.Development.json`**.
- [ ] Arranque, login y `/companies` OK; un PDF de boleta generado.
- [ ] Una DDJJ de prueba **en un entorno de prueba**, no en producción.

---

# Parte C — Estado verificado el 2026-11-04

| | |
|---|---|
| Repositorio SIRGA | al día, `main` = `origin/main` = `0dc31d6` (+ los commits de este instructivo) |
| Build / tests | 0 errores, 0 avisos de código, **168/168** tests |
| Publicaciones | las 3 de la tabla B.1, compiladas en Release ese mismo día |
| Base `SIRGA_DB` | 4.507 empresas · 27.934 empleados · 36.011 asignaciones · 248.163 DDJJ · 1.269.999 detalles |
| Padrón `Sindicato` | 27.924 socios · 36.019 relaciones socio-empresa |
| Escalas | cargadas hasta **2026-10** (noviembre usa la de octubre por *fallback*) |
| Tasas | 2,75% resarcitoria / 3,50% punitoria desde 01/07/2025 |

**Hallazgo en SIGESWEB (no corregido, es de la otra sesión)**: al publicar aparecen
**2 avisos `MUD0002`**: `Illegal Attribute 'OnValueChanged' on 'MudNumericField'` en
`Components/Pages/Estudios/GestionCobranzaDialog.razor`. Es el mismo tipo de defecto que encontramos en
SIRGA: el atributo no existe, se renderiza al HTML y **no hace nada**. Conviene revisarlo.

---

# Parte D — Pendientes conocidos (no bloquean la demo)

1. **Permisos del repo de SIGESWEB**: contrastar la cadena de producción con el servidor real antes de
   usarla (`192.168.1.240`).
2. **La bitácora de empleado** no se puede filtrar por fecha ni por usuario todavía.
3. **La solicitud al estudio contable** para cambiar datos que no sean teléfono/domicilio: es un flujo que
   vive en SIGESWEB, no en SIRGA.
4. **2 commits sin pushear** al momento de escribir esto (informe de períodos sin DDJJ). Antes de congelar
   la versión para producción, confirmar que `git status` está limpio y `origin/main` al día.
