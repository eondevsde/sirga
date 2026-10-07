# Plan: registrar el archivo del banco y actualizar las tablas

> Estado: **propuesta**, sin ejecutar. Todo lo que dice acá abajo está **medido** contra `SIRGA_DB` y la
> copia local de `Sindicato`; lo que no se pudo medir está marcado como **abierto**.
> Última actualización: 2026-10-07.

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

| Tabla nueva | Para qué | Contenido inicial |
|---|---|---|
| `PaymentMethods` | catálogo de formas de pago | Efectivo, Transferencia, Cheque, **Rendición bancaria**, MercadoPago, Débito |
| `Banks` | ya existe (se agregó para la boleta) | BSE |
| `PaymentOrigins` (opcional) | de dónde vino el registro | Manual, Importación del banco, MercadoPago |

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
