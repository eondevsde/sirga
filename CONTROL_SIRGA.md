# CONTROL DE SIRGA — recorrido completo antes de producción

> **Para qué sirve**: probar SIRGA de punta a punta, en el orden en que se usa de verdad, y anotar qué
> pasa. **No es una lista de lo que falta programar**: casi todo está hecho. Es una lista de lo que hay
> que **ver funcionando**.
> Fecha: 2026-10-20 · App: `http://localhost:5122`

---

## CÓMO SE HACE

**Vos hacés clic. Yo verifico en la base.**

Muchas cosas no se pueden probar desde la pantalla: que el registro se haya guardado bien, que la DDJJ
haya quedado en el estado correcto, que el pago haya impactado donde debe. **Eso lo verifico yo con una
consulta**, y te digo si lo que viste coincide con lo que quedó guardado.

**Para cada punto anotamos**: ✅ funciona · ⚠️ funciona con algo raro · ❌ no funciona.

---

## ETAPA 1 — PREPARACIÓN (rol Admin)

Esto se hace una vez, y sin esto lo demás no funciona.

| # | Pantalla | Qué probar | Qué miro yo en la base |
|---|---|---|---|
| 1.1 | `/unions` | **Crear un sindicato** (ABM completo: alta, edición, desactivar) | Que quede en `Unions` con sus datos y el % de aporte |
| 1.2 | `/admin/salary-scales` | Cargar una **escala salarial** para un período | Que quede en `SalaryScales` con sus categorías |
| 1.3 | `/admin/interest-rates` | Cargar una **tasa de interés** | Que quede en `InterestRates` y que después la use el cálculo |
| 1.4 | `/admin/bancos` | Configurar un **banco** (los datos de la boleta) | Que quede en la config del banco |

⚠️ **Ojo con 1.3**: si no hay tasa para el período de la DDJJ, el cálculo del interés falla. Es la trampa
que ya nos apareció una vez.

---

## ETAPA 2 — ESTUDIOS CONTABLES (roles de estudio y Sindicato)

Acá está tu punto **1 (ABM)** y tu punto **3 (aceptación)**.

| # | Pantalla | Qué probar | Qué miro yo |
|---|---|---|---|
| 2.1 | `/register` | **Registrarse como estudio contable** | Que se cree el usuario **y** el estudio, y que el estudio quede **sin aprobar** |
| 2.2 | `/studio-pending` | Entrar con un estudio sin aprobar | Que lo mande a esa pantalla y **no lo deje operar** |
| 2.3 | `/admin/studios-approval` | **Aprobar** el estudio | Que `AccountingStudios.IsApproved` pase a 1 y quede la fecha |
| 2.4 | `/admin/studios-approval` | **Rechazar** un estudio | Que guarde el motivo y el estudio no pueda operar |
| 2.5 | `/union/studios` | Ver los estudios de un sindicato | Que liste los aprobados de ese sindicato |
| 2.6 | `/admin/subscriptions` | Asignar / ver una **suscripción** | Que quede con su vencimiento |
| 2.7 | `/admin/subscription-requests` | Pedido de suscripción | Que se pueda aprobar |

---

## ETAPA 3 — EMPRESAS Y EMPLEADOS (rol Estudio contable)

| # | Pantalla | Qué probar | Qué miro yo |
|---|---|---|---|
| 3.1 | `/companies` | **Alta de empresa** con su CUIT | Que el CUIT quede **normalizado** y no se pueda repetir |
| 3.2 | `/companies` | Editar, buscar, desactivar | Que la baja **no borre** (desactiva) |
| 3.3 | `/union/company-search` | Buscar una empresa como Sindicato | Que la encuentre y muestre su estado |
| 3.4 | `/empresa/{id}/empleados` | **Alta de empleado** con su DNI y categoría | Que quede en `Employees` **y** la asignación a la empresa separada |
| 3.5 | `/empresa/{id}/empleados` | Editar la **jornada** (completa / parcial) | Que la jornada sea **por asignación**, no por empleado |
| 3.6 | `/employees` | Buscar empleados (Admin/Auditor) | Que respete el rol |

⚠️ **Ojo con 3.4**: un mismo empleado puede estar en **dos empresas** con jornadas distintas. Probá ese
caso: es el que más se rompe.

---

## ETAPA 4 — HACER UNA DDJJ (rol Estudio contable) ← **tu punto 4**

| # | Paso | Qué probar | Qué miro yo |
|---|---|---|---|
| 4.1 | `/declarations/new` | Cargar una DDJJ **a mano** | Que calcule el 2% y la cuota socio |
| 4.2 | `/declarations/new` | Cargar una DDJJ **por Excel** | Que lea el archivo y muestre la previsualización |
| 4.3 | — | **Presentarla** | Que quede en estado **2 (Presentada)** |
| 4.4 | — | **Verificar el volcado al legado** | Que la fila esté en `ddjjt` y los renglones en `ddjj` |
| 4.5 | `/declarations/new` | Intentar **rectificar** | Que la anterior pase a **6 (Rectificada)** y la nueva quede vigente |
| 4.6 | — | Intentar presentar **sin tasa de interés** | Que avise y no rompa |

