# SIRGA — Contexto, estado y traspaso

> **Este archivo es la memoria del proyecto.** Si sos un agente que arranca una sesión: **leé esto
> primero**, entero. Contiene lo que no se deduce del código: las reglas que no se negocian, los
> atajos que ya nos costaron caro y lo que falta hacer.

Última actualización: **2026-11-03** · 165/165 tests · build 0 avisos del compilador (los NU1902/NU1903
de NuGet son 56 y no son del código, ver trampa 20).
(El conteo de commits no se anota acá a propósito: quedaba viejo con cada commit y este documento no
puede mentir. Se consulta con `git rev-list --count HEAD`.)

---

## 1. Qué es SIRGA

SaaS gremial. Los **estudios contables** liquidan **DDJJ mensuales por empresa** para los **sindicatos**:
aporte de ley (2%), cuota socio (2%), seguro de sepelio, pluriempleo, transferencias de empresas entre
estudios. Stack: **.NET 10 Blazor Server** (Interactive Server, prerender ON) + **MudBlazor 9.4.0**,
**EF Core 10 / SQL Server**, Clean Architecture (`Domain`, `Application`, `Infrastructure`, `WebUI`,
`Migration`, `Tests`), **QuestPDF**, ClosedXML, MercadoPago, Identity.

---

## 2. Reglas que NO se negocian

1. **Nunca tocar producción.** Se verifica **siempre en desarrollo**. La base de producción no se
   consulta ni se escribe, ni con credenciales a mano.
2. **Nunca borrar datos del usuario.** Los borradores, bancos y afiliaciones se **desactivan** o se
   respaldan; no se eliminan. Antes de un `UPDATE` masivo: **simulacro, respaldo y OK explícito**.
3. **Todos los scripts SQL son idempotentes.** Se tienen que poder correr las veces que haga falta.
4. **No hay fallos silenciosos.** Si algo no se puede hacer, se ve en pantalla. Nada de botones muertos,
   nada de "pendiente" invisible, nada de datos que se pierden sin avisar.
5. **Nunca commitear sin build Y tests en verde.** El gate salvó varias veces de subir algo roto.
6. **Commitear sólo los archivos propios** (`git add` con rutas explícitas). Los archivos del usuario
   (`.dsh-*`, `SIRGA_Init-historial.md`) quedan **sin trackear a propósito**.

---

## 3. Entorno

| | |
|---|---|
| Repositorio | `C:\Proyectos\SIRGA` (sólo este proyecto) |
| App | `http://localhost:5122` — se levanta como **job en background** con `workdir = SIRGA.WebUI` |
| Base de desarrollo | `SIRGA_DB` en `localhost` (Windows auth) |
| Copia del sistema anterior | `Sindicato` en `localhost` (SQL auth `sqladmin`) — **es una copia local, no producción** |
| Credenciales | En `.dsh-tools\.sqlpwd` (ignorado por git). **Nunca escribir la contraseña en un comando, código ni informe**: se lee del archivo |
| Credenciales que **faltan** | **MercadoPago** (`MercadoPago:AccessToken`) y **SMTP** no están configurados en este entorno. Sin el token de MercadoPago, generar el link de pago da **403 UNAUTHORIZED** ("At least one policy returned UNAUTHORIZED") y sin SMTP el mail con la boleta no sale: los dos se avisan, no son bugs del código. La app los lee de configuración (variable de entorno `MercadoPago__AccessToken`, o user-secrets), nunca de un archivo versionado |
| Pestaña SQL | Plugin de DSH en `C:\Users\drop1\.dsh\profiles\node_modules\@deepseek-ai\dsh-sql-tab` (sólo lectura, dev + legacy, sin producción) |

**Para levantar la app** (job en background, `workdir = SIRGA.WebUI`), **siempre en Development**:
`$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet "C:\Proyectos\SIRGA\SIRGA.WebUI\bin\Debug\net10.0\SIRGA.WebUI.dll" --urls "http://localhost:5122"`

**Para compilar hay que matar la app primero** (bloquea los DLL): buscar el proceso que **escucha el
puerto 5122** con `Get-NetTCPConnection -LocalPort 5122` y matarlo. No matar por nombre.

**Ojo con el sandbox de las sesiones de agente** (verificado el 2026-10-06):

- Con el sandbox en modo restringido (`workspace-write`), **`Get-NetTCPConnection` devuelve vacío aunque
  la app esté escuchando**: da un falso "puerto libre". Usar **`netstat -ano | Select-String ":5122"`**,
  que sí lo ve. Con ese falso negativo se puede llegar a creer que la app está caída cuando está andando.
- En ese mismo modo, **ningún cliente SQL conecta** (ni `sqlcmd`, ni ODBC, ni SqlClient): fallan con
  *"Proveedor de SSL: No hay credenciales disponibles en el paquete de seguridad"*, porque el sandbox le
  bloquea Schannel/SSPI al proceso. **No es la base ni el código.** El servidor es `MSSQL16.DIEGONBHP`
  con `ForceEncryption = 0`. Con el sandbox abierto, conecta sin problema.
- Si un arranque anterior falló al bindear el puerto, puede **quedar un `dotnet` vivo reteniendo los
  DLL** (`error MSB3021/MSB3027: el archivo está bloqueado por ".NET Host"`). Buscar el PID en la línea
  del error y matarlo: matar sólo el que escucha no alcanza.

---

## 4. Trampas que ya nos costaron caro

1. **Comentarios de Razor (`@* *@`) NUNCA dentro de la lista de atributos** de un componente: Razor los
   emite como texto del atributo, el navegador tira `InvalidCharacterError` y **se cae el circuito** de
   Blazor. La pantalla queda muerta. Van en zona de **marcado**, fuera de la etiqueta.
2. **`MudSelect<T>` y sus `MudSelectItem` tienen que compartir el tipo.** Si el select es `T="int?"` y
   los ítems son `int`, MudBlazor no puede convertir el padre, **excepciona al renderizar y mata el
   circuito**. Patrón: `Value="@((int?)x.Id)"`. Este bug apareció 3 veces, dos las arreglamos y una la
   introdujimos.
3. **Los repositorios NO están en el contenedor de DI**: los construye la `UnitOfWork` con **su propio
   contexto** (`new Repository<T>(_context, dbLock)`). Un servicio que inyecte `IRepository<T>` directo
   usa **otro contexto**, y `CompleteAsync()` guarda en el suyo: **las escrituras se pierden en
   silencio**. Para agregar una tabla: propiedad en `IUnitOfWork` + propiedad en `UnitOfWork` construida
   con el mismo contexto y el mismo semáforo.
4. **Blazor Server: el `HttpClient` de un componente no lleva la cookie de sesión.** Las pantallas llaman
   a los **servicios** directamente; los endpoints protegidos devuelven 401 a los componentes.
5. **Disparar y olvidar (`_ = MetodoAsync()`) en un evento de UI traba el circuito.** Los handlers tienen
   que ser `async` y **esperar**.
6. **Cotejaciones distintas entre `SIRGA_DB` y `Sindicato`** (`Modern_Spanish_CI_AS` vs
   `SQL_Latin1_General_CP1_CI_AS`): comparar o concatenar texto entre las dos bases falla. Comparar por
   **número** (casts a `int`/`bigint`) o usar `COLLATE DATABASE_DEFAULT`.
7. **Primero el esquema, después la consulta.** Y nada de SQL dinámico anidado si se puede escribir
   derecho.
8. **Compilar y pasar los tests NO prueba que guarde.** Cuando se toca **escritura de datos**, la
   verificación tiene que ser: guardar y **volver a leer contra la base**.
9. **EF genera `defaultValue: 0` al agregar una columna de enum**, y 0 suele **no ser un valor válido**
   del enum (acá `JornadaType` es 1 = Completa, 2 = Parcial). Si no se corrige, todas las filas
   existentes quedan con un valor inexistente. Al agregar una columna de enum hay que **editar la
   migración a mano** y poner el valor correcto (`defaultValue: 1`).
10. **La migración es la fuente de verdad del dato, no el nombre del campo.** `Assignments.CreatedAt`
   *parece* un timestamp técnico, pero en realidad guarda `SOCEMP_FECHAING` (fecha de ingreso a la
   empresa): es el dato de negocio del que sale la **antigüedad**. `Employees.HireDate` es la fecha de
   alta al sindicato y **da otra antigüedad**. Antes de usar un campo de fecha, mirar de dónde lo trajo
   `SIRGA.Migration/Program.cs`.
11. **La app tiene que arrancar en `Development`, o la UI se ve sin estilos y "gigante".** Sin
   `ASPNETCORE_ENVIRONMENT=Development` arranca como **Production**, y en Production los *static web
   assets* de los paquetes **no se sirven**: `_content/MudBlazor/MudBlazor.min.css` devuelve **500** y
   `SIRGA.WebUI.styles.css` **404**. El HTML se renderiza pelado (fuentes y SVG enormes) y **parece**
   que se rompió el marcado, pero no: el `.razor` está bien. En el log se ve
   `Hosting environment: Production` y
   `Static Web Assets are not enabled`. Se comprueba en un segundo pidiendo el CSS:
   `Invoke-WebRequest http://localhost:5122/_content/MudBlazor/MudBlazor.min.css` → tiene que dar 200
   (≈594 KB). Después de arreglarlo, el navegador necesita **recarga forzada (Ctrl+F5)**, porque quedó
   con el CSS roto en caché.
12. **QuestPDF necesita una carpeta temporal escribible.** Si `%TEMP%` no se puede escribir, la
   generación del PDF **revienta** con `Unable to find a suitable temporary storage location` (y encima
   como `TypeInitializationException`, al inicializar la clase). Se resuelve en `Program.cs`, que prueba
   `%TEMP%\sirga-questpdf` y si no `bin\...\questpdf-tmp`, **escribiendo de verdad** para comprobar. Se
   detectó porque un proceso lanzado con token restringido no podía escribir en `%TEMP%`.
13. **Los literales de fecha `yyyy-mm-dd` se MALINTERPRETAN contra columnas `datetime`.** La sesión de
   `sqlcmd` viene con `language = Español` y **`dateformat = dmy`**, y con eso `'2026-03-01'` contra un
   `datetime` se lee como **año-día-mes**: queda **2026-01-03** (¡y no avisa!). Medido:

   | Literal | Destino `date` | Destino `datetime` |
   |---|---|---|
   | `'2026-03-01'` | 2026-03-01 ✓ | **2026-01-03** ✗ |
   | `'2006-07-01'` | 2006-07-01 ✓ | **2006-01-07** ✗ |
   | `'2999-12-31'` | ok ✓ | **error** de conversión ✗ |

   Consecuencia real: `WHERE Desde = '2006-07-01'` sobre un `datetime` devolvía **0 filas sin error**.
   **Regla: contra `datetime`, escribir el literal SIN guiones (`'20060701'`), que nunca depende del
   `dateformat`.** Con `date` el guionado funciona, pero conviene usar siempre la forma sin guiones.
   Un `'1900-01-01'` zafa por casualidad (día y mes son el mismo número), así que los guionados que ya
   existen en `script_sirga_db_upgrade_produccion.sql` **se verificaron y están bien**: no hay que
   tocarlos.
14. **Todo `decimal` se fuerza a `decimal(18,2)`, y eso redondea a CERO cualquier tasa o coeficiente.**
   En `SIRG_DbContext.OnModelCreating` hay un bucle que recorre **todas** las propiedades `decimal` y les
   pone `decimal(18,2)` (convención pensada para importes). Un campo que no es plata —una tasa, un
   porcentaje, un coeficiente— queda redondeado: `InterestRates.MonthlyRate` valía 0,0275 (2,75%) y se
   guardaba **0,03**; una tasa diaria como 0,00091667 quedaba directamente **0,00**, sin ningún aviso.
   **La excepción se declara DESPUÉS del bucle y con `HasColumnType`**, no con `HasPrecision`: el bucle deja el
   tipo de columna fijado como texto y le gana a la precisión (con `HasPrecision`, EF no generaba ni el
   cambio de columna).
15. **Un índice FILTRADO obliga a `SET QUOTED_IDENTIFIER ON` para escribir en esa tabla.** SQL Server
   rechaza el `UPDATE`/`INSERT` sobre una tabla con índice filtrado si `QUOTED_IDENTIFIER` está en OFF, y
   **`sqlcmd` lo trae OFF por defecto**: el error es *"Error de UPDATE porque las siguientes opciones SET
   tienen una configuración incorrecta: 'QUOTED_IDENTIFIER'"*. Le pasó a los scripts que escriben en
   `SalaryScales` (índice filtrado por `IsActive`) y en `InterestRates` (por `IsActive`). **Todo script que
   escriba en esas tablas —o en `Companies`, que tiene índice filtrado— lleva `SET QUOTED_IDENTIFIER ON;`
   al principio.** Verificado corriendo los 4 scripts: sin eso fallan, con eso dan 0 cambios.
16. **Al cambiar el esquema, revisar los scripts y diagnósticos que consultan esas tablas.** Después de
   mover las escalas y categorías a la rama, el script de antigüedad y el diagnóstico de DDJJ en cero
   (`diag_ddjj_cero.sql`) seguían usando `SalaryScales.UnionId`, que ya no existe: **fallaban al
   compilar** (y un batch no ejecuta nada si no compila). Ya se corrigieron; el que se agregue algo al
   esquema, que busque `UnionId` en los `.sql` antes de dar por cerrado el cambio.
17. **No verificar cuentas de plata con SQL: SQL Server redondea la división `decimal` distinto que C#.**
   Al verificar el interés, la misma fórmula dio en SQL **1.982,39** y en la app **1.982,53** (y 467,47
   contra 467,50), porque `0.0275 / 30` no da lo mismo en los dos motores. **La app es C#**: las cuentas
   se verifican con el motor real (una prueba temporal que cargue las tasas de la base), no con una
   consulta. SQL sirve para *contar filas y ver datos*, no para rehacer una cuenta de dinero.
18. **Dividir una tasa mensual por 30 da un decimal periódico: hay que redondear el tramo, no el total.**
   `0,04 / 30 = 0,00133333...` hacía que un tramo de 15 días sobre 100.000 diera
   **1.999,9999999999999999999999950** en vez de 2.000,00. Si se suman los tramos sin redondear, el
   **desglose no cierra con el total** (y el desglose es lo que el estudio audita). Se redondea **cada
   tramo** a 2 decimales y el total es la suma.
19. **Un parámetro de MudBlazor mal escrito NO rompe el build: se renderiza al DOM y no hace nada.**
    `MudTable` **no tiene** `HorizontalScroll`; el parámetro real es **`HorizontalScrollbar`** (XML de
    MudBlazor 9.4: *"Shows a horizontal scroll bar if the content exceeds the maximum width"*). Las **35**
    tablas del proyecto lo usaban mal desde el 2026-11-03, así que el "scroll horizontal de las 36 tablas"
    que el traspaso daba por hecho **no existía**: el atributo salía al DOM y la tabla seguía desbordando.
    Lo mismo con `IconSize` en `MudMenuItem` (no es parámetro de ese componente). **El síntoma es un aviso
    `MUD0002` del analizador de MudBlazor, no un error**: con 71 avisos el build "pasaba" y nadie los miraba.
    **Regla: un aviso MUD0002 es un defecto de hecho, no ruido.** Corregido en `0989dad` (0 avisos MUD0002).
20. **"Build en 0 avisos" no es literal: los NU1902/NU1903 son de NuGet, no del código.** Hay **56**
    avisos de vulnerabilidades conocidas de paquetes (`SixLabors.ImageSharp` 2.1.11,
    `System.Security.Cryptography.Pkcs` 9.0.0/9.0.4). No se arreglan desde acá: hay que **actualizar los
    paquetes**. Cuando se diga "0 avisos", aclarar que son **0 del compilador y del analizador**.
21. **El `dotnet restore` NO funciona con el sandbox restringido de las sesiones de agente** (2026-11-03):
    falla con *"The SSL connection could not be established"* y MSBuild informa **"ERROR al compilar" con
    0 errores**, que parece un error del código y no lo es. Con el sandbox abierto restaura sin problema.
    Si el build falla así, compilar con **`--no-restore`** y decir la verdad: se verificó sin restaurar.
