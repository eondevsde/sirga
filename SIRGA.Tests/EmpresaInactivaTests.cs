using SIRGA.Application.Services;
using SIRGA.Domain.Entities;
using SIRGA.Domain.Enums;
using Xunit;

namespace SIRGA.Tests;

/// <summary>
/// Una empresa inactiva no puede presentar DDJJ. La regla vivía sólo en la pantalla (y mal: el chip miraba
/// <c>IsActive</c> y el botón miraba <c>Status</c>), así que una empresa marcada Inactiva se podía declarar
/// igual. Ahora está en el servicio y esto es lo que impide que se pierda.
/// </summary>
public class EmpresaInactivaTests
{
  private static Company Empresa(bool activa, CompanyStatus status) => new()
  {
    Id = 1,
    BusinessName = "EMPRESA DE PRUEBA S.R.L.",
    Cuit = "30716310015",
    IsActive = activa,
    Status = status
  };

  [Fact]
  public void EmpresaActiva_NoLanza()
  {
    var excepcion = Record.Exception(() =>
        DeclarationService.ValidarEmpresaActiva(Empresa(activa: true, CompanyStatus.Active)));

    Assert.Null(excepcion);
  }

  [Fact]
  public void EmpresaInactiva_LanzaConElMotivo()
  {
    var excepcion = Assert.Throws<Exception>(() =>
        DeclarationService.ValidarEmpresaActiva(Empresa(activa: false, CompanyStatus.Active)));

    Assert.Contains("inactiva", excepcion.Message);
    Assert.Contains("EMPRESA DE PRUEBA S.R.L.", excepcion.Message);
  }

  [Fact]
  public void EmpresaBajaPorEstado_LanzaAunqueIsActiveEsteEnTrue()
  {
    // Las dos banderas pueden estar desincronizadas: alcanza con que una diga que no está activa.
    foreach (var estado in new[] { CompanyStatus.Inactive, CompanyStatus.PendingAuthorization, CompanyStatus.Suspended })
    {
      Assert.Throws<Exception>(() =>
          DeclarationService.ValidarEmpresaActiva(Empresa(activa: true, estado)));
    }
  }
}