⚠️ **4.3 y 4.4 son el corazón de SIRGA**: si la DDJJ no se vuelca al legado, SIGES sigue viendo la deuda.
**Ya lo probamos con una DDJJ y funcionó** ✓ — falta probarlo **desde la pantalla**.

---

## ETAPA 5 — EMITIR BOLETA (rol Estudio contable / Sindicato) ← **tu punto 5**

| # | Paso | Qué probar | Qué miro yo |
|---|---|---|---|
| 5.1 | `/declarations/history` | **Emitir la boleta** de la DDJJ presentada | Que genere el PDF y que el código de barras tenga el **DV correcto** |
| 5.2 | `/declarations/history` | Emitir la boleta de un **borrador** | Que **NO** la emita (control nuevo) |
| 5.3 | `/declarations/history` | Emitir la de una **ya pagada** | Que **NO** la emita (control nuevo) |
| 5.4 | `/declarations/history` | Emitir la de una **rectificada** | Que **NO** la emita |
| 5.5 | — | Ver el PDF | Que el logo, el QR y los talones se vean bien |
| 5.6 | `/company/{id}/balance` | Ver el saldo de la empresa | Que la boleta emitida **no** cambie el saldo |

---

## ETAPA 6 — COBRAR: EL `.bse` Y LOS PAGOS

| # | Paso | Qué probar | Qué miro yo |
|---|---|---|---|
| 6.1 | `/conciliacion/banco` | **"Revisar ahora"** con un archivo en la carpeta | Que lo procese y muestre el resultado |
| 6.2 | — | **El asentamiento de verdad** (todavía **no probado** ✗) | Que se cree el pago en SIRGA **y** se marque `fpago`/`impban1` en el legado |
| 6.3 | — | **Reprocesar el mismo archivo** | Que **no duplique** nada (idempotencia) |
| 6.4 | `/admin/payments` | Ver el pago registrado | Que aparezca con su importe y su fecha |
| 6.5 | `/reports/collections` | Reporte de cobranzas | Que sume lo cobrado |

⚠️ **6.2 es lo único de todo este documento que NUNCA se probó escribiendo de verdad.** Es el paso que
más hay que controlar antes de producción. **Se hace con respaldo previo** (ya tenemos las 36 bases
respaldadas ✓) y verificando después.

---

## ETAPA 7 — CONTROL Y AUDITORÍA

| # | Pantalla | Qué probar |
|---|---|---|
| 7.1 | `/audit/dashboard` | Que muestre el panorama |
| 7.2 | `/audit/review` | Aprobar / rechazar una DDJJ — **y ver si la observación se guarda** (hoy puede ignorarse ✗) |
| 7.3 | `/inspectors` · `/union/inspection` | Alta de inspector y visita |
| 7.4 | `/legal-notices` · `/sindicato/renuncias` · `/association-requests` | Los circuitos sindicales |
| 7.5 | `/sindicato/dashboard` · `/admin/dashboard` · `/estudio/dashboard` | Los tres tableros |

---

## LO QUE YA SÉ QUE ESTÁ FLOJO (para no sorprenderse)

| Tema | Estado |
|---|---|
| **`ApproveDeclarationAsync` / `RejectDeclarationAsync` ignoran la observación** | Los parámetros existen pero no se guardan |
| **Borrados duros (hard delete) en algunos ABM** | Contradice la regla "nunca borrar datos" |
| **`PresencialBarCode` guarda un código ficticio** | No corresponde al formato real del banco |
| **Las 3 guardas de la boleta no tienen test** | Si alguien las borra, los tests siguen en verde |
| **El mail al estudio adjunta el comprobante viejo** | Hay que revisar qué documento manda |
| **`Company.TradeName` vacío** | Puede afectar la razón social de la boleta |

---

## REGISTRO DE HALLAZGOS

*(se completa a medida que probamos)*

| Etapa | Qué se probó | Resultado | Observación |
|---|---|---|---|
| 1 | Datos de preparación en la base | ⚠️ | **Están cargados**, pero con 3 cosas a revisar (abajo) |
| 1.2 | Escalas salariales | ✅ | **CORREGIDO**: hay escalas ACTIVAS hasta **10/2026**, 21 categorías por mes. (A las 11:37 del 21/10 mi consulta mostró hasta 03/2026; los datos se cargaron después. Lección: medir de nuevo antes de afirmar que falta algo.) |
| 1.3 | Tasas de interés | ⚠️ | **La última es de 01/07/2025** (2,75% y 3,50% mensual). **15 meses sin actualizar.** ¿La tasa del sindicato es fija o falta cargarla? |
| 1.4 | Banco / La Banda | ❌ | **El sindicato 20 (La Banda) NO tiene config de banco** ⇒ no puede emitir boletas. Su CCT además dice "PENDIENTE". |
| — | Sindicato 19 (Santiago) | ✅ | CCT 130/75, 2% ley + 2% socio, config de banco completa (ente 0134, cliente 18588639, vto día 15, 10 días al 2do) |
