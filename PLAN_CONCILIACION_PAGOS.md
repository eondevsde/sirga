# Plan: registrar el archivo del banco y actualizar las tablas

> Estado: **propuesta**, sin ejecutar. Todo lo que dice acá abajo está **medido** contra `SIRGA_DB` y la
> copia local de `Sindicato`; lo que no se pudo medir está marcado como **abierto**.
> Última actualización: 2026-10-07.

## 0. Arquitectura (definida por el usuario, 2026-10-07)

**El reparto de responsabilidades es este:**

| Sistema | Qué hace |
|---|---|
| **SIRGA** | **Maneja las DDJJ (CRUD) y REGISTRA LOS PAGOS.** Es la fuente de verdad. |
| **SIGES WEB** | **Lee las actualizaciones**, determina qué empresa tiene deuda y cuál no, **emite actas** (fiscalización), y maneja la **administración del sindicato**. |

⇒ **SIRGA tiene que alimentar `ddjj` y `ddjjt`** (y algún dato más que pueda surgir). Nada más.
**SIGES WEB es consumidor, no socio: no hay que alimentar sus tablas de cobranza.**

**Consecuencia 1 — el "esquema duplicado" de SIGES WEB NO nos bloquea.** Lo que quedó marcado como
problema abierto en `MAPA_MIGRACION_LEGACY_A_WEB.md` (legacy `Cobro`/`Recibo` vs `Fisc_*`) es sobre
**cómo SIGES WEB cobra sus actas y planes**, que es **otro dominio**. SIRGA **no** escribe ahí.
⇒ Queda despejado: **SIRGA escribe `ddjj`/`ddjjt` y listo.**

**Consecuencia 2 — la forma de pago vive en SIRGA, no en el legado.** Como `ddjjt` **no tiene un campo
confiable de forma de pago** (`medio1` viene en 0), y SIRGA no escribe en las tablas de cobranza de
SIGES WEB, **el catálogo de formas de pago es interno de SIRGA** (`PaymentTransactions.PaymentMethod`).
Para los informes "más nutridos" que pidió el usuario, conviene **usar los mismos nombres que las filas
de `FormaDePago` de SIGES WEB**, así los dos sistemas hablan el mismo idioma aunque no compartan tabla.

**Consecuencia 3 — el CRUD de DDJJ también escribe el legado.** Cuando SIRGA crea o modifica una DDJJ,
tiene que dejar `ddjj` (detalle por empleado) y `ddjjt` (cabecera con `titem1`/`titem2`) coherentes,
porque **SIGES WEB lee de ahí para decidir si hay deuda**. Una DDJJ creada sólo en SIRGA y no volcada al
legado **es invisible para SIGES WEB** y no genera acta.

## 1. El escenario

El banco manda el **archivo de rendición** con los cobros del día. Hay que:

1. registrarlos en SIRGA (pagos + estado de la DDJJ),
2. y dejar **`ddjjt`** (y `ddjj`) del sistema legado actualizados, porque **SIGES WEB se alimenta de ahí**.

## 2. Lo que se midió (no es supuesto)

### Dónde vive el pago

| Base | Tabla | Rol |
|---|---|---|
| `SIRGA_DB` | **`PaymentTransactions`** | el pago (una fila por cobro) |
| `SIRGA_DB` | **`Declarations`** | `Status`, `PaymentDate`, `BalanceAmount`, `TotalAmount` |
| `Sindicato` | **`ddjjt`** (261.975 filas) | **cabecera de la DDJJ + los pagos** |
| `Sindicato` | **`ddjj`** (1.350.524 filas) | detalle por empleado |
| `Sindicato` | `ddjj_`, `ddjjt_` | **son respaldos: NO se tocan** (lo confirmó el usuario) |

### Campos de pago en `ddjjt`, medidos

| Campo | Qué tiene | Medición |
|---|---|---|
| **`fpago`** | **fecha de pago** | **198.513 filas llenas** |
| `fpago2`, `fpago3` | 2ª y 3ª fecha | **0 filas: NUNCA se usan** |
| `impban1` | **importe pagado por banco** | lleno en las pagadas |
| `impban2`, `impban3` | 2º y 3º importe | no se usan |
| `trans1/2/3` | nº de transacción | **`trans1` = 0** en los ejemplos recientes |
| `medio1/2/3` | ¿forma de pago? | **`medio1` = 0** en los ejemplos recientes |
| `fmod`, `usuario` | cuándo y quién modificó | para dejar rastro |
| `estado` | valores **0, 1, 10, 11, 20, 21** | **las DDJJ PAGADAS tienen `estado` = 0** |

