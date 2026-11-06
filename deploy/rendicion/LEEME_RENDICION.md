# RENDICIÓN DEL BANCO — circuito completo (cifrado → SIRGA → pagos asentados)

> Validado el 2026-11-06 con archivos reales del banco.

---

## 1. Qué manda el banco

Tres archivos, **los tres cifrados con OpenPGP** (RSA-2048) a la clave del sindicato:

| Archivo | Qué es | Adónde va |
|---|---|---|
| `Pagos_1_160926.bse` | **Los pagos** | `entrada\` → **SIRGA asienta** |
| `Detalle_Recaudacion_20260916.pdf` | Reporte de detalle | `comprobantes\` |
| `Totales Recaudacion Diaria_20260916.pdf` | Reporte de totales | `comprobantes\` |

- Clave: `66A05347345D5F24` (RSA 2048, creada 30/09/2019), *Sindicato Empleados de Comercio*
- La clave **privada no tiene contraseña** (verificado: descifra sin pedirla). Eso permite automatizar sin
  guardar contraseñas, pero obliga a proteger la carpeta del llavero con permisos.
- ⚠️ **Los tres llegan cifrados**, pero sólo el `.bse` es de pagos. Los PDF **no deben ir a `entrada\`**:
  SIRGA intentaría leerlos como rendición y los mandaría a `Errores\`.

---

## 2. El circuito

```
cifrados\  (el banco deja los .pgp; o una regla de Outlook los mueve acá)
    │
    │  01_descifrar-rendicion.ps1   (gpg --decrypt)
    ▼
    ├──► entrada\        *.bse / *.txt   → SIRGA los procesa y asienta los pagos
    ├──► comprobantes\   *.pdf           → para adjuntar / archivar
    ├──► cifrados-procesados\            → los .pgp originales (NUNCA se borran)
    └──► errores\                        → lo que no se pudo descifrar
```

Después, SIRGA toma `entrada\` (config `RendicionBanco:CarpetaEntrada`), asienta los pagos y mueve el
archivo a `Procesados\`. **Ni una persona abre el archivo.**

---

## 3. Preparar el servidor (una sola vez)

**1. Instalar Gpg4win** (ya viene con el servidor si usan Kleopatra). Verificar:

```powershell
gpg --version
```

**2. Crear el llavero propio del sistema** (no el del usuario que lo corre):

```powershell
gpg --homedir C:\Demo\rendicion\gnupg --list-keys
```

**3. Importar la clave privada del sindicato en ese llavero.** Exportarla desde Kleopatra
(*Exportar claves secretas*) o con:

```powershell
gpg --export-secret-keys 66A05347345D5F24 > C:\Demo\clave-privada.asc
gpg --homedir C:\Demo\rendicion\gnupg --import C:\Demo\clave-privada.asc
gpg --homedir C:\Demo\rendicion\gnupg --list-secret-keys       # tiene que aparecer "sec"
```

**4. Borrar el `.asc`** después de importarlo (es la clave privada en texto).

**5. Proteger la carpeta** — sólo administradores y SYSTEM:

```powershell
icacls C:\Demo\rendicion\gnupg /inheritance:r /grant "Administrators:(OI)(CI)F" /grant "SYSTEM:(OI)(CI)F"
```

**6. Probar con un archivo real, en simulacro y después de verdad:**

```powershell
.\01_descifrar-rendicion.ps1                              # muestra qué haría
.\01_descifrar-rendicion.ps1 -Procesar                    # descifra y reparte
```

**7. Programarlo cada 5 minutos:**

```powershell
.\01_descifrar-rendicion.ps1 -Procesar -ProgramarTarea
```

Crea la tarea *"SIRGA - Descifrar rendicion del banco"* cada 5 minutos como SYSTEM.
Ojo: la clave tiene que estar en el llavero de `C:\Demo\rendicion\gnupg` (por eso es un llavero propio
y no el del usuario: la tarea corre como SYSTEM).

---

## 4. Conectar con SIRGA

En el `appsettings.Production.json` del sitio:

```json
"RendicionBanco": {
  "CarpetaEntrada": "C:\\Demo\\rendicion\\entrada",
  "IntervaloMinutos": 30,
  "ModoSimulacro": false
}
```

- `ModoSimulacro: false` → **asienta** los pagos. En `true` lee y concilia pero no escribe.
- La pantalla **/conciliacion/banco** (roles Sindicato/Admin) muestra la carpeta, el intervalo, el modo
  y el resultado de la última revisión, con el botón **"Revisar ahora"**.
- Si el link no aparece en el menú, se llega por URL: `/conciliacion/banco`.

---

## 5. Resultado de la prueba (2026-11-06)

```
Detalle_Recaudacion_20260916.pdf.pgp   -> comprobantes\  (9,3 KB)  OK
Pagos_1_160926.bse.pgp                 -> entrada\       (2,3 KB)  OK
Totales Recaudacion Diaria.pdf.pgp     -> comprobantes\  (3,9 KB)  OK

RESUMEN: 3 descifrados, 0 con error
```

Y el `.bse` resultante es **idéntico (SHA256)** al que SIRGA ya procesó correctamente en una prueba
anterior: el formato que sale del descifrado es exactamente el que el parser espera.

---

## 6. Diagnóstico

| Síntoma | Qué mirar |
|---|---|
| `gpg: decryption failed: No secret key` | La privada no está en **ese** llavero (`--homedir`) |
| El archivo queda en `errores\` | Abrir `descifrado.log` (guarda el motivo) |
| SIRGA no asienta nada | ¿`ModoSimulacro` en `true`? ¿`CarpetaEntrada` apuntando a `entrada\`? |
| El `.bse` cae en `Errores\` de SIRGA | Revisar que sea `.bse`/`.txt` y que no esté el PDF mezclado |
| La tarea corre pero no descifra | La tarea corre como SYSTEM: la clave tiene que estar en el llavero de `<Base>\gnupg` |

**Log**: `<Base>\descifrado.log` — una línea por archivo, con OK o el error de gpg.
