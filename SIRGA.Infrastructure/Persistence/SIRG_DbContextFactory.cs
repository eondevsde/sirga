using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SIRGA.Infrastructure.Persistence;

public class SIRG_DbContextFactory : IDesignTimeDbContextFactory<SIRG_DbContext>
{
  public SIRG_DbContext CreateDbContext(string[] args)
  {
    string connectionString = "Server=localhost;Database=SIRGA_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

    var builder = new DbContextOptionsBuilder<SIRG_DbContext>();
    builder.UseSqlServer(connectionString);

    return new SIRG_DbContext(builder.Options);
  }
}