### Las tres conclusiones que cambian el diseño

1. **El campo que marca "pagada" es `fpago`, no `estado`.** Lo confirmó el usuario y lo confirma la
   medición: las DDJJ pagadas más recientes tienen **`estado` = 0**, igual que las impagas.
   ⇒ **Al registrar un pago hay que llenar `fpago`** (y `impban1`), y **no hay que inventar `estado`**.
2. **El sistema viejo usa UN solo juego de fecha/importe**: `fpago2`, `fpago3`, `impban2`, `impban3`
   están **vacíos en las 261.975 filas**. Los tres juegos existen en la tabla pero **nunca se usaron**.
   ⇒ Se escribe **siempre en el juego 1**.
3. **No hay de dónde sacar la forma de pago.** `medio1` viene en **0** en los pagos recientes y `trans1`
   también: no son confiables como forma de pago. `PaymentTransactions.PaymentMethod` es **texto libre**.
   ⇒ Por eso hay que **crear las tablas de parametrización** (punto 3).

### Riesgo medido: la unión `ddjj` ↔ `ddjjt` NO es única

El cruce `d.cuite = t.cuit AND d.periodo = t.periodo AND d.rect = t.rect` devuelve **1.352.753 filas**
sobre 1.350.524 de `ddjj` ⇒ **hay `ddjjt` repetidos para la misma clave** (probablemente por
`nver`/versión). **Antes de escribir hay que definir cuál es la fila buena** (¿la de mayor `Id`?, ¿la de
`nver` más alto?). Si se elige mal, se marca pagada la versión equivocada.

## 3. Paso 1: tablas de parametrización (lo que pidió el usuario)

Para poder emitir informes más nutridos y no guardar texto libre:

> ⚠️ **ANTES DE CREAR NADA: LA PARAMETRIZACIÓN YA EXISTE EN SIGES WEB.**
> Relevado en `C:\Proyectos\SIGESWEB` (2026-10-07). SIGES WEB tiene **47 entidades mapeadas** y entre ellas
> **ya están las de pagos**: **`FormaDePago`**, **`TipoDePago`**, `Cheque`, `Transferencia`, `Cobro`,
> `Recibo`, `ReciboDetalle`, `ReciboCuota`, `CondonacionInteres`, `LoteCobranza`, `PlanDePago`,
> `CuotaPlan`, `SecuenciaPeriodo` — y todo el bloque nuevo **`Fisc_*`** (`FiscPago` + `FiscPagoEfectivo`
> / `+Transferencia` / `+Cheque`, `FiscConcepto`, `FiscApiKey`).
> ⇒ **NO hay que inventar un catálogo de formas de pago en SIRGA: hay que usar `FormaDePago`/`TipoDePago`
> de SIGES WEB**, si no quedan dos catálogos que dicen lo mismo y tarde o temprano no coinciden.
>
> **Y ojo con esto, que es un problema ABIERTO en el propio SIGES WEB**: su documento
> `MAPA_MIGRACION_LEGACY_A_WEB.md` dice textualmente que *"conviven el modelo legacy (`Cobros`,
> `Recibos`, `FormasDePago`, `PlanDePagoActas`) y el nuevo `Fisc_*`"* y lista como pendiente
> *"definir y documentar la fuente de verdad de pagos: legacy vs `Fisc_*`"*.
> ⇒ **SIGES WEB todavía no decidió dónde vive un pago.** Entonces, antes de escribir el registrador de
> SIRGA, hay que **preguntar cuál de los dos modelos es el vigente**: de eso depende qué tabla tiene que
> alimentar SIRGA, y no se puede adivinar desde SIRGA.

| Tabla nueva | Para qué | Contenido inicial |
|---|---|---|
| ~~`PaymentMethods`~~ | **usar `FormaDePago`/`TipoDePago` de SIGES WEB** | ver arriba |
| `Banks` | ya existe (se agregó para la boleta) | BSE |
| `PaymentOrigins` (opcional) | de dónde vino el registro | Manual, Importación del banco, MercadoPago |

