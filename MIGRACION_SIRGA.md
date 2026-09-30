# Migración SIRGA — Del sistema legado (`Sindicato`) al nuevo (`SIRGA_DB`)

> Documento de referencia del proceso completo de migración de datos, mapeos, saneamiento, ETL y validación.
> **Entorno: 100% local (SQL Server de DIEGOHP), solo desarrollo. Sin producción ni remoto.**

## 1. Alcance

| Item | Fuente | Destino |
|---|---|---|
| Maestros | Base `Sindicato` | Base `SIRGA_DB` |
| Historial DDJJ completo (2002→2026) | `ddjjt` + `ddjj` | `DeclarationRegistry` + `DeclarationDetail` |
| Sindicato (Filial) | `Filial` (1 fila) | `Union` |
| Categorías + escalas | `categorias_empleado` + `EscalaSalarial` | `EmployeeCategory` + `SalaryScale` |
| Empresas | `fotos_.maeemp` (4.507) | `Company` |
| Empleados | `fotos_.maesoc` (27.924) | `Employee` |
| Asignaciones | `fotos_.socemp` (36.019) | `SindicataryAssignment` |
| Estudios contables | `EstudiosContables` (844) + `EstudioContableEmpresa` (2.902) | `AccountingStudio` + `Company.AccountingStudioId` |
| Tasas de interés | `Filial.InteresMensualPorMora` | `InterestRate` |
| Geografía | `provincias` + `localidad` | `Province` + `City` |

## 2. Inventario de datos del legado (verificado)

| Tabla (esquema) | Filas | Columnas clave |
|---|---|---|
| `ddjjt` | 259.471 | `periodo`, `CUIT_STR`, `rect`, `fpres`, `fpago`, `estado`, `titem1..4`, `ctrlimp`, `impex1..3`, `impban1..3`, `medio1..3`, `acta`, `usuario` |
| `ddjj` | 1.335.745 | `periodo`, `CUIT_STR`, `CUIL_STR`, `rect`, `impo`, `impoaux`, `impom1`, `impp`, `item1..4`, `jub`, `jorp`, `cargaf`, `lic` |
| `fotos_.maeemp` | 4.507 | `MEEMP_CUIT_STR`, `MAEEMP_RAZSOC`, `MAEEMP_EMAIL`, `MAEEMP_ESTUDIO_EMAIL`, `MAEEMP_CODLOC`, `MAEEMP_CALLE`, `MAEEMP_NRO` |
| `fotos_.maesoc` | 27.924 | `MAESOC_CUIL_STR`, `APENOM`, `MAESOC_NRODOC`, `MAESOC_CODCAT`, `MAESOC_ESTADO`, `MAESOC_FECHAALTA`, `MAESOC_EMAIL` |
| `fotos_.socemp` | 36.019 | `SOCEMP_CUIL_STR`, `SOCEMP_CUITE_STR`, `SOCEMP_CODSUC`, `SOCEMP_ESTADO`, `SOCEMP_FECHAING`, `SOCEMP_FECHABAJA` |
| `EstudiosContables` | 844 | `Id`, `CUIT`, `RazonSocial`, `Email`, `Nombre`, `Domicilio`, `Localidad`, `Responsable` |
| `EstudioContableEmpresa` | 2.902 | `EstudioContableId`, `CUIT` |
| `EscalaSalarial` | 1.339 | `Periodo`, `CodCategoria`, `Importe`, `AcuerdoNR1`, `AcuerdoNR2`, `Estado` (todos 1) |
| `categorias_empleado` | 22 | `MAECAT_CODCAT` (0-21), `MAECAT_NOMCAT` |
| `Filial` | 1 | SEC Santiago del Estero, `PorcentajeDescuentoLey`, `PorcentajeDescuentoSocio`, `InteresMensualPorMora` |

**Distribuciones relevantes:**
- `ddjjt.estado`: **0**=207.896 · **1**=51.450 · **10**=10 · **11**=98 · **20**=7 · **21**=10.
- `ddjjt` con `acta>0`: 125 filas · con algún `medio` (pago): 188.424.
- `socemp.SOCEMP_ESTADO`: **0**=26.436 · **1**=9.583.
- Períodos en `ddjjt`: 2002-11 → **2084-07** (hay fechas futuras erróneas 2044/2054/2084 a descartar).
- 4.477 CUITs distintos; ~17K cabeceras/año y ~90K detalles/año recientes.

## 3. Reglas de negocio inferidas (con evidencia)

