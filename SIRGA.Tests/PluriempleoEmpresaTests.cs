using System.Linq.Expressions;
using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Pluriempleo de una empresa completa, en pocas consultas: es lo que va a consumir la pantalla de
/// DDJJ (que lista todos los empleados) y la marca del PDF. Se prueba que devuelva SÓLO los empleados
/// que están en más de una empresa, con el estudio dueño de la otra.
/// </summary>
public class PluriempleoEmpresaTests
{
  private static EmployeeService Build(
      IEnumerable<SindicataryAssignment> asignaciones,
      IEnumerable<Company> empresas,
      IEnumerable<AccountingStudio> estudios)
  {
    var asignacionesList = asignaciones.ToList();

    var assignmentsRepo = new Mock<IRepository<SindicataryAssignment>>();
    assignmentsRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SindicataryAssignment, bool>>>()))
        .ReturnsAsync((Expression<Func<SindicataryAssignment, bool>> p) => asignacionesList.Where(p.Compile()).ToList());

    // El servicio ahora pide SÓLO las empresas/estudios que necesita, por id (antes traía las 4.507 y
    // las 853 enteras, que era lo que hacía lenta la consulta). Los mocks aplican el predicado.
    var companiesRepo = new Mock<IRepository<Company>>();
    companiesRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Company, bool>>>()))
        .ReturnsAsync((Expression<Func<Company, bool>> p) => empresas.Where(p.Compile()).ToList());

    var studiosRepo = new Mock<IRepository<AccountingStudio>>();
    studiosRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<AccountingStudio, bool>>>()))
        .ReturnsAsync((Expression<Func<AccountingStudio, bool>> p) => estudios.Where(p.Compile()).ToList());

    var uow = new Mock<IUnitOfWork>();
    uow.SetupGet(u => u.Assignments).Returns(assignmentsRepo.Object);
    uow.SetupGet(u => u.Companies).Returns(companiesRepo.Object);
    uow.SetupGet(u => u.AccountingStudios).Returns(studiosRepo.Object);

    return new EmployeeService(uow.Object);
  }

  private static SindicataryAssignment Asignacion(int id, int employeeId, int companyId, bool activa = true) =>
      new() { Id = id, EmployeeId = employeeId, CompanyId = companyId, IsActive = activa, UnionId = 19 };

  [Fact]
  public async Task Devuelve_Solo_Los_Empleados_Que_Estan_En_Mas_De_Una_Empresa()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10), Asignacion(2, 100, 20), Asignacion(3, 101, 10) },
        new[]
        {
          new Company { Id = 10, BusinessName = "LA PRIMERA SRL", AccountingStudioId = 1 },
          new Company { Id = 20, BusinessName = "LA SEGUNDA SRL", AccountingStudioId = 2 }
        },
        new[]
        {
          new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" },
          new AccountingStudio { Id = 2, Name = "ESTUDIO DOS" }
        });

    var mapa = await service.GetPluriempleoDeEmpresaAsync(10);

    // El 100 está también en la 20 y el 101 no: el mapa trae sólo al 100.
    Assert.Single(mapa);
    Assert.True(mapa.ContainsKey(100));
    Assert.Equal("LA SEGUNDA SRL", mapa[100][0].CompanyName);
    Assert.Equal("ESTUDIO DOS", mapa[100][0].StudioName);
  }

  [Fact]
  public async Task Si_Nadie_Tiene_Pluriempleo_El_Mapa_Queda_Vacio()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10), Asignacion(3, 101, 10) },
        new[] { new Company { Id = 10, BusinessName = "LA PRIMERA SRL", AccountingStudioId = 1 } },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Empty(await service.GetPluriempleoDeEmpresaAsync(10));
  }

  [Fact]
  public async Task Una_Empresa_Sin_Asignaciones_Activas_Devuelve_Vacio()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10, activa: false) },
        new[] { new Company { Id = 10, BusinessName = "LA PRIMERA SRL", AccountingStudioId = 1 } },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Empty(await service.GetPluriempleoDeEmpresaAsync(10));
  }
}