> **Dato adicional**: `SIGESWEB` **tampoco escribe `fpago`/`impban1`** — se verificó que no hay ninguna
> asignación a esos campos en todo el proyecto (sólo los lee). Es decir: **el escritor de `ddjjt` no
> existe en ningún lado; SIRGA sería el primero.** Y `SindicatoDbContext` mapea `ddjjt` con
> **`HasKey(x => x.Id)`**, no con la clave compuesta, así que al escribir hay que ser explícito con el
> `Id` de la fila elegida.

`PaymentTransactions.PaymentMethod` (hoy `nvarchar`) pasa a apuntar al catálogo por `PaymentMethodId`,
**conservando el texto viejo** para no perder historia (se migra: los valores existentes se mapean al
catálogo y se deja el texto original en una columna de respaldo).

> **Decisión pendiente**: cuántas formas de pago quiere el estudio en el catálogo. Se define cuando se vea
> qué trae el archivo del banco.

## 4. Paso 2: importar el archivo del banco

**La llave de la conciliación es el CÓDIGO DE BARRAS**, no el CUIT ni el importe:

- El archivo del banco trae, entre otros, los campos **`Comprobante`**, **`Importe`**, **`Código de Barra`**,
  `Fecha de Proceso` (documentados en `info-tecnica`).
- **El código de barras contiene el CUIT y el período** (posiciones 6 a 25: período MMAA + CUIT), y el
  importe (posiciones 32 a 41). Es decir: **una línea del archivo identifica la DDJJ por sí sola**.
- Flow: `código` → verificar el dígito verificador → extraer CUIT + período + importe → buscar la DDJJ en
  SIRGA por (empresa, período) → **si el importe coincide, es un cobro válido**.

> ⚠️ **Hoy esto NO funciona**: `BankReconciliationService.ProcessBankExcelAsync` usa un **"mapeo tentativo"
> propio** (Col A fecha, B referencia, C importe, D CUIT/ID) que **no coincide** con el archivo del banco y
> **no usa el código de barras**. Es lo primero que hay que reescribir.

Guardar **todo el archivo en una tabla de staging** (`BankRenditionLines`) antes de tocar nada: permite
reprocesar, auditar y no depender de tener el Excel a mano.

## 5. Paso 3: escribir en SIRGA

Por cada línea válida y **no procesada antes** (idempotencia: identificar por `ExternalReference` =
código de barras + fecha de proceso):

1. `PaymentTransactions`: `DeclarationId`, `Amount`, `PaymentDate`, `PaymentMethodId`, `ExternalReference`,
   `Notes`.
2. `Declarations`: `Status = Paid`, `PaymentDate`, y recalcular `BalanceAmount`.

## 6. Paso 4: escribir en el legado (`ddjjt`)

**Sólo `ddjjt`.** El detalle por empleado (`ddjj`) **no tiene ningún campo de pago** (verificado columna por
columna: son período, cuit, cuil, importes y banderas). Lo que hay que escribir es:

```sql
UPDATE t
SET t.fpago  = @fechaPago,      -- ES el campo que marca la DDJJ como pagada
    t.impban1 = @importe,       -- importe pagado por banco
    t.fmod   = GETDATE(),       -- deja rastro de cuándo se tocó
    t.usuario = @usuario        -- y quién
FROM Sindicato.dbo.ddjjt t
WHERE t.cuit  = @cuit
  AND t.periodo = @periodo
  AND t.rect   = @rect
  AND t.fpago IS NULL;          -- idempotente: no reescribe lo ya pagado
```

**NO tocar**: `estado` (no se entiende y las pagadas lo tienen en 0), `trans*`, `medio*` (no son
confiables), `ddjj_`, `ddjjt_` (respaldos), ni `ddjj`.

> **Regla del proyecto**: antes de correr esto contra datos reales → **simulacro (dry-run) → respaldo de
> las filas afectadas → OK explícito del usuario**. Nunca contra producción.

## 7. Paso 5: verificar (no alcanza con que "no dio error")