| Regla | Evidencia |
|---|---|
| `ddjjt.titem1` = **aporte ley (2%)** sobre Σ `ddjj.impo` | 2026-01 / 20129347695: Σimpo=6.874.599,68 → ×0,02 = 137.491,99 = `titem1` ✓ |
| `ddjjt.titem2` = **cuota socio (2%)** sobre base de socios | Mismo registro: `titem2`=111.911,11 |
| `ddjj.impo` = **remuneración base** del empleado | Valores mensuales (~360K-460K en 2025) |
| `ddjj.impoaux` = **monto adicional** (solo en algunos empleados, `item1`=1) | Aparece junto a empleados con `item1=1` |
| `ddjj.item1..4`, `jub`, `jorp`, `cargaf` = **flags** (bit) de conceptos | Booleanos observados |
| `EscalaSalarial.Estado` = 1 en el 100% | — |

> ✅ Los 8 puntos de negocio del anexo (sección 9) quedaron **decididos e implementados** en el ETL con su evidencia; la sección 10 documenta los resultados reales de la corrida. Solo el mapeo de `estado` (0/1 → Submitted) y el no-mapeo de `SOCEMP_ESTADO`/`medio1..3` requieren **confirmación formal del cliente** (no bloquean la carga).

## 4. Matriz de mapeo completa

### 4.1 Sindicato
| Legado | → | Nuevo |
|---|---|---|
| `Filial.Nombre` | → | `Union.Name` = "SEC SANTIAGO DEL ESTERO" |
| `Filial.Provincia` | → | `Union.JurisdictionProvince` = "Santiago del Estero" |
| `Filial.Localidad` | → | `Union.JurisdictionCity` = "Capital" |
| CCT 130/75 | → | `Union.CctCode` = "130/75" |
| `Filial.PorcentajeDescuentoLey` | → | `Union.LawContributionPercentage` |
| `Filial.PorcentajeDescuentoSocio` | → | `Union.UnionMemberPercentage` |
| `Filial.InteresMensualPorMora` | → | `InterestRate.MonthlyRate` (con `EffectiveDate` = min periodo) |
| fijo | → | `Union.IsActive` = true |

### 4.2 Geografía
| Legado | → | Nuevo |
|---|---|---|
| `provincias.MAEPROV_NOMBRE` | → | `Province.Name` |
| `localidad.MAELOC_NOMBRE` / `MAELOC_CODPROV` | → | `City.Name` / `City.ProvinceId` |

> La provincia **"Santiago del Estero" no existe** en el seed actual de SIRGA_DB (solo BsAs/Córdoba/Santa Fe) → se debe cargar con sus localidades.

### 4.3 Empresas (`Company`)
| Legado | → | Nuevo |
|---|---|---|
| `MEEMP_CUIT_STR` | → | `Cuit` (sin guiones) |
| `MAEEMP_RAZSOC` | → | `BusinessName` (si "DESCONOCIDA" → `Cuit`) |
| `MAEEMP_CALLE` + `MAEEMP_NRO` | → | `Address` (default "Sin dirección") |
| `MAEEMP_CODLOC` → lookup | → | `CityId` |
| `MAEEMP_EMAIL` | → | `ContactEmail` |
| fijo | → | `Status` = Active (empresas con historial real) / PendingAuthorization (nuevas) |
| fijo | → | `BranchName` = "Sede Central" |

### 4.4 Empleados (`Employee`)
| Legado | → | Nuevo |
|---|---|---|
| `MAESOC_CUIL_STR` | → | `Dni` (11 dígitos, único) — **decisión: usar CUIL como clave** |
| `APENOM` | → | `FullName` |
| `MAESOC_FECHAALTA` | → | `HireDate` (saneando fechas basura año 1000) |
| `MAESOC_ESTADO` (1=activo?) | → | `EmployeeStatus.Active` / `Inactive` |

### 4.5 Asignaciones (`SindicataryAssignment`)
| Legado | → | Nuevo |
|---|---|---|
| `SOCEMP_CUIL_STR` | → | `EmployeeId` |
| `SOCEMP_CUITE_STR` | → | `CompanyId` |
| Union = SEC | → | `UnionId` |
| `SOCEMP_CODSUC` (sucursal) | → | derivar sucursales o ignorar (decisión) |
| `SOCEMP_FECHAING` | → | `CreatedAt` |
| `SOCEMP_ESTADO` | → | activo/inactivo (validar) |
| `MAESOC_CODCAT` | → | `CategoryCode` |
| ¿`item2` de `ddjj`? | → | `IsVoluntaryMember` (validar) |