22. **El build también puede fallar por permisos de Windows en las carpetas `obj`** (2026-11-03):
    `error MSB3491 ... Access to the path 'obj\Debug\net10.0\xxxx.fik~' is denied`. No es lock de la app
    (ese es MSB3021/MSB3027) ni se ve con `icacls`: era **`WRITE_OWNER` faltante** en `SIRGA.Domain` y sus
    `obj`. Se repara con la skill `diagnose-windows-sandbox-acl`; deja respaldos y el comando de vuelta
    atrás en **`.dsh-tools\acl-recovery`** (ignorado por git).
23. **Un `:setvar` dentro del script PISA el `-v` de `sqlcmd`.** El script de empleados tenía
    `:setvar Simulacro 1` adentro y `-v Simulacro=0` por afuera: **ganó el de adentro** y la corrida
    salió en simulacro **sin ningún error**, con el script diciendo "no se escribió nada". Es la misma
    familia de fallos silenciosos que las otras trampas. **Regla: el modo se pasa por parámetro y el
    script no trae un valor propio que lo pise** (`DECLARE @Simulacro bit = $(Simulacro);`).
24. **Las fechas del sistema anterior traen centinela `1900-01-01`** para "sin fecha" (7.849 empleados
    en `HireDate`). Un centinela mostrado como fecha **miente**: en la ficha y el padrón se muestra
    **"—"** cuando el año es < 1950. Antes de mostrar una fecha del legado, mirar si es centinela.

---

## 5. Datos: el sistema anterior y el mapeo

La app vieja vive en la base `Sindicato` (copia local) y **sí** tiene lo que faltaba en SIRGA:

| Legacy (`Sindicato`) | SIRGA | Notas |
|---|---|---|
`maeemp` (4.507) | `Companies` (4.507) | 1 a 1 |
`maesoc` (**esquema `fotos_`**) (27.924) | `Employees` (27.934) | el maestro de socios |
`MAESOC_CODCAT` (1..21) | `Assignments.CategoryCode` | **la categoría de cada empleado** |
`MAESOC_CUIL` | `Employees.Dni` | **la llave del cruce** (¡en SIRGA la columna es `Dni`!) |
`categorias_empleado` (22) | `EmployeeCategories` | códigos **1..21**; el 0 = "TODAS" es un filtro, no una categoría |
`EscalaSalarial` (1.339) | `SalaryScales` | `CodCategoria`/`Importe`/`AcuerdoNR1`/`AcuerdoNR2` |
`socemp` (36.019) | `Assignments` | relación socio-empresa |
`Intereses` (70) + `Intereses_` (52) | `InterestRates` (74) | **las tasas**: la segunda **no** es un subconjunto, aporta 4 filas que tapan huecos |

**Ojo:** hay **dos** tablas `maesoc` (la vacía en `dbo` y la que tiene datos en el esquema `fotos_`) y el
servidor tiene **cotejación sensible a mayúsculas**. Por eso la primera consulta daba cero.

---

## 6. Hecho

- **Búsqueda de empresas** `/union/company-search`: por CUIT (con o sin guiones), razón social o **nombre
  de fantasía**, con el estudio actual, su **usuario** y el **historial de traspasos**. Auditor: sólo su
  sindicato.
- **Bancos y boletas** `/admin/bancos`: CRUD de bancos + configuración **por sindicato** (número de
  cliente, código de ente, formato, día del 1er vencimiento y días al 2°). Tablas `Banks` y
  `UnionBankConfigs`; el Banco Santiago del Estero con cliente **18588639** y ente **0134**.
- **Código de barras del BSE** (`BoletaBse`): 58 posiciones, verificado **contra una boleta real**
  carácter por carácter. Fechas julianas `aaddd`, "datos generales" = período + CUIT, dígito verificador
  (pesos 1-2 desde la derecha, DV con peso 2, complemento / 2).
- **PDF de la boleta** (`BoletaPdfService`): **triplicado** — talón **cliente**, **banco** y
  **sindicato** — con el código dibujado vía ZXing + QuestPDF.
- **Intereses resarcitorios: un solo motor, por DÍAS y por TRAMOS** (`InterestCalculator`, Application,
  función pura y probable). Lo usan **los dos** cálculos de interés del sistema:
  - la **mora de la DDJJ** (`DeclarationService`), y
  - el **interés de la boleta** (`BoletaService`): desde el día siguiente al vencimiento hasta la fecha de
    pago declarada por el cliente, que además pasa a ser el vencimiento de la boleta.
  Regla implementada, según la **especificación de ARCA que pasó el usuario**:
  1. Si paga **en término o antes**, el interés es **0**.
  2. La mora corre **desde el día siguiente al vencimiento hasta la fecha de pago**, ambos inclusive.
  3. La deuda se **fragmenta por tramos de tasa**: si el período atraviesa un cambio de tasa, cada parte
     se calcula con la suya (ARCA las cambia bimestralmente).
  4. **Tasa diaria = tasa mensual ÷ 30 SIEMPRE**, sin importar si el mes tiene 28, 29, 30 o 31 días.
  5. Interés del tramo = capital × tasa diaria × días del tramo, **redondeado a 2 decimales**, y el total
     es la suma de los tramos.
  - **La escala migrada del sistema anterior ES la matriz de ARCA**: verificado contra los tres tramos
    que pasó el usuario (01/02/2025-28/02/2025 = 7,26%; 01/03/2025-30/06/2025 = 4,00%; 01/07/2025 en
    adelante = 2,75%) y sus tasas diarias dan exactas (0,00242 / 0,00133333 / 0,00091667).
  - **Esto REEMPLAZÓ la convención anterior** (prorratear la tasa mensual por los días del mes) y también
    la mora por **meses enteros con mínimo de 1**, que cobraba un mes completo por pagar un día tarde:
    para una DDJJ de 102.988,71 del período 2026-08 pagada el 06/10, la regla vieja cobraba **2.832,19**
    y la nueva cobra **1.982,53** (21 días reales).
  - **OJO — pendiente de contrastar**: la **única boleta real que tenemos** (GAMMAROTA, 72.703,76 en 10
    días) tiene impreso **644,96**, y con la regla /30 da **666,45** (diferencia 21,49). Con días del mes
    daba 644,95. O sea que esa boleta parece haber usado días del mes, no /30. **Es una sola boleta**: hay
    que contrastar con más (basta el número tipeado) para decidir si el banco/SIRGA usa otra convención.
    Está anotado en `InterestCalculatorTests.BoletaRealGammarota_...` para que no se pierda.
- **Idioma: TODO EL FRONT VA EN ESPAÑOL** (regla 8 de `AGENTS.md`, pedida por el usuario el 2026-10-07).
  Se encontraron y corrigieron dos clases de texto en inglés:
  1. **Estados mostrados crudos**: varias pantallas hacían `@context.Status`, así que se veía **"Paid",
     "Submitted", "Draft", "Pending"**. Ahora se traducen siempre con **`SIRGA.WebUI.Services.Textos`**
     (`EstadoDeclaracion`, `EstadoEmpresa`, `EstadoActa`, `EstadoSolicitud`, `EstadoEmpleado`). La
     comparación interna sigue siendo contra el valor original, nunca contra el texto traducido.
  2. **Textos internos de MudBlazor**: vienen **en inglés y no los toma de la cultura**, así que se veían
     **"PREVIOUS"/"NEXT"** (los pasos de la DDJJ), **"Rows per page:"** (paginador) y "Ok"/"Cancel" (diálogos).
     Se traducen en **`MudLocalizerEspanol`** (`SIRGA.WebUI/Services`), registrado en `Program.cs`. **La
     clave es el propio texto en inglés** (así funciona MudBlazor por dentro): si una clave no está en el
     diccionario, se devuelve el original y sólo queda en inglés, no se rompe nada. Las claves se sacaron
     de la tabla de recursos del ensamblado `MudBlazor.dll`.
  - Además se fijó la **cultura en `es-AR` explícitamente** en `Program.cs` (antes dependía de la del
    servidor): meses, separadores de miles y fechas en formato argentino también en producción.
  - **Verificado por código** (el navegador no lo puede operar el agente): `Next`→Siguiente,
    `Previous`→Anterior, `Rows per page:`→Filas por página:, `{0}-{1} of {2}`→`{0}-{1} de {2}`,
    `Paid`→Pagada, `Submitted`→Presentada, `Draft`→Borrador.
