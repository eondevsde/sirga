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
    Assignments = new Repository<SindicataryAssignment>(_context, dbLock);
    SalaryScales = new Repository<SalaryScale>(_context, dbLock);
    AccountingStudios = new Repository<AccountingStudio>(_context, dbLock);
    InterestRates = new Repository<InterestRate>(_context, dbLock);
    PaymentTransactions = new Repository<PaymentTransaction>(_context, dbLock);
    CompanyBalances = new Repository<CompanyBalance>(_context, dbLock);
    BalanceHistories = new Repository<BalanceHistory>(_context, dbLock);
    AssociationRequests = new Repository<CompanyAssociationRequest>(_context, dbLock);
    EmployeeCategories = new Repository<EmployeeCategory>(_context, dbLock);
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
  }

  public IRepository<Employee> Employees { get; }
  public IRepository<Company> Companies { get; }
  public IRepository<Union> Unions { get; }
  public IRepository<DeclarationRegistry> Declarations { get; }
  public IRepository<SindicataryAssignment> Assignments { get; }
  public IRepository<SalaryScale> SalaryScales { get; }
  public IRepository<AccountingStudio> AccountingStudios { get; }
  public IRepository<InterestRate> InterestRates { get; }
  public IRepository<PaymentTransaction> PaymentTransactions { get; }
  public IRepository<CompanyBalance> CompanyBalances { get; }
  public IRepository<BalanceHistory> BalanceHistories { get; }
  public IRepository<CompanyAssociationRequest> AssociationRequests { get; }
  public IRepository<EmployeeCategory> EmployeeCategories { get; }
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
