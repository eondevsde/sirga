# Especificación — Boleta de pago con código de barras (Banco BSE)

**Alcance:** boleta de pago de DDJJ con código de barras para el cobro en el Banco Santiago del Estero (BSE), **para el Sindicato de Empleados de Comercio de Santiago del Estero (capital)**.

**Estado:** análisis de los instructivos del banco **+ decodificación de una boleta real** del sistema que se reemplaza. **No hay código escrito todavía**: primero se aprueba esta especificación.

---

## 0. Hallazgos de la boleta real (decodificada) — decisiones que se cierran

El usuario aportó una boleta real del sistema que se reemplaza. **Su código de barras se decodificó** y coincide campo por campo con lo impreso, así que varios puntos quedan resueltos **con evidencia**, no por interpretación.

Código de la boleta: `0134407250000027121022325125224000727037625234000733487202` (58 dígitos).

| Campo | Valor decodificado | Dice la boleta impresa |
|-------|--------------------|------------------------|
| Código del ente | **0134** | S.E.C. Santiago del Estero ✓ |
| Formato diseño | 4 | — |
| Datos generales | `0725` + `00000` + `27121022325` | Período **07/2025** + CUIT **27-12102232-5** ✓ |
| Código de moneda | 1 | pesos ✓ |
| 1er vencimiento (aaddd) | `25224` → **12/08/2025** | 12/08/2025 ✓ |
| Importe 1er vencimiento | `0007270376` → **72.703,76** | $ 72.703,76 ✓ |
| 2do vencimiento (aaddd) | `25234` → **22/08/2025** | 22/08/2025 ✓ |
| Importe 2do vencimiento | `0007334872` → **73.348,72** | $ 73.348,72 ✓ |
| Sin uso | 0 | — |
| Dígito verificador | 2 | — |

**1. El formato es el PROPUESTO (58 posiciones).** No es una propuesta: **ya está en producción**. Con 54 posiciones el importe `0007270376` no se podría representar. **Decisión cerrada.**

**2. "Datos generales" (20 posiciones) = período (MMAA) + relleno + CUIT sin guiones.** En el ejemplo: `0725` + `00000` + `27121022325`. Para la boleta nueva (una por DDJJ) va el **período + el CUIT de la empresa**, que es también lo que permite conciliar el cobro con su declaración.

**3. Las fechas julianas `aaddd` quedaron verificadas** contra la boleta real (25224 = día 224 de 2025 = 12/08/2025).

**4. Entre el 1° y el 2° vencimiento hay 10 días** (12/08 → 22/08), y **el segundo lleva recargo**, que la boleta muestra como "Imp. Rec." ($ 644,96 en el ejemplo): importe 2° = importe 1° + recargo.

**5. Dígito verificador — ⚠️ CORREGIDO EL 2026-10-07: LO QUE DECÍA ACÁ ERA FALSO.**

Hasta el 2026-10-07 este punto decía *"RESUELTO y verificado"* y proponía **pesos 1 y 2 desde la derecha,
complemento a 10 y dividido por 2**. **Esa fórmula es imposible y quedó descartada**: termina dividiendo
por 2, así que **sólo puede dar dígitos del 0 al 5**, y las boletas reales del formato de 54 posiciones
tienen **8**. Se había ajustado a **una sola** boleta.

**Estado actual:** `BoletaBse.DigitoVerificador` usa **módulo 10 con pesos 3 y 1 alternados y suma de los
dígitos de cada producto**. Reproduce el dígito de la boleta real de 58 posiciones (suma 188 → DV 2) **y
puede dar los diez dígitos** (probado sobre 2000 códigos).

**PERO SIGUE SIN CONFIRMAR, y con una salvedad importante:** se probaron ~50 fórmulas, subconjuntos de
campos y las dos direcciones de anclaje, y **14 fórmulas distintas (todas sanas) reproducen el dígito de la
única boleta de 58 que tenemos**. No se puede saber cuál usa el banco con una sola muestra. Además, las
boletas del formato de 54 posiciones responden a **otro** algoritmo.

**Por eso la fórmula está declarada en la ficha técnica que se le entrega al banco, pidiendo confirmación.**
Si el banco confirma otra, se cambia **sólo ese método** y se reemiten las boletas.
**El detalle completo y la evidencia están en `CONTEXTO_SIRGA.md` (pendiente 0).**

