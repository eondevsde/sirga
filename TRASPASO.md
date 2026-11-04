# TRASPASO — cómo retomar sin perder nada

> **Para el próximo agente (o yo mismo en una sesión nueva).** Leé esto **antes** de tocar nada, y después
> `CONTEXTO_SIRGA.md` completo.
> Fecha: **2026-11-03 22:30** · Último commit: **4071b66**

---

## 1. ESTADO AHORA MISMO

| | |
|---|---|
| **Repositorio** | **Todo commiteado** (verificado con `git status`) |
| **Build** | **0 errores** |
| **Tests** | **165/165** |
| **App** | **Corriendo** en `http://localhost:5122` con el binario **nuevo** |
| **Remoto** | `https://github.com/eondevsde/sirga.git` (sincronizado hasta `9984c02`; faltan pushear los últimos commits) |

```powershell
git -C "C:\Proyectos\SIRGA" push      # quedan commits locales sin subir
```

### Archivos SIN trackear (a propósito, NO borrar)

```
INFORME_ESCALAS_TASAS_2026.md                    <- trabajo de la OTRA sesión
script_sirga_db_escalas_2025_12_corregir.sql     <- idem
script_sirga_db_escalas_2026_abril_septiembre.sql
script_sirga_db_escalas_2026_octubre.sql
script_sirga_db_escalas_huecos_2021_2023.sql
script_sirga_db_tasas_interes_2026.sql
tools/Respuesta al banco/                        <- PDF y DOCX para el Secretario General (datos del banco: NO versionar)
```

⚠️ **El usuario tiene OTRA SESIÓN abierta en el mismo workspace** que carga escalas y tasas. **No tocar sus
archivos** y **no trabajar en los mismos archivos a la vez** (se bloquean los `.dll` y se mezclan commits).

---

## 2. LO QUE SE ESTABA HACIENDO: la pantalla "Gestión de Empresas"

**Pedido del usuario**: *"reordena / organiza esta pantalla algo más profesional"* (`/companies`).

**Hecho y commiteado** (`ed1dde3`, `4071b66`):

- ✅ **Cabecera rehecha**: título + subtítulo con la cantidad de empresas, y el botón **Nueva Empresa** a la derecha.
- ✅ **Rótulo "Clientes / Empresas" eliminado** (parecía un breadcrumb roto).
- ✅ **Botón duplicado eliminado** de la barra de filtros (quedó sólo el de la cabecera).
- ✅ **Márgenes `ml-4` → `ml-2`** (los filtros quedan en una fila con aire).

**LO QUE FALTA en esa pantalla** (y es lo que más se nota):

> **Cada fila tiene 8 iconos sueltos** en la columna "Acciones" (declarar, empleados, ver, editar, boleta,
> etc.). **Conviene agruparlos en un menú `⋮` (MudMenu)** con las acciones adentro, dejando visibles sólo las
> dos o tres principales.
> **Antes de hacerlo hay que identificar qué hace cada icono** (leer la zona de `RowTemplate`/`Acciones` de
> `Companies.razor`, alrededor de la línea 120-180).

**Y no se ha empezado la reorganización de las OTRAS pantallas.** El usuario dijo que *"muchas pantallas
tienen objetos que no renderizan bien"*: se arreglaron **las tablas** (scroll horizontal, 36 tablas) y **los
números de las tarjetas** (clamp), pero **falta el rediseño fino** de cada pantalla.

---

## 3. LOS ERRORES DE ESTA SESIÓN — leerlos, no repetirlos

**Todo lo que sigue pasó de verdad y está en el historial de git.**

### 3.1. El patrón grave: el commit dice más de lo que hizo

Pasó **cuatro veces**: el mensaje afirmaba que algo estaba hecho y **no lo estaba** (el email del menú dos
veces, el redirect del estudio, los filtros en grilla). La causa **siempre** fue la misma:

> **El `git commit` corría aunque la verificación no hubiera pasado.**

**REGLA**: la verificación va **antes** y el commit **sólo si da verdadero**. Si no, `exit 1` y no se
commitea. Y **nunca** escribir el mensaje antes de ver el resultado.

### 3.2. Verificar la cosa equivocada

El control de "un solo botón Nueva Empresa" contaba **todas** las apariciones del texto, incluido **el título
del diálogo** (`ShowAsync<AddCompanyDialog>("Registrar Nueva Empresa", ...)`). Dio 2 y bloqueó un commit
**correcto**.

**REGLA**: la verificación tiene que apuntar **exactamente** a lo que se cambió, no a un texto genérico.

### 3.3. Variables reservadas de PowerShell