1. Contar filas afectadas y compararlas con las líneas del archivo.
2. **Volver a leer** `ddjjt` y comparar `fpago`/`impban1` contra el archivo.
3. Sumar los importes del archivo y compararlos contra la suma de `PaymentTransactions` del día.
4. Que ninguna DDJJ haya quedado pagada **dos veces** (idempotencia: reimportar el mismo archivo no debe
   cambiar nada).

## 8. Lo que dice el código de SIGES WEB (esto es la fuente de verdad)

Relevado en `C:\Proyectos\SIGESWEB` el 2026-10-07. **SIGES WEB lee `ddjjt` DIRECTAMENTE**, con su propio
`SindicatoDbContext` (`SIGESWEB.Infrastructure\Data\SindicatoDbContext.cs`), y la pantalla es
`/actas/verificacion-de-deuda` (`Components\Pages\Actas\VerificacionDeDeuda.razor` + `ActasService`).

### La regla de "pagada", textual del código

`SIGESWEB.Infrastructure\Services\ActasService.cs`, línea 198:

```csharp
Estado = item.fpago.HasValue ? "Pagado" : "Pendiente",
```

⇒ **Una DDJJ está pagada si y sólo si `fpago` tiene valor.** `estado` **no se mira nunca** (queda
confirmado por el código, no por deducción).

### La regla de "cuál es la fila buena", textual del código

`EstudiosService.cs` (líneas 61, 205, 252) y `ReportesService.cs` (línea 42) usan **exactamente** esto:

```csharp
.Select(g => g.FirstOrDefault(x => x.fpago != null && (x.titem1 ?? 0) > 0)
              ?? g.OrderByDescending(x => x.rect).First())
```

⇒ **Agrupado por (cuit, período): gana la fila que está PAGADA y tiene `titem1 > 0`**; si no hay
ninguna, **la de mayor `rect`** (que es lo que indicó el usuario). Las dos condiciones juntas, no
sólo `rect`.

> **CONSECUENCIA CRÍTICA PARA NUESTRO ESCRITOR:** si escribimos `fpago` en una fila cuyo **`titem1` es
> 0**, SIGES WEB **NO la va a elegir** y va a seguir viendo la deuda. ⇒ **La fila que marquemos pagada
> tiene que tener `titem1 > 0`.** Hay que verificarlo fila por fila antes de escribir, no asumirlo.

### La deuda se calcula así

`EstudiosService.cs` línea 72:

```csharp
Deuda = g.Sum(d => (d.titem1 ?? 0) + (d.titem2 ?? 0) - (d.impban1 ?? 0))
```

⇒ **`titem1` + `titem2` = lo que se debe; `impban1` = lo que se pagó; deuda = la diferencia.** Con
`impban1` igual al total, la deuda del período queda en **0** y SIGES lo muestra como pagado.

### Cómo reporta la caja

`CajaService.cs` líneas 240-320: agrupa por **el mes de `fpago`** y suma **`impban1`** ⇒ *"lo recaudado
del mes"*. Es decir: **`fpago` manda la fecha y `impban1` el importe**, exactamente los dos campos que
nuestro escritor tiene que llenar.

### La clave primaria de `ddjjt`

Es **`periodo + cuit + rect + repite`** (está en el `FindByperiodocuitrectrepite` del DataSet del sistema
viejo). Por eso hay repetidas: la misma DDJJ puede tener varias versiones (`rect`) y repeticiones
(`repite`). **El escritor tiene que apuntar a una sola fila, nunca a todas.**

## 9. Abierto (lo que queda sin cerrar)

1. **Formato del archivo del banco**: no se tiene todavía. Cuando llegue, se define el parseo y el
   catálogo de formas de pago.
2. **Qué significan `estado` y `medio1/2/3`**: el usuario lo consulta en el sistema legacy. **Tarea
   pendiente anotada.** Dato nuevo: **SIGES WEB no usa ninguno de los dos** (usa `fpago`), así que **no
   bloquean el escritor** — se pueden dejar intactos.
3. ~~Cuál es la fila buena de `ddjjt`~~ → **RESUELTO** (punto 8): pagada con `titem1 > 0`, si no la de
   mayor `rect`.
