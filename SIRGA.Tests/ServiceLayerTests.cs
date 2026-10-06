using System.Linq.Expressions;
using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Tests de la capa de servicios tocada al cerrar los endpoints anónimos: las lecturas de
/// empresas dejaron de pasar por HTTP y ahora las resuelve CompanyService, así que interesa fijar
/// el filtrado por estudio y el orden.
/// Los mocks aplican el predicado que reciben (no devuelven siempre la misma lista), para que el
/// test verifique de verdad el filtro y no sólo que se llamó.
/// </summary>
public class ServiceLayerTests
{
  private static (CompanyService Service, Mock<IRepository<Company>> Companies, Mock<IRepository<AccountingStudio>> Studios)
      BuildCompanyService(IEnumerable<Company> companies, IEnumerable<AccountingStudio> studios)
  {
    var companiesList = companies.ToList();
    var studiosList = studios.ToList();

    var companiesRepo = new Mock<IRepository<Company>>();
    companiesRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(companiesList);
    companiesRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Company, bool>>>()))
        .ReturnsAsync((Expression<Func<Company, bool>> p) => companiesList.Where(p.Compile()).ToList());

    var studiosRepo = new Mock<IRepository<AccountingStudio>>();
    studiosRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountingStudio, bool>>>()))
        .ReturnsAsync((Expression<Func<AccountingStudio, bool>> p) => studiosList.Where(p.Compile()).ToList());

    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork.SetupGet(u => u.Companies).Returns(companiesRepo.Object);
    unitOfWork.SetupGet(u => u.AccountingStudios).Returns(studiosRepo.Object);

    // CompanyService ahora también recibe las validaciones y las solicitudes de vinculación,
    // porque el alta y la edición se movieron del controlador al servicio.
    var validation = new CompanyValidationService(unitOfWork.Object, new Mock<ILegacyCompanyLookup>().Object);
    var associations = new AssociationService(unitOfWork.Object);

    return (new CompanyService(unitOfWork.Object, validation, associations), companiesRepo, studiosRepo);
  }

  [Fact]
  public async Task GetAllOrderedAsync_OrdendaPorRazonSocial_SinImportarMayusculas()
  {
    var (service, _, _) = BuildCompanyService(
        new[]
        {
          new Company { Id = 1, BusinessName = "zeta S.A." },
          new Company { Id = 2, BusinessName = "Alfa SRL" },
          new Company { Id = 3, BusinessName = "beta" }
        },
        Array.Empty<AccountingStudio>());

    var resultado = (await service.GetAllOrderedAsync()).ToList();

    Assert.Equal(new[] { "Alfa SRL", "beta", "zeta S.A." }, resultado.Select(c => c.BusinessName));
  }

  [Fact]
  public async Task GetByStudioUsernameAsync_SinEstudioAsociado_DevuelveVacio()
  {
    var (service, _, _) = BuildCompanyService(
        new[] { new Company { Id = 1, BusinessName = "Alfa", AccountingStudioId = 7 } },
        new[] { new AccountingStudio { Id = 7, ResponsibleUsername = "otro@test.com" } });

    var resultado = await service.GetByStudioUsernameAsync("nadie@test.com");

    Assert.Empty(resultado);
  }

  [Fact]
  public async Task GetByStudioUsernameAsync_DevuelveSoloLasEmpresasDeEseEstudio()
  {
    var (service, _, _) = BuildCompanyService(
        new[]
        {
          new Company { Id = 1, BusinessName = "Alfa", AccountingStudioId = 7 },
          new Company { Id = 2, BusinessName = "Beta", AccountingStudioId = 99 },
          new Company { Id = 3, BusinessName = "Gamma", AccountingStudioId = null }
        },
        new[]
        {
          new AccountingStudio { Id = 7, ResponsibleUsername = "estudio@test.com" },
          new AccountingStudio { Id = 99, ResponsibleUsername = "ajeno@test.com" }
        });

    var resultado = (await service.GetByStudioUsernameAsync("estudio@test.com")).ToList();

    Assert.Single(resultado);
    Assert.Equal("Alfa", resultado[0].BusinessName);
  }

  [Fact]
  public async Task LookupAsync_CuitVacio_NoConsultaEmpresasNiLegado()
  {
    var unitOfWork = new Mock<IUnitOfWork>();
    var legacy = new Mock<ILegacyCompanyLookup>();
    var service = new CompanyValidationService(unitOfWork.Object, legacy.Object);

    // Ojo: un CUIT corto como "123" se normaliza a "00000000123" (11 dígitos) y por lo tanto SI
    // se consulta; el camino corto es el CUIT vacío, que no llega a tener 11 dígitos.
    var resultado = await service.LookupAsync("", studioId: 5);

    Assert.Equal(string.Empty, resultado.NormalizedCuit);
    Assert.False(resultado.CuitExists);
    Assert.False(resultado.LegacyFound);
    unitOfWork.VerifyGet(u => u.Companies, Times.Never);
    legacy.Verify(l => l.FindAsync(It.IsAny<string>()), Times.Never);
  }

  [Fact]
  public async Task EnsureUnionUserAsync_SinEmail_DevuelveErrorSinTocarIdentity()
  {
    // El manager se pasa nulo a proposito: el metodo tiene que salir antes de usarlo.
    var service = new UnionSubscriptionService(new Mock<IUnitOfWork>().Object, null!, null!);

    var (creado, yaExistia, error) = await service.EnsureUnionUserAsync("   ");

    Assert.False(creado);
    Assert.False(yaExistia);
    Assert.NotNull(error);
  }

  [Fact]
  public async Task InterestRateService_CreateAsync_PersisteYPideGuardar()
  {
    var repo = new Mock<IRepository<InterestRate>>();
    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork.SetupGet(u => u.InterestRates).Returns(repo.Object);

    var service = new InterestRateService(unitOfWork.Object);
    var tasa = new InterestRate { Id = 0, MonthlyRate = 3.5m, EffectiveDate = new DateTime(2026, 1, 1) };

    var creada = await service.CreateAsync(tasa);

    Assert.Same(tasa, creada);
    repo.Verify(r => r.AddAsync(tasa), Times.Once);
    unitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
  }

  /// <summary>
  /// La escala de tasas NO se borra: una tasa que no corresponde se desactiva. Este test fija esa regla:
  /// con un id inexistente no toca nada y devuelve el motivo.
  /// </summary>
  [Fact]
  public async Task InterestRateService_SetActive_Inexistente_NoCambiaNadaYDevuelveElMotivo()
  {
    var repo = new Mock<IRepository<InterestRate>>();
    repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((InterestRate?)null);
    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork.SetupGet(u => u.InterestRates).Returns(repo.Object);

    var service = new InterestRateService(unitOfWork.Object);

    var (ok, error) = await service.SetActiveAsync(404, activa: false);

    Assert.False(ok);
    Assert.Equal("La tasa no existe.", error);
    repo.Verify(r => r.UpdateAsync(It.IsAny<InterestRate>()), Times.Never);
    repo.Verify(r => r.RemoveAsync(It.IsAny<InterestRate>()), Times.Never);
    unitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
  }

  [Fact]
  public async Task InterestRateService_SetActive_DesactivaSinBorrar()
  {
    var tasa = new InterestRate { Id = 7, MonthlyRate = 0.0275m, IsActive = true };
    var repo = new Mock<IRepository<InterestRate>>();
    repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(tasa);
    repo.Setup(r => r.UpdateAsync(tasa)).Returns(Task.CompletedTask);
    var unitOfWork = new Mock<IUnitOfWork>();
    unitOfWork.SetupGet(u => u.InterestRates).Returns(repo.Object);
    unitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    var service = new InterestRateService(unitOfWork.Object);

    var (ok, error) = await service.SetActiveAsync(7, activa: false);

    Assert.True(ok);
    Assert.Null(error);
    Assert.False(tasa.IsActive);
    // Lo importante: se actualiza, NO se borra.
    repo.Verify(r => r.UpdateAsync(tasa), Times.Once);
    repo.Verify(r => r.RemoveAsync(It.IsAny<InterestRate>()), Times.Never);
  }
}
