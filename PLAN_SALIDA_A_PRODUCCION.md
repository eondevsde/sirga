# PLAN DE SALIDA A PRODUCCIÓN — SIRGA + SIGESWEB

> Procedimiento **probado en la demo** (2026-11-05). Es el mismo camino que se sigue para producción:
> primero en simulacro, después en serio. Cada paso informa qué haría antes de hacerlo.
>
> **Regla de oro**: nada se escribe sin mirar antes el simulacro, y al final el verificador tiene que
> decir **CONSISTENTE**.

---

## 0. Antes de empezar (bloqueantes)

| # | Qué | Cómo se verifica |
|---|---|---|
| 1 | **.NET 10 ASP.NET Core Hosting Bundle** en el servidor | `dotnet --list-runtimes` muestra `Microsoft.AspNetCore.App 10.x` |
| 2 | **SQL Server**: producción corre en **Express**, con el archivo en **9,0 GB** de un límite de **10 GB** | `SELECT name, size*8/1024 FROM sys.master_files` — si está cerca del techo, archivar antes |
| 3 | **Cadenas de conexión de SIRGA en `appsettings.Production.json`**: `DefaultConnection`, `LegacyConnection` **y** `Legacy` (las tres) | Sin las tres, la app no arranca o escribe en la base equivocada |
| 4 | **`ApiBaseUrl`** con la URL pública | `appsettings.json` |
| 5 | **Respaldo completo** de las bases de producción | `BACKUP ... WITH COPY_ONLY` y verificar el archivo |
| 6 | **Contraseña de SQL de SIGESWEB rotada** (pendiente anotado) | Antes de subir el repo |

---

## 1. Base del sindicato (`Sindicato`): poner al día el esquema

La base de producción **ya existe**; sólo le faltan las migraciones del **carnet** (14 tablas en el
esquema `carnet`).

```
sqlcmd -S <servidor> -E -b -I -d Sindicato -i SIGESWEB_migraciones_idempotente.sql
```

- `-I` es obligatorio con `sqlcmd` (índices filtrados exigen `QUOTED_IDENTIFIER ON`).
- Idempotente: se puede correr dos veces.
- **Esperado**: tablas `213 → 227`, historial `21 → 27`.

---

## 2. Base de SIRGA (`SIRGA_DB`): crear y poblar el esquema

> ⚠️ **La cadena de migraciones de SIRGA no sirve para crear una base desde cero** (hay 8 columnas
> agregadas dos veces y migraciones escritas a mano). Para una base nueva se parte de una **copia
> sólo-esquema** de la base de desarrollo.

1. `script_sirga_db_produccion_01_crear_base.sql` → crea la base (SIMPLE, tamaños explícitos).
2. Copia sólo-esquema → restaura el esquema completo con el historial de migraciones correcto.
3. `script_sirga_db_produccion_03_verificar.sql` → comprueba tablas y columnas.

---

## 3. Datos: el ETL (`SIRGA.Migration`)

Todos los pasos aceptan `--origen <base>`, `--destino <base>` y `--simulacro`.

```
# 3.1 Geografía, sindicato, categorías y escalas (una sola vez)
dotnet run --project SIRGA.Migration -- all --origen Sindicato --destino SIRGA_DB --simulacro

# 3.2 Empresas, empleados y asignaciones (incrementales: se pueden repetir sin duplicar)
dotnet run --project SIRGA.Migration -- companies   --origen Sindicato --destino SIRGA_DB
dotnet run --project SIRGA.Migration -- employees   --origen Sindicato --destino SIRGA_DB
dotnet run --project SIRGA.Migration -- assignments --origen Sindicato --destino SIRGA_DB

# 3.3 Estados de altas y bajas (empleados y asignaciones). SÓLO en la carga inicial.
dotnet run --project SIRGA.Migration -- align --origen Sindicato --destino SIRGA_DB

# 3.4 Declaraciones + renglones + estados de las DDJJ (incremental, repara incompletas)
dotnet run --project SIRGA.Migration -- sync --origen Sindicato --destino SIRGA_DB
```