> **CORRECCIÓN de lo que decía el punto anterior:** las 9 fotos **no fallaron al leerse**. Los códigos de
> 54, 55 y 56 dígitos eran **boletas reales del formato VIEJO de 54 posiciones** (el banco documenta los dos
> formatos), y varias fotos además **cortan el número impreso por el borde**. Eso explica los largos, y es
> lo que permitió descubrir que **el formato cambió a 58 a mediados de 2025** y que los dos formatos usan
> dígitos verificadores distintos. **El proyecto usa sólo el de 58** (decisión del usuario, 2026-10-07).

**6. Datos fijos tomados del ejemplo:** Banco Santiago del Estero – **Sucursal Central**; **Nota de Crédito Cta. Cte. N° 18588639**; encabezado *"Sindicato de Empleados de Comercio de Santiago del Estero"* y *"Aportes Art. 100° Convenio 130/75 | Aportes Cuota Sindical"*.

**7. La boleta es por duplicado:** "Talón para el CLIENTE" y "Talón para el BANCO", con los mismos datos y el mismo código de barras, más el pie *"Pagos posteriores al primer vencimiento, sujetos a actualización"* y un sello de fecha y hora de emisión.

**8. Alcance confirmado por el usuario:** **una boleta por DDJJ, por período, por el total de la declaración.**

**9. El recargo del 2° vencimiento sale de la tasa que ya está en la base** (confirmado por el usuario), y **hay 10 días entre un vencimiento y otro** (confirmado también con la boleta real: 12/08 → 22/08).

---

## 0.b Lo que el sistema YA tiene (y dónde se engancha la boleta)

Revisando el código, hay tres cosas que ya existen y una que hay que corregir:

**a) El sistema ya calcula mora con la tasa de la base** (`DeclarationService`, líneas 171-183): toma `InterestRates`, elige la tasa vigente por `EffectiveDate`, calcula los meses de atraso (mínimo 1) y aplica `saldo × tasa × meses`.

**b) El sistema ya genera un código de barras… pero ficticio** (línea 187): `"001" + CUIT + período + importe` y lo guarda en `declaration.PresencialBarCode`. **Ese es el punto exacto de enganche**: hay que reemplazar ese código inventado por los **58 dígitos reales** del banco. El campo ya existe, así que **no hace falta agregar columnas** para el código.

**c) La tabla de tasas tiene una sola fila**: 3% mensual, fecha 1900, *"Tasa mensual por mora (default SIRGA)"*. O sea que **la escala de AFIP no está cargada**: está el valor por defecto. La estructura la soporta (`EffectiveDate` permite cargar varias), pero hoy sólo hay una.

**d) Dos diferencias a resolver antes de imprimir:**

1. **La fecha del 1° vencimiento no coincide.** El sistema calcula el **día 15 del mes siguiente** (`nextMonth.Day = 15`). La boleta real del sistema anterior tiene, para el período **07/2025**, el **12/08/2025**. Hay que definir cuál vale: el día 15, o el día que usaba el sistema anterior.
2. **El recargo no es mensual, es por 10 días.** Hoy la mora se calcula **por mes entero** (mínimo un mes). La boleta necesita el recargo **prorrateado a los 10 días** entre vencimientos. En la boleta real: recargo **$644,96 sobre $72.703,76 = 0,887% en 10 días**, lo que equivale a ~2,66% mensual. **Ese número es un caso de prueba**: cuando el cálculo esté, tiene que dar 644,96. Si da distinto, la convención de días no es la misma que usaba el sistema anterior.

**e) Datos que la boleta necesita y que ya se pueden derivar sin tocar la base:** 1° vencimiento (`ExpirationDate`), 2° vencimiento (+10 días), importe del 1° (`TotalAmount`) e importe del 2° (`TotalAmount` + recargo).

**Fuentes analizadas:**
- `Estrucuturas.docx` — estructuras de código de barras de los cuatro sindicatos (Santiago del Estero, La Banda, Frías, Añatuya), en versión **actual** y **propuesta**.
- `Información tecnica - Código de barras y archivo de rendición.doc` — instructivo técnico del BSE.

---

## 1. Estructura del código de barras — S.E.C. Santiago del Estero