- **RECTIFICATIVAS: una nueva versión del mismo período** (`DeclarationService`, `PaymentService`).
  Reglas confirmadas por el usuario (2026-10-07):
  1. **Se puede rectificar una DDJJ pagada** (el sistema anterior lo hacía: 15.438 rectificativas suyas
     están pagadas).
  2. La versión anterior se marca **`Rectificada`** y **la vigente es la nueva**. No se borra nada.
  3. Una DDJJ **rechazada también cuenta como presentada**: corregirla es una rectificativa.
  4. **"Presentada" (`Submitted`) se usa de verdad**: presentar deja la DDJJ en ese estado. Antes
     `CreateDeclarationAsync` la dejaba en `Draft` aunque la pantalla dijera "presentada con éxito", y
     **ninguna parte del código ponía `Submitted` nunca**.
  - **Cómo se decide**: presentar una DDJJ de un período que ya tiene una versión **vigente** (no
    borrador ni rectificada) ES una rectificativa, y `VersionNumber` pasa a 2, 3… Es el criterio del
    sistema anterior (su columna `rect` era el número de versión: 0 la original, 1.. las rectificativas).
  - **EL ENCABEZADO DICE QUÉ ES Y DE QUÉ EMPRESA (pedido del usuario, 2026-10-07)**: arriba de los tres
    pasos —está **fuera del stepper**, así se ve siempre— la pantalla muestra el título **"Nueva DDJJ
    (Rectificación)"** cuando ya hay una versión vigente del período, y debajo **Empresa: Razón Social /
    Nombre de fantasía** y **CUIT**, más un aviso con la versión anterior y cómo va a quedar. Antes decía
    siempre "Nueva Declaración Jurada" y no mostraba de qué empresa era. El dato se recalcula al cambiar
    empresa, sindicato o período (`ActualizarRectificacionAsync`), no recién al presentar.
    > **OJO con el nombre de fantasía**: el campo existe (`Company.TradeName`) y **lo tienen las 0 de las
    > 4.507 empresas** (medido). Así que hoy el encabezado muestra sólo la Razón Social; el `/ Nombre de
    > fantasía` aparece solo cuando el dato esté cargado. **No es un bug**: es un dato que falta.
  - **DE DÓNDE SE GENERA (esto faltaba y el usuario lo preguntó)**: en el **historial de DDJJ** cada fila
    vigente tiene un botón **"Rectificar"** (ícono de nota, ámbar) que manda a **Nueva DDJJ con la empresa,
    el sindicato y el período ya elegidos** (`/declarations/new?empresa=&sindicato=&periodo=`). Antes el
    camino existía pero **no se descubría**: había que ir a Nueva DDJJ y acordarse de elegir la misma
    empresa y el mismo período a mano. **No se ofrece sobre una versión ya reemplazada**: la rectificativa
    se hace siempre contra la vigente.
    - **UN BORRADOR NO ES UNA RECTIFICATIVA, y esto confundió al usuario (2026-10-07)**: el botón del
      historial decía "Rectificar" también sobre las filas en **borrador**, y al presionarlo la pantalla no
      avisaba nada (correcto: un borrador **nunca se presentó**, así que presentarlo es su primera
      presentación, no una rectificativa). Ahora la acción se llama por su nombre: sobre un borrador es
      **"Continuar este borrador donde quedó"** (ícono ▶, azul) y sobre una presentada es
      **"Presentar una versión nueva (rectificativa)"** (ícono de nota, ámbar).
    - **Y había un bug real detrás**: al reanudar un borrador se **borran sus detalles y se recalculan**, así
      que **los sueldos que el estudio ya había cargado se perdían**. Por eso ahora hay
      **`ObtenerBaseAsync`** = el **borrador** si existe, si no la **versión vigente**, y la precarga usa la
      base (no la vigente). Criterio de qué se copia:
      - de un **borrador**: **todos** los importes (es trabajo en curso del estudio, y en las filas
        anteriores a la columna `UsaEscala` la procedencia no está registrada, así que copiar sólo las
        marcadas como tipeadas perdería lo cargado);
      - de una versión **vigente** (rectificación): **sólo las cargadas a mano**, porque las de escala se
        recalculan solas y en las DDJJ migradas están **todas** marcadas como escala (copiarlas sería copiar
        importes viejos).
      - Verificado con el caso del reporte (empresa 28469, período 2026-08, en borrador): `ObtenerVigenteAsync`
        devuelve **null** (no es rectificativa) y `ObtenerBaseAsync` devuelve **el borrador**, y la precarga
        copia **3 de 3**. Para un período presentado (2026-07) la vigente es `Submitted` y la precarga copia
        **0 de 3** (DDJJ migrada): no se copian importes viejos.
    - **Las remuneraciones cargadas A MANO SÍ se precargan** (pedido del usuario, 2026-10-07), así en una
      rectificativa **sólo se cambia lo que hace falta** y no hay que volver a cargar todos los sueldos.
      Para poder hacerlo se guarda **`DeclarationDetail.UsaEscala`**: si el importe salió de la escala o se
      cargó a mano. **Sin ese dato no se pueden distinguir** (en `BaseSalary` está el resultado, no el
      origen) y copiar todo **congelaría el mínimo viejo como si fuera un importe tipeado**.
      - **Las que salieron de la escala NO se copian**: se recalculan solas con la escala vigente. Se
        precargan sólo las tipeadas, y la pantalla **dice cuántas** y de dónde salieron
        (`_importesDeVersionAnterior`), con la aclaración de que los importes no cargados salen de la escala.
      - **Trampa de EF otra vez**: la migración proponía `defaultValue: false` para `UsaEscala`, que es el
        valor **INSEGURO**: marcaría los **1.269.993** detalles que ya existen como "cargados a mano" y una
        rectificativa de una DDJJ migrada copiaría importes viejos como si fueran tipeados. Corregido a
        mano a **`true`** (la escala) y verificado después de aplicar.
      - Si la precarga falla, se avisa y se cargan a mano: no puede impedir rectificar.
      - **Verificado** (transacción revertida): con un empleado con remuneración tipeada y otro por escala,
        se guarda `UsaEscala = False` para el primero y `True` para el segundo, y la precarga copia **1 de
        2** (sólo la tipeada). Rectificar cargando lo mismo reproduce el mismo total (no cambia el
        resultado de quien no toca nada).
  - **El código de barras va PEGADO AL PIE de cada talón**: el talón se arma en **dos capas**
    (`Layers`): la principal con el encabezado y los datos, y otra con **`AlignBottom()`** que ancla al
    fondo el código, su número y la nota.
    > **POR QUÉ CON CAPAS Y NO CON UN ESPACIADOR**: un espaciador de alto fijo se corre en cuanto el
    > contenido mide una línea más (el detalle del interés, que sólo aparece si la DDJJ está vencida). Con
    > la capa alineada abajo, el pie queda al fondo **siempre**, sin depender de lo de arriba.
    > **Medido sobre el PDF** (trackeando la matriz de transformación, porque hay una por talón): en los 3
    > talones el código queda a **32 pt del piso** de su caja, y las 3 cajas miden **259 pt** (el tercio de
    > hoja). *(El eje del PDF viene invertido: el dato crudo se lee como la coordenada de arriba.)*
    > Herramienta: `.dsh-tools/medir_barras.py`.
  - **Los tres talones se reparten TODO el alto de la hoja** (antes quedaban apilados arriba y sobraba un
    tercio de papel): cada talón tiene un **alto fijo** calculado del tamaño de hoja y los márgenes.
    > **TRAMPA DE QUESTPDF (las dos probadas)**: `Extend()` **no sirve para repartir**: con varios elementos
    > en la columna de la página, **cada uno se queda con la hoja entera** y la boleta se va a **6 páginas**.
    > Y un `Extend()` **dentro** del talón para empujar el pie hacia abajo se expande contra el alto de la
    > **PÁGINA**, no el del talón, y **manda el código de barras a la hoja siguiente** (4 páginas). El alto se
    > calcula: `(PageSizes.A4.Height - márgenes - separaciones) / 3`.
  - **El nombre del sindicato y su jurisdicción van en DOS líneas** (pedido del usuario), así que **el nombre
    no debe incluir la provincia**: el sindicato 19 se llamaba "…COMERCIO SANTIAGO DEL ESTERO" y la provincia
    ya estaba en `Unions.JurisdictionProvince`, o sea salía **duplicada**. Se renombró a "SINDICATO
    EMPLEADOS DE COMERCIO" (`script_sirga_db_sindicato_nombre_sin_provincia.sql`, idempotente, respaldo
    `dbo._BackupUnionName`, simulacro previo, 2ª corrida = 0). `BoletaDatosDto.Jurisdiccion` sale de esa
    columna.
  - **El vencimiento y el importe van en DOS renglones** (antes era uno largo):
    `Vencimiento 07/10/2026` / `Importe Total a Depositar: $ 58.995,14`.
  - **La boleta lleva un CÓDIGO QR** con **el mismo contenido del código de barras** (los 58 dígitos), como
    alternativa de escaneo. Va en el **encabezado de cada talón**, a la derecha.
    > **OJO CON DÓNDE VA**: se puso en el encabezado y **no** en una línea propia abajo porque agregar una
    > línea de 50 pt por talón **empujaba la boleta a DOS hojas**, y el triplicado tiene que entrar en una.
    > En el encabezado está igual de lejos de la línea del código de barras (que un lector láser barre
    > horizontalmente) y no agrega alto.
    > **Se arma como IMAGEN**: QuestPDF no tiene API pública de SVG, así que el QR se codifica a mano como
    > **BMP de 24 bits** (sin compresión, blanco y negro puro) y Skia lo lee. **A 8 px por módulo (232×232) y
    > con `UseOriginalImage()`** para que NO lo reescale: si lo reescala interpola los píxeles y los bordes de
    > los módulos quedan borrosos, que es lo que perjudica la lectura.
    > **Verificado decodificándolo con ZXing**: devuelve los 58 dígitos exactos.
  - **La boleta lleva el LOGO del sindicato** (emblema de Santiago del Estero, pedido por el usuario):
    `SIRGA.WebUI/wwwroot/images/logo-santiago-del-estero.jpg`, dibujado en el **encabezado de cada talón**
    (44×44 pt) a la izquierda del nombre del sindicato.
    > **Va EMBEBIDO en el ensamblado** (`EmbeddedResource` en `SIRGA.WebUI.csproj`) y se lee del propio
    > ensamblado, **no del disco**. La primera versión buscaba el archivo subiendo carpetas y **no
    > funcionaba desde un verificador** (vive en otra rama del árbol): el PDF salía sin logo y sin avisar.
    > Si la imagen faltara, la boleta se emite igual sin el logo: no se puede dejar de emitir por una imagen.
  - **La acción de la boleta es sólo el ÍCONO de impresión** (pedido del usuario, 2026-10-07: el texto
    "Boleta bancaria (triplicada)" ocupaba demasiado ancho en la grilla) y **el archivo se descarga como
    `aaaa-MM_CUIT_Boleta_Pago.pdf`**. Se sacó el ícono del **comprobante interno** del historial (y se
    limpió su método y su inyección, que quedaron sin uso).
    > **"Que no pregunte dónde guardar" es una PREFERENCIA DEL NAVEGADOR, no del sistema.** El JS
    > (`downloadFileFromBase64`, en `App.razor`) ya crea un `<a download>` y lo clickea, que es la descarga
    > directa sin visor ni pestaña. Si el navegador igual pregunta, hay que destildar **"Preguntar dónde
    > guardar cada archivo"** en Configuración > Descargas. Se le cambió el tipo MIME a `application/pdf`.
  - **En el historial, la fila reemplazada se ve VACÍA** (el usuario pidió sacar la leyenda "Reemplazada
    por la versión vigente"): se identifica por su estado `Rectificada` y su versión. Y el **encabezado del
    historial muestra siempre los datos de la empresa** (Razón Social / nombre de fantasía y CUIT) cuando se
    entra por `/empresa/{id}/historial`: se cargan del **Id de la ruta**, no de la lista, así se ven aunque
    el historial esté vacío. Antes sólo decía "Historial de Declaraciones Juradas" y no se sabía de qué
    empresa era.
  - **Una versión REEMPLAZADA no tiene NINGUNA acción** (pedido del usuario, 2026-10-07). Antes, la fila
    `Rectificada` seguía ofreciendo **imprimir su boleta**, o sea que se podía cobrar dos veces la misma
    DDJJ. Ahora, en el historial, una fila `Rectificada` **no muestra ningún botón** (ni boleta, ni
    comprobante interno, ni rectificar): dice **"Reemplazada por la versión vigente"**.
    - **El bloqueo está también en el SERVICIO**, no sólo en la pantalla: `BoletaService.ArmarAsync`
      **rechaza** una DDJJ `Rectificada` con un mensaje que manda a emitir la de la versión vigente. Si
      estuviera sólo en la pantalla, quedaría evitable por API.
    - Junto con las guardas de `ApproveDeclarationAsync` y `PaymentService.ConfirmPaymentAsync`, las tres
      puertas de una DDJJ reemplazada (aprobar, cobrar, emitir boleta) están cerradas.
    - **Verificado sobre la rectificación real del usuario** (empresa 29718, período 2026-08: v1
      `Rectificada` y v2 `Submitted`): la v1 **no emite** boleta (devuelve el error) y la v2 sí
      (vencimiento 20/10/2026 por 30.606,70).
  - **El aviso es obligatorio porque es automático**: la pantalla de Nueva DDJJ muestra el modal
    `RectificationWarningDialog` con la versión anterior, su total, **lo que se pagó** y la
    **comparación EMPLEADO POR EMPLEADO** (base y aporte antes/ahora, y el estado: Igual, Sube, Baja,
    Nuevo o Sale). Que sea automático no quiere decir que tenga que ser invisible. Comparando sólo los
    totales, si un empleado sube y otro baja la diferencia lo esconde.
  - **Un solo cálculo para las dos cosas**: `CalcularAsync` es el ÚNICO lugar donde se calculan los
    renglones, y lo usan tanto el guardado como `PrevisualizarRectificativaAsync`. Si la previsualización
    calculara por su lado, podría mostrar un importe distinto del que se termina guardando.
    **Verificado**: se pidió la previsualización y después se presentó la rectificativa **con el mismo
    pedido**, y el total previsualizado fue **exactamente** el guardado (180.000,00), con los importes por
    empleado coincidiendo uno a uno (un empleado "Sube" a 9.000.000 y el que "Sale" no queda guardado).
    Todo dentro de una transacción revertida: la base quedó intacta.
  - **LO MÁS IMPORTANTE — `PreviousAmount` es lo que se PAGÓ, no el total declarado.** El código restaba
    el total de la versión anterior, y eso **cobraba de menos** cuando la anterior no se había pagado.
    Caso real medido en el sistema anterior (EL NENE SRL, período 2026-06): DDJJ presentada por
    **418,95**, rectificada a **20.947,42**, y la original **nunca se pagó** (su `impban1` = 0). Restar
    418,95 cobraba 20.528,47 en lugar de **20.947,42**.
  - **OJO con el "pagado" en los datos migrados**: `PaymentTransactions` se migró **vacía**, así que una
    DDJJ migrada está `Paid` pero **sin transacciones**. `MontoPagadoAsync` usa la suma de las
    transacciones y, **si no hay ninguna y la DDJJ está Pagada, cae a su `TotalAmount`**. Sin ese
    fallback, rectificar una DDJJ migrada cobraría el total completo otra vez.
  - **Guardas para no cobrar dos veces**: `ApproveDeclarationAsync` y `PaymentService.ConfirmPaymentAsync`
    **rechazan** una DDJJ `Rectificada` (con mensaje en el primer caso, `false` + log en el segundo).
  - **Verificado contra la base real sin dejar rastro** (transacción revertida al final): v1 queda
    `Submitted` y versión 1; se paga **la mitad**; la segunda presentación da **rectificativa versión 2**
    con `PreviousAmount` = **exactamente la mitad pagada** (no el total); la v1 pasa a `Rectificada`;
    aprobar y cobrar la v1 se **rechaza**; cobrar la v2 se **acepta**.
  - **La migración**: EF propuso `defaultValue: 0` para `VersionNumber`, que habría dejado las **248.158**
    DDJJ existentes en versión 0. Corregido a mano a **1**.
  - **MIGRACIÓN DE DATOS — limitación conocida**: las **20.977 rectificativas** que vinieron del sistema
    anterior quedaron **sin su original** y con `PreviousAmount = 0` (SIRGA tiene **una sola** DDJJ por
    empresa y período: 248.158 pares, todos con una). Verificado que la migración conservó **la última
    versión** (EL NENE: quedó en 20.947,42 y Pagada, o sea el importe correcto para facturar) y que lo
    que se perdió es el **historial de versiones**, no el importe. El sistema anterior sí conservaba
    todas (`rect` 0 a 33).
- **Botón para imprimir la boleta bancaria** (la que va al banco): está en el **historial de DDJJ** y también
  en el paso final de **Nueva DDJJ**. El armado de los datos está en `BoletaService` (Application), **no**
  en la pantalla: junta la DDJJ, la empresa, el sindicato y su **configuración bancaria** (`UnionBankConfigs`
  + `Banks`) y genera el **código de barras de 58 dígitos**. La pantalla sólo pide y descarga.
  - **LA BOLETA TIENE UNA SOLA FECHA DE VENCIMIENTO** (pedido del usuario, 2026-10-06): se eliminó el
    **2° vencimiento**. Si la DDJJ **todavía no venció**, la boleta sale por el total con la fecha del
    banco y **sin interés**. Si **ya venció**, al imprimir aparece el **modal `PaymentDateDialog`**, el
    cliente indica **cuándo va a pagar**, y **esa fecha pasa a ser el vencimiento de la boleta** con el
    **interés resarcitorio** desde el día siguiente al vencimiento original hasta ese día (motor
    `InterestCalculator`). O sea: desde el primer día de mora la boleta ya sale con su interés.
  - **Verificado con la DDJJ real 1737080** (ABDALA LLAMIL, período 2026-08, total 51.000; vence
    15/09/2026): pagando el **20/10/2026** la boleta sale por **$52.636,25** = 51.000 + **1.636,25** de
    interés (3,2083%, **35 días** al 2,75%, `R (MEC) 823/2025`), y el código
    `0134408260000020405264693126293000526362526293000526362502`. Pagando el 10/09 (en término) sale por
    $51.000,00 con vencimiento 15/09/2026 y **sin interés**.
  - **El formato del banco tiene DOS juegos de fecha+importe** y la boleta usa uno solo: se **repite el
    mismo** en los dos (lo eligió el usuario) para que el código siga teniendo los **58 dígitos** que el
    banco lee. Verificado: la 1ª y la 2ª fecha son idénticas, y los dos importes también. `BoletaBse.Generar`
    conserva la sobrecarga con dos juegos distintos sólo para reproducir una boleta real en las pruebas.
  - `UnionBankConfig.DaysToSecondDue` (los 10 días al 2° vencimiento) **quedó sin uso**: ya no está en la
    pantalla de Bancos ni en el armado. **La columna se dejó en la base a propósito** para no borrar
    configuración del usuario (si hiciera falta volver atrás, el dato está). Ojo: el diálogo de
    configuración **sigue copiando el valor** al editar, así que no se pisa al guardar.
  > **OJO: HAY DOS GENERADORES DE BOLETA DISTINTOS, Y ESO YA CONFUNDIÓ AL USUARIO.**
  > - `BoletaPdfService` (WebUI) = **la boleta del BANCO**: **3 talones** (cliente, banco, sindicato) con el
  >   código de barras real de 58 dígitos. Es la que se paga. La alimenta `BoletaService`.
  > - `PdfReportService.GeneratePaymentBoleta` (Application) = **comprobante interno de SIRGA**: **1 boleta**
  >   con membrete propio y **SIN código de barras del banco**.
  > Medido abriendo los PDF: el triplicado tiene **51 bloques de texto (17 × 3 talones: los 16 de siempre más
  > la línea de interés)** y **588 rectángulos** (los 3 códigos); el interno, 29 bloques y **6 rectángulos**.
  > Los dos botones eran íconos parecidos y la diferencia sólo se veía al pasar el mouse, así que el usuario
  > imprimió el interno creyendo que era la boleta. Ahora **la del banco es un botón con texto** y el interno
  > un ícono secundario avisado.
  - El **capital de la boleta es el TOTAL de la DDJJ**, no el saldo (que puede venir con crédito aplicado o
    con la mora ya sumada al presentar). El interés se calcula sobre ese total.
  - Si el sindicato **no tiene configuración bancaria activa**, el botón **avisa** en pantalla en lugar
    de emitir una boleta inventada.
- **Categorías de los empleados migradas** desde `maesoc`: **9.586** asignaciones activas (quedan **7**
  sin resolver, empleados con `FullName = "SIN NOMBRE"`). Respaldo en
  `dbo._BackupAssignmentsCategoryCode`.
- **Control anti-cero**: el alta de DDJJ **no guarda** si no calculó ningún importe y hay empleados
  asignados (antes guardaba en cero como si estuviera bien).
- **Borrador que se reanuda**: si existe un borrador de esa empresa y período se **reutiliza** (antes
  tiraba una excepción y dejaba al usuario sin salida).
- **Pestaña SQL** en el GUI de DSH: sólo lectura, doble candado (lista negra + transacción con ROLLBACK),
  sólo `SIRGA_DB` y `Sindicato`.
- **Modelo de ramas COMPLETO (pasos 1 a 4)**: las categorías y las escalas cuelgan de la **rama de
  actividad** (el convenio) y no del sindicato. Las escalas y las paritarias son **nacionales por rama**,
  así que va un solo juego de categorías y escalas por convenio, compartido por todos sus sindicatos.
  - **Paso 1**: tabla `Ramas`.
  - **Paso 2**: rama **Comercio (130/75)** creada y **los 2 sindicatos vinculados** (`Unions.RamaId`,
    migración `LinkUnionsToRama`, script `script_sirga_db_ramas_vincular_sindicatos.sql`, 0 sin rama).
    Se agregó el repositorio `Ramas` a la `IUnitOfWork` (mismo contexto y semáforo), se asigna desde la
    pantalla de Sindicatos y la grilla avisa **"Sin rama"** cuando falta.
  - **Pasos 3 y 4**: `EmployeeCategories.RamaId` (migración `MoveScalesAndCategoriesToRama`) y
    **`SalaryScales` se quedó SIN `UnionId`**: la rama se llega por la categoría de la escala, así no
    puede quedar una escala apuntando a una rama distinta de la de su propia categoría. Las consultas
    (`DeclarationService`, `SalaryScaleService`, `EmployeeCategoryService` y las pantallas) resuelven la
    **rama del sindicato**; si un sindicato no tiene rama, **se avisa** en vez de liquidar cualquier cosa.
  - **Verificado con datos reales**: sindicato 19 → rama Comercio → categoría 13 → escala 2026-03 →
    mínimo **$1.478.431,61**, exactamente el mismo número que antes de mover todo.
  - **Respaldo** del vínculo viejo en `dbo._BackupScalesCategoriesUnionId` (21 categorías + 1.339
    escalas), porque la migración elimina dos columnas.
  - **Ojo al generar migraciones acá**: EF propuso un **`RenameColumn` de `UnionId` a `RamaId`**, que
    está MAL: conserva los valores viejos (19 = id del sindicato) como si fueran ids de rama y el FK
    falla. Hay que **derivar** el valor (la rama del sindicato) y hacer el `ADD COLUMN` nullable →
    `UPDATE` → `ALTER NOT NULL` → FK. Ya se corrigió a mano en la migración.
  - **Ojo**: La Banda (sindicato 20) tiene el `CctCode` en **`"PENDIENTE"`**; se vinculó a Comercio por su
    nombre y porque la documentación de `Rama` dice que comparten 130/75. **Falta completarle el CctCode.**
  - Al editar un sindicato, el diálogo tiene que devolver `RamaId` (y los porcentajes): si no, guardar
    **borra la rama** sin avisar. Ya está corregido en `Unions.razor`.
- **Los detalles de una DDJJ se REEMPLAZAN al re-presentar un borrador** (antes se acumulaban). Era un
  bug real: `declaration.Details.Add(...)` **agregaba** sobre lo que ya había, así que cada presentación
  del mismo borrador sumaba los detalles de nuevo. Se detectó con una DDJJ de prueba que quedó con **12
  detalles para 4 empleados** (3 presentaciones) y la cabecera diciendo **112.622,46** mientras los
  detalles sumaban **337.867,38**. Ahora, si se reanuda un borrador, se borran sus detalles y se
  regeneran; se reanudan **sólo borradores**, nunca una DDJJ presentada, aprobada o pagada. Verificado:
  el mismo borrador pasó de 12 detalles a **4**, con la cabecera igual a la suma de los detalles.
- **Escalas duplicadas resueltas por DESACTIVACIÓN**: había **42 pares (período, categoría)** con dos
  escalas (84 filas), de **dos meses importados dos veces** (2022-06 y 2022-07) y **copias exactas**. Se
  desactivó la copia sobrante conservando la de menor Id: **1.297 activas, 42 inactivas, 1.339 totales
  (no se borró ninguna)**. Respaldo `dbo._BackupSalaryScalesDuplicadas`, script idempotente
  `script_sirga_db_escalas_duplicadas_desactivar.sql`.
  - **`SalaryScale.IsActive` existía pero NINGUNA consulta lo miraba**: desactivar una escala no hacía
    nada. Ahora la DDJJ y los servicios filtran por activas, y hay un **índice único por (período,
    categoría) filtrado por `IsActive = 1`** para que no vuelva a haber dos activas para lo mismo.
  - El **borrado de escalas** se cambió por **desactivar** (y el botón de la pantalla era un `stub` que
    no borraba ni avisaba). La pantalla lista también las inactivas para poder reactivarlas.
- **Tasas de interés migradas del sistema anterior** (`Sindicato.dbo.Intereses` → `InterestRates`):
  **74 filas**, **37 por cada serie** (resarcitoria y punitoria), de **1901-01-01** al **2025-07-01**.
  Script idempotente `script_sirga_db_intereses_desde_sistema_anterior.sql`, respaldo en
  `dbo._BackupInterestRates`, simulacro en `.dsh-tools/simulacro_intereses.sql`.
  - **Ojo con el origen**: `dbo.Intereses_` (52 filas) **NO es un subconjunto** de `dbo.Intereses` (70).
    Aporta **4 filas que tapan huecos** de la escala (2004-06-01 tipo 1; 2004-09-01 tipos 1 y 2;
    2006-07-01 tipo 2). Las otras 2 (2019-08-01) ya estaban cubiertas por un rango estirado con la misma
    tasa, así que agregarlas habría creado un solapamiento. Con las dos tablas: **0 huecos, 0
    solapamientos**, verificado con una consulta.
  - La tabla de SIRGA se **extendió** (migración `ExtendInterestRates` + `AddInterestRateUniqueIndex`):
    `TypeOfInterest`, `ValidTo`, `Norm`, `DailyRate`, y **`MonthlyRate` pasó de `decimal(18,2)` a
    `(18,6)`** (con la escala vieja, 2,75% = 0,0275 se guardaba como **0,03**). Índice único por
    (fecha, serie) para que "la tasa vigente" no vuelva a ser ambigua.
  - **Unidades**: en SIRGA la tasa va como **FRACCIÓN** (0,0275 = 2,75%) y en el sistema anterior como
    **PORCENTAJE** (2,75). La migración divide por 100 y la pantalla ahora **pide el porcentaje** y
    convierte al guardar.
  - **Las tasas NO se borran: se desactivan** (`InterestRate.IsActive`). La escala es histórica y una
    tasa que no corresponde se desactiva dejando rastro; el borrado definitivo se eliminó del servicio,
    del controlador y de la pantalla. Los cálculos (mora de la DDJJ y recargo de la boleta) **sólo miran
    las activas**, y el índice único por (fecha, serie) está **filtrado por activas**, así que desactivar
    una tasa deja cargar la que la reemplaza para la misma fecha. También se puede **corregir** una tasa
    (editar), que es lo que hay que hacer si quedó mal tipeada.
  - Verificado guardando y **releyendo de la base con un contexto nuevo**: corregir la tasa y
    desactivarla se persisten de verdad (no queda sólo en memoria).
- **Mínimo de escala calculado** (la corrección que el usuario reportó el 2026-10-06). La columna
  "Mínimo Escala" de `/declarations/new` ya no muestra el básico pelado ni $0: calcula
  **(básico + antigüedad + NR1 + NR2) × jornada**, con **antigüedad = 1% × años cumplidos** contados
  desde el **ingreso del empleado a esa empresa** (`Assignments.CreatedAt`, origen `SOCEMP_FECHAING`),
  y **jornada Parcial = 50%** (también prorratea los NR). Muestra el estado **Debajo / Igual /
  Superior** contra lo que el estudio carga en "Remuneración", con el desglose a la vista.
  - La fórmula vive en **un solo lugar**: `SalaryMinimumCalculator` (Application). Antes estaba
    duplicada con criterios distintos (la pantalla usaba sólo el básico; el servicio contaba la
    antigüedad desde `Employees.HireDate`) y eso hacía que la pantalla y la base dijeran cosas
    diferentes. **No volver a copiar la fórmula en la pantalla.**
  - **Jornada**: campo nuevo (`SindicataryAssignment.Jornada`, enum `JornadaType`), migración
    `AddJornadaToAssignments`. Las **36.011** asignaciones quedaron en **Completa** (el sistema
    anterior no tenía el dato; darlo por parcial sería inventar una quita). Se edita desde la fila de
    la DDJJ y **se persiste en la asignación**, igual que la categoría.
  - **Antigüedad al 1% cargada** en las **1.339** escalas (estaban todas en 0), con respaldo en
    `dbo._BackupSalaryScalesAntiguedad` y script idempotente
    `script_sirga_db_antiguedad_escalas.sql`. Simulacro en `.dsh-tools/simulacro_antiguedad_escalas.sql`.

---

## 7. Pendiente, en orden

0. **TRES TAREAS EN CURSO (2026-10-07) — el detalle completo está en `PLAN_CONCILIACION_PAGOS.md`.**
   El usuario definió tres tareas, con el reparto así: **SIRGA maneja las DDJJ (CRUD) y registra los
   pagos**; **SIGES WEB lee** las actualizaciones, determina qué empresa debe y emite actas, y administra
   el sindicato. O sea: **SIRGA escribe `ddjj`/`ddjjt` y SIGES WEB los lee.**

   | # | Tarea | Estado |
   |---|---|---|
   | 1 | CRUD de DDJJ que **vuelque** a `ddjj`/`ddjjt` | escritor de PAGOS listo y probado; **falta el de DDJJ** |
   | 2 | Conciliar el **archivo del banco** + bajar del correo solo | lector y planificador listos; falta el registro y el IMAP |
   | 3 | **Caja en la Sede** con tipo de cobro (Efectivo/Cheque/Transferencia/QR) | sin empezar |

   **Lo que YA está hecho y verificado** (todo en `main`):
   - `LegacySindicatoRepository` (Infrastructure/Legacy): lee la **fila buena** de `ddjjt` con la misma
     regla que SIGES WEB, `SimularPagoAsync` (no escribe) y `MarcarPagadaAsync` (escribe **sólo `fpago` e
     `impban1`**, con **respaldo en `_BackupDdjjtPago`** y verificación releyendo, todo en una transacción).
     Registrado en DI; la cadena `ConnectionStrings:Legacy` está en `appsettings.Development.json`
     (**falta la de producción**).
   - `BankRenditionParser`: lee el archivo de rendición. El **código de barras son los últimos 54 dígitos**
     de cada línea de 218; la línea de totales (88) trae la cantidad y el importe. **Verificado contra el
     archivo real de La Banda**: 7/7 líneas y el total cierra al centavo.
   - `BankReconciliationPlanner`: decide qué hacer con cada línea **sin escribir nada**. Estados: Exacta /
     **ConInteres** (pagó de más: es la mora, el pago es COMPLETO) / Parcial / SinDdjjEnSirga / YaPagada.
   - **159 tests** (antes 146).
   > **LA FÓRMULA DEL LEGADO, VERIFICADA AL CENTAVO** (esto es lo que necesita cualquier escritor):
   > `impo + impoaux` = **la remuneración del empleado** · `item2 = 1` ⇒ **es socio** ·
   > **`titem1` = 2% de TODOS los empleados** (no sólo los no socios) · **`titem2` = 2% de los socios** ·
   > `fpago` es **la marca de pagada** (`estado` no se mira) · `impban1` = lo pagado (capital + interés).
   > **Regla de consistencia**: SIGES calcula el aporte desde `ddjj` en unas pantallas y lo lee de `ddjjt`
   > en otras ⇒ **`ddjjt.titem1` tiene que ser igual al 2% de la suma de `ddjj`**.
   >
   > **TRAMPAS NUEVAS APRENDIDAS ACÁ**:
   > - Cruzar SIRGA con el legado necesita **`COLLATE DATABASE_DEFAULT`** en cada comparación (las bases
   >   tienen cotejaciones distintas: sin eso **falla con error**, no devuelve vacío) y el período del
   >   legado es `datetime` ⇒ normalizarlo a `YYYY-MM`.
   > - El período del código de barras es **MMAA**; formatearlo al revés daba `26-09` en vez de `2026-09` y
   >   **la conciliación no habría encontrado ninguna DDJJ nunca** (lo atrapó una prueba, no la vista).
   > - Los importes del legado son **`float`** (`1234.5599999999999`): **no comparar por igualdad exacta**.
   > - **SIRGA no tiene las DDJJ de La Banda** para 2026-09 (0 candidatas de 7 CUIT): la conciliación de
   >   La Banda hoy no puede registrar nada. Y **La Banda usa el formato de 54 posiciones** mientras
   >   Santiago usa 58 ⇒ **el formato tiene que ser configurable por sindicato** (`BoletaBse` arma siempre 58).
   >
   > **DECISIONES DE NEGOCIO PENDIENTES (frenan la tarea 1):**
   > 1. ¿SIRGA **crea** la DDJJ en el legado cuando no existe, o rechaza y avisa? (afecta a todo La Banda).
   > 2. ¿La Banda se concilia en SIRGA o sigue en el sistema viejo? (si sigue, hay que filtrar por ente).
   > 3. Para la caja: **¿qué QR?** y **¿maneja turnos con arqueo?**
   >
   > **CUIDADO**: los archivos del banco (`tools/Rendicion del Banco/`) tienen **CUITs e importes reales** y
   > quedan **sin trackear**: conviene agregarlos al `.gitignore`. Las pruebas usan datos **inventados**.

0.1 **PAQUETE PARA EL BANCO (BSE)** — armado el 2026-10-07 en `.dsh-tools/paquete-banco/` (ignorado por
   git): las **5 boletas de prueba** + la **ficha técnica en PDF** + un `README.md` con el índice.
   Se regenera con `dotnet run --project .dsh-tools/verif-boleta -- paquete` y después `-- ficha`.
   **Lo que el banco pide** (su documentación, *Etapas de implementación*): estructura del código de
   barras, estructura del archivo de rendición y **boletas de prueba (mínimo 5)**.
   > **LA DOCUMENTACIÓN DEL BANCO NO DICE A DÓNDE ENVIARLO**: no hay dirección, correo, teléfono ni
   > área responsable. **A pedido del usuario, la ficha NO le pregunta eso al banco**: el destinatario se
   > resuelve por fuera (se lo pregunta él).
   > **El circuito va al revés de lo que suena**: el ENTE le entrega al BANCO la estructura + las 5
   > boletas de prueba (para que el banco desarrolle en su sistema de Caja), y es el **BANCO el que le
   > manda al ENTE el archivo de rendición** de esas boletas **para que el ENTE lo apruebe**.
   > **Falta confirmar la fórmula del dígito verificador** (el banco la pide explícitamente y es el
   > único campo sin especificar; la nuestra es una propuesta verificada contra una boleta real).
   >
   > **⚠️ HALLAZGO (2026-10-07): LA FÓRMULA IMPLEMENTADA MUY PROBABLEMENTE ESTÁ MAL.**
   > `BoletaBse.DigitoVerificador` (pesos 1-2 desde la derecha, complemento a 10 **dividido 2**) se
   > ajustó a **UNA SOLA** boleta. Probada contra **3 boletas reales**:
   >
   > | Boleta real | DV impreso | Implementada | Luhn / mód. 11 |
   > |---|---|---|---|
   > | 58 posiciones (GAMMAROTA) | 2 | **2 ✓** | 0 ✗ |
   > | 54 posiciones (impr. $93.792,92) | 8 | 0 ✗ | **8 ✓** |
   > | 54 posiciones (impr. $20.283,86) | 8 | 3 ✗ | **8 ✓** |
   >
   > **Ninguna fórmula acierta las 3.** Las dos de 54 posiciones (formato *actual*) tienen importes que
   > coinciden EXACTO con los impresos ⇒ transcripciones confiables ⇒ **la implementada falla en las dos**.
   > Y el `/2` final no es un algoritmo estándar: es la firma de haber ajustado a un solo ejemplo.
   > **Luhn/mód.11** (pesos 2-1 desde la derecha **sumando los dígitos** de cada producto, DV con peso 1,
   > total múltiplo de 11) acierta **2 de 2** boletas independientes.
   > **Si la fórmula real es la de Luhn, TODAS las boletas que SIRGA imprime hoy llevan un dígito
   > verificador que el banco va a rechazar.**
   > **CÓMO SE LLEGÓ ACÁ (y el error de proceso)**: el banco **no** da la fórmula, así que se dedujo de una
   > boleta real. Después el usuario aportó 9 códigos más, pero `dv4.py` **los descartó a TODOS** por tener
   > 54/55/56 dígitos en vez de 58, así que imprimió *"ninguna formula acierta en todas"* habiendo probado
   > **CERO** boletas: **la validación fue vacía y nadie lo notó** (justo el fallo silencioso que las reglas
   > prohíben). Revisando esos 9 hoy: **2 son boletas reales válidas del formato de 54** ⇒ sirven para
   > validar ⇒ y refutan la fórmula implementada. Ver `.dsh-tools/bse/dv5.py` y `dv6.py`.
   > **Para cerrarlo hace falta**: (a) que el banco confirme la fórmula, o (b) **2-3 boletas reales más de
   > 58 posiciones** con sus importes impresos, para desempatar.
   >
   > **AVANCE (2026-10-07, con las fotos de boletas reales que aportó el usuario)**:
   > 1. **El formato CAMBIÓ con el tiempo**: las boletas de 2024-12 a 2025-02 son de **54 posiciones** y la
   >    de **07/2025 es de 58**. O sea que el "formato propuesto" (58) **ya está en uso** desde mediados de
   >    2025, y las viejas eran el "formato actual" (54). Es la evidencia de que el 58 es el vigente.
   > 2. **La implementada es imposible, no sólo "no ajusta"**: termina en `/2`, así que **sólo puede dar
   >    dígitos 0 a 5**, y las dos boletas reales de 54 tienen **DV 8** ⇒ ninguna puede venir de esa fórmula.
   > 3. Tabla real (3 boletas verificadas: sus campos —período, CUIT, fechas e importes— coinciden EXACTO
   >    con lo impreso):
   >
   >    | Fórmula | 58 (07/2025) | 54 (12/2024) | 54 (01/2025) | Aciertos |
   >    |---|---|---|---|---|
   >    | **La implementada** | **2 ✓** | 0 ✗ | 3 ✗ | **1/3** |
   >    | pesos 2-1 / mód. 11 / sumando dígitos | 0 ✗ | **8 ✓** | **8 ✓** | **2/3** |
   >    | pesos 3-1 / mód. 10 / sumando dígitos | 2 ✓ | 3 ✗ | 8 ✓ | 2/3 (ruido) |
   >    | *impreso* | *2* | *8* | *8* | |
   > 4. **La dirección del anclaje NO explica la diferencia**: los cuerpos son de 53 y 57 dígitos (impares
   >    los dos), así que anclar los pesos a la izquierda o a la derecha da lo mismo.
   > 5. **Queda UN solo testigo del formato de 58** — el mismo problema que al principio: cualquier fórmula
   >    ajustada a un único ejemplo no prueba nada. Las dos de 54, en cambio, coinciden entre sí en
   >    **Luhn/mód. 11**.
   > **PRÓXIMO PASO**: conseguir **2-3 boletas de 58 posiciones posteriores a 06/2025** (las de la carpeta
   > del usuario son casi todas del formato viejo). Y **no sirven fotos en ángulo**: se intentó decodificar
   > los códigos de barras de las fotos con ZXing (`-- fotos` del verificador) y **no se pudo leer ninguno**
   > (sólo un falso positivo). Hace falta el número, o una foto de frente o un escaneo.
   > Scripts: `.dsh-tools/bse/dv5.py` a `dv8.py` (tabla por boleta y por fórmula).
   >
   > **DECISIÓN (2026-10-07): el proyecto usa SÓLO el formato de 58 posiciones** (lo indicó el usuario: el
   > de 54 no le interesa). Se **cambió la fórmula implementada** porque la anterior era *imposible*, no
   > sólo dudosa:
   > - **Antes**: pesos 1-2 desde la derecha, complemento a 10 **dividido 2** ⇒ sólo podía dar **0 a 5**.
   >   Un verificador que nunca puede ser 6, 7, 8 ni 9 no es un verificador: las boletas reales de 54
   >   posiciones tienen **8**, así que ninguna puede venir de esa fórmula.
   > - ⚠️ **CORREGIDO (2026-10-08) Y CONFIRMADO POR EL BANCO (2026-10-20)**: `BoletaBse.DigitoVerificador` =
   >   se multiplica cada dígito por su peso (**3 y 1 alternados, desde la IZQUIERDA**), se **suman los
   >   productos tal cual** (NO se suman los dígitos de cada producto), se **divide por 2 con división
   >   ENTERA** y se toma el **módulo 10**. DV en la posición 58, sobre los 57 dígitos anteriores.
   >   **Esto era lo que estaba mal**: la versión que sumaba los dígitos de cada producto y hacía complemento
   >   a 10 era incorrecta — y por eso el banco **rechazó el primer envío**.
   > - **Verificado**: reproduce el DV de la boleta real de 58 posiciones (suma 188 → DV **2** = el
   >   impreso) **y puede dar los diez dígitos** (probado sobre 2000 códigos). Build 0 avisos, 146/146 tests.
   > - ✅ **CONFIRMADO POR EL BANCO EL 2026-10-20: APROBARON EL ARCHIVO.** El banco nos mandó su propio
   >   archivo de cálculo de DV (el Excel de boletas de prueba y de producción) y, aplicando **su** fórmula,
   >   **aprobó las 5 boletas del paquete**. Ya no hay nada que deducir: la fórmula está documentada en
   >   `CALCULO_DIGITO_VERIFICADOR.md` y **validada por el banco**.
   >   **Lo que sigue en este bloque a partir de acá es HISTORIAL del camino** (sirve para no repetir los
   >   intentos fallidos), no estado actual.
   > - **La ficha técnica del banco se corrigió** para no afirmar que "funciona": ahora declara la fórmula,
   >   dice contra qué se verificó, informa el hallazgo de 54 vs 58 y **pide al Banco que la confirme**.
   >   Si el Banco confirma otra, se cambia **sólo ese método** y se reemiten las boletas.
   > - **El CUIT no es la clave**: se probó usar el dígito verificador del CUIT (que está dentro del código)
   >   y aplicar el algoritmo del CUIT al cuerpo. Los CUIT están bien formados (5, 5 y 9) y los códigos
   >   terminan en 2, 8 y 8 ⇒ descartado (`.dsh-tools/bse/dv9.py`).
   > - **Leer los códigos de las fotos NO funcionó**: se agregó un lector ZXing (`-- fotos` del verificador,
   >   con SkiaSharp 3.119.1 que se sumó al csproj del verificador) y **no leyó ninguno de los 11**: las
   >   fotos están **en ángulo** y un lector 1D necesita la línea del código recta. Hace falta el número, o
   >   una foto de frente o un escaneo. **Ojo**: en varias fotos el número impreso **está cortado por el
   >   borde** — eso explica los 54/55/56 dígitos que se venían manejando.
   >
   > **ESTADO AL CIERRE DEL 2026-10-07: EL PAQUETE ESTÁ LISTO PARA ENVIAR.** Las 5 boletas + la ficha
   > técnica + el cuerpo del correo están en el escritorio (`Boletas de prueba para el banco`) y en
   > `.dsh-tools/paquete-banco`. **El usuario lo envía y espera la respuesta del banco (2026-10-08).**
   >
   > **CONTESTARON EL 2026-10-20: APROBARON EL ARCHIVO.** ⇒ **El formato y el dígito verificador quedan
   > cerrados.** Se termina acá el problema que ocupó media sesión.
   > - **Lo que sigue abierto, y es poco**: (a) la **simbología** del código de barras (Code 128 vs
   >   Interleaved 2 of 5) — que hayan podido leer el archivo sugiere que Code 128 está bien, pero **no está
   >   dicho explícitamente**; (b) si el sistema de Caja **puede cobrar boletas vencidas** (la ficha lo
   >   pregunta).
   > - Si en el futuro piden un cambio de estructura → `ESPECIFICACION_BOLETA_BSE.md`.
   >
   > **QUEDA UNA PREGUNTA SIN AGREGAR (decisión pendiente del usuario):** además del dígito verificador y
   > de las boletas vencidas, **falta consultar la SIMBOLOGÍA del código de barras**. La especificación la
   > deja pendiente: *"no está en los instructivos; el formato es compatible con Interleaved 2 of 5 o
   > Code 128"*. **SIRGA usa Code 128**; si el banco espera Interleaved 2 of 5, **su lector no lee ninguna
   > boleta nuestra** (peor que el dígito: el código entero sería ilegible). Se le ofreció al usuario
   > agregarla a la ficha y al correo y **quedó sin responder**. Tampoco hay evidencia propia: no se pudo
   > decodificar ninguna foto de boleta real con ZXing (están en ángulo).
   > **Falta definir si el sistema de Caja puede cobrar boletas vencidas** — el banco lo pregunta.
   >
   > **DESAJUSTE DETECTADO**: `BankReconciliationService.ProcessBankExcelAsync` usa un **"mapeo
   > tentativo" propio** (Col A fecha, B referencia, C importe, D CUIT/ID) que **NO coincide** con el
   > archivo de rendición documentado por el banco (Sucursal, Comprobante, Moneda, Tipo movimiento,
   > Importe, Fecha de proceso, CUIL del usuario, Usuario, Hora, **Código de Barra**, Grupo de
   > Terminal…) y **no usa el código de barras**, que es la clave más confiable. Para poder aprobar la
   > rendición hay que leer el formato real del banco.

1. **Probar la DDJJ de ABDALA LLAMIL** (empresa 29656, sindicato 19): sus dos empleados ya tienen
   categoría (13 MAESTRANZA B y 2 ADMINISTRATIVO B) y escala vigente 2026-03. **Tiene que calcular
   importes.** Es la prueba de que el problema de los ceros quedó cerrado para las 2.551 empresas.
   > **ESTADO (2026-10-06): el usuario YA LA PROBÓ y reportó que "hay cosas que corregir", sin
   > detallar cuáles.** Lo primero al arrancar la próxima sesión: **pedirle el detalle de esas
   > correcciones** antes de tocar nada. No inventar qué falló ni asumir que funciona.
   >
   > **Lo que sí quedó demostrado con datos:** el cálculo **funciona**. La DDJJ **1737080** (ABDALA
   > LLAMIL, período **2026-08**, creada 2026-10-06 09:13) quedó con **2 detalles y total $51.000,00**,
   > contra las 1737078/1737079 que habían quedado en 0 con 0 detalles. O sea: la migración de
   > categorías resolvió el problema de fondo. Las correcciones que menciona el usuario son **otra
   > cosa** (pantalla, importes exactos, estados, lo que sea): hay que preguntarle.
   >
   > Ojo: la 1737080 quedó en estado **borrador (1)**, igual que las anteriores. Vale la pena revisar si
   > el paso final de presentación completa o si el flujo deja todo en borrador.
   >
   > **ESTADO (2026-10-06, sesión siguiente): el usuario detalló UNA de esas correcciones: el campo
   > "Mínimo Escala" tenía que calcularse por antigüedad, categoría, jornada y socio, y proponer una
   > remuneración de referencia. Ya está implementado (ver "Hecho").**
   > > Detalle importante de lo que pidió: "Remuneración" es lo que el estudio tipea del **recibo de
   > > sueldo** y **no se prellena**; "Mínimo Escala" es el sueldo que **debería** cobrar y sirve
   > > **sólo para mostrar** si paga debajo, igual o por encima de la escala. Ser socio **no** cambia
   > > el mínimo: sólo agrega la cuota socio del 2%.
   > > **Falta que el usuario lo verifique en pantalla** (los clics y lo visual no los puede hacer un
   > > agente) y que diga si quedan **otras** correcciones sin detallar.
2. ~~**Modelo de ramas, pasos 3 y 4**~~ **HECHO (2026-10-06)**: las categorías y las escalas ya cuelgan
   de la **rama** y las consultas filtran por la rama del sindicato. Ver "Hecho".
   > ~~**Lo que queda de este tema**~~ **RESUELTO (2026-10-06)**: las **42 escalas duplicadas** (84 filas
   > de 1.339) se **desactivaron**, no se borraron. Eran **dos meses enteros importados dos veces**
   > (2022-06 y 2022-07, 21 categorías cada uno), **copias exactas**, así que no hubo que decidir cuál
   > valía: se conservó la de menor Id y se desactivó la otra. Respaldo en
   > `dbo._BackupSalaryScalesDuplicadas`, script `script_sirga_db_escalas_duplicadas_desactivar.sql`
   > (idempotente, verificado con dos corridas).
   > **Lo importante que apareció**: `SalaryScale.IsActive` **existía pero ninguna consulta lo miraba**,
   > así que desactivar una escala no hacía nada. Ahora la DDJJ y los servicios filtran por activas, y
   > hay un **índice único por (período, categoría) filtrado por activas** que impide que vuelva a haber
   > dos escalas activas para lo mismo.
   > También se cambió el **borrado de escalas por desactivar** (el botón de la pantalla era un `stub`
   > que no hacía nada: ni borraba ni avisaba) y la pantalla muestra las inactivas para poder reactivarlas.
3. ~~**Botón de imprimir la boleta** que dispare el PDF ya escrito.~~ **HECHO (2026-10-06)**: está en el
   historial de DDJJ, y el recargo ya usa la **tasa real migrada** (ver punto 4).
4. **Recargo del 2° vencimiento**: 10 días después del 1° (configurable), con la tasa de `InterestRates`.
   Caso de prueba real: la boleta de GAMMAROTA tenía recargo **$644,96 sobre $72.703,76 = 0,887% en 10
   días** → el cálculo tiene que reproducir ese número.
   > **HECHO (2026-10-06)**. Tres cosas quedaron resueltas:
   > 1. **La escala de AFIP se migró del sistema anterior** (`Sindicato.dbo.Intereses`): **74 filas**, 37
   >    por cada serie, de 1901 al 2025-07-01. Ver "Hecho".
   > 2. **La convención de días se MIDIÓ, no se eligió**: el recargo se prorratea por los **días del mes
   >    del 2° vencimiento** (10/31 en agosto). Con eso el caso real da **644,95 contra 644,96** (falla 1
   >    centavo, redondeo del sistema anterior). Con **días/30** daba **666,45** y fallaba **$21,49**. La
   >    columna `Diario` del sistema anterior es `Mensual/30` exacto, así que **tampoco sirve** para esto.
   > 3. **La serie que aplica es la resarcitoria** (la más baja), confirmado por el usuario.
   >
   > **Lo que queda de este punto**:
   > - La tasa vigente hoy es **2,75% (R (MEC) 823/2025)** desde 2025-07-01 y quedó **abierta**: si el
   >   sindicato publica una nueva, **hay que cargarla**, porque el sistema va a seguir usando la de julio
   >   2025 para siempre. Eso es dato, no código.
   > - **Falta que el usuario confirme la convención de días** contra la realidad del banco: la evidencia
   >   es **una sola boleta**, y el propio instructivo del banco decía que cuantas más boletas se
   >   contrasten, mejor. Con más casos se confirma o se corrige.
   > - `DeclarationService` (la mora de la DDJJ) usa la misma serie resarcitoria, pero sigue calculando
   >   **por meses enteros**, no por días: son dos conceptos distintos y por ahora conviven.
5. **Licencia médica** (definido con el usuario): se **marca al cargar la DDJJ**, período a período, con
   **certificado adjunto**. El empleado con licencia genera un **detalle en cero con motivo** (el único
   cero legítimo).
6. **Legajo de pluriempleo**: motivo obligatorio + comentario + adjuntos + estados + aprobación del
   sindicato, y la nota al pie en el PDF.
7. **Columnas ordenables** en `/empresa/{id}/empleados`; **desvinculación** de empleado (motivo + fecha);
   **validar el destino del traspaso de empleado en el servicio**, no sólo en la lista.
8. **Alcance del Auditor** en las pantallas que faltan (dashboard, inspection-stats, inspectors,
   union/inspection, legal-notices, admin/payments, unions) y en los endpoints de export.
9. **Bug del eje Y** en `CollectionsReport.razor:74` (los valores son múltiplos exactos de MiB).
10. **Calidad de datos**: 7 empleados sin nombre; empresas cuyo `BusinessName` es un CUIT o un fragmento;
    **el `CctCode` del sindicato 20 (La Banda) está en `"PENDIENTE"`** y debería ser `130/75`.
    (`AntiguedadPercentage = 0` en las 1.339 escalas **ya está resuelto**: se cargó el 1%.)
11. ~~**`DeclarationService` se come los errores del PDF y del mail en silencio**~~ **HECHO (2026-10-06)**:
    se le inyectó `ILogger<DeclarationService>` y el `catch` vacío pasó a registrar el error con el id
    de la DDJJ, la empresa y el período. La DDJJ **sigue sin fallar** por el mail (eso estaba bien), pero
    el problema ya no queda invisible. Era el **único `catch` vacío de todo el código**.
12. ~~**La pantalla de tasas ya no borra**~~ **HECHO (2026-10-06)**: se cambió el borrado definitivo por
    **desactivar** (el patrón que usan los bancos) y se agregó **edición**. Lo que sigue pendiente acá:
    - Sigue siendo **global**, no por sindicato (`InterestRate` no tiene `UnionId`), y **sólo para rol
      Admin**, no para el estudio contable. Las dos cosas están bien por ahora (el sindicato que factura
      es uno solo), pero conviene revisarlo cuando entren los otros sindicatos.
13. **Los scripts de esquema generados quedaron viejos tras el modelo de ramas.**
    `script_sirga_db_produccion.sql` y `script_sirga_db_upgrade_produccion.sql` **crean
    `SalaryScales.UnionId` y `EmployeeCategories.UnionId`**, columnas que ya no existen, y no crean
    `RamaId`. Son artefactos generados (no se tocan a mano y **no se corren desde acá**): hay que
    **regenerarlos** con `dotnet ef migrations script` cuando se prepare un despliegue, si no el esquema
    que armen no va a coincidir con el modelo. Lo mismo con `.dsh-tools/schema_verify.sql`.
14. **El mail al estudio adjunta la boleta VIEJA, no la del banco.** `DeclarationService` genera el
    adjunto con `PdfReportService.GeneratePaymentBoleta` (el comprobante interno, sin código de barras),
    cuando debería mandar la boleta bancaria triplicada. No es un cambio de una línea: `BoletaPdfService`
    está en **WebUI** y `DeclarationService` en **Application**, así que hay que mover el renderizado de
    la boleta a Application (o invertir la dependencia) para que el mail pueda usarlo. **Va junto con el
    pendiente del `PresencialBarCode`, que sigue guardando el código ficticio** (`001` + CUIT + período +
    importe) en lugar de los 58 dígitos reales del banco.
15. **Confirmar la convención de la tasa diaria con más boletas reales.** El motor usa **tasa diaria =
    mensual / 30** (especificación de ARCA que pasó el usuario), pero la **única boleta real** que tenemos
    (GAMMAROTA: 72.703,76, vencía el 12/08/2025 y tiene impreso un recargo de **644,96** por 10 días) da
    **666,45** con /30 (21,49 de diferencia); con días del mes daba 644,95. Con **una** boleta no alcanza
    para decidir si esa boleta usó otra convención o si el número impreso está mal. **Ya no afecta al 2°
    vencimiento** (se eliminó), pero **sí afecta a la mora y al interés de la boleta**, o sea a lo que se
    cobra. Pidiendo 2 o 3 boletas más (basta el importe, las fechas y el total) se cierra.
    > **Se encontró un SEGUNDO caso real que usa días del mes, no /30** (2026-10-07): la DDJJ de EL NENE
    > SRL del período 2026-06 pagó **21.133,24** sobre un total de **20.947,42**, o sea **185,82** de
    > interés, que es exactamente `20.947,42 × 2,75% × 10/31`. Dos casos exactos apuntan a "días del mes".
    > **El usuario decidió que se use /30 como dice ARCA, que es lo oficial**, y revisar esto más adelante
    > con más datos. **Queda pendiente, no cerrado.**
16. **Rectificativas: lo que todavía no está.**
    - ~~La comparación empleado por empleado~~ **HECHO (2026-10-07)**: ver "Hecho".
    - **`ApproveDeclarationAsync(id, observation)` y `RejectDeclarationAsync(id, observation)` siguen
      IGNORANDO la observación**: se pide y no se guarda en ningún lado. Ya estaba anotado como pendiente.
    - **No existe "guardar borrador"**: la única acción es presentar, así que el estado `Draft` sólo
      aparece si una presentación queda a medias.
    - **Las 20.977 rectificativas migradas** no tienen original ni `PreviousAmount`: su saldo es el total
      completo (que es lo correcto si la original no se pagó, pero no se puede verificar caso por caso).

---

## 8. Reglas de negocio que no se deducen del código

- **Las escalas y las paritarias son NACIONALES por rama** (confirmado por el usuario): **no existe el
  caso de dos sindicatos de la misma rama con escalas distintas**. Por eso las escalas cuelgan de la
  **rama** y no del sindicato, y no hace falta definir precedencia ni unificar nada al migrarlas.
- **La transferencia de una empresa entre estudios la decide y ejecuta el Sindicato.** El estudio destino
  no puede pedirla. Queda historial (`CompanyStudioHistories`) y aviso al estudio anterior.
- **El Sindicato asigna a sus auditores** desde una pantalla, y el **Auditor sólo ve su sindicato en
  todas las pantallas**. Sin asignar: no ve nada y se le avisa.
- **La boleta es una por DDJJ, por período, por el total.** Es **triplicada** (cliente/banco/sindicato).
- **El recargo del 2° vencimiento** sale de la escala de tasas en base; 1er vencimiento el **15 del mes
  posterior** al período y el 2° **10 días después** (ambos configurables por sindicato).
  **La tasa que aplica es la RESARCITORIA** (la serie más baja de las dos que trae la escala), y el
  recargo se prorratea por los **días del mes del 2° vencimiento** (no por 30): medido contra la boleta
  real de GAMMAROTA, 2,75% × 10/31 = **644,95** contra 644,96 impreso, mientras que con /30 daba 666,45.
- **El aporte de ley y la cuota socio son 2% cada uno**, y la **cuota socio es por empresa** (el
  pluriempleo paga dos cuotas: es correcto).
- **La DDJJ se puede presentar con el legajo de pluriempleo pendiente**; el Sindicato aprueba ese legajo.
- **Un empleado puede pertenecer a dos empresas y dos estudios distintos.**
- **Cada sindicato tiene su código de ente** en el código de barras: Santiago del Estero **0134**, La
  Banda 0135, Frías 0136, Añatuya 0137.
- **El mínimo de escala es el sueldo que el empleado DEBERÍA cobrar** según escala, antigüedad y
  jornada. Es un **valor de referencia para comparar**: se muestra si el estudio está pagando **debajo,
  igual o superior**. **No reemplaza** la remuneración del recibo de sueldo, que carga el estudio a mano.
- **La antigüedad es el 1% por año trabajado**, y se cuenta desde la **fecha de ingreso del empleado a
  esa empresa** (`SOCEMP_FECHAING`), no desde el alta al sindicato. Son **años cumplidos**: si el
  aniversario todavía no llegó en el mes del período, ese año no cuenta.
- **Ser socio NO cambia el mínimo de escala**: el mínimo es el de convenio para todos. Ser socio sólo
  agrega la **cuota socio del 2%** al descuento.
- **La jornada es Completa o Parcial, y Parcial = 50% del mínimo completo**, incluidos los acuerdos no
  remunerativos.
- **El mínimo de escala INCLUYE los acuerdos no remunerativos (NR1 + NR2)**, pero los NR **no** entran
  en la **base de aportes**: el 2% se calcula sobre básico + antigüedad. NR = no remunerativo.
- **La jornada es por asignación, no por empleado**: el mismo empleado puede estar a jornada completa en
  una empresa y parcial en otra.

---

## 9. Cómo se verifica el trabajo

- **Build con 0 avisos** y **tests en verde** antes de cada commit (hoy **165/165**).
- **Consultas contra la base** para probar afirmaciones de datos (no suponer: medir).
- **Simulacro (dry-run) antes de cualquier escritura masiva**, con respaldo del estado anterior.
- **Al terminar: la app queda levantada** en `http://localhost:5122` y **avisar al usuario** lo que no se
  pudo verificar (por ejemplo, lo visual en el navegador o un clic en la UI, que un agente no puede hacer).
   > - **`AddHostedService<T>()` registra el servicio SÓLO como `IHostedService`, no como `T`** (usa
   >   `TryAddEnumerable`). Si además hay que inyectarlo en una pantalla, **hay que registrarlo dos veces**:
   >   un `AddSingleton<T>()` y el `AddHostedService(p => p.GetRequiredService<T>())`. Sin eso la página
   >   devuelve **HTTP 500** y el log no dice nada útil (costó una ronda entera encontrarlo).
   > - **Una página nueva se prueba con `Invoke-WebRequest`, no con el build**: que compile no dice que
   >   cargue. La pantalla de conciliación pasó el build en 0 avisos y devolvía 500 igual.
---

## 10. ESTADO AL 2026-10-20 — LO QUE CAMBIÓ Y LO QUE FALTA

> Esta sección es el estado **actual**. Las secciones de arriba son historial: donde se contradigan,
> **manda esta**.

### 10.1. ✅ EL BANCO APROBÓ LAS BOLETAS

El 2026-10-19 el banco (Fabio Aoad) respondió: **"se cobraron correctamente las boletas con los cambios
en el algoritmo del DV"**. Con eso queda **cerrado** el problema que ocupó media sesión.

- **La fórmula del DV está validada por el banco**, no es una propuesta. Ver `CALCULO_DIGITO_VERIFICADOR.md`:
  pesos **3 y 1 alternados desde la izquierda**, suma de los productos **tal cual** (sin sumar los dígitos),
  **división por 2 con parte entera**, y **módulo 10**. Verificador en la **posición 58**, sobre los 57.
- **El banco hizo la prueba en su AMBIENTE DE DESARROLLO**, con boletas del 09/10/2026, y **modificó
  el CB (código de barras) que está en producción** para hacerla. ⚠️ Hay que preguntarles **si eso quedó
  revertido** o si producción ya acepta el DV nuevo.
- **Preguntas que nos hicieron y siguen sin respuesta nuestra**:
  1. **¿Las boletas de 58 van a trabajarse sólo con el DV nuevo, sin convivencia con el anterior?**
     → **DECIDIDO POR EL USUARIO (2026-10-22): NO HAY CONVIVENCIA.** SIRGA emite **sólo** con el DV nuevo, y
     **las boletas emitidas con el sistema anterior dejan de tener validez a partir de la fecha de
     implementación del nuevo sistema.**
     ⚠️ **Consecuencia que hay que manejar**: toda boleta vieja **impaga** al momento del corte **queda
     incobrable**. ⇒ La fecha se elige sabiendo cuántas boletas viejas quedan, y las que queden se **reemiten**.
  2. **¿Desde qué fecha se pone en producción el CB nuevo?** → **decisión del usuario**, pendiente.

### 10.2. ✅ EL CIRCUITO COMPLETO, PROBADO CONTRA EL BANCO

El archivo que mandaron (`Pagos_1_091026.bse`, 5 líneas, 222 caracteres, formato de 58) **concilió** con
las 5 DDJJ de SIRGA. **Es la primera vez que el circuito se prueba de punta a punta con datos del banco:**

| Boleta | CUIT | Período | Importe | DDJJ | Resultado |
|---|---|---|---|---|---|
| 1 | 20957235191 | 2026-08 | 29.655,26 | **1737095** | Exacta |
| 2 | 30709341622 | 2026-07 | 16.923,22 | **1737076** | Con interés |
| 3 | 33699784929 | 2026-07 | 209.549,93 | **1737077** | Con interés |
| 4 | 30590360763 | 2026-06 | 15.585.473,57 | **1728873** | **Ya pagada** |
| 5 | 20072020163 | 2025-04 | 205.626,59 | **1699459** | Con interés |

⇒ **SIRGA leyó el archivo del banco, encontró las 4 DDJJ, calculó el interés y supo qué escribir.**
En el legado, 3 de esas 4 ya figuraban pagadas y **no se tocaron** (idempotencia). Sólo una se escribiría:
CUIT 20072020163 → fila Id **239018**, `fpago = 09/10/2026`, `impban1 = 205.626,59`.

⚠️ **ESTE ARCHIVO ERA UNA PRUEBA EN DESARROLLO: no movió plata real.** **NUNCA** procesarlo con escritura
habilitada: marcaría 4 DDJJ reales como pagadas por ~$461.755 que nadie pagó.

### 10.3. ✅ CONTROL NUEVO: UNA DDJJ PAGADA NO EMITE BOLETA

**Lo encontró la prueba del banco**: entre las 5 boletas estaba la de la DDJJ **1728873**, que **ya figuraba
pagada**, y el banco **la cobró**. Para el banco es una boleta válida — **no tiene forma de detectarlo**.
El control tiene que estar de nuestro lado.

`BoletaService.ArmarAsync` ahora **rechaza** el estado `Paid` (commit `f3d42c4`). **Las tres guardas de la
boleta, todas en el servicio** (no en la pantalla, para que no se salteen por API):

| Estado | ¿Emite? | Motivo |
|---|---|---|
| **Draft** | ❌ | No está presentada; SIGES todavía no la ve (el volcado al legado ocurre en la presentación) |
| **Rectificada** | ❌ | La deuda es de la versión vigente |
| **Paid** | ❌ | Se cobraría dos veces |

⚠️ **Ninguna de las tres tiene test unitario** (`BoletaService` depende de `IUnitOfWork`). Se verifican con
el verificador contra la base real. **Si alguien borra una guarda, los 165 tests siguen en verde**:
cubrirlas con tests de integración es un pendiente real.

### 10.4. ⚠️ EL VERIFICADOR SE ROMPIÓ Y SE REPARÓ: LA LECCIÓN

Durante esta etapa **rompí dos veces `.dsh-tools\verif-boleta\Verificador.cs`** editándolo **por número de
línea**. El segundo intento empeoró: el build fallaba por los DLL bloqueados (la app no estaba cerrada) y
**creí que era sólo eso**, sin sospechar de mi edición.

**Lo que hay que hacer siempre:**
1. **Matar la app antes de compilar** (bloquea los `.dll`). Si el build dice `MSB3027` / "archivo bloqueado",
   es esto.
2. **No editar código por índices de línea.** Restaurar del zip y reemplazar por **texto literal con guarda**:
   si el texto no aparece, **no escribir nada**.
3. **La prueba de que un archivo está bien es que COMPILE**, no que el texto se vea bien.
4. Verificar el tamaño: el original tiene **39.951 bytes**; la versión rota tenía **39.263**.

Detalle práctico: el verificador lee rutas **hardcodeadas**, y al cambiarlas hay que cuidar que el archivo
usa **barras dobles** (`entrada\\Procesados\\`). Hoy apunta a
`C:\BackUp\SEC SGO\20261009\Sind. Empleados de Comercio\Pagos_1_091026.bse`. **Es frágil**: si se mueve el
archivo, hay que volver a cambiarla.

### 10.5. ✅ RESPALDO DEL PROYECTO (hecho) — Y LO QUE FALTA

- **9 zips** del código (sin `obj`/`bin`/`node_modules`): **2,3 GB** en `C:\Proyectos\_RESPALDOS\2026-10-08`.
- **9 repositorios con commits**: SIRGA, SIGESWEB, SIGES (con `SIGES\src` adentro), Maratón, Talleres,
  FDDEDC, Bicicleteada, siges-futbol, SECSantiagoHome. Cuatro de ellos **no tenían git** y se inicializaron.
  `SIGES` tenía **487 cambios sin guardar** y su último commit era de **mayo de 2023**.
- **36 bases respaldadas** con `COPY_ONLY` (no altera las cadenas de respaldo existentes): **1,2 GB** en
  `C:\Proyectos\_RESPALDOS\2026-10-15-bases`. Incluye `Sindicato` (627 MB), `SIRGA_DB`, `SIGES_Articulos`,
  las de los eventos y `EstudioContableDb_Prod`.
- ❌ **PENDIENTE URGENTE: los respaldos están en el MISMO DISCO que los proyectos.** Un respaldo en el
  mismo disco **no protege de una falla de disco**. Hay que copiar `_RESPALDOS` a un disco externo, NAS o
  nube (3,5 GB).

### 10.6. ⚠️ HALLAZGOS DE SEGURIDAD (sin resolver)

- **Contraseñas de SQL en texto plano** en `SIGESWEB.Web\appsettings.json` y `publish\appsettings.json`,
  **incluida la de producción** (`192.168.1.240`). Ya están en los commits. ⇒ **Antes de subir a GitHub**
  hay que sacarlas a un archivo ignorado **y rotar la contraseña** (ya estuvo expuesta).
- **`EstudioContableDb_Prod`** existe en este servidor local. El usuario dijo que producción está en **otra
  red**, así que debería ser una copia vieja — **pero el nombre dice lo contrario: confirmar qué es.**

### 10.7. LAS CAJAS: TODO DOCUMENTADO, NADA IMPLEMENTADO

El plan completo está en **`C:\Proyectos\SIGESWEB`** (4 documentos, escritos con el usuario):

| Documento | Qué tiene |
|---|---|
| `PLAN_CAJAS_UNIFICADAS.md` | arquitectura: núcleo compartido + frentes + servicio central; las 5 partes |
| `COMO_FUNCIONA_LA_NUMERACION.md` | offline-first, numeración por bloques jerárquicos, stock por punto, factura vs recibo |
| `INVENTARIO_PROYECTOS.md` | el escaneo de los 10 proyectos (¡los eventos son copias del mismo!) |
| `PENDIENTES.md` | pendientes con dueño + todas las decisiones cerrradas |

**Decisiones centrales ya tomadas:** la caja va en **SIGESWEB**; el núcleo se extrae de `SIGES\src`
(que ya tiene caja, impresora térmica y los dos comprobantes); **un ticket de 80 mm Y media hoja A4**;
**una sola serie de recibos** para todo el sistema; **factura sólo cantina y farmacia**; **Holistor se
alimenta, no se reemplaza**; **bases separadas** por sistema.
⚠️ **Lote y vencimiento** hay que preverlos desde el principio: la farmacia los necesita y si el stock
nace sin ellos, hay que rehacerlo.

### 10.8. LO QUE FALTA, EN ORDEN

1. **Copiar `_RESPALDOS` a otro disco** (lo único que protege contra un disco roto).
2. **La respuesta al banco**: fecha de producción y convivencia de DVs (decisión del usuario) + preguntar
   si revirtieron el CB de producción.
3. **Sacar las contraseñas de SQL del repositorio y rotarlas** (antes de GitHub).
4. **Tests de integración de las 3 guardas de la boleta.**
5. **Datos que dependen de terceros**: el **plan de cuentas** (Administración, "el martes"), **cómo facturan
   hoy cantina y farmacia** (AFIP web service vs controlador fiscal), **cuántos puntos de venta**, y el
   **formato de importación de Holistor** (estudio contable).
6. **Las cajas**: empezar por listar las tablas de caja reales de cada base (sólo lectura).
7. **Las decisiones de datos de 10.9** (NR1 de enero–marzo, si el importe de la escala incluye las
   sumas no remunerativas, el diciembre 2025 duplicado y los dos huecos viejos).

### 10.9. ✅ ESCALAS 2026 AL DÍA Y TASAS VERIFICADAS (2026-10-20)

Se revisaron las dos tablas que estaban "viejas" y se generaron los scripts. **Informe completo, con
fuentes y mediciones: `INFORME_ESCALAS_TASAS_2026.md`.**

**Escalas — FALTABAN CARGAR (no era que no hubieran salido).** La paritaria está firmada, homologada y
publicada: **5%** en abril–junio (2% / 1,5% / 1,5%) y **5,7%** en julio–septiembre (1,9% × 3). Octubre
**no tiene aumento**: la revisión se hace en octubre y los valores nuevos impactan **desde noviembre**.

- La copia del sistema anterior (`Sindicato.dbo.EscalaSalarial`) tiene **exactamente los mismos 1.339
  registros** y también termina en 2026-03 ⇒ el atraso viene del operador del sistema viejo, no de SIRGA.
- Cargadas **173 filas**: **126** de abril a septiembre (21 categorías × 6 períodos), **21** de octubre
  (sin aumento, igual a septiembre) y **26** de los huecos viejos (2021-04 completo y las 5 categorías
  que faltaban en 2023-04, **copiadas del mes siguiente** por decisión del usuario). 1.339 → **1.512**
  escalas, 62 → **70** períodos: la serie queda **completa de 2021-01 a 2026-10, sin meses faltantes** y
  con 21 activas en cada uno. Se aplica el porcentaje sobre el importe de marzo; `NR2` sigue en 120.000
  (se incorpora al básico desde diciembre) y `AntiguedadPercentage` en 1%.
  > **Ojo con los valores de 2021-04 y 2023-04**: son **derivados** (copia del mes posterior). El aumento
  > que estaba en mayo pasa a estar en abril y abril = mayo. Si el sindicato tiene la escala real de esos
  > meses, hay que reemplazarlas.
- **Octubre** no tiene aumento (la paritaria se reabre en octubre y los valores impactan desde
  noviembre), así que se cargó igual a septiembre. Antes funcionaba por el fallback de
  `DeclarationService` (`Period <= targetDate`), pero ahora cada período declarado tiene su fila.
- **Diciembre 2025 estaba roto y se corrigió**: las 21 filas eran una **copia exacta de diciembre 2024**
  (mínimo 877.687,16, un 33% abajo de noviembre; 1.310 DDJJ afectadas). Se rehizo con la escala oficial
  de FAECyS (acuerdo 12/2025): diciembre = noviembre × 1,06/1,05 (1% mensual no acumulativo, base junio
  2025) y los NR de noviembre ⇒ mínimo **1.326.131,31**. Respaldo `dbo._BackupSalaryScalesDic2025`.
  Script `script_sirga_db_escalas_2025_12_corregir.sql` (idempotente: sólo toca las filas que siguen
  iguales a 2024).
- Script `script_sirga_db_escalas_2026_abril_septiembre.sql`: **simulacro por defecto** (`@Simulacro=1`),
  respaldo en `dbo._BackupSalaryScales2026`, idempotente (2ª corrida = **0 insertadas**), con controles de
  categorías por período, duplicadas y monotonía.

**Tasas — NO FALTAN.** La última es del **2025-07-01** (2,75% resarcitoria / 3,50% punitoria, R (MEC)
823/2025) y **ARCA no la cambió** hasta hoy: verificado contra la página de Estudio Paz (actualizada al
19/08/2026), Fiscal.com.ar (2026) y las notas de la baja de julio 2025 (CPCE Córdoba, FECOI, Primera
Edición). ⇒ **los intereses no están mal por tasa vieja.** Lo único que estaba mal era un dato: la fila
resarcitoria de 2025-07 tenía `DailyRate = NULL` (se completaron 2 filas, no se tocó ninguna tasa) y
**no había ningún control** que avisara si la tasa queda sin revisar.
- Script `script_sirga_db_tasas_interes_2026.sql`: no inserta tasas, completa `DailyRate`, verifica 0
  huecos / 0 solapamientos, y mantiene `dbo.ControlTasasRevision` (avisa si pasan **90 días** sin
  contrastar con ARCA; si nunca se revisó, avisa en vez de dar OK). Idempotente (2ª corrida = 0).
- La prueba de dinero sigue dando exacto: 51.000 al 2,75% por 35 días = **1.636,25** (DDJJ 1737080).

**⚠️ HALLAZGOS QUE NECESITAN UNA DECISIÓN (son datos, no código):**

1. **El NR1 de enero–marzo** viene creciendo 2% / 3,5% / 5% del básico, que es la estructura de un
   adelanto no remunerativo del 5%. ¿Es el adelanto del 5% de abril (habría que ponerlo en 0) o una suma
   aparte (se arrastra)? Hoy el script **lo arrastra** (`@AbsorberNR1 = 0`) porque no hay fuente que
   autorice borrar un concepto. Si se pone en 0, el mínimo de junio vuelve exactamente al de marzo y el
   aumento se ve recién en julio (abril y mayo quedan 2,6% y 1,3% por debajo).
2. **El importe cargado parece incluir las sumas no remunerativas**: `Importe − 120.000` queda a ±2% del
   básico oficial de marzo en las categorías administrativas. Si es así, el mínimo está **$120.000
   inflado** porque además se suma `NR2`. El mínimo cargado ya estaba **+15,6% a +21,5%** arriba de
   (básico oficial + 120.000) **antes** de esta carga. Confirmar con el sindicato.

> Los otros dos hallazgos quedaron cerrados: **diciembre 2025 se corrigió** (ver arriba) y **los huecos
> de 2021-04 y 2023-04 se completaron** copiando el mes siguiente.

**Lo que no se pudo verificar**: la pantalla (el agente no opera el navegador) y la tabla oficial mes por
mes de mayo a agosto (se aplicaron los porcentajes del acuerdo; sólo abril y octubre están publicados con
todos los importes).

### 10.10. ✅ `/companies` CON MENÚ ⋮ Y EL SCROLL QUE NO EXISTÍA (2026-11-03)

Commits **`0989dad`** y **`27c0ede`**. Build **0 errores**, **0 avisos** del compilador y del analizador
(los 56 NU1902/NU1903 son de NuGet, ver trampa 20), **165/165** tests, app levantada en
`http://localhost:5122` con el binario nuevo.

- **Las acciones de `/companies` van en un menú `⋮`.** Antes eran **7 iconos sueltos** por fila (el traspaso
  decía 8). Quedan **a la vista 2**: **Ver Empleados** y **Nueva DDJJ**; al menú pasan **Historial de DDJJ**,
  **Estado de Cuenta**, **Añadir Sucursal**, **Editar Empresa** y **Ver Ubicación**. Lo mismo en la fila de
  la **sucursal** (debajo de la tabla), para que las dos se lean igual. Se respetó lo que ya estaba: los
  ítems que dependen del estado de la empresa siguen deshabilitados si no está activa, y cada ítem lleva su
  ícono y su color como antes.
- **`Nueva DDJJ` ahora abre con la empresa de la fila elegida** (`/declarations/new?empresa={id}`): antes
  caía en un formulario vacío y había que volver a buscar la empresa. La pantalla ya soportaba el
  parámetro (lo usa el botón "Rectificar"), y contempla la sucursal.
- **Se corrigió el scroll horizontal de las 35 tablas** (ver trampa 19): el parámetro es
  **`HorizontalScrollbar`**. Esto es lo que el usuario venía reclamando como *"objetos que no renderizan
  bien"*: el scroll pedido en la sesión anterior nunca funcionó.
- **Los filtros de `/companies` van en una franja propia a todo el ancho** (commits `b34a7bd` y `c3e6fe3`,
  pedido del usuario con captura): antes se apretaban en un bloque a la izquierda y sobraba ancho.
  > **NO SE PUEDE PEDIRLE EL ANCHO A UN CONTROL DE MUD CON `Style`.** El ancho lo decide el contenedor
  > (`.mud-input-control` es `display:flex; flex:1 1 auto; max-width:100%`), así que `Style="flex: …"` o
  > `min-width` en `MudTextField`/`MudSelect` **no estira nada** y la fila queda corta. Con `flex` en la
  > barra de la tabla quedaba un **hueco de 264 px** a la derecha. La solución que funciona: **sacar los
  > filtros de `ToolBarContent`** a un `MudPaper` propio (ancho de página) y adentro una **grilla**
  > `repeat(auto-fit, minmax(190px, 1fr))` con cada control envuelto en un `div` que es la celda.
  > **Medido** con navegador headless sobre el CSS real de MudBlazor (`.dsh-tools/filtros-test/test.html`):
  > 4 celdas de **246 px** y último borde en **1060** de 1076 disponibles (sólo el padding). La flex
  > anterior daba celdas de 184 px y el borde derecho en **812**.
  - **Los cuatro controles tienen que quedar alineados** (commit `5e3ce0b`): el buscador **no tenía rótulo**
    y los selectores sí, así que su línea quedaba ~14 px más arriba. Ahora el buscador tiene rótulo
    (**"Buscar empresa"**, con el texto de ayuda como placeholder) y **cada celda alinea por abajo**
    (`display:flex; align-items:flex-end`), así la línea coincide aunque el control de arriba mida distinto.
    Medido: 4 columnas de **254 px**, las cuatro líneas terminan en **y=73**.
- **El diálogo de alta de empresa se rediseñó** (commit `487a363`, pedido del usuario: *"debe ser más
  profesional, no tan sencilla"*): encabezado con ícono y **título corto** ("Registrar nueva empresa") con
  el estudio y quién aprueba en el **subtítulo** (antes el título se cortaba en dos renglones), **seis
  secciones con rótulo** (`Typo.overline` + `MudDivider`): Identificación fiscal, Datos de la empresa,
  Ubicación, Referencia en la empresa, Sucursales y Sindicato; campos **pareados en dos columnas**
  (razón social a ancho completo), **ícono en cada campo** y ayuda junto al CUIT y en Sindicato. El
  `MudDialog` pasa de `MaxWidth.Small` a **`MaxWidth.Medium`**.
  > **La lógica no se tocó**: al rehacer el marcado se comparó contra el commit anterior y quedaron
  > **iguales** los **13 `@bind-Value`**, los **8 campos obligatorios**, el bloqueo por CUIT
  > (`inert` + atenuado), el aviso neón (`alerta-cuit-neon`) y los handlers (`HandleCuitBlur`,
  > `ValidateCuitField`, `Submit`, `Cancel`). Si se vuelve a tocar el diálogo, **comparar esos conteos**
  > antes de commitear: es la forma barata de no perder un campo en un rediseño.
  - **La búsqueda ahora mira más datos**: antes sólo CUIT y razón social, así que buscar una **localidad**
    o el **nombre de fantasía** no encontraba nada. Ahora también `TradeName` y el nombre de la ciudad
    (con el mapa de `_cities` que ya estaba cargado), y el **CUIT con o sin guiones** vía
    `NormalizedCuit`. Todo **null-safe**: hay empresas sin CUIT, sin fantasía y sin localidad.

**Lo que NO se pudo verificar** (lo tiene que ver el usuario en pantalla): que el menú `⋮` **abra** y se
vea bien, que los ítems se vean habilitados/deshabilitados como corresponde, y que el **scroll horizontal**
se comporte con el mouse. Un agente no opera el navegador.

**Sobre el entorno (esto costó tiempo y está en las trampas 21 y 22)**: el build falló primero por
permisos de Windows en `SIRGA.Domain\obj` y después porque `dotnet restore` no pasa por el sandbox
restringido. Con la sesión en acceso completo, build y tests corren normales.

### 10.11. ✅ EMPLEADOS: LOS DATOS DE `maesoc` EN LA FICHA, LA GRILLA Y EL ALTA (2026-11-03)

El usuario marcó que la pantalla de empleados *"debe ser más profesional"*, que **son muy pocos datos
cuando los campos de maesoc son muchos**, que **no pide CUIL** y que **apellido y nombre no están
separados**. Medido: `Sindicato.fotos_.maesoc` tiene **49 columnas** y `Employees` tenía **7**.

- **`Employees` pasó a tener los datos del socio** (migración `AgregarDatosDeMaesocAEmpleados`, **18
  columnas nullable**): `LastName`, `FirstName` (separados), `BirthDate`, `Sex`, `DocumentTypeCode`,
  `DocumentNumber`, `Phone`, `Mobile`, `Email`, `WorkEmail`, `Address`, `AddressNumber`, `Floor`,
  `Apartment`, `Neighborhood`, `ZipCode`, `AffiliationNumber`, `CardNumber`.
  > **`Employees.Dni` ES EL CUIL**, no el número de documento (`MAESOC_CUIL` → `Dni`). El documento es
  > otro dato (`DocumentTypeCode` + `DocumentNumber`). La pantalla lo llamaba "DNI" y pedía un DNI.
- **El alta pide CUIL validado** (máscara `00-00000000-0`, validación de dígito verificador) y
  **apellido y nombre por separado**; `FullName` se arma siempre `Apellido Nombre` desde el servicio.
  El CUIL se guarda **normalizado** (11 dígitos) porque es la llave del cruce con el legado y las DDJJ.
- **La grilla** muestra CUIL formateado, **apellido**, **nombre**, categoría, **ingreso a la empresa**,
  estado, socio y un menú `⋮`; arriba, contadores de **activos / socios / bajas** y filtros en franja
  propia (buscar por CUIL/apellido/nombre, estado, socio y categoría).
- **Ficha del empleado** (`EmployeeDetailDialog`, nueva): identidad, contacto, domicilio y situación
  sindical, con los datos que faltan visibles como `—` (no como huecos).
- **`script_sirga_db_empleados_desde_maesoc.sql`**: completa las columnas nuevas desde `maesoc`.
  **Simulacro por defecto**, idempotente (sólo escribe donde está NULL), con respaldo
  `dbo._BackupEmployeesDatosMaesoc` y el `UPDATE` de vuelta atrás comentado al pie.
  - Medido en simulacro: **27.924** empleados cruzan por CUIL; hay apellido 27.924, nombre 27.916,
    nacimiento 27.913, sexo 27.924, documento 27.924, celular **3.013**, email **73**, domicilio
    27.809 y número de afiliado **7.227**. **10 empleados de SIRGA no existen en el legado** (se
    informan, no se tocan).
  - **Tipo de documento**: 3 = DNI (27.910), 2 = LE, 1 = LC, 4 = CI. El legado **no trae tabla de
    descripción**; el código desconocido se muestra como código (`Textos.TipoDocumento`), nunca con
    una etiqueta inventada. **Estado civil NO se migró**: hay 7.550 con código (`C`, `S`, `B`, `D`,
    `V`, `E`) y **no existe la tabla de códigos**.
  - ✅ **APLICADO el 2026-11-03 con el OK del usuario**: `UPDATE` de **27.924** filas. Verificado
    releyendo de la base (no con el output del script): CUIL `20217178836` → `CORONEL` / `ORACIO
    RUBEN`, nacimiento 18/07/1973, sexo M, documento DNI 21717883. Respaldo
    `dbo._BackupEmployeesDatosMaesoc` con **27.924** filas (todas con la columna previa en NULL).
    **Segunda corrida = mismas cifras** (idempotente, no toca el respaldo).
  - **Quedan 10 empleados sin datos**: son los `"SIN NOMBRE <cuil>"` que tampoco están en el legado
    (se informan, no se tocan). Coincide con el pendiente de calidad de datos de la sección 7.
  > **OJO CON LAS FECHAS CENTINELA**: el sistema anterior usa **`1900-01-01`** como "sin fecha" y hay
  > **7.849** empleados así en el alta al sindicato (`HireDate`). Mostrarla como fecha real es una
  > mentira silenciosa: la ficha y el padrón muestran **"—"** cuando el año es < 1950. El ingreso a la
  > empresa (`Assignments.CreatedAt`) **no tiene centinelas** (mínimo 1961), así que esa columna sí es
  > confiable.

**Categoría y jornada (commit `f03908c`, pedido del usuario: *"¿por qué aparece eso en categoría?"* y
*"¿por qué no aparecen para cargar la jornada?"*)**

- **La pantalla mostraba `Código 7` porque leía `Assignments.CategoryCode` y nunca resolvía el
  nombre.** La categoría cuelga de la **rama** del sindicato (`EmployeeCategories.Code` + `Name`):
  `EmployeeService.CategoriasPorCodigoAsync` arma el mapa y el DTO lleva `CategoryName`. El **7** es
  **"AUXILIAR ESPECIALIZADO A"**, el 2 "ADMINISTRATIVO B" y el 13 "MAESTRANZA B". Si el código no
  existe en la rama se muestra el código, nunca un nombre inventado.
- **La categoría y la jornada ahora se cargan** en el alta y en la **edición** (el botón "Editar
  empleado" era un aviso de *"no implementada"* y ahora abre el diálogo): van en la **asignación**
  (`Assignments.CategoryCode` / `Jornada`), porque el mismo empleado puede tener otra jornada en otra
  empresa. Sin categoría la DDJJ no puede calcular el mínimo de escala.
- **Verificado con transacción revertida** (`.dsh-tools/verif-empleado`, no versionado): guarda por el
  servicio real, **relee de SQL con el `ChangeTracker` limpio** y comprueba nombre, categoría, jornada,
  socio y contacto; después `ROLLBACK` y confirma que volvió al estado original. **Un `SaveChanges`
  que compila no prueba que guarde**: esto sí.
- **Fechas de doble entrada** (commit `dab877a`, pedido del usuario: *"que sea combinado el ingreso"*):
  los `MudDatePicker` estaban **sólo con calendario** y había que cazar el día a clics. Ahora llevan
  **`Editable="true"`** y aceptan el tipeo `dd/mm/aaaa` **o** el calendario: nacimiento e ingreso a la
  empresa (ficha), fecha de pago (`PaymentDateDialog`), conciliación y vigencias de tasas.
  > **Ojo con `@bind-Date`**: `MudDatePicker.Date` es `DateTime?`. Si el DTO tiene `DateTime`, el
  > binding no compila (`EventCallback<DateTime>` vs `EventCallback`): se ata a un campo local
  > `DateTime?` y se copia al DTO al guardar.
- **El período y el año van en el ENCABEZADO de Nueva DDJJ** (commit `1d888f1`): antes había que volver
  al paso 1 para saber qué mes se estaba declarando, y en los pasos 2 y 3 se declaraba a ciegas. El
  encabezado (fuera del stepper, visible en los tres pasos) muestra Empresa, CUIT, **"Período: Noviembre
  2026"** y un chip con el estado de ese período (`Ya presentada (…)` o `Sin DDJJ`).
- **Una empresa INACTIVA no puede presentar DDJJ** (commit `5b7e1c2`): la regla está en
  **`DeclarationService.ValidarEmpresaActiva`**, que se llama desde `CalcularAsync` — o sea, cubre el
  guardado **y** la previsualización de rectificativa, no se puede saltear por API.
  > **La causa era tener DOS banderas y mirar una sola**: la grilla muestra el chip con `Company.IsActive`
  > y el botón de Nueva DDJJ se habilitaba con `Company.Status`. Una empresa con `IsActive = 0` y
  > `Status = Active` se podía declarar. **La regla mira las dos.** Ahora el ícono también, la pantalla de
  > Nueva DDJJ no lista inactivas y, si llega una por query string, **avisa** en vez de ignorarla.
  > Cubierto con **3 pruebas** (`EmpresaInactivaTests`, 168/168): activa no lanza, inactiva lanza con el
  > nombre de la empresa, y un `Status` distinto de `Active` lanza aunque `IsActive` esté en `true`.
- **Se agregó la FECHA DE INGRESO A LA EMPRESA** al alta y a la edición: **no existía**, y el alta la
  dejaba en la fecha del día. De esa fecha sale la **antigüedad** del mínimo de escala, así que ahora se
  puede cargar la real (el alta arranca con hoy y se corrige a mano).
- **Nueva DDJJ: el selector de mes dice qué períodos ya tienen DDJJ** (commit `87fdcfe`, pedido del
  usuario). El paso 1 se separó en **"Empresa y sindicato"** y **"Período a declarar"** (antes era una
  grilla de 5 campos que se partía en dos renglones sin criterio), y **cada mes del selector lleva un
  chip con su estado**: `Sin DDJJ` / Borrador / Presentada / Aprobada / Pagada / Rechazada, más la
  fecha de presentación y el total al lado del mes elegido.
  > **La versión que manda es la vigente**: se agrupa por período y se toma la de **mayor
  > `VersionNumber` que no esté `Rectificada`**. Los estados se cargan al elegir empresa/sindicato y al
  > cambiar el año (`CargarEstadoDelAnioAsync`, con `GetDeclarationsByCompanyAsync`).
  > **⚠️ TRAMPA (me pasó y lo vio el usuario): `@for` + `MudSelectItem` muestra el ÚLTIMO valor.**
  > El contenido del ítem es un `RenderFragment` que se evalúa **al final del bucle**, así que los doce
  > meses leían la misma variable `mes` (que terminaba en 13) y la pantalla mostraba **"Mes 13"** en
  > todos, con "Sin DDJJ". Hay que **copiar la variable en cada vuelta**: `var mesActual = mes;` y usar
  > `mesActual` en el `Value` y en el contenido. Es el cierre sobre la variable del bucle de siempre, y
  > **compila igual**: sólo se ve ejecutando. **Cómo verificarlo sin navegador**: agregar
  > `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` al csproj, compilar y mirar
  > `obj\...\generated\...\NewDeclaration_razor.g.cs` (cada ítem debe recibir su propio `mesActual`);
  > después **quitar la bandera**.
- **Paso 2 de la DDJJ (detalle de empleados) despejado** (commit `0fbd7f3`, pedido del usuario:
  *"información muy amontonada, sobre todo la composición del sueldo de escala"*): encabezado con título
  y explicación (antes un `MudAlert` que empujaba los botones), **columna Empleado con el CUIL debajo**,
  y el mínimo de escala en **renglones alineados** —Básico / Antigüedad (con los años) / No remunerativo,
  importes a la derecha— en lugar de la frase corrida *"Básico X + antig. Y + NR Z"* que se partía en
  tres líneas. Se quitó el **"Debajo de escala" duplicado** (estaba como chip y como texto de error del
  campo) y las filas dejaron de ser densas.
- **Ancho de la página y de la tabla de la DDJJ** (commit `7d53f5d`): el contenedor del layout pasó de
  **`MaxWidth.Large` (1280 px) a `ExtraLarge` (1536 px)** — gana ~256 px en todas las pantallas— y la
  tabla del paso 2 achica la letra de celdas y selectores a **0.78rem**, recorta paddings, limita los
  desplegables de categoría/jornada a **185 px** y recorta el nombre de la categoría con puntos
  suspensivos. Los estilos van en **`NewDeclaration.razor.css`** (scoped, se sirven en
  `SIRGA.WebUI.styles.css`), colgados de un **`div` propio del componente**: la clase puesta en `MudTable`
  puede terminar en un elemento interno y ahí el `::deep` no engancha.
  > **`wwwroot/css/app.css` NO está linkeado** en `App.razor`: los estilos propios de una página van en
  > su `.razor.css` (o en `css/sirga-alertas.css`, que sí está linkeado).
- **Alta de empleado: más ancha y sin leyendas** (commits `348ecb8` y `d2e6177`): se quitaron dos leyendas
  que ocupaban media fila (*"Con el CUIL se cruzan las DDJJ…"* y *"Se puede escribir a mano…"*), **CUIL +
  Apellido + Nombre** pasaron a una sola fila (4 columnas cada uno) y el diálogo va en **`MaxWidth.Large`**.
  > **El ancho del diálogo lo decide QUIEN LO ABRE, no el diálogo**: `AddEmployeeDialog` se abre desde
  > `/empresa/{id}/empleados` **y** desde la DDJJ (`NewDeclaration`), cada uno con sus `DialogOptions`.
  > Cambiar uno solo deja la mitad del problema: con la DDJJ en `Small` los rótulos seguían cortados
  > (*"Tipo de documen…"*, *"Ingreso a la em…"*). **Si se agrega otro lugar que abra este diálogo, pasarle
  > `MaxWidth.Large`.**
- **Diferencia entre la remuneración y el mínimo, dentro de la composición** (commits `0e3a039` y
  `0f3c870`): la cuenta es **remuneración − mínimo de escala**, en **verde** cuando está **igual o por
  encima** y en **rojo** cuando está **por debajo**, con el signo (`+` / `−`) siempre visible.
  > **Va como CUARTA LÍNEA de la composición** (debajo de "No remunerativo", separada con una línea), **no
  > como columna**: el usuario la pidió así y agregar una novena columna volvía a apretar la tabla.
  > La resta la hace la app (C#) sobre el `MinSalary` que calculó `SalaryMinimumCalculator`; **no rehacer
  > esa cuenta en SQL** (ver trampa 17).
- **Totales del paso 2 en un panel propio debajo de la tabla** (commits `02cdff3` y `548f764`):
  **Remuneración ingresada**, **Total por escala**, **Diferencia** (verde/rojo) y **Aportes (ley +
  voluntario)**, más una línea con cuántos empleados hay, cuántos tienen escala y el detalle de cada aporte.
  **Los empleados sin escala no entran en la diferencia** (no tienen con qué compararse).
  > **NO poner los totales en el `FooterContent` de `MudTable`**: el ancho de las columnas de aportes
  > queda en ~60 px (lo fija el cuerpo de la tabla), los importes se parten en dos renglones y los
  > rótulos se desalinean. En un `MudPaper` aparte con una grilla `auto-fit` se leen bien.

### 10.12. ✅ LO QUE LE FALTABA AL EMPLEADO RESPECTO DEL SISTEMA ANTERIOR (2026-11-04)

El usuario mostró la pantalla de **Alta de Empleado del sistema viejo** y preguntó qué datos nos faltaban
para que el estudio no salga de lo que ya usa. Se comparó campo por campo y se agregó el **lote 1**
(commit posterior al `5b7e1c2`), todo migrado del legado:

| Campo | Dónde vive | Origen | Cobertura |
|---|---|---|---|
| **Actividad** (catálogo de 349) | `Assignments.ActivityCode` | `maesoc.MAESOC_CODACT` | 36.004 asignaciones |
| **Obra social** (catálogo de 297) | `Employees.HealthInsuranceCode` | `maesoc.MAESOC_CODOSM` | 24.837 (el 0 = sin obra social va NULL) |
| **Nro. de legajo** | `Assignments.FileNumber` | `socemp.SOCEMP_NROLEG` | 35.941 asignaciones |
| **Es jubilado** | `Employees.IsRetired` | `maesoc.MAESOC_JUBIL` (0/1/2 → los dos últimos, sí) | 281 |
| **Carga familiar** | `Employees.HasFamilyCharge` | `maesoc.MAESOC_CARGAFLIA` (0/1, es sí/no) | 3.844 |

- Tablas nuevas **`Activities`** y **`HealthInsurances`**, cargadas en
  `script_sirga_db_empleados_actividad_os.sql` (simulacro por defecto, idempotente, respaldo en
  `_BackupEmployeesActividadOs` y `_BackupAssignmentsActividadLegajo`).
  > **Ojo con los catálogos del legado**: `actividad` y `obrasocial` traen filas con la descripción
  > **NULL**, así que el INSERT tiene que usar `ISNULL(NULLIF(…), '(sin descripción)')` y agrupar por
  > código, o revienta por la columna NOT NULL.
- La **actividad y el legajo van en la ASIGNACIÓN**, no en el empleado: el mismo socio tiene otro legajo
  y otra actividad en cada empresa, igual que en el origen (`socemp`). La **obra social, jubilado y carga
  familiar** son del empleado.
- La pantalla muestra los **nombres**, no los códigos: `EmployeeService.ActividadesPorCodigoAsync` /
  `ObrasSocialesPorCodigoAsync` resuelven los catálogos (chicos: 349 y 297).

**Lo que sigue faltando, y no se puede inventar:**
1. **Estado civil**: 7.550 socios tienen código (`C`, `S`, `B`, `D`, `V`, `E`) pero **no existe la tabla
   de códigos** en el sistema viejo.
2. **Nacionalidad**: sólo 254 cargados y sin catálogo.
3. **Sexo "no especificado"**: el viejo lo permite, nosotros sólo M/F (es agregar la opción).
4. **Localidad del empleado ligada a provincia/ciudad** y **causa de baja** (`socemp.SOCEMP_CAUSABAJA`).
5. **Grupo sanguíneo** (`MAESOC_GRUPOSANG`, 100%): es dato de salud, no se migró sin preguntar.

### 10.13. ✅ AL PRESENTAR LA DDJJ SE CREA EL SOCIO EN EL PADRÓN DEL SINDICATO (2026-11-04)

**El problema que descubrió el usuario**: la tabla `ddjj` **no lleva el nombre del empleado**. SIGES WEB
arma el nombre buscándolo en `fotos_.maesoc` por CUIL y, si no está, **muestra el CUIL con el nombre y el
DNI vacíos, sin avisar**. Medido: **295** renglones de `ddjj` con un CUIL sin maestro y **10** empleados
de SIRGA que no están en `maesoc`. Como SIRGA pasó a ser el que da de alta a los empleados, cada alta
nueva iba a sumarse a esa lista.

- **`LegacyDeclarationWriter.EscribirAsync`** ahora, **dentro de la misma transacción** en que vuelca
  `ddjj`/`ddjjt`, llama a `AsegurarMaestroAsync`: crea el socio en **`fotos_.maesoc`** y su relación con
  la empresa en **`fotos_.socemp`**.
  > **Sólo INSERTA lo que falta** (`ExisteSocioAsync` / `ExisteRelacionAsync` por CUIL y CUIT): el padrón
  > lo mantiene el sindicato a mano desde hace años y **no se pisa**.
  > **Va en la presentación** (decisión del usuario): antes, el empleado no "existe" para el sindicato.
- Los datos salen del empleado (apellido, nombre, documento, nacimiento, sexo, contacto, domicilio, obra
  social, jubilado, carga familiar) y de la asignación (categoría, actividad, legajo, fecha de ingreso).
  **Lo que SIRGA no tiene va vacío** (decisión del usuario: no se inventan valores).
- **Centinelas del legado copiados de una fila real**: las fechas desconocidas van con **`1000-01-01`**
  (`MAESOC_FECHADOC`, `MAESOC_FECHADESEMPLEO`, `SOCEMP_FECHABAJA`), `MAESOC_ESTCIV = ' '`,
  `MAESOC_ESTADO = 1`, `SOCEMP_ULT_EMPRESA = 'S'` y `MAESOC_USUMOD = 'SIRGA'`. **Todas las columnas son
  NOT NULL** (aun las que no tienen dato), así que hay que llenarlas todas.
  > **`SOCEMP_CODSUC`** se hereda del que ya usan los demás empleados de esa empresa (el legado lo numera
  > por sucursal); si la empresa no tiene a nadie, 0.
  > **`maesoc` no tiene identity ni clave**: la clave es `MAESOC_CUIL` (numérico, float).
- **Verificado contra una copia de las tablas** (base `SIRGA_Test_Legado`, creada y borrada en la misma
  sesión; `.dsh-tools/verif-padron`, no versionado): la primera presentación crea el socio y la relación
  con todos los datos; la segunda dice **"padrón sin cambios"** (idempotente); un socio preexistente
  queda intacto; y el padrón real terminó con las mismas 27.924 filas.
  > **Nunca probar esto contra `Sindicato` directamente**: `EscribirAsync` hace `COMMIT` por dentro, así
  > que no se puede revertir desde afuera. La forma segura es la copia en otra base.

**Espejado de los datos que el estudio edita (commit `65f1b6c`)**

Antes, si el estudio corregía el celular de un socio que ya existía, **SIGES seguía mostrando el viejo**:
el volcado sólo creaba lo que faltaba. Ahora, al presentar, `ActualizarSocioAsync` espeja en `maesoc` los
campos que SIRGA maneja (apellido, nombre, `APENOM`, documento, tipo de documento, nacimiento, sexo,
teléfono, celular, email, email laboral, calle, número, piso, depto, barrio, CP, categoría, actividad y
obra social).

> **TRES REGLAS QUE NO SE ROMPEN**: (1) un valor **vacío** en SIRGA **no pisa** el del padrón (si el
> estudio borra el celular, en `maesoc` queda el último que había); (2) los campos que SIRGA **no tiene**
> (estado civil, nacionalidad, grupo sanguíneo, CBU, impuestos, nro. de afiliado, carnet, alta/baja,
> estado) **no se tocan**; (3) **jubilado y carga familiar sólo se ponen en 1** — nunca se borra un "sí"
> que haya cargado el sindicato.
> El `WHERE` compara campo por campo, así que **no escribe ni cuenta de más** cuando no hay cambios.

- Verificado contra la copia (5 casos): socio nuevo; segunda presentación **"padrón sin cambios"**; cambio
  de celular y email **reflejado**; borrado en SIRGA que **no borra** en el padrón; y un socio preexistente
  (CORONEL) espejado **sin tocar** estado civil, nacionalidad ni grupo sanguíneo (`USUMOD` pasa a `SIRGA`,
  que es el rastro de quién escribió). El padrón real quedó con las mismas **27.924** filas.