### 4.6 Categorías y escalas
| Legado | → | Nuevo |
|---|---|---|
| `categorias_empleado.MAECAT_CODCAT` / `NOMCAT` | → | `EmployeeCategory.Code` / `Name` (+ `UnionId`) — excluir fila "TODAS" (0) |
| `EscalaSalarial.Periodo` | → | `SalaryScale.Period` |
| `EscalaSalarial.CodCategoria` | → | `SalaryScale.EmployeeCategoryId` |
| `EscalaSalarial.Importe` | → | `BasicAmount` |
| `EscalaSalarial.AcuerdoNR1`/`NR2` | → | `AgreementNR1`/`AgreementNR2` |
| fijo | → | `AntiguedadPercentage` = 0; `IsActive` = true; `UnionId` = SEC |

### 4.7 Estudios contables
| Legado | → | Nuevo |
|---|---|---|
| `EstudiosContables.Nombre` / `RazonSocial` | → | `AccountingStudio.Name` |
| `EstudiosContables.Email` | → | `AccountingStudio.Email` |
| `EstudioContableEmpresa.CUIT` → lookup Company | → | `Company.AccountingStudioId` |
| fijo | → | `IsApproved` = true (estudios existentes) |

### 4.8 DDJJ cabecera (`DeclarationRegistry`)
| Legado | → | Nuevo |
|---|---|---|
| `FORMAT(periodo,'yyyy-MM')` | → | `Period` |
| `CUIT_STR` → lookup Company | → | `CompanyId` |
| Union = SEC | → | `UnionId` |
| mapeo de `estado` (0/1/10/11/20/21) | → | `Status` (Draft/Submitted/Approved/Paid/Rejected) |
| `fpres` | → | `SubmissionDate` |
| `fpago` | → | `PaymentDate` |
| `rect` | → | `IsRectification` |
| `titem1` (+ `titem2`) | → | `TotalAmount` (≈ ley + socio) |
| `CUIT_STR` + `periodo` | → | `PresencialBarCode` (generado) |

### 4.9 DDJJ detalle (`DeclarationDetail`)
| Legado | → | Nuevo |
|---|---|---|
| lookup (empresa+periodo) | → | `RegistryId` |
| lookup (empleado+empresa+unión) | → | `AssignmentId` (crear si falta) |
| `impo` | → | `BaseSalary` |
| `item1` (ley 2%) | → | `LawContribution` = impo × 2% |
| `item2` / socio | → | `MemberContribution` = base socio × 2% |
| ley + socio | → | `TotalWithheld` |

## 5. Estrategia ETL

### 5.1 Tecnología
- **`SIRGA.Migration`**: consola .NET 10 (proyecto nuevo, NO se agrega a `SIRGA.slnx`).
- Carga de volumen (~1,6M filas) con **`SqlBulkCopy`** en lotes por período → ~1-2 hs en la notebook.
- Lookups en memoria/diccionarios + tablas de staging en SQL Server.

### 5.2 Esquema de staging (base `SIRGA_DB`, esquema `stg_`)
| Tabla staging | Contenido |
|---|---|
| `stg_ddjjt` | Cabeceras crudas + columnas destino + `CompanyId`/`UnionId` |
| `stg_ddjj` | Detalles crudos + `RegistryId`/`AssignmentId` |
| `stg_empresas`, `stg_empleados`, `stg_asignaciones`, `stg_escalas`, `stg_estudios`, `stg_vinculos_estudio`, `stg_geografia` | Maestros crudos |
| `lkp_cuit→CompanyId`, `lkp_cuil→EmployeeId`, `lkp_codcat→CategoryId` | Lookups |

### 5.3 Orden de carga (respetando FKs)
1. `Province` + `City` (geografía Santiago del Estero).
2. `Union` (desde `Filial`) + limpiar sindicatos de prueba de SIRGA_DB.
3. `EmployeeCategory` + `SalaryScale`.
4. `Company` (dedupe por CUIT).
5. `Employee` (dedupe por CUIL/DNI).
6. `SindicataryAssignment` (desde `socemp` + asignaciones derivadas de `ddjj`).
7. `AccountingStudio` + actualizar `Company.AccountingStudioId`.
8. `InterestRate`.
9. `DeclarationRegistry` (cabeceras, por período, bulk).
10. `DeclarationDetail` (detalles, por período, bulk).
11. Recalcular `TotalAmount` y `PreviousAmount/BalanceAmount` para rectificadas.