4. **Si `ddjj` necesita algo al pagar**: verificado que **no tiene campos de pago**, y **SIGES WEB no lo
   consulta para el estado de deuda** (usa `ddjjt`). En principio **no se toca**.
5. **Símbolo del código de barras** (Code 128 vs Interleaved 2 of 5): afecta que el banco pueda leerlo.
6. **Verificar que la fila a marcar tenga `titem1 > 0`** (ver la consecuencia crítica del punto 8): hay
   que medirlo en los casos reales antes de escribir.

### MAPEO MEDIDO: qué es cada campo de `ddjjt` (2026-10-07)

Se midieron DDJJ pagadas reales de 2026-08. Resultado:

| periodo | CUIT | titem1 | titem2 | titem3 | titem4 | impban1 | fpago |
|---|---|---|---|---|---|---|---|
| 2026-08 | 20442023981 | 28.384,70 | 0 | 0 | 0 | 28.384,70 | 09/09/2026 |
| 2026-08 | 20061299093 | 31.568,41 | **31.568,41** | 0 | 0 | **63.136,82** | 01/09/2026 |
| 2026-08 | 20940514186 | 14.859,29 | 0 | 0 | 0 | **15.009,12** | 18/09/2026 |
| 2026-08 | 20952916751 | 12.221,37 | 0 | 0 | 0 | **12.355,81** | 10/09/2026 |

**Conclusiones:**

1. **`titem1` = APORTE DE LEY** y **`titem2` = CUOTA SINDICAL.** Prueba: la fila 2 tiene los dos
   iguales (31.568,41 = 2% + 2%) y **`impban1` = 63.136,82 = exactamente la suma de los dos**.
   Las demás tienen `titem2` = 0 porque no tienen socios.
2. **`titem3` y `titem4` están SIEMPRE en 0** ⇒ no se usan.
3. **`impban1` = lo efectivamente pagado, que incluye el INTERÉS.** Prueba: en las filas 3 y 5
   `impban1` **supera** a `titem1 + titem2` (15.009,12 > 14.859,29 y 12.355,81 > 12.221,37): esa
   diferencia es la mora.
4. Por eso la fórmula de deuda de SIGES WEB (`titem1 + titem2 − impban1`) da 0 o **negativo** cuando
   se pagó con interés ⇒ **no hay deuda** ✓.

**MAPEO A SIRGA (esto es lo que tiene que escribir el registrador):**

| `ddjjt` (legado) | SIRGA | 
|---|---|
| `titem1` | `Declaration.AporteLey` |
| `titem2` | `Declaration.CuotaSindical` |
| `impban1` | `PaymentTransactions.Amount` (capital + interés) |
| `fpago` | `PaymentTransactions.PaymentDate` |
| `fmod` / `usuario` | fecha del proceso / usuario que registró |
## 10. FORMATO DEL ARCHIVO DE RENDICION DEL BANCO (decodificado 2026-10-07)

Archivo real: `tools\Rendicion del Banco\BSELB20261006.txt` (Sindicato de **La Banda**).
**8 lineas: 7 de detalle (218 caracteres) + 1 de totales (88).**

### Lineas de detalle (empiezan con `1`), 218 caracteres

| Posiciones | Largo | Contenido |
|---|---|---|
| 1 | 1 | `1` = registro de detalle |
| 2 a 5 | 4 | numero de registro (`0001`..`0007`) |
| 6 a 13 | 8 | **fecha de proceso `DDMMAAAA`** (`06102026` = 06/10/2026) |
| 14 a 164 | 151 | relleno con ceros; **incluye el importe** (ej. `20610369` = $206.103,69) |
| **165 a 218** | **54** | **EL CODIGO DE BARRAS** |

⇒ **El codigo de barras son los ULTIMOS 54 caracteres de cada linea** (164 + 54 = 218 exacto).
Se decodifica con la estructura del **formato de 54 posiciones** y **ente 0135 (La Banda)**:

```
0135 4 0926 00000 30707713840 1 26282 20610369 26292 20793204 0 7
ente f  per  rell    CUIT      m  venc1  imp1     venc2  imp2    su DV
```
- **periodo** = posiciones 6-9 del codigo (`0926` = **09/2026**)
- **CUIT** = posiciones 15-25
- importes y fechas julianas de los dos vencimientos

