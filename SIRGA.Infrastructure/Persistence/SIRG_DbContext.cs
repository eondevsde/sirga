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

        // Configuración de decimales para precisión monetaria
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}