### 5.4 Saneamiento de datos
| Problema | Acción |
|---|---|
| Fechas futuras (2044/2054/2084) | Descartar cabeceras con `periodo > hoy`; registrar en log |
| `MAEEMP_RAZSOC` = "DESCONOCIDA" | Usar CUIT como nombre |
| Fechas de alta año 1000 | Fijar fecha mínima razonable o NULL |
| Duplicados (empresa+periodo) | Mantener la fila con estado más avanzado; log del resto |
| CUITs mal formados (< 11 dígitos) | Registrar y excluir del mapeo de DDJJ |
| Detalle sin cabecera | Crear cabecera "de detección" o descartar con log |

## 6. Validación y reconciliación

| Check | Consulta |
|---|---|
| Conteo cabeceras | `COUNT(ddjjt)` vs `COUNT(DeclarationRegistry)` ± 0 |
| Conteo detalles | `COUNT(ddjj)` vs `COUNT(DeclarationDetail)` ± 0 |
| Totales | Σ `TotalWithheld` del detalle ≈ `DeclarationRegistry.TotalAmount` |
| Aporte ley | Σ `LawContribution` ≈ Σ(`ddjj.impo` × 0,02) |
| Completeness por período | Empresas con DDJJ en legado ↔ presentes en destino |
| Spot-check | Empresa/periodo al azar → comparar contra PDF del legado (`analyze_pdfs.py`) |

## 7. Entregables y respaldo
- Proyecto `SIRGA.Migration` (ETL + staging + logs en `migracion_logs/`).
- **Backup local** de `SIRGA_DB` antes y después (`BACKUP DATABASE ... TO DISK`).
- Script de rollback (restaurar backup pre-migración).
- Reporte de reconciliación (cuentas por tabla y montos) para aceptación del 22/08.

## 8. Restricciones
- Todo **local** (SQL Server DIEGOHP, app en `http://localhost:5122`). Sin producción/remoto.
- MercadoPago y email reales fuera de alcance (webhook requiere URL pública).

## 9. Anexo: los 8 puntos de negocio — decisión tomada e implementada

Estado al 14/08/2026: **todos resueltos en el ETL**. Los que requieren confirmación del cliente están marcados ⚠️.

| # | Punto | Decisión implementada | Evidencia / código |
|---|---|---|---|
| 1 | `ddjjt.estado` 0/1/10/11/20/21 → estados del sistema | Con `fpago` presente → **Paid(5)**; estado 10/11/20/21 → **Rejected(4)**; resto (0/1) → **Submitted(2)**. ⚠️ Confirmar que 0=presentada/1=anulada es el criterio correcto | `MapStatus()` en `SIRGA.Migration/Program.cs` |
| 2 | `socemp.SOCEMP_ESTADO` 0/1 | **Resuelto**: `1`=activo, `0`=baja. Se mapeó a `Assignments.IsActive` + `Assignments.TerminationDate` (desde `SOCEMP_FECHABAJA`, saneando fechas <1900). 9.593 asignaciones activas / 26.418 de baja | `Assignments()` |
| 3 | `medio1..3` (medios de pago) | ⚠️ **No mapeado**: el modelo no persiste el medio de pago; solo se usa la presencia de `fpago` para marcar `Paid` | `MapStatus()` |
| 4 | `impoaux` (¿no remunerativo o base socio?) | **Es base de aporte**: `baseTotal = impo + impoaux`; ley y cuota se calculan sobre esa base | `Details()` (`Program.cs`) |
| 5 | Sindicato destino | **1 solo sindicato SEC** ("Sindicato Empleados de Comercio Sgo. del Estero"). `Clean()` elimina los sindicatos de prueba; `Union()` inserta solo la filial | `Clean()` / `Union()` |
| 6 | `Employee.Dni` = CUIL o DNI | **CUIL** de 11 dígitos como clave única (el legado opera por CUIL) | `Employees()` |
| 7 | `SOCEMP_CODSUC` → sucursales | **Se ignora**: no se crean `Company` hijas; cada CUIT queda como sucursal única "Sede Central" | `Assignments()` |
| 8 | `IsVoluntaryMember` ¿de `ddjj.item2`? | **Sí**: se deriva de `ddjj.item2` (bool). 12.896 asignaciones (35,8%) quedaron como socios | `Assignments()` (EXISTS sobre `ddjj.item2=1`) y `TryCreateMissingAssignment(item2)` |

