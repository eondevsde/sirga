# SIRGA — Contexto, estado y traspaso

> **Este archivo es la memoria del proyecto.** Si sos un agente que arranca una sesión: **leé esto
> primero**, entero. Contiene lo que no se deduce del código: las reglas que no se negocian, los
> atajos que ya nos costaron caro y lo que falta hacer.

Última actualización: **2026-10-06** · 102 commits en `main` · 119/119 tests · build 0 avisos.

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
| Pestaña SQL | Plugin de DSH en `C:\Users\drop1\.dsh\profiles\node_modules\@deepseek-ai\dsh-sql-tab` (sólo lectura, dev + legacy, sin producción) |

**Para levantar la app** (job en background, `workdir = SIRGA.WebUI`):
`dotnet "C:\Proyectos\SIRGA\SIRGA.WebUI\bin\Debug\net10.0\SIRGA.WebUI.dll" --urls "http://localhost:5122"`

**Para compilar hay que matar la app primero** (bloquea los DLL): buscar el proceso que **escucha el
puerto 5122** con `Get-NetTCPConnection -LocalPort 5122` y matarlo. No matar por nombre.

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
  **sindicato** — con el código dibujado vía ZXing + QuestPDF. *Escrito y compilando; todavía sin botón
  que lo dispare.*
- **Categorías de los empleados migradas** desde `maesoc`: **9.586** asignaciones activas (quedan **7**
  sin resolver, empleados con `FullName = "SIN NOMBRE"`). Respaldo en
  `dbo._BackupAssignmentsCategoryCode`.
- **Control anti-cero**: el alta de DDJJ **no guarda** si no calculó ningún importe y hay empleados
  asignados (antes guardaba en cero como si estuviera bien).
- **Borrador que se reanuda**: si existe un borrador de esa empresa y período se **reutiliza** (antes
  tiraba una excepción y dejaba al usuario sin salida).
- **Pestaña SQL** en el GUI de DSH: sólo lectura, doble candado (lista negra + transacción con ROLLBACK),
  sólo `SIRGA_DB` y `Sindicato`.
- **Tabla `Ramas`** creada (paso 1 de separar las escalas del sindicato).

---

## 7. Pendiente, en orden

1. **Probar la DDJJ de ABDALA LLAMIL** (empresa 29656, sindicato 19): sus dos empleados ya tienen
   categoría (13 MAESTRANZA B y 2 ADMINISTRATIVO B) y escala vigente 2026-03. **Tiene que calcular
   importes.** Es la prueba de que el problema de los ceros quedó cerrado para las 2.551 empresas.
   > **ESTADO (2026-10-06): el usuario YA LA PROBÓ y reportó que "hay cosas que corregir", sin
   > detallar cuáles.** Lo primero al arrancar la próxima sesión: **pedirle el detalle de esas
   > correcciones** antes de tocar nada. No inventar qué falló ni asumir que funciona.
2. **Modelo de ramas, pasos 2 a 4**: vincular `Unions` a `Rama`; pasar `EmployeeCategories` y
   `SalaryScales` a colgar de la **rama**; y recién al final cambiar las consultas que filtran por
   `UnionId` para que filtren por la rama del sindicato. **Las escalas son nacionales y dependen de la
   rama** (Comercio 130/75, Panaderos, Farmacéuticos...), no de la ciudad: hoy hay que copiarlas por
   sindicato, que es el error que este modelo corrige. El paso 4 toca el cálculo de la DDJJ: **hacerlo
   descansado y con la prueba al lado**.
3. **Botón de imprimir la boleta** que dispare el PDF ya escrito.
4. **Recargo del 2° vencimiento**: 10 días después del 1° (configurable), con la tasa de `InterestRates`.
   **Dato importante: la tabla de tasas tiene sólo el 3% por defecto; la escala de AFIP NO está cargada.**
   Caso de prueba real: la boleta de GAMMAROTA tenía recargo **$644,96 sobre $72.703,76 = 0,887% en 10
   días** → el cálculo tiene que reproducir ese número.
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
    `AntiguedadPercentage = 0` en las 1.339 escalas.

---

## 8. Reglas de negocio que no se deducen del código

- **La transferencia de una empresa entre estudios la decide y ejecuta el Sindicato.** El estudio destino
  no puede pedirla. Queda historial (`CompanyStudioHistories`) y aviso al estudio anterior.
- **El Sindicato asigna a sus auditores** desde una pantalla, y el **Auditor sólo ve su sindicato en
  todas las pantallas**. Sin asignar: no ve nada y se le avisa.
- **La boleta es una por DDJJ, por período, por el total.** Es **triplicada** (cliente/banco/sindicato).
- **El recargo del 2° vencimiento** sale de la escala de tasas en base; 1er vencimiento el **15 del mes
  posterior** al período y el 2° **10 días después** (ambos configurables por sindicato).
- **El aporte de ley y la cuota socio son 2% cada uno**, y la **cuota socio es por empresa** (el
  pluriempleo paga dos cuotas: es correcto).
- **La DDJJ se puede presentar con el legajo de pluriempleo pendiente**; el Sindicato aprueba ese legajo.
- **Un empleado puede pertenecer a dos empresas y dos estudios distintos.**
- **Cada sindicato tiene su código de ente** en el código de barras: Santiago del Estero **0134**, La
  Banda 0135, Frías 0136, Añatuya 0137.

---

## 9. Cómo se verifica el trabajo

- **Build con 0 avisos** y **tests en verde** antes de cada commit (hoy 119/119).
- **Consultas contra la base** para probar afirmaciones de datos (no suponer: medir).
- **Simulacro (dry-run) antes de cualquier escritura masiva**, con respaldo del estado anterior.
- **Al terminar: la app queda levantada** en `http://localhost:5122` y **avisar al usuario** lo que no se
  pudo verificar (por ejemplo, lo visual en el navegador o un clic en la UI, que un agente no puede hacer).
