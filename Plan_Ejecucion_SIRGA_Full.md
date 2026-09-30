# Blueprint Técnico: Proyecto SIRGA

## Sistema Integral de Recaudación y Gestión de Afiliados

### 1. Perfil del Agente y Objetivo

Actúa como Senior Full Stack Developer. Tu objetivo es implementar **SIRGA** en .NET 10. El sistema permite que Estudios Contables liquiden DDJJ de empresas para diversos Sindicatos, permitiendo el control de aportes de ley (obligatorios) y aportes de socio (voluntarios).

### 2. Stack Tecnológico

- **Framework:** .NET 10.
- **Frontend:** Blazor Web App (Interactive Server) + **MudBlazor**.
- **ORM:** Entity Framework Core (SQL Server 2025).
- **Arquitectura:** Clean Architecture (Domain, Infrastructure, Application, WebUI).

### 3. Modelo de Datos Detallado (Estructura Definida)

Debes implementar las siguientes entidades en `SIRGA.Domain` respetando estas reglas:

#### A. Módulo de Autoridad

- **Sindicato:** `Id`, `Nombre`, `ProvinciaJurisdiccion` (Clave para sucursales), `CodigoCCT`, `EsActivo`.
- **EstudioContable:** `Id`, `Nombre`, `UsuarioResponsable`. (Maneja múltiples empresas).

#### B. Módulo de Afiliación (Core)

- **Empleado:** `Id`, `DNI` (Unique Index), `NombreCompleto`, `FechaAlta`, `FechaBaja`, `EstadoActividad` (Enum: Activo, Licencia, etc), `DatosBiometricos` (byte[] para huella).
- **Empresa:** `Id`, `RazonSocial`, `CUIT`, `ProvinciaSede`, `SucursalNombre`.
- **AsignacionSindicataria (Tabla de Cruce Crítica):**
  - Relaciona: `Empleado` <-> `Empresa` <-> `Sindicato`.
  - Campo: `EsSocioVoluntario` (bool): Define si se descuenta el 2% adicional.

#### C. Módulo Transaccional

- **EscalaSalarial:** `Id`, `Categoria`, `MontoMinimo`, `MontoMaximo`, `VigenciaInicio/Fin`.
- **DDJJ_Registro:** `Id`, `Periodo` (YYYY-MM), `EmpresaId`, `SindicatoId`, `Estado` (Borrador, Pendiente, Aprobado, Pagado), `MontoTotal`, `EsRectificatoria`.
- **Detalle_DDJJ:** `Id`, `RegistroId`, `AsignacionId`, `RemuneracionBase`, `AporteLey` (Calculado), `AporteSocio` (Calculado), `TotalRetenido`.

### 4. Lógica de Negocio Requerida (Application Layer)

1. **Validación Multi-Jurisdiccional:** El sistema debe permitir que una Empresa con sucursales en diferentes provincias liquide al Sindicato que corresponda según la `ProvinciaSede` de la sucursal.
2. **Cálculo Automático:**
   - Aporte Ley: Aplicar % obligatorio sobre `RemuneracionBase`.
   - Aporte Socio: Solo si `EsSocioVoluntario == true`.
3. **Gestión de Rectificativas:** Si se presenta una nueva DDJJ para un periodo ya "Pagado", debe generar un saldo deudor/acreedor comparando el `TotalRetenido` anterior.

### 5. Interfaz de Usuario (WebUI con MudBlazor)

- **Layout:** `MudLayout` con `MudDrawer` (menú) y `MudAppBar`.
- **Portal Estudio Contable:** Dashboard para ver sus empresas clientes y el estado de sus presentaciones.
- **Portal Sindicato:** Grilla para auditar DDJJ recibidas y validar el padrón de afiliados.

### 6. Instrucciones de Inicio para el Agente

1. Lee este archivo por completo.
2. Comienza con la **Fase 1: Creación de Entidades en SIRGA.Domain**.
3. Asegúrate de incluir los Data Annotations y las propiedades de navegación necesarias para EF Core.
4. Genera el código en Inglés pero mantén comentarios y UI en Español.

---

_Nota: Este proyecto se ejecuta en una notebook DIEGOHP (Ryzen 7, 16GB RAM). Optimiza el código para evitar leaks de memoria y usa consultas asíncronas (async/await) en todo momento._
