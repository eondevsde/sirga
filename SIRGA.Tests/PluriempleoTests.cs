using System.Linq.Expressions;
using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Detección de pluriempleo: el mismo empleado trabajando en dos empresas a la vez. Es legal y la
/// cuota socio se aporta POR EMPRESA, así que el sistema NO lo bloquea ni corrige importes: sólo lo
/// detecta, para marcarlo en la DDJJ y pedir el legajo. Puede estar en dos estudios distintos, por eso
/// el dato del estudio dueño viaja (el sindicato es el único que ve el cuadro completo).
/// La consulta se hace siempre excluyendo la empresa actual: "¿trabaja además en otra?".
/// </summary>
public class PluriempleoTests
{
  private static EmployeeService Build(
      IEnumerable<SindicataryAssignment> asignaciones,
      IEnumerable<Company> empresas,
      IEnumerable<AccountingStudio> estudios)
  {
    var asignacionesList = asignaciones.ToList();
    var empresasList = empresas.ToList();
    var estudiosList = estudios.ToList();

    var assignmentsRepo = new Mock<IRepository<SindicataryAssignment>>();
    assignmentsRepo
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<SindicataryAssignment, bool>>>()))
        .ReturnsAsync((Expression<Func<SindicataryAssignment, bool>> p) => asignacionesList.Where(p.Compile()).ToList());

    var companiesRepo = new Mock<IRepository<Company>>();
    companiesRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => empresasList.FirstOrDefault(c => c.Id == id));

    var studiosRepo = new Mock<IRepository<AccountingStudio>>();
    studiosRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
        .ReturnsAsync((int id) => estudiosList.FirstOrDefault(e => e.Id == id));

    var uow = new Mock<IUnitOfWork>();
    uow.SetupGet(u => u.Assignments).Returns(assignmentsRepo.Object);
    uow.SetupGet(u => u.Companies).Returns(companiesRepo.Object);
    uow.SetupGet(u => u.AccountingStudios).Returns(studiosRepo.Object);

    return new EmployeeService(uow.Object);
  }

  private static SindicataryAssignment Asignacion(int id, int employeeId, int companyId, bool activa = true) =>
      new() { Id = id, EmployeeId = employeeId, CompanyId = companyId, IsActive = activa, UnionId = 19 };

  private static Company Empresa(int id, string nombre, int estudioId) =>
      new() { Id = id, BusinessName = nombre, AccountingStudioId = estudioId };

  [Fact]
  public async Task Detecta_La_Otra_Empresa_Cuando_El_Empleado_Esta_En_Dos()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10), Asignacion(2, 100, 20) },
        new[] { Empresa(10, "LA PRIMERA SRL", 1), Empresa(20, "LA SEGUNDA SRL", 2) },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" }, new AccountingStudio { Id = 2, Name = "ESTUDIO DOS" } });

    var otras = (await service.GetPluriempleoAsync(100, excluirCompanyId: 10)).ToList();

    Assert.Single(otras);
    Assert.Equal(20, otras[0].CompanyId);
    Assert.Equal("LA SEGUNDA SRL", otras[0].CompanyName);
    // El otro empleador puede ser de OTRO estudio: ese dato viaja para que el sindicato lo vea.
    Assert.Equal(2, otras[0].StudioId);
    Assert.Equal("ESTUDIO DOS", otras[0].StudioName);
  }

  [Fact]
  public async Task No_Detecta_Nada_Si_Tiene_Una_Sola_Empresa()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10) },
        new[] { Empresa(10, "LA PRIMERA SRL", 1) },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Empty(await service.GetPluriempleoAsync(100, excluirCompanyId: 10));
  }

  [Fact]
  public async Task Una_Asignacion_Inactiva_No_Cuenta_Como_Pluriempleo_Vigente()
  {
    // Estuvo en la empresa 20 pero ya no: eso no es pluriempleo vigente.
    var service = Build(
        new[] { Asignacion(1, 100, 10), Asignacion(2, 100, 20, activa: false) },
        new[] { Empresa(10, "LA PRIMERA SRL", 1), Empresa(20, "LA SEGUNDA SRL", 1) },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Empty(await service.GetPluriempleoAsync(100, excluirCompanyId: 10));

    // Y al revés: activo sólo en la segunda, la primera ya no cuenta (está inactiva).
    var service2 = Build(
        new[] { Asignacion(1, 100, 10, activa: false), Asignacion(2, 100, 20) },
        new[] { Empresa(10, "LA PRIMERA SRL", 1), Empresa(20, "LA SEGUNDA SRL", 1) },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Empty(await service2.GetPluriempleoAsync(100, excluirCompanyId: 20));
  }

  [Fact]
  public async Task Sin_Excluir_Empresa_Lista_Todas_Sus_Asignaciones_Activas()
  {
    var service = Build(
        new[] { Asignacion(1, 100, 10), Asignacion(2, 100, 20) },
        new[] { Empresa(10, "LA PRIMERA SRL", 1), Empresa(20, "LA SEGUNDA SRL", 1) },
        new[] { new AccountingStudio { Id = 1, Name = "ESTUDIO UNO" } });

    Assert.Equal(2, (await service.GetPluriempleoAsync(100)).Count);
  }
}