### Linea de totales (empieza con `2`), 88 caracteres

| Posiciones | Contenido |
|---|---|
| 1 | `2` = registro de totales |
| 2 a 5 | **cantidad de registros** (`0007` = las 7 lineas de detalle) |
| 6 a 13 | fecha `DDMMAAAA` (`07102026`) |
| fin | **importe total** (`49547238` = $495.472,38) |

**VERIFICADO**: la suma de los 7 importes da **$495.472,38**, exactamente el total del archivo ✓.

### Como se identifica de quien es el pago

**Por PERIODO (MMAA) + CUIT**, que estan dentro del codigo de barras ⇒ **una linea identifica la DDJJ
por si sola**, sin depender del CUIT del encabezado ni del importe. (Es lo que detecto el usuario.)

### HALLAZGO IMPORTANTE: el formato depende del SINDICATO

Las boletas de **La Banda (0135)** son del **formato de 54 posiciones**, incluso con periodo **09/2026**.
En cambio **Santiago (0134)** ya usa **58** desde mediados de 2025.
⇒ **El formato del codigo (54 o 58) tiene que ser CONFIGURABLE POR SINDICATO**, igual que el ente.
Hoy `BoletaBse` arma siempre 58: **para La Banda emitiria un codigo que el banco no lee.**

### PENDIENTE
Decodificar los 151 caracteres del medio del encabezado (el banco tiene su propia especificacion del
archivo de rendicion). Para la conciliacion **no hace falta**: alcanza con el codigo de barras del final.
## 11. MAPEO DE `ddjj` (detalle por empleado) — relevado 2026-10-07

### Como se cruzan las dos bases (VERIFICADO)

La union entre SIRGA y el legado necesita **tres cosas**, y sin cualquiera de ellas da CERO filas:

```sql
JOIN Sindicato.dbo.ddjj l
  ON REPLACE(l.CUIL_STR,'-','') COLLATE DATABASE_DEFAULT
   = REPLACE(e.Dni,'-','')     COLLATE DATABASE_DEFAULT        -- CUIL: SIRGA guarda Employees.Dni
 AND l.CUIT_STR COLLATE DATABASE_DEFAULT = c.Cuit COLLATE DATABASE_DEFAULT
 AND CONVERT(varchar(7), l.periodo, 126) = s.Period            -- 'YYYY-MM' contra datetime
```

1. **`COLLATE DATABASE_DEFAULT` OBLIGATORIO**: las dos bases tienen cotejaciones distintas
   (`SQL_Latin1_General_CP1_CI_AS` vs `Modern_Spanish_CI_AS`) y sin esto el cruce **falla con error**, no
   devuelve vacio.
2. **La llave del empleado es `Employees.Dni`** (un CUIL de 11 digitos, sin guiones).
3. **El periodo del legado es `datetime`**: hay que normalizarlo a `YYYY-MM` para compararlo con
   `Declarations.Period`.

### Lo que se verifico comparando fila por fila

| SIRGA | Legado | Estado |
|---|---|---|
| `DeclarationDetails.BaseSalary` | `ddjj.impo` | **NO es el mismo dato** (ver abajo) |
| `DeclarationDetails.LawContribution` | — | **= `BaseSalary` × 2%** (verificado exacto) |
| `DeclarationDetails.MemberContribution` | `ddjj.item2` | **`item2` = 1 si el empleado es socio** (correlacion perfecta) |
| — | `ddjj.item1` | **= 1 siempre** en las filas comparadas |
| — | `ddjj.impom1` | **NO se usa nunca** (0 de 1.350.524 filas) |
| — | `ddjj.impp`, `ddjj.impoaux` | se usan poco (12% y 17%) |
| — | `ddjj.activo` | correlaciona con la condicion de socio: **falta confirmar** |

### Dos avisos que salieron del relevamiento

1. **`impo` NO es `BaseSalary`.** En las filas comparadas `BaseSalary` = 1.445.000 y `impo` = 1.391.956,98;
   en otra `BaseSalary` = 1.482.763 y `impo` = 1.835.674. **No hay proporcion constante** ⇒ `impo` es
   *otro* concepto (probablemente **la remuneracion declarada**, con antiguedad y NR incluidos).
   **NO mapear `impo` a `BaseSalary`**: hay que determinar de donde sale `impo` en SIRGA antes de escribir.
