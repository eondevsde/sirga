# 📋 INSTRUCTIVO: Configurar MudBlazor en Blazor Server (.NET 8)

## 🏗️ 1. Estructura de la Solución

```
MiProyecto/
├── MiProyecto.Web/                  ← Proyecto Blazor Server
│   ├── Components/
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor     ← Layout PRINCIPAL
│   │   │   └── NavMenu.razor
│   │   ├── Pages/
│   │   │   ├── Home.razor
│   │   │   └── ...
│   │   ├── _Imports.razor
│   │   ├── App.razor
│   │   └── Routes.razor
│   ├── Program.cs
│   ├── MiProyecto.Web.csproj
│   └── appsettings.json
```

---

## 📦 2. Instalar NuGet Package

En `MiProyecto.Web.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="MudBlazor" Version="7.*" />
</ItemGroup>
```

O desde terminal:
```bash
dotnet add package MudBlazor
```

---

## ⚙️ 3. Program.cs - Registrar MudBlazor

```csharp
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Registrar MudBlazor (OBLIGATORIO)
builder.Services.AddMudServices();

// Blazor Server interactivo
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// ... resto del código ...

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

---

## 🎨 4. _Imports.razor - Importar MudBlazor

En `Components/_Imports.razor` agregar:

```razor
@using MudBlazor
```

---

## 🏠 5. MainLayout.razor - LOS 4 COMPONENTES OBLIGATORIOS

```razor
@inherits LayoutComponentBase

<MudThemeProvider />                           ← 1. SIEMPRE primero
<MudPopoverProvider />                         ← 2. Para DatePicker, Menús, Tooltips
<MudDialogProvider />                          ← 3. Para diálogos modales
<MudSnackbarProvider />                        ← 4. Para notificaciones tipo toast

<MudLayout>
    <MudAppBar>
        <!-- Tu AppBar aquí -->
    </MudAppBar>

    <MudDrawer>
        <NavMenu />
    </MudDrawer>

    <MudMainContent>
        <MudContainer>
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>
```

**⚠️ REGLA DE ORO:** Los 4 componentes deben estar en el orden indicado y **ANTES** del `<MudLayout>`.

---

## 📄 6. App.razor - Sin cambios especiales

```razor
<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <base href="/" />
    <HeadOutlet />
    <link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css" />
    <link rel="stylesheet" href="app.css" />
</head>
<body>
    <Routes />
    <script src="_framework/blazor.web.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
</body>
</html>
```

---

## 🛣️ 7. Routes.razor - Sin render mode global

```razor
<Router AppAssembly="@typeof(Program).Assembly">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Página no encontrada</p>
        </LayoutView>
    </NotFound>
</Router>
```

---

## 🖥️ 8. Página con interactividad (DatePickers, Botones, etc.)

✅ **FORMA CORRECTA (usando prerender: false):**

```razor
@page "/mi-pagina"
@rendermode @(new InteractiveServerRenderMode(prerender: false))
@inject ISnackbar Snackbar

<PageTitle>Mi Página</PageTitle>

<MudContainer>
    <MudDatePicker @bind-Date="_fecha" Label="Seleccionar fecha" Variant="Variant.Outlined" />
    
    <MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Saludar">
        Haz clic
    </MudButton>
</MudContainer>

@code {
    private DateTime? _fecha = DateTime.Today;

    private void Saludar()
    {
        Snackbar.Add("¡Hola! La fecha es: " + _fecha?.ToString("dd/MM/yyyy"), Severity.Success);
    }
}
```

---

## ✅ 9. Checklist de verificación

| # | Componente | ¿Lo tienes? |
|---|---|---|
| 1 | `MudThemeProvider` en Layout | ☐ |
| 2 | `MudPopoverProvider` en Layout | ☐ |
| 3 | `MudDialogProvider` en Layout | ☐ |
| 4 | `MudSnackbarProvider` en Layout | ☐ |
| 5 | `builder.Services.AddMudServices()` en Program.cs | ☐ |
| 6 | `@using MudBlazor` en _Imports.razor | ☐ |
| 7 | `<script src="_content/MudBlazor/MudBlazor.min.js">` en App.razor | ☐ |
| 8 | `<link rel="stylesheet" href="_content/MudBlazor/MudBlazor.min.css">` en App.razor | ☐ |
| 9 | Páginas interactivas con `prerender: false` | ☐ |

---

## ❌ Errores comunes y soluciones

| Error | Causa | Solución |
|---|---|---|
| `Missing <MudPopoverProvider />` | Falta `MudPopoverProvider` en el Layout O el circuito se cayó por otro error | Agregar en Layout y revisar consola del servidor |
| `Cannot access a disposed object` | El circuito Blazor Server se cayó | Buscar la excepción real en los logs del servidor |
| `The connection is closed` | Error de BD o timeout | Revisar connection string o consultas SQL |
| DatePicker no abre al hacer clic | Circuito desconectado o prerender activo | Usar `prerender: false` |
