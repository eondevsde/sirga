# CÁLCULO DEL DÍGITO VERIFICADOR (DV) DE LAS BOLETAS DEL BSE

> **Fuente**: archivo del banco *"Calculo de DV - Boletas de prueba y las que se encuentran en producción.xlsx"*.
> Verificado contra los **cuatro ejemplos** que trae ese archivo.
> ## ✅ **APROBADO POR EL BANCO EL 2026-10-20**
>
> Con esta fórmula se rearmó el paquete de 5 boletas y **el banco aprobó el archivo**.
> La fórmula y la posición del verificador quedan **validadas por el banco**: no son una propuesta.
>
> **Este documento reemplaza cualquier fórmula anterior.** La que estaba implementada en SIRGA era incorrecta
> y es la causa de que el banco rechazara el archivo de prueba.

---

## 1. LA FÓRMULA EN 5 PASOS

```
1. Tomar el código de barras SIN el dígito verificador.
2. Multiplicar cada dígito por su PESO (ver sección 2).
3. Sumar todos los productos.                          -> SUMA
4. Dividir por 2 y quedarse con la PARTE ENTERA.       -> ENTERO = truncar(SUMA / 2)
5. El dígito verificador es ENTERO módulo 10.          -> DV = ENTERO mod 10
```

⚠️ **Los pasos 4 y 5 son los que no son obvios**, y están **escritos por el banco** en su propio archivo:
en la hoja de 54 posiciones anotó al lado del cálculo: *"tomo la parte entera, es decir, 489"* — y 489 es
exactamente **979 ÷ 2 truncado**. No es una interpretación: **está en el archivo**.

---

## 2. LOS PESOS: HAY DOS ESQUEMAS

| Esquema | Pesos | Dónde |
|---|---|---|
| **ACTUALES** | `1, 3, 5, 7, 9, 3, 5, 7, 9, 3, 5, 7, 9…` (la secuencia 1-3-5-7-9 que se repite) | boletas de **54 y 58** posiciones que se emiten hoy |
| **NUEVAS** | `3, 1, 3, 1, 3, 1, 3, 1…` (alternado, empezando por 3) | **las boletas nuevas que pide el banco** |

**Los pesos se repiten a lo largo de todo el código**, desde el primer dígito hasta el último.
Si el código tiene 57 dígitos, se usan 57 pesos.

---

## 3. EJEMPLO COMPLETO — BOLETA ACTUAL DE 58 POSICIONES

Es un caso real del archivo del banco, y sirve para verificar cualquier implementación.

**Código de barras (58 dígitos):**

```
0 1 3 4 4 0 9 2 6 0 0 0 0 0 3 0 7 1 0 6 6 7 3 9 6 1 2 6 2 8 6 0 0 2 5 7 9 8 8 7
1 2 6 2 9 6 0 0 2 6 0 2 7 7 3 3 0 7
```

**Pesos (58):**

```
1 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7
9 3 5 7 9 3 5 7 9 3 5 7 9 3 5 7 9 3
```

**Productos** (dígito × peso), los primeros y los últimos:

```
0×1=0   1×3=3   3×5=15  4×7=28  4×9=36  0×3=0   9×5=45  2×7=14  6×9=54 …
… 3×3=9  0×5=0   0×7=0   0×9=0   2×3=6   5×5=25  7×7=49  9×9=81  8×3=24 …
… 2×5=10 6×7=42  0×9=0   2×3=6   7×5=35  7×7=49  3×9=27  3×3=9   0×5=0
```

**Cálculo:**

| Paso | Valor |
|---|---|
| **SUMA** de los 57 productos | **1254** |
| ÷ 2 | 627 |
| **Parte entera** | **627** |
| **DV = 627 mod 10** | **7** ✓ |

**El último dígito del código de barras es 7** ⇒ **coincide** ✓

> ⚠️ **Detalle importante**: la suma usa `SUM(A3:BE3)`, o sea **los primeros 57 dígitos** — **el DV queda
> afuera de su propio cálculo**, que es lo correcto. El DV ocupa la **posición 58**.

---

## 4. LOS OTROS TRES EJEMPLOS (para verificar la implementación)

