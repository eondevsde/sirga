using SIRGA.Application.Interfaces;
using SIRGA.Domain.Entities;

namespace SIRGA.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
  private readonly SIRG_DbContext _context;
  private readonly SemaphoreSlim _dbLock;

  public UnitOfWork(SIRG_DbContext context, SemaphoreSlim dbLock)
  {
    _context = context;
    _dbLock = dbLock;
    Employees = new Repository<Employee>(_context, dbLock);
    Companies = new Repository<Company>(_context, dbLock);
    Unions = new Repository<Union>(_context, dbLock);
    Declarations = new Repository<DeclarationRegistry>(_context, dbLock);
    DeclarationDetails = new Repository<DeclarationDetail>(_context, dbLock);
    Assignments = new Repository<SindicataryAssignment>(_context, dbLock);
    SalaryScales = new Repository<SalaryScale>(_context, dbLock);
    AccountingStudios = new Repository<AccountingStudio>(_context, dbLock);
    InterestRates = new Repository<InterestRate>(_context, dbLock);
    PaymentTransactions = new Repository<PaymentTransaction>(_context, dbLock);
    CompanyBalances = new Repository<CompanyBalance>(_context, dbLock);
    BalanceHistories = new Repository<BalanceHistory>(_context, dbLock);
    AssociationRequests = new Repository<CompanyAssociationRequest>(_context, dbLock);
    EmployeeCategories = new Repository<EmployeeCategory>(_context, dbLock);
    Activities = new Repository<Activity>(_context, dbLock);
    HealthInsurances = new Repository<HealthInsurance>(_context, dbLock);
    AuditLogs = new Repository<AuditLog>(_context, dbLock);
    InspectionActs = new Repository<InspectionAct>(_context, dbLock);
    PaymentPlans = new Repository<PaymentPlan>(_context, dbLock);
    Inspectors = new Repository<Inspector>(_context, dbLock);
    InspectionVisits = new Repository<InspectionVisit>(_context, dbLock);
    PrejudicialNotices = new Repository<PrejudicialNotice>(_context, dbLock);
    Provinces = new Repository<Province>(_context, dbLock);
    Cities = new Repository<City>(_context, dbLock);
    UnionSubscriptions = new Repository<UnionSubscription>(_context, dbLock);
    UnionSubscriptionPayments = new Repository<UnionSubscriptionPayment>(_context, dbLock);
    UnionSubscriptionRequests = new Repository<UnionSubscriptionRequest>(_context, dbLock);
    CompanyStudioHistories = new Repository<CompanyStudioHistory>(_context, dbLock);
    UnionAuditors = new Repository<UnionAuditor>(_context, dbLock);
    Banks = new Repository<Bank>(_context, dbLock);
    UnionBankConfigs = new Repository<UnionBankConfig>(_context, dbLock);
    // Mismo contexto y mismo semáforo que el resto: si se construyera con otro contexto, las
    // escrituras se perderían en silencio al llamar a CompleteAsync.
    Ramas = new Repository<Rama>(_context, dbLock);
  }

  public IRepository<Employee> Employees { get; }
  public IRepository<Company> Companies { get; }
  public IRepository<Union> Unions { get; }
  public IRepository<DeclarationRegistry> Declarations { get; }
  public IRepository<DeclarationDetail> DeclarationDetails { get; }
  public IRepository<SindicataryAssignment> Assignments { get; }
  public IRepository<SalaryScale> SalaryScales { get; }
  public IRepository<AccountingStudio> AccountingStudios { get; }
  public IRepository<InterestRate> InterestRates { get; }
  public IRepository<PaymentTransaction> PaymentTransactions { get; }
  public IRepository<CompanyBalance> CompanyBalances { get; }
  public IRepository<BalanceHistory> BalanceHistories { get; }
  public IRepository<CompanyAssociationRequest> AssociationRequests { get; }
  public IRepository<EmployeeCategory> EmployeeCategories { get; }
  public IRepository<Activity> Activities { get; }
  public IRepository<HealthInsurance> HealthInsurances { get; }
  public IRepository<AuditLog> AuditLogs { get; }
  public IRepository<InspectionAct> InspectionActs { get; }
  public IRepository<PaymentPlan> PaymentPlans { get; }
  public IRepository<Inspector> Inspectors { get; }
  public IRepository<InspectionVisit> InspectionVisits { get; }
  public IRepository<PrejudicialNotice> PrejudicialNotices { get; }
  public IRepository<Province> Provinces { get; }
  public IRepository<City> Cities { get; }
  public IRepository<UnionSubscription> UnionSubscriptions { get; }
  public IRepository<UnionSubscriptionPayment> UnionSubscriptionPayments { get; }
  public IRepository<UnionSubscriptionRequest> UnionSubscriptionRequests { get; }
  public IRepository<CompanyStudioHistory> CompanyStudioHistories { get; }
  public IRepository<UnionAuditor> UnionAuditors { get; }

  // Bancos y configuración bancaria por sindicato: con el MISMO contexto y el mismo semáforo que el
  // resto, para que CompleteAsync() guarde lo que escriben.
  public IRepository<Bank> Banks { get; }
  public IRepository<UnionBankConfig> UnionBankConfigs { get; }
  public IRepository<Rama> Ramas { get; }

  public async Task<int> CompleteAsync()
  {
    await _dbLock.WaitAsync();
    try { return await _context.SaveChangesAsync(); }
    finally { _dbLock.Release(); }
  }

  public void Dispose()
  {
    _context.Dispose();
    _dbLock.Dispose();
  }
}
