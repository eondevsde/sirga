# SIRGA — Contexto, estado y traspaso

> **Este archivo es la memoria del proyecto.** Si sos un agente que arranca una sesión: **leé esto
> primero**, entero. Contiene lo que no se deduce del código: las reglas que no se negocian, los
> atajos que ya nos costaron caro y lo que falta hacer.

Última actualización: **2026-10-06** · 143/143 tests · build 0 avisos.
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
  - **El aviso es obligatorio porque es automático**: la pantalla de Nueva DDJJ muestra el modal
    `RectificationWarningDialog` con la versión anterior, su total y **lo que se pagó**, antes de
    confirmar. Que sea automático no quiere decir que tenga que ser invisible.
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
    - **La comparación empleado por empleado** antes de presentar (mi propuesta original): hoy el modal
      avisa con los **totales** (versión anterior, total, pagado) y el saldo. Comparar el detalle
      empleado por empleado permitiría ver *qué* cambió; hoy sólo se compara el total, así que si uno sube
      y otro baja la diferencia lo esconde.
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

- **Build con 0 avisos** y **tests en verde** antes de cada commit (hoy 142/142).
- **Consultas contra la base** para probar afirmaciones de datos (no suponer: medir).
- **Simulacro (dry-run) antes de cualquier escritura masiva**, con respaldo del estado anterior.
- **Al terminar: la app queda levantada** en `http://localhost:5122` y **avisar al usuario** lo que no se
  pudo verificar (por ejemplo, lo visual en el navegador o un clic en la UI, que un agente no puede hacer).
