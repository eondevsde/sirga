# AGENTS.md — instrucciones para agentes que trabajen en SIRGA

## Lo primero, siempre

**Leé `CONTEXTO_SIRGA.md` completo antes de tocar nada.** Ahí está el estado del proyecto, las trampas
que ya nos costaron caro, el mapeo de datos del sistema anterior y lo que queda pendiente en orden.
Si no lo leés, vas a repetir errores que ya cometimos.

Al **cerrar** la sesión, **actualizá `CONTEXTO_SIRGA.md`** con lo que hiciste y lo que quedó pendiente:
si no, se convierte en un documento viejo que miente, que es peor que no tenerlo.

Respetá también lo que diga `.github/copilot-instructions.md`.

---

## Reglas que no se negocian

1. **Nunca tocar producción.** Se verifica **siempre en desarrollo** (`SIRGA_DB`, y la copia local
   `Sindicato` del sistema anterior). Producción no se consulta ni se escribe.
2. **Nunca borrar datos del usuario.** Se desactiva o se respalda. Antes de un `UPDATE` masivo:
   **simulacro (dry-run) → respaldo → OK explícito del usuario**.
3. **Todos los scripts SQL, idempotentes.**
4. **No hay fallos silenciosos.** Nada de botones muertos, nada de datos que se pierdan sin aviso.
5. **No commitear sin build en 0 avisos Y tests en verde.** Es un gate, no un trámite.
6. **`git add` con rutas explícitas.** Los archivos del usuario (`.dsh-*`, `SIRGA_Init-historial.md`)
   quedan sin trackear a propósito: **nunca** `git add -A`.
7. **La contraseña de SQL se lee de `.dsh-tools\.sqlpwd`** (ignorado por git). Nunca se escribe en un
   comando, en código, en un log ni en un informe.

---

## Cómo trabajar acá

- **Compilar requiere matar la app primero** (bloquea los DLL): buscá el proceso que **escucha el puerto
  5122** con `Get-NetTCPConnection -LocalPort 5122` y matalo. **No** mates por nombre de proceso.
- **La app se levanta como job en background** con `workdir = C:\Proyectos\SIRGA\SIRGA.WebUI`, y
  **siempre con `ASPNETCORE_ENVIRONMENT=Development`**:
  `$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet "C:\Proyectos\SIRGA\SIRGA.WebUI\bin\Debug\net10.0\SIRGA.WebUI.dll" --urls "http://localhost:5122"`
  Si la levantás desde la raíz del repo, falla por la cadena de conexión.
  **Sin `ASPNETCORE_ENVIRONMENT=Development` la app arranca como Production, los *static web assets*
  no se sirven y el CSS de MudBlazor da 500**: la pantalla se ve **sin estilos y gigante**. No es un
  problema del marcado. Ver `CONTEXTO_SIRGA.md`, sección 4.
- **Al terminar, dejá la app levantada** y decile al usuario en qué URL.
- **No podés operar el navegador**: lo visual, los clics y los popups de MudBlazor los verifica el
  usuario. Decí explícitamente **qué no pudiste verificar**, en lugar de darlo por bueno.
- **Cuando toques escritura de datos, verificá guardando y volviendo a leer de la base.** Que compile y
  que pasen los tests **no** prueba que guarde.
- Para consultar la base tenés la **pestaña SQL** del GUI (sólo lectura) o `sqlcmd`. **Medí antes de
  afirmar**: si decís que un dato está mal, mostrá la consulta y el resultado.
