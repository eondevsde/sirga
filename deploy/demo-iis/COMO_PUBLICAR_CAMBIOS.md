# CÓMO PUBLICAR CAMBIOS EN LA DEMO (IIS) — guía de bolsillo

> Todo lo que aprendimos publicando SIRGA en IIS. Si algo falla después de un cambio,
> buscá el error en la tabla del final: están los cinco que nos costaron la noche.

---

## 1. La configuración que FUNCIONA (no la cambies sin motivo)

| Pieza | Valor | Por qué |
|---|---|---|
| Publicación | **autocontenida** (`win-x64`, `--self-contained true`) | Lleva su propio .NET adentro: IIS no tiene que resolver ningún framework |
| `web.config` → `processPath` | **`.\SIRGA.WebUI.exe`** | Usa el runtime de la propia carpeta |
| `hostingModel` | `inprocess` | Es el recomendado y es el que anda |
| App Pool | `managedRuntimeVersion = v4.0`, `32bit=False`, `Integrated`, `ApplicationPoolIdentity` | **Anda así: no tocar** |
| Permisos de carpeta | `IIS APPPOOL\<sitio>` con **FullControl** | Sin esto: 500.0 y `logs` vacío |
| Config | `SIRGA_DB_DEMO` + `SINDICATO_DEMO`, `Server=.\SQLEXPRESS` | Escritas por el script, **nunca** copiadas del repo |

### Sitios

| Sitio | Carpeta | App Pool | Base |
|---|---|---|---|
| `sirga-demo` | `C:\inetpub\sirga-demo` | `sirga-demo` | `SIRGA_DB_DEMO` + `SINDICATO_DEMO` |
| `sigesweb-demo` | `C:\inetpub\sigesweb-demo` | `sigesweb-demo` | `SINDICATO_DEMO` |
| `sigesweb-demo-carnet` | `C:\inetpub\sigesweb-demo-carnet` | `sigesweb-demo-carnet` | `SINDICATO_DEMO` |

### Rutas

| Qué | Dónde |
|---|---|
| Código (máquina de desarrollo) | `C:\Proyectos\SIRGA` |
| Publicación autocontenida (desarrollo) | `C:\Proyectos\SIRGA_DEMO_SC` |
| Publicación en el servidor (staging) | `C:\Publicaciones\SIRGA_DEMO_SC` |
| Scripts de despliegue en el servidor | `C:\Demo\instalador` |

---

## 2. Ciclo para publicar una corrección (3 minutos)

### Paso 1 — En la máquina de desarrollo

```powershell
cd C:\Proyectos\SIRGA
.\deploy\demo-iis\07_publicar_desde_dev.ps1 -Servidor "\\SERVIDOR"
```

Ese script compila en Release, publica autocontenido y copia a `\\SERVIDOR\C$\Publicaciones\SIRGA_DEMO_SC`.

### Paso 2 — En el servidor

```powershell
cd C:\Demo\instalador
.\08_desplegar_en_servidor.ps1                      # simulacro
.\08_desplegar_en_servidor.ps1 -Aplicar             # despliega
```

Ese script: para el pool → vacía la carpeta del sitio → copia → **escribe la configuración demo** →
deja el `web.config` con el `.exe` → arregla permisos → arranca el pool → **hace la prueba HTTP**.

### Paso 3 — Verificar

```powershell
.\04_verificar.ps1
```

Y abrir `https://sirga-demo.secsantiago.com.ar` con `admin@sirga.com` / `12345`.

---

## 3. Reglas que no se negocian

1. **Nunca copiar `appsettings.json` del repo**: apunta a `SIRGA_DB` (la base REAL). El script de despliegue **siempre** reescribe la configuración.
2. **Nunca `robocopy /MIR` sobre una carpeta que no sea `*demo*`**: borra lo que sobra en el destino.
3. **El `publish` pisa `appsettings.json`** con la del repo. Si publicás a mano, volvé a escribir la configuración.
4. **Los `.ps1` van con BOM** (UTF-8 con BOM). Sin BOM, PowerShell 5.1 rompe la sintaxis por los acentos.
5. **No tocar** el `managedRuntimeVersion=v4.0` ni el bitness del pool: funcionan.
6. **`iisreset` después de tocar bindings, certificados o módulos.**