| Ejemplo | Pesos | SUMA | ÷ 2 | Entero | **DV** |
|---|---|---|---|---|---|
| **BOLETAS ACTUALES 54** | 1,3,5,7,9… | 979 | 489,5 | **489** | **9** |
| **BOLETAS ACTUALES 58** | 1,3,5,7,9… | 1254 | 627 | **627** | **7** |
| **BOLETAS NUEVAS** (ej. 1) | 3,1,3,1… | 375 | 187,5 | **187** | **7** |
| **BOLETAS NUEVAS** (ej. 2) | 3,1,3,1… | 402 | 201 | **201** | **1** |

---

## 5. IMPLEMENTACIÓN (C#)

```csharp
/// <summary>
/// Dígito verificador de la boleta del BSE.
/// </summary>
/// <param name="codigoSinDv">El código de barras SIN el dígito verificador.</param>
/// <param name="esquemaNuevo">true = pesos 3,1,3,1 (boletas nuevas); false = 1,3,5,7,9 (actuales).</param>
public static int CalcularDigitoVerificador(string codigoSinDv, bool esquemaNuevo)
{
    // Pesos de las boletas ACTUALES: la secuencia 1-3-5-7-9 que se repite.
    int[] pesosActuales = { 1, 3, 5, 7, 9 };

    int suma = 0;
    for (int i = 0; i < codigoSinDv.Length; i++)
    {
        int digito = codigoSinDv[i] - '0';
        int peso = esquemaNuevo
            ? (i % 2 == 0 ? 3 : 1)
            : pesosActuales[i % pesosActuales.Length];

        suma += digito * peso;
    }

    // DIVISIÓN ENTERA: trunca, NO redondea. 187,5 tiene que dar 187.
    int entero = suma / 2;

    return entero % 10;
}
```

### ⚠️ Las tres trampas al implementarlo

1. **La división tiene que TRUNCAR, no redondear.** `979 / 2 = 489,5` → **489** ✓ (no 490 ✗).
   Por eso el código usa `int / int`, que en C# **trunca**. Si se usara `decimal` y `Math.Round`, da mal.
2. **Los pesos se repiten a lo largo de TODO el código**, no se reinician ni se cortan.
3. **El DV no entra en su propia suma.** Se calcula sobre el código **sin** el DV (por eso el método recibe
   `codigoSinDv`).

---

## 6. LO QUE HAY QUE CONFIRMARLE AL BANCO

En el archivo hay una **diferencia entre las dos hojas**, y conviene aclararla **antes** de programar la
boleta nueva:

| Hoja | La suma abarca | ¿Incluye el último dígito? |
|---|---|---|
| **ACTUALES** | `SUM(A3:BE3)` — 57 dígitos | **NO** ✓ (correcto) |
| **NUEVAS** | `SUM(A3:BF3)` — 58 dígitos | **SÍ** ✗ |

⇒ **Pregunta concreta**: *en las boletas nuevas, ¿la última posición es el dígito verificador —y por lo
tanto queda fuera de la suma— o es un dato, y el DV va en otra posición?*

**Con los dos ejemplos de la hoja "BOLETAS NUEVAS" debería alcanzar para que el banco lo confirme.**

---

## 7. LO QUE ESTABA MAL ANTES (para que no se repita)

- **La implementación anterior de SIRGA usaba otra fórmula** (módulo 10 con pesos 3-1 sin el paso de la
  división por 2, entre otras variantes probadas).
- **Prueba de que estaba mal**: con **10 boletas reales de Santiago** (formato de 58), sólo **3 de 10**
  dígitos verificadores coincidían — y eso **descarta** una fórmula, no la confirma.
- **Por eso el banco rechazó el archivo de prueba**, y tenía razón.

⚠️ **No "optimizar" ni reinterpretar esta fórmula.** Está verificada contra los cuatro ejemplos del propio
banco. Si algún cálculo no cierra, **el error está en los datos de entrada, no en la fórmula.**

---

## 8. PENDIENTE INMEDIATO

1. Implementar `CalcularDigitoVerificador` en `BoletaBse`, con el **esquema nuevo** (pesos 3,1).
2. **Rearmar el paquete de prueba** (las 5 boletas) con el DV corregido.
3. **Reenviarlo al banco**, aclarando la consulta de la sección 6.