Código de ente del sindicato: **0134**. Los otros sindicatos tienen el suyo (La Banda 0135, Frías 0136, Añatuya 0137), así que **el código de barras tiene que ser configurable por sindicato, no fijo en el código**.

### Formato ACTUAL (54 posiciones)

| N° | Campo | Desde | Hasta | Long | Detalle |
|----|-------|-------|-------|------|---------|
| 01 | Código del ente | 1 | 4 | 4 | Fijo **0134** |
| 02 | Formato diseño | 5 | 5 | 1 | Constante **4** |
| 03 | Datos generales | 6 | 25 | 20 | Variable (identifica la boleta) |
| 04 | Código de moneda | 26 | 26 | 1 | **1** = pesos |
| 05 | Fecha 1er vencimiento | 27 | 31 | 5 | **aaddd** (juliano) |
| 06 | Importe 1er vencimiento | 32 | 39 | 8 | 6 enteros + 2 decimales |
| 07 | Fecha 2do vencimiento | 40 | 44 | 5 | **aaddd** (juliano) |
| 08 | Importe 2do vencimiento | 45 | 52 | 8 | 6 enteros + 2 decimales |
| 09 | Sin uso | 53 | 53 | 1 | Fijo **0** |
| 10 | Dígito verificador | 54 | 54 | 1 | 1 entero |

### Formato PROPUESTO (58 posiciones)

Igual al actual, con **una sola diferencia**: los **importes pasan de 8 a 10 posiciones** (8 enteros + 2 decimales), y por eso "sin uso" se corre a 57 y el dígito verificador a 58.

| N° | Campo | Desde | Hasta | Long | Detalle |
|----|-------|-------|-------|------|---------|
| 01 | Código del ente | 1 | 4 | 4 | Fijo **0134** |
| 02 | Formato diseño | 5 | 5 | 1 | Constante **4** |
| 03 | Datos generales | 6 | 25 | 20 | Variable |
| 04 | Código de moneda | 26 | 26 | 1 | **1** = pesos |
| 05 | Fecha 1er vencimiento | 27 | 31 | 5 | **aaddd** (juliano) |
| 06 | Importe 1er vencimiento | 32 | 41 | **10** | 8 enteros + 2 decimales |
| 07 | Fecha 2do vencimiento | 42 | 46 | 5 | **aaddd** (juliano) |
| 08 | Importe 2do vencimiento | 47 | 56 | **10** | 8 enteros + 2 decimales |
| 09 | Sin uso | 57 | 57 | 1 | Fijo **0** |
| 10 | Dígito verificador | 58 | 58 | 1 | 1 entero |

### Recomendación técnica: usar el PROPUESTO

El formato actual sólo admite importes hasta **999.999,99**. Una DDJJ de una empresa grande puede superarlo, y en ese caso **la boleta no se puede emitir**: el importe no entra en el código. El propuesto admite hasta **99.999.999,99**. Es la única diferencia entre los dos, y es la que decide.

**Decisión pendiente:** confirmar con el sindicato que se implementa el **propuesto** (y avisarle al banco cuál se usa).

---

## 2. Puntos que el banco exige y que hay que definir nosotros

El instructivo dice que **el ente informa** la estructura, y pide puntualmente:

1. **La fórmula del dígito verificador.** Los instructivos **no la traen**: hay que proponerla y el banco la valida.
   **Propuesta:** módulo 10 sobre los dígitos anteriores al DV, con pesos alternados 1 y 2 de derecha a izquierda, sumando los dígitos de cada producto y tomando el complemento a 10. Es el esquema habitual en códigos de barras bancarios de Argentina. **Se confirma con el banco antes de imprimir.**
2. **Fecha de vencimiento y boletas vencidas.** Hay **dos vencimientos** (1° y 2°, cada uno con su importe), en formato **juliano aaddd** (año + día del año; el instructivo da el ejemplo 05/02/2011 = 11036). Falta definir: **cuántos días** hay entre el 1° y el 2°, **qué recargo** se aplica en el 2°, y **si el banco puede cobrar boletas vencidas**.
3. **Qué va en "Datos generales" (20 posiciones).** Es el campo donde el sindicato identifica la boleta. **Propuesta:** el número de DDJJ, alineado y completado con ceros. Es el dato que después vuelve en el archivo de rendición, así que es el que permite conciliar.

---

