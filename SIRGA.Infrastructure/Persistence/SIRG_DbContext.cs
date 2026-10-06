using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIRGA.Domain.Entities;

namespace SIRGA.Infrastructure.Persistence;

public class SIRG_DbContext : IdentityDbContext
{
    public SIRG_DbContext(DbContextOptions<SIRG_DbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Union> Unions => Set<Union>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyAssociationRequest> AssociationRequests => Set<CompanyAssociationRequest>();
    public DbSet<SindicataryAssignment> Assignments => Set<SindicataryAssignment>();
    public DbSet<DeclarationRegistry> Declarations => Set<DeclarationRegistry>();
    public DbSet<DeclarationDetail> DeclarationDetails => Set<DeclarationDetail>();
    public DbSet<EmployeeCategory> EmployeeCategories => Set<EmployeeCategory>();
    public DbSet<SalaryScale> SalaryScales => Set<SalaryScale>();
    public DbSet<AccountingStudio> AccountingStudios => Set<AccountingStudio>();
    public DbSet<InterestRate> InterestRates => Set<InterestRate>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<CompanyBalance> CompanyBalances => Set<CompanyBalance>();
    public DbSet<BalanceHistory> BalanceHistories => Set<BalanceHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<InspectionAct> InspectionActs => Set<InspectionAct>();
    public DbSet<InspectionActPeriod> InspectionActPeriods => Set<InspectionActPeriod>();
    public DbSet<PaymentPlan> PaymentPlans => Set<PaymentPlan>();
    public DbSet<PaymentPlanInstallment> PaymentPlanInstallments => Set<PaymentPlanInstallment>();
    public DbSet<Inspector> Inspectors => Set<Inspector>();
    public DbSet<InspectionVisit> InspectionVisits => Set<InspectionVisit>();
    public DbSet<PrejudicialNotice> PrejudicialNotices => Set<PrejudicialNotice>();
    public DbSet<NoticeAttachment> NoticeAttachments => Set<NoticeAttachment>();
    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<UnionSubscription> UnionSubscriptions => Set<UnionSubscription>();
    public DbSet<UnionSubscriptionPayment> UnionSubscriptionPayments => Set<UnionSubscriptionPayment>();
    public DbSet<UnionSubscriptionRequest> UnionSubscriptionRequests => Set<UnionSubscriptionRequest>();

    // Bancos donde los sindicatos cobran las boletas de las DDJJ, y la configuración bancaria de cada
    // sindicato (número de cliente, código de ente y reglas de vencimiento). Se modela aparte porque
    // un sindicato puede operar con más de un banco y cada uno tiene su estructura de código de barras.
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<UnionBankConfig> UnionBankConfigs => Set<UnionBankConfig>();

    // Rama de actividad (convenio colectivo): las escalas y categorías son nacionales y dependen de la
    // rama, no del sindicato. Varios sindicatos comparten la misma rama.
    public DbSet<Rama> Ramas => Set<Rama>();
    public DbSet<CompanyStudioHistory> CompanyStudioHistories => Set<CompanyStudioHistory>();
    public DbSet<UnionAuditor> UnionAuditors => Set<UnionAuditor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Desactivar cascada en DeclarationDetail para evitar ciclos de borrado
        modelBuilder.Entity<DeclarationDetail>()
            .HasOne(d => d.Registry)
            .WithMany(r => r.Details)
            .HasForeignKey(d => d.RegistryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DeclarationDetail>()
            .HasOne(d => d.Assignment)
            .WithMany()
            .HasForeignKey(d => d.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configuración para evitar ciclos de cascada en SalaryScales y EmployeeCategories
        modelBuilder.Entity<SalaryScale>()
            .HasOne(s => s.Union)
            .WithMany()
            .HasForeignKey(s => s.UnionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeCategory>()
            .HasOne(c => c.Union)
            .WithMany(u => u.Categories)
            .HasForeignKey(c => c.UnionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Traspaso de empresas entre estudios: el historial es la fuente del dueño actual (la fila
        // con To vacío). El índice único filtrado garantiza en la base que una empresa no pueda
        // quedar con dos estudios a cargo a la vez, aunque dos traspasos corran juntos.
        modelBuilder.Entity<CompanyStudioHistory>()
            .HasOne(h => h.Company)
            .WithMany()
            .HasForeignKey(h => h.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanyStudioHistory>()
            .HasOne(h => h.AccountingStudio)
            .WithMany()
            .HasForeignKey(h => h.AccountingStudioId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CompanyStudioHistory>()
            .HasIndex(h => h.CompanyId)
            .HasFilter("[To] IS NULL")
            .IsUnique();

        // Aplicar configuraciones automáticas de este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SIRG_DbContext).Assembly);

        // Configuración de índices únicos específicos
        modelBuilder.Entity<Employee>()
            .HasIndex(e => e.Dni)
            .IsUnique();

        modelBuilder.Entity<Company>()
            .HasIndex(c => new { c.Cuit, c.BranchName })
            .IsUnique();

        modelBuilder.Entity<Company>()
            .Property(c => c.NormalizedCuit)
            .HasComputedColumnSql("ISNULL(REPLACE(REPLACE([Cuit], '-', ''), ' ', ''), '')", stored: true);

        // Un CUIT solo puede pertenecer a una casa central (las sucursales comparten CUIT con su central)
        modelBuilder.Entity<Company>()
            .HasIndex(c => c.NormalizedCuit)
            .IsUnique()
            .HasFilter("[ParentCompanyId] IS NULL");

        // Índices para las consultas más calientes. Se verificó contra la base real que
        // faltaban: Declarations tiene 248.151 filas y solo estaba indexada por sus claves
        // foráneas, así que filtrar por período/estado o listar por sindicato ordenando por
        // fecha terminaba en un escaneo completo.
        modelBuilder.Entity<DeclarationRegistry>()
            .HasIndex(d => d.Period);

        modelBuilder.Entity<DeclarationRegistry>()
            .HasIndex(d => new { d.Status, d.SubmissionDate });

        modelBuilder.Entity<DeclarationRegistry>()
            .HasIndex(d => new { d.CompanyId, d.Period });

        modelBuilder.Entity<DeclarationRegistry>()
            .HasIndex(d => new { d.UnionId, d.SubmissionDate });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(l => l.Timestamp);

        modelBuilder.Entity<BalanceHistory>()
            .HasIndex(h => h.CompanyId);

        // Configuración de decimales para precisión monetaria
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}