> **¿Se puede repetir? Sí, todas las veces que haga falta.** Mientras el sistema viejo siga siendo el
> operativo (hasta el corte), **los datos cambian todos los días**: se presentan DDJJ, se pagan, se
> rectifican, entran socios y se dan de baja. Todo el proceso es **incremental e idempotente**: se puede
> volver a correr entero y sólo trae lo nuevo, repara lo incompleto y refresca los estados.
>
> **La regla del `align`**: se corre en **cada** sincronización **mientras el sistema viejo sea el
> operativo**, porque en esa etapa el legado es la fuente de verdad. **Después del corte ya no se corre**:
> desde ahí SIRGA es el dueño del ciclo de vida (crea empleados, los da de baja y empuja los cambios al
> padrón al guardar la ficha). Correrlo después revertiría las altas y bajas hechas en SIRGA.

**Nota**: el `sync` puede necesitarse **dos veces** seguidas: la primera crea empresas que sólo están
en las DDJJ y la segunda carga sus declaraciones.

---

## 4. Estudios contables

```
dotnet run tools/importar-estudios.cs -- "<ruta csv>" "SIRGA_DB"                    # simulacro
dotnet run tools/importar-estudios.cs -- "<ruta csv>" "SIRGA_DB" --aplicar --bloquear
```

- Actualiza datos del estudio, crea los que faltan, cambia el login si el CSV trae otro email.
- Pone la contraseña **`12345`** a todos los usuarios de estudio.
- Con `--bloquear`, da de baja lógica (`IsBlocked`) los estudios que no figuran en el archivo.
- **Revisar antes** la lista de bloqueados: los que parecen reales no deberían bloquearse.

---

## 5. Verificación: criterio de aceptación

```
dotnet run --project SIRGA.Migration -- check --origen Sindicato --destino SIRGA_DB
```

**Tiene que decir: `CONSISTENTE: no hay diferencias que requieran accion.`**

Eso incluye los **estados** (sección 5): empleados y asignaciones tienen que coincidir en alta/baja y en la fecha de baja.

Los ítems informativos (no son problemas) que puede listar:

| Ítem | Por qué es normal |
|---|---|
| Declaraciones sin renglones (períodos 2010-2016) | El sistema viejo tampoco los tiene |
| Empleados que sólo existen en las DDJJ | Aparecen en declaraciones pero no tienen ficha en el padrón |
| Borradores de SIRGA | Declaraciones todavía no presentadas |
| Asignaciones de SIRGA que no están en el legado | Empleados con DDJJ pero sin relación en `socemp` |
| Pares con CUIT `99999999999` | Empresa genérica del sistema viejo (se ignora) |

---

## 6. Aplicación: publicación y prueba

1. Publicar SIRGA (`dotnet publish -c Release`) a la carpeta del sitio.
2. **Borrar `appsettings.Development.json`** de la carpeta publicada (apunta a la base de desarrollo).
3. Verificar las tres cadenas y que **no haya referencias a la base real en la config de la demo**.
4. Probar: login, Empresas, Empleados, Nueva DDJJ (presentar una de prueba) y el Historial.
5. Verificar en SQL que la DDJJ quedó en la base correcta y **no** en la otra.

---

## 7. Lo que queda pendiente de decisión (no bloquea la salida)

- **SQL Express y los 10 GB**: decidir licencia Standard o archivar datos viejos (`ddjj_`, `ddjjt_`).
- **Recuperación**: las bases quedaron en **SIMPLE** (sin recuperación a un punto en el tiempo). Si se
  quiere, programar backups de log y volver a FULL.
- **Respaldo fuera del disco**: los respaldos siguen en el mismo disco que las bases.
- **Contraseña `12345` para estudios**: en producción conviene forzar el cambio en el primer ingreso.
