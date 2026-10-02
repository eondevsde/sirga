using System.Linq.Expressions;
using Moq;
using SIRGA.Application.Interfaces;
using SIRGA.Application.Services;
using SIRGA.Domain.Entities;

namespace SIRGA.Tests;

/// <summary>
/// Alcance del rol Auditor. La regla acordada: ve SÓLO el sindicato al que el propio sindicato lo
/// asignó, y si todavía no se lo asignaron no ve datos de NINGÚN sindicato (antes veía todo: los 853
/// estudios y las 4.507 empresas). Estas pruebas fijan esa regla, que es la que después aplican las
/// pantallas para filtrar. Los mocks aplican el predicado, así que se verifica el filtro de verdad.
/// </summary>
public class AuditorScopeTests
{
  private static UnionService Build(IEnumerable<UnionAuditor> asignaciones)
  {
    var lista = asignaciones.ToList();

    var repo = new Mock<IRepository<UnionAuditor>>();
    repo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<UnionAuditor, bool>>>()))
        .ReturnsAsync((Expression<Func<UnionAuditor, bool>> p) => lista.Where(p.Compile()).ToList());

    var uow = new Mock<IUnitOfWork>();
    uow.SetupGet(u => u.UnionAuditors).Returns(repo.Object);

    return new UnionService(uow.Object);
  }

  [Fact]
  public async Task Auditor_Asignado_Resuelve_Su_Sindicato()
  {
    var service = Build(new[] { new UnionAuditor { UnionId = 19, Username = "auditor@sirga.com" } });

    Assert.Equal(19, await service.GetUnionIdForAuditorAsync("auditor@sirga.com"));
  }

  [Fact]
  public async Task Auditor_Sin_Asignar_No_Resuelve_Ningun_Sindicato()
  {
    var service = Build(Array.Empty<UnionAuditor>());

    // null es la clave: la pantalla lo interpreta como "no mostrar datos", no como "mostrar todo".
    Assert.Null(await service.GetUnionIdForAuditorAsync("auditor@sirga.com"));
  }

  [Fact]
  public async Task Auditor_No_Resuelve_El_Sindicato_De_Otro()
  {
    var service = Build(new[] { new UnionAuditor { UnionId = 19, Username = "auditor@sirga.com" } });

    Assert.Null(await service.GetUnionIdForAuditorAsync("otro.auditor@sirga.com"));
  }

  [Fact]
  public async Task Sin_Usuario_No_Resuelve_Nada()
  {
    var service = Build(new[] { new UnionAuditor { UnionId = 19, Username = "auditor@sirga.com" } });

    Assert.Null(await service.GetUnionIdForAuditorAsync("   "));
    Assert.Null(await service.GetUnionIdForAuditorAsync(""));
  }
}
