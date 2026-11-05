# DEMO — Guion para la presentación

> **Mañana 8:00.** Todo se levanta con un comando y las bases de prueba ya están cargadas y consistentes.

---

## Antes de empezar (5 minutos)

### 1. Levantar todo

```powershell
cd C:\Proyectos\SIRGA
.\tools\levantar-demo.ps1 -ConSigesweb
```

Tarda ~1 minuto y verifica solo. Al final tiene que decir:

```
  raíz .......... HTTP 200
  estilos ....... HTTP 200
  login ......... HTTP 200
  A qué base está conectada:
    SIRGA_DB_DEMO
  DEMO LISTA: http://localhost:5200
```

Si dice **`SIRGA_DB`** (sin `_DEMO`) en lugar de `SIRGA_DB_DEMO`, **no presentar**: la demo estaría
usando la base real. Correr de nuevo el lanzador.

### 2. Abrir el navegador

| Aplicación | URL | Usuario | Clave |
|---|---|---|---|
| **SIRGA** (el estudio) | http://localhost:5200 | `admin@sirga.com` | `12345` |
| **SIGESWEB** (el sindicato) | http://localhost:5300 | el de siempre | el de siempre |

Para mostrar un usuario de estudio: **cualquier email de estudio + `12345`**
(por ejemplo `cpngmc@gmail.com`).

### 3. El seguro (si algo se ensucia)

```powershell
.\tools\restaurar-demo.ps1
```

Vuelve las bases demo al estado de hoy en ~2 minutos y levanta todo.

---

## Recorrido sugerido (15 minutos)

| # | Pantalla | Qué decir |
|---|---|---|
| **1** | **Empresas** (`/companies`) | "Están las 4.543 empresas del sindicato. Se busca por CUIT, por razón social o por ciudad." Probar una búsqueda. |
| **2** | **Menú `⋮` de una empresa → Ver empleados** | "Cada empresa con sus empleados: CUIL, categoría con nombre, jornada, ingreso y si es socio." |
| **3** | **Ficha de un empleado** (ícono de credencial) | "Todos los datos: actividad, legajo, obra social, jubilado, carga de familia." Bajar y mostrar **Historial de cambios**. |
| **4** | **Editar ese empleado** | Cambiar el **celular** y guardar. "Esto se sincroniza en el acto con el padrón del sindicato." |
| **5** | **Nueva DDJJ** (`/declarations/new`) | Elegir una empresa con pocos empleados. "El sistema avisa qué períodos **no** tienen DDJJ." |
| **6** | **Paso 2 de la DDJJ** | "El mínimo de escala se calcula por categoría, antigüedad y jornada." Cargar **`1758785.23`** con **punto** y mostrar que lo toma bien. "La diferencia se ve en verde si paga por encima y en rojo si está debajo." |
| **7** | **Presentar** | "Al presentar, la DDJJ se guarda acá y **se vuelca al sistema del sindicato** en la misma operación." |
| **8** | **Historial de DDJJ** → **Ver detalle** | "Queda congelado qué mínimo regía y qué cargó el estudio, con la diferencia. Sirve para auditar." |
| **9** | **SIGESWEB** (opcional) | "El sindicato ve la misma DDJJ del otro lado, porque las dos aplicaciones comparten los datos." |

> **Datos para el paso 5-6**: una empresa con 2 o 3 empleados tarda menos y no distrae.
> Si querés que el sistema muestre el aviso de **"períodos sin DDJJ"**, elegí una empresa a la que le
> falten meses (por ejemplo, cualquiera con datos hasta septiembre y mirar octubre/noviembre).

---

## Lo que NO conviene mostrar

| Tema | Por qué |
|---|---|
| **Link de pago / MercadoPago** | Sin la credencial de producción da error 403 |
| **Envío de mails** | No hay SMTP configurado en este entorno |
| **Escala de noviembre** | No está cargada (la paritaria sale después); el sistema usa la de octubre |
| **Datos "reales"** | La demo es una **copia**: no cargar datos que después haya que pasar a producción |

Si preguntan por esos puntos: *"la integración está hecha, falta cargar la credencial / el servidor de
correo del sindicato"*.

---

## Si algo falla en vivo

| Síntoma | Qué hacer |
|---|---|
| La pantalla se ve **sin estilos** (gigante) | Faltó publicar bien: correr `.\tools\levantar-demo.ps1` de nuevo |
| **No entra** con `admin@sirga.com` / `12345` | Verificar que la URL sea **5200** (la demo) y no 5122 (desarrollo) |
| La app **no responde** | `.\tools\levantar-demo.ps1` (el lanzador baja y levanta solo) |
| Datos **raros** o de prueba | `.\tools\restaurar-demo.ps1` (vuelve todo al estado de hoy) |
| Todo falla | Presentar con la app de desarrollo (`http://localhost:5122`), que tiene la base real y también funciona. **Último recurso**: cambia el mensaje, no los datos que se muestran |

---

## Estado de la demo (verificado hoy)

| | |
|---|---|
| Empresas | 4.543 |
| Empleados | 28.160 |
| Asignaciones | 36.352 |
| Declaraciones | 252.165 (3.902 marcadas históricas) |
| Renglones | 1.293.502 |
| Estudios contables | 876 (12 bloqueados) |
| Usuarios | 910 (905 con rol de estudio, clave `12345`) |
| Consistencia con el sindicato | **CONSISTENTE** (verificador sin diferencias) |
| Respaldos | `C:\Proyectos\_RESPALDOS\demo-2026-11-05\` |