---

## 4. Catálogo de errores (los que ya nos pasaron)

| Error | Qué significa | Solución |
|---|---|---|
| **500.31** — *Failed to load ASP.NET Core runtime* | IIS no resuelve el framework de la app (publicación framework-dependent) | Publicar **autocontenido** y `processPath=".\SIRGA.WebUI.exe"` |
| **500.0** — *ASP.NET Core IIS hosting failure (in-process)* + `logs` vacío | El proceso se lanza pero no arranca: **no puede escribir el log** o no puede leer la carpeta | `icacls <carpeta> /grant "IIS APPPOOL\<sitio>:(OI)(CI)F" /T` y reciclar el pool |
| **500.30** — *In-Process Handler Load Failure* | La app **sí** arranca y se cae (config, base, excepción de start) | Mirar `logs\stdout_*.log`; casi siempre `Login failed for user 'IIS APPPOOL\...'` |
| **502.5** — *Process Failure* | La app no llegó a escuchar | Igual que 500.30: log de stdout |
| **404** con el sitio "iniciado" | El binding no coincide con el dominio pedido | `Get-WebBinding -Name <sitio>`; corregir `bindingInformation`; tener **puerto 80 y 443** |
| **"Not secure"** en el navegador | El sitio no tiene certificado propio | Copiar el de otro sitio que ande: `AddSslCertificate($huella, "my")` + `iisreset` |
| **500.31 persiste con autocontenido** | El módulo ANCM es viejo | Instalar el **.NET 10 Hosting Bundle** y `iisreset` |
| **La pantalla se ve sin estilos** | No se copió `wwwroot` o se publicó en Development | Ver que exista `wwwroot` y que `ASPNETCORE_ENVIRONMENT=Production` |
| **No conecta a la base** | Instancia mal escrita | Es `.\SQLEXPRESS`, **no** `localhost` |
| **`Token ... inesperado` / `Falta la cadena en el terminador`** al correr un `.ps1` | Archivo sin BOM | `$t=[IO.File]::ReadAllText($f); [IO.File]::WriteAllText($f,$t,(New-Object Text.UTF8Encoding($true)))` |

---

## 5. Diagnóstico rápido (los 4 comandos que sirven)

```powershell
# 1) ¿el .exe arranca fuera de IIS? (el dato más útil)
cd C:\inetpub\sirga-demo
$env:ASPNETCORE_ENVIRONMENT="Production"
.\SIRGA.WebUI.exe --urls "http://localhost:5305"

# 2) el log de arranque
Get-Content (Get-ChildItem C:\inetpub\sirga-demo\logs\*.log | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -Tail 40

# 3) el evento de Windows
Get-WinEvent -FilterHashtable @{LogName='Application'; StartTime=(Get-Date).AddHours(-2)} -MaxEvents 100 |
  Where-Object { $_.Message -like "*SIRGA*" -or $_.ProviderName -like "*AspNetCore*" } |
  Select-Object -First 3 TimeCreated, Message | Format-List

# 4) estado del sitio y del pool
Get-Website -Name sirga-demo | Format-List Name, State, physicalPath, applicationPool
Get-WebAppPoolState sirga-demo
```

**Regla de oro del diagnóstico**: si el `.exe` arranca a mano y en IIS no, el problema es **IIS**
(permisos, bindings, módulo). Si no arranca a mano, el problema es **la app o la configuración**.

---

## 6. Volver atrás (rollback)

Guardar la carpeta del sitio antes de desplegar:

```powershell
Copy-Item C:\inetpub\sirga-demo "C:\Demo\backup-sirga-demo-$(Get-Date -Format yyyyMMdd-HHmm)" -Recurse
```

Y para volver: parar el pool, borrar la carpeta, copiar el backup, arrancar el pool.

---

## 7. Cuando haya que pasar a producción

Este mismo procedimiento sirve, cambiando:
- sitios y carpetas (`sirga`, `sigesweb`) en lugar de los `-demo`,
- las bases (`SIRGA_DB`, `Sindicato`) en lugar de las `_DEMO`,
- la publicación: producción **no** se toca sin respaldo y sin ventana.

El detalle está en `PLAN_SALIDA_A_PRODUCCION.md`.