## 3. Archivo de rendición (del banco al sindicato)

Después del cobro, el banco le envía al sindicato un archivo con los cobros. El instructivo trae **dos formatos**:

**Formato estándar, 35 posiciones:**

| Ord | Campo | Desde | Hasta | Long | Detalle |
|-----|-------|-------|-------|------|---------|
| 1 | Empresa de servicio | 1 | 4 | 4 | Código de la empresa (0134) |
| 2 | Nro de comprobante o cuenta | 5 | 10 | 6 | Identifica unívocamente al cliente |
| 3 | Primer vencimiento | 11 | 16 | 6 | AAMMDD o juliano AAddd |
| 4 | Monto 1er vencimiento | 17 | 22 | 6 | 4 enteros + 2 decimales |
| 5 | Segundo vencimiento | 23 | 28 | 6 | AAMMDD |
| 6 | Monto 2do vencimiento | 29 | 34 | 6 | 4 enteros + 2 decimales |
| 7 | Dígito verificador | 35 | 35 | 1 | |

**Formato ampliado** (el detalle que acompaña cada cobro): sucursal BSE, nombre de sucursal, moneda, comprobante (hasta 30 posiciones), **tipo de movimiento** (1 = egresos, 2 = recaudación), importe cobrado, fecha de proceso (yyyy/mm/dd), **CUIL del usuario que cobró**, usuario, hora (HHMMSS) y **el código de barras original con el que se cobró**.

**Lo que SIRGA tiene que hacer con esto:** importar el archivo y **conciliar cada cobro con su DDJJ** para marcarla pagada. El proyecto ya tiene un módulo de pagos y conciliación, así que va ahí. El campo **"código de barras original"** es la llave de esa conciliación: por eso el campo "Datos generales" tiene que llevar el número de DDJJ.

---

## 4. Plan de implementación

| Fase | Qué | Requiere decisión |
|------|-----|-------------------|
| 0 | **Esta especificación**: mapa de campos, DV y vencimientos | **Sí** (formato, DV, vencimientos) |
| 1 | **Configuración por sindicato**: código de ente, sucursal, versión de formato y mapa de campos | No |
| 2 | **Generación del código de barras** con dígito verificador y fechas julianas | **Sí**: autorizar la dependencia **ZXing.Net** |
| 3 | **Boleta en PDF** con QuestPDF (ya se usa en el proyecto), con una plantilla por sindicato | No |
| 4 | **Importación del archivo de rendición** y conciliación con las DDJJ | No |
| 5 | **Pruebas con el banco**: mínimo **5 boletas de prueba**, el banco las cobra, envía la rendición y el sindicato la aprueba | Coordinación externa |

**Simbolología del código de barras:** no está en los instructivos. Hay que confirmarla con el banco; el formato (54/58 dígitos numéricos) es compatible con **Interleaved 2 of 5** o **Code 128**.

---

## 5. Camino de aceptación con el banco (según el instructivo)

El banco enumera los pasos: definir la estructura → **imprimir un mínimo de 5 boletas de prueba** → el banco desarrolla la opción en el sistema de Caja → pruebas con esas boletas → **el banco envía al sindicato el archivo de rendición de esas pruebas para su aprobación** → aprobado eso, se pasa al ambiente de testing final.

O sea: **la aprobación del archivo de rendición de las boletas de prueba es el hito que habilita la puesta en producción.**

---

## 6. Decisiones que faltan, para cuando estés

1. **¿Formato actual o propuesto?** Recomiendo el **propuesto** (importes hasta 99.999.999,99).
2. **¿La boleta es una por DDJJ o una por empleado?** Define qué va en "Datos generales" y qué se concilia.
3. **¿Autorizás la dependencia ZXing.Net** para dibujar el código de barras?
4. **Días entre el 1° y el 2° vencimiento, recargo del 2°, y si se aceptan boletas vencidas.**
5. **Confirmar con el banco**: la fórmula del dígito verificador propuesta y la simbolología.

**Aparte:** en tu carpeta hay un `Re Solicitud de formato de código de barras para sistemas de cobro (1).zip`. Por el instructivo, **el ente debe informarle al banco la estructura del código**, así que ese zip parece ser **el formulario de solicitud** que hay que completar y presentar. Queda pendiente abrirlo y ver qué pide exactamente.