- `$home` **es la variable automática `HOME`** (case-insensitive, sólo lectura). Mi asignación falló en
  silencio y el reemplazo se hizo sobre la ruta vacía ⇒ **el redirect del estudio quedó sin cambiar** y el
  build **no lo detectó** (era un string) ⇒ el estudio habría caído en **404**.
- `"$fin:"` **no es válido** (hay que usar `${fin}`). Un error de sintaxis que abortó un script entero.

**REGLA**: no usar `$home`, `$host`, `$pid`, `$input`, `$args`, `$error`, `$profile`. Y en strings con `:`
usar `${}`.

### 3.4. Commits con el build roto

Dos veces: commiteé con el build fallando porque los `.dll` estaban bloqueados por la app corriendo.

**REGLA**: **matar la app antes de compilar**. Si el build dice `MSB3027` / "archivo bloqueado", es eso.
Y el commit va **atado** al resultado del build.

### 3.5. El puerto 5122 "libre" que no estaba libre

`netstat | Select-String ":5122\s"` **no muestra** un proceso que tiene el puerto **tomado pero no en
LISTENING** (una instancia colgada). La app no arrancaba y el error era `address already in use`.

**REGLA**: usar **`Get-NetTCPConnection -LocalPort 5122`** (lo dice el `AGENTS.md`), que muestra **todos**
los estados. Y la app **tarda más de 20 segundos** en arrancar: esperar 35-40.

---

## 4. CÓMO ARRANCAR

```powershell
# 1. matar la app (si está)
Get-NetTCPConnection -LocalPort 5122 -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object { Stop-Process -Id $_ -Force }
Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" | Where-Object { $_.CommandLine -like "*SIRGA.WebUI.dll*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

# 2. build + tests (gate)
dotnet build "C:\Proyectos\SIRGA\SIRGA.slnx" --nologo     # 0 errores (los avisos NU1902/NU1903 son de NuGet, no del código)
dotnet test  "C:\Proyectos\SIRGA\SIRGA.slnx" --nologo     # 165/165

# 3. levantar (SIEMPRE con Development, si no el CSS da 500)
$env:ASPNETCORE_ENVIRONMENT="Development"
Start-Process dotnet -ArgumentList '"C:\Proyectos\SIRGA\SIRGA.WebUI\bin\Debug\net10.0\SIRGA.WebUI.dll" --urls "http://localhost:5122"' -WorkingDirectory "C:\Proyectos\SIRGA\SIRGA.WebUI" -WindowStyle Hidden
Start-Sleep -Seconds 35
Get-NetTCPConnection -LocalPort 5122
```

**Usuarios para probar** (las contraseñas las sabe el usuario):

| Rol | Usuario |
|---|---|
| Admin | `admin@sirga.com` |
| Sindicato | `sindicato@sirga.com` · `SECLABANDA@HOTMAIL.COM` |
| Estudio Contable | `abdala.empleadores@gmail.com` (o cualquiera de los 853) |
| Auditor | `auditor@sirga.com` |

**Al entrar con un Estudio Contable, ahora cae en `/companies`** (se eliminó el Portal Estudio).

---

## 5. PENDIENTES, EN ORDEN

1. **Terminar `/companies`**: agrupar los 8 iconos de acciones en un menú `⋮`.
2. **Rediseñar las demás pantallas** que el usuario marque (con **capturas**: él las manda, yo las leo).
3. **`tools/Respuesta al banco/`** → agregar al `.gitignore` (tiene datos del Banco y está sin trackear).
4. **Push** de los commits locales al remoto.
5. **Copiar `_RESPALDOS` a otro disco** (3,5 GB, siguen en el mismo disco).
6. **Contestarle al banco** (el PDF con la carta está en `tools/Respuesta al banco/`): falta la **fecha de
   producción** y la respuesta sobre **convivencia de DV** (ya decidida: **NO hay convivencia**).
7. **SIGESWEB**: sacar las contraseñas de SQL en texto plano y rotarlas, antes de subirlo a GitHub.
8. **Las cajas**: todo documentado en `C:\Proyectos\SIGESWEB` (4 documentos), sin implementar.

---

## 6. LO QUE YA ESTÁ CERRADO (no rehacer)

- **Dígito verificador de las boletas**: fórmula aprobada por el banco. Ver `CALCULO_DIGITO_VERIFICADOR.md`.
- **Volcado de DDJJ al legado**: hecho y verificado.
- **Conciliación de rendiciones**: completa, en simulacro. Ver `PLAN_CONCILIACION_PAGOS.md`.
- **Guardas de la boleta**: borrador, rectificada y pagada no emiten.
- **Respaldo**: 9 zips + 9 repos + 36 bases (en `C:\Proyectos\_RESPALDOS\`).
- **Tablas con scroll horizontal** (36) y **números de tarjeta con clamp** (23).
- **Portal Estudio eliminado** y el redirect del estudio apuntando a `/companies`.