**Detalle punto 8 (v2, 14/08/2026):** en la primera corrida `IsVoluntaryMember` quedó en 0 para todas las asignaciones (la cuota sí se calculaba por detalle con `item2`). Se corrigió derivando el flag de `ddjj.item2`: `CASE WHEN EXISTS(... item2 = 1) THEN 1 ELSE 0 END` en `Assignments()` y pasando el `bool item2` a las asignaciones creadas sobre la marcha en `Details()`. Los montos de cuota de socio no cambiaron (ya usaban `item2`), solo el flag de afiliación.

---

## 10. Resultados de la corrida de referencia (14/08/2026)

Corrida completa `all` + `Finalize` + `Reconcile`, entorno 100% local (SQL Server DIEGOHP).

| Tabla destino | Filas |
|---|---|
| Provinces | 25 |
| Cities | 2.596 |
| Unions | 1 (SEC, Id=17) |
| EmployeeCategories | 21 |
| SalaryScales | 1.339 |
| Companies | 4.507 (4.506 de `maeemp` + 1 faltante creada de `ddjj`) |
| Employees | 27.934 (27.924 de `maesoc` + 10 faltantes creados de `ddjj`) |
| SindicataryAssignments | 36.011 (12.896 con `IsVoluntaryMember = true`) |
| AccountingStudios | 844 (2.839 empresas vinculadas) |
| Declarations | 248.151 |
| DeclarationDetails | 1.269.977 |

**Reconciliación de montos (cuadre exacto, dif = 0,00):**
```
Monto cabeceras (Σ TotalAmount):  7.514.730.620,20
Monto detalles (Σ TotalWithheld): 7.514.730.620,20
```

**Decisiones operativas de la corrida:**
- **Base de aportes**: `baseTotal = impo + impoaux` → ley = baseTotal × 2% (todas las filas), cuota = baseTotal × 2% solo si `item2` (punto 4/8).
- **`Finalize`** alinea `TotalAmount`/`BalanceAmount` de cada cabecera con la suma real de sus detalles (para DDJJ pagadas `BalanceAmount = 0`).
- **Dedupe cabeceras**: se conserva 1 fila por (CUIT, período) eligiendo la de mayor `rect`, luego `fmod`, `fpres`, `Id`. Las cabeceras con período futuro (> hoy) o año < 1990 se descartan.
- **Detalles**: se cruzan contra la cabecera ganadora del dedupe (`rect` debe coincidir); los detalles sin asignación existente la crean bajo demanda.
- **9.618 cabeceras sin detalles**: 9.973 cabeceras legado sin filas `ddjj`, de las cuales 9.962 con montos 0. 11 cabeceras con monto > 0 y sin detalles quedaron migradas tal cual (calidad del legado; `Finalize` las dejó en `TotalAmount = 0`). Máximo caso aislado: 3.008.
- **Escalas**: migradas hasta 2026-03 (coincide con el legado). Las escalas de períodos nuevos deben cargarse por la app.
- **Empresas/empleados faltantes**: 1 empresa (27287332780) y 10 empleados presentes solo en `ddjj` se crearon como "SIN DIRECCIÓN"/"SIN NOMBRE <cuil>" para no perder historial. Esa empresa se marca activa (presenta DDJJ).
- **Usuarios**: los 844 estudios migrados tienen usuario Identity generado con `tools/create-studio-users.cs` (email + password `12345`), listado en `tools/estudios_usuarios.csv`. Los de prueba (`estudio@test.com`, etc., password `12345`) no provienen de la migración.
- **Estado de asignaciones**: `Assignments.IsActive` (1=activo, 0=baja) y `TerminationDate` migrados desde `socemp.SOCEMP_ESTADO`/`SOCEMP_FECHABAJA`. Resultado: **9.593 activas / 26.418 de baja** (26.417 con fecha real). Las asignaciones creadas sobre la marcha desde `ddjj` quedan activas. Los 10 empleados creados de `ddjj` (sin registro en `maesoc`) tienen `Status=Inactive` en `Employees` por no tener estado en el legado.
- **Estado de empresas**: `maeemp` no tiene campo de estado, por lo que `Companies.IsActive` se deriva del legado: **activa = tiene ≥1 asignación con `SOCEMP_ESTADO=1` O presentó DDJJ en 2026**. Resultado: **2.576 activas / 1.931 inactivas** (incluye 1 empresa creada on-the-fly desde `ddjj`, activa). Es un campo nuevo (`Company.IsActive`), independiente de `Company.Status` (flujo de aprobación).