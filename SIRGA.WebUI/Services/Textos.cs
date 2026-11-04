using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;

namespace SIRGA.WebUI.Services;

/// <summary>
/// Textos en ESPAÑOL para los estados que se muestran en pantalla.
///
/// **REGLA DEL PROYECTO: TODO EL FRONT VA EN ESPAÑOL.** Varias pantallas mostraban el nombre del enum
/// crudo (`@context.Status`), así que el usuario veía **"Paid", "Submitted", "Draft", "Pending"** en la
/// grilla. Los estados ahora se traducen siempre con estas funciones, y la comparación interna se sigue
/// haciendo contra el valor original (nunca contra el texto traducido).
/// </summary>
public static class Textos
{
  /// <summary>Estado de una DDJJ. Recibe el nombre del enum tal como viene en el DTO ("Paid", etc.).</summary>
  public static string EstadoDeclaracion(string? estado) => estado switch
  {
    "Draft" => "Borrador",
    "Submitted" => "Presentada",
    "Approved" => "Aprobada",
    "Rejected" => "Rechazada",
    "Paid" => "Pagada",
    "Rectificada" => "Rectificada",
    null or "" => "Sin estado",
    // Un estado nuevo que todavía no está acá: se muestra tal cual en lugar de vacío.
    _ => estado!
  };

  /// <summary>Estado de una empresa.</summary>
  public static string EstadoEmpresa(CompanyStatus estado) => estado switch
  {
    CompanyStatus.PendingAuthorization => "Pendiente de autorización",
    CompanyStatus.Active => "Activa",
    CompanyStatus.Suspended => "Suspendida",
    CompanyStatus.ChangeStudioRequested => "Cambio de estudio pedido",
    CompanyStatus.Inactive => "Inactiva",
    CompanyStatus.Rejected => "Rechazada",
    _ => estado.ToString()
  };

  /// <summary>Estado de una empresa. Recibe el nombre del enum tal como viene en el DTO ("Active", etc.).</summary>
  public static string EstadoEmpresa(string? estado) => estado switch
  {
    "PendingAuthorization" => "Pendiente de autorización",
    "Active" => "Activa",
    "Suspended" => "Suspendida",
    "ChangeStudioRequested" => "Cambio de estudio pedido",
    "Inactive" => "Inactiva",
    "Rejected" => "Rechazada",
    null or "" => "Sin estado",
    _ => estado!
  };

  /// <summary>Estado de un acta de inspección.</summary>
  public static string EstadoActa(InspectionActStatus estado) => estado switch
  {
    InspectionActStatus.Pending => "Pendiente",
    InspectionActStatus.Notified => "Notificada",
    InspectionActStatus.InCovenant => "En convenio de pago",
    InspectionActStatus.Paid => "Pagada",
    InspectionActStatus.Cancelled => "Anulada",
    InspectionActStatus.Legal => "En instancia legal",
    _ => estado.ToString()
  };

  /// <summary>Estado de una solicitud de asociación de empresa al sindicato.</summary>
  public static string EstadoSolicitud(RequestStatus estado) => estado switch
  {
    RequestStatus.Pending => "Pendiente",
    RequestStatus.Approved => "Aprobada",
    RequestStatus.Rejected => "Rechazada",
    _ => estado.ToString()
  };

  /// <summary>Estado de un empleado.</summary>
  public static string EstadoEmpleado(EmployeeStatus estado) => estado switch
  {
    EmployeeStatus.Active => "Activo",
    EmployeeStatus.LeaveOfAbsence => "Con licencia",
    EmployeeStatus.MaternityLeave => "Licencia por maternidad",
    EmployeeStatus.AccidentLeave => "Licencia por accidente",
    EmployeeStatus.Inactive => "Inactivo",
    _ => estado.ToString()
  };

  /// <summary>
  /// Tipo de documento. Los códigos vienen del sistema anterior (<c>MAESOC_TIPODOC</c>), que no trae
  /// tabla de descripción: los más usados son 3 = DNI (27.910 de 27.924 filas), 2 = LE, 1 = LC y
  /// 4 = CI. Si aparece un código desconocido se muestra el código, nunca una etiqueta inventada.
  /// </summary>
  public static string TipoDocumento(string? codigo) => codigo?.Trim() switch
  {
    "3" => "DNI",
    "2" => "LE",
    "1" => "LC",
    "4" => "CI",
    null or "" => "Documento",
    _ => $"Documento (cód. {codigo})"
  };
}
