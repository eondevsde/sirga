using System.Linq.Expressions;
using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Traspaso de una empresa de un estudio a otro (A -> B), que decide y ejecuta el admin del sindicato.
/// Se fija lo que no puede fallar en el negocio: que cambie el dueño, que se cierre la fila vigente
/// del historial del estudio anterior, que se abra la del nuevo anotando quién y por qué sindicato lo
/// autorizó, y que no se permita un traspaso inválido.
/// </summary>
public class CompanyTransferTests
{
  private static (AssociationService Service, Company Empresa, CompanyStudioHistory FilaVigente, CompanyStudioHistory?[] Caja)
      Build(int duenioActual = 1)
  {
    var empresa = new Company
    {
      Id = 10,
      BusinessName = "VEGA METALES SRL",
      Cuit = "30-71157059-0",
      AccountingStudioId = duenioActual
    };

    var filaVigente = new CompanyStudioHistory
    {
      Id = 1,
      CompanyId = empresa.Id,
      AccountingStudioId = duenioActual,
      From = DateTime.UtcNow.AddYears(-1),
      To = null
    };
    var historial = new List<CompanyStudioHistory> { filaVigente };

    // Caja de una sola posición: el mock de AddAsync deja ahí la fila nueva para poder revisarla.
    var caja = new CompanyStudioHistory?[1];

    var companiesRepo = new Mock<IRepository<Company>>();
    companiesRepo.Setup(r => r.GetByIdAsync(empresa.Id)).ReturnsAsync(empresa);
    companiesRepo.Setup(r => r.UpdateAsync(It.IsAny<Company>())).Returns(Task.CompletedTask);

    var studiosRepo = new Mock<IRepository<AccountingStudio>>();
    studiosRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => new AccountingStudio { Id = id, Name = $"Estudio {id}" });

    var historyRepo = new Mock<IRepository<CompanyStudioHistory>>();
    historyRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CompanyStudioHistory, bool>>>()))
        .ReturnsAsync((Expression<Func<CompanyStudioHistory, bool>> p) => historial.Where(p.Compile()).ToList());
    historyRepo.Setup(r => r.UpdateAsync(It.IsAny<CompanyStudioHistory>())).Returns(Task.CompletedTask);
    historyRepo.Setup(r => r.AddAsync(It.IsAny<CompanyStudioHistory>()))
        .Callback<CompanyStudioHistory>(h => caja[0] = h)
        .Returns(Task.CompletedTask);

    var uow = new Mock<IUnitOfWork>();
    uow.SetupGet(u => u.Companies).Returns(companiesRepo.Object);
    uow.SetupGet(u => u.AccountingStudios).Returns(studiosRepo.Object);
    uow.SetupGet(u => u.CompanyStudioHistories).Returns(historyRepo.Object);
    uow.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    return (new AssociationService(uow.Object), empresa, filaVigente, caja);
  }

  [Fact]
  public async Task Traspaso_CambiaElDuenio_CierraLaFilaAnterior_YAbreLaDelEstudioNuevo()
  {
    var (service, empresa, filaVigente, caja) = Build(duenioActual: 1);

    var (ok, error) = await service.TransferCompanyAsync(
        empresa.Id, toStudioId: 2, unionId: 19,
        authorizedBy: "sindicato@sirga.com", reason: "pedido del sindicato");

    Assert.True(ok);
    Assert.Null(error);

    // La empresa pasa a ser del estudio nuevo.
    Assert.Equal(2, empresa.AccountingStudioId);

    // La fila del estudio anterior queda cerrada (con fecha de fin).
    Assert.NotNull(filaVigente.To);

    // Y queda abierta la del nuevo, con el sindicato, quién lo autorizó y el motivo.
    Assert.NotNull(caja[0]);
    Assert.Equal(empresa.Id, caja[0]!.CompanyId);
    Assert.Equal(2, caja[0]!.AccountingStudioId);
    Assert.Equal(19, caja[0]!.UnionId);
    Assert.Equal("sindicato@sirga.com", caja[0]!.AuthorizedByUsername);
    Assert.Equal("pedido del sindicato", caja[0]!.Reason);
    Assert.Null(caja[0]!.To);
  }

  [Fact]
  public async Task Traspaso_Falla_SiLaEmpresaNoExiste()
  {
    var (service, _, _, caja) = Build();

    var (ok, error) = await service.TransferCompanyAsync(
        999, toStudioId: 2, unionId: 19, authorizedBy: "sindicato@sirga.com", reason: null);

    Assert.False(ok);
    Assert.NotNull(error);
    Assert.Null(caja[0]);   // no se escribió nada en el historial
  }

  [Fact]
  public async Task Traspaso_Falla_SiElEstudioDestinoEsElMismoQueYaLaTiene()
  {
    var (service, empresa, filaVigente, caja) = Build(duenioActual: 1);

    var (ok, error) = await service.TransferCompanyAsync(
        empresa.Id, toStudioId: 1, unionId: 19, authorizedBy: "sindicato@sirga.com", reason: null);

    Assert.False(ok);
    Assert.Contains("ya pertenece", error);
    Assert.Equal(1, empresa.AccountingStudioId);   // sigue siendo del mismo estudio
    Assert.Null(filaVigente.To);                   // y la fila vigente no se cerró
    Assert.Null(caja[0]);
  }
}