2. **El cruce con `ddjj` es 1 a N**: hay **CUIL repetidos** para el mismo CUIT y periodo en el legado.
   La comparacion de arriba mezcla filas por eso ⇒ **para terminar el mapeo hay que deduplicar primero**
   (es la misma clase de problema que la "fila buena" de `ddjjt`).
3. **Los importes del legado son `float`** y traen artefactos (`1729461.1599999999`). Al comparar y al
   escribir, tenerlo presente: **no se puede comparar por igualdad exacta**.

### Lo que falta para cerrar el mapeo de `ddjj`
- Deduplicar el legado y volver a comparar para confirmar `impo` contra el campo de SIRGA que le
  corresponde (¿la remuneracion calculada del detalle?).
- Confirmar `activo`, `cargaf`, `jub`, `jorp`, `item3`, `item4` (en las filas medidas dieron 0).
## 12. `ddjj` — resultado de la comparacion deduplicada (2026-10-07)

Se deduplico el legado (mayor `rect`, luego mayor `Id`) por (CUIT, CUIL) y se comparo contra
`DeclarationDetails`. **Tres errores propios que costaron tiempo y quedan anotados:**

1. **Literal de fecha**: la sesion usa `dateformat dmy`, asi que `periodo = '2026-08-01'` **no matchea
   nada** (devuelve 0 filas sin error). Va en `yyyymmdd`: `periodo >= '20260801' AND periodo < '20260901'`.
   Es la trampa que ya estaba documentada; volvi a caer.
2. **`COLLATE DATABASE_DEFAULT`** en cada comparacion entre las dos bases (si no, error).
3. **Deduplicar `ddjj`** antes de comparar: hay CUIL repetidos por CUIT y periodo.

### RESULTADO: `impo` NO se puede mapear todavia

| ddjj | BaseSalary | LawContribution | MemberContribution | `impo` | impo/base |
|---|---|---|---|---|---|
| 1737095 | 1.482.763,00 | 29.655,26 | 0 | 1.835.674,0 | 1,2380 |
| 1737084 | 1.442.050,00 | 28.841,00 | 0 | **707.102,0** | 0,4903 |
| 1737084 | 1.538.581,00 | 30.771,62 | 0 | **707.102,0** | 0,4596 |
| 1737084 | 1.393.500,00 | 27.870,00 | 0 | **707.102,0** | 0,5074 |
| 1737082 | 1.445.000,00 | 28.900,00 | 28.900,00 | 1.391.956,98 | 0,9633 |
| 1737082 | 1.705.760,77 | 34.115,22 | 34.115,22 | 1.729.461,16 | 1,0139 |

**`impo` es constante entre empleados de una misma DDJJ en algunos casos (707.102,0 en 4 empleados de
1737084) y varia en otros (1737082, que tiene socios).** No hay proporcion contra `BaseSalary`.
⇒ **Su significado sigue sin determinarse y NO se debe mapear por suposicion.** Escribirlo mal serian
importes incorrectos en 1,35 millones de filas.

### LO QUE DESBLOQUEA EL ESCRITOR (hallazgo de esta ronda)

**La deuda que calcula SIGES WEB NO usa `ddjj.impo`.** Usa **`ddjjt.titem1 + titem2 − impban1`**
(`EstudiosService.cs` linea 72, ya verificado). Y `ActasService` linea 198 decide *pagada* por **`fpago`**.

⇒ **Para el circuito deuda → acta, lo que SIRGA tiene que escribir bien es `ddjjt`** (`titem1`, `titem2`,
`impban1`, `fpago`), que **ya esta mapeado y verificado**.
⇒ `ddjj` (el detalle) hace falta para la **pantalla de detalle de empleados**, no para la deuda.

### PROXIMO PASO (acotado)

En vez de ingenieria inversa de las 19 columnas de `ddjj`, buscar **el servicio detras de
`DetalleEmpleadosDialog`** (`SIGESWEB.Web\Components\Pages\Actas\DetalleEmpleadosDialog.razor`) y ver
**que campos de `ddjj` lee exactamente**. Esos son los unicos que hay que escribir bien. El resto, si no
lo lee nadie, no se toca.