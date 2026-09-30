using SIRGA.Domain.Entities;
using System.Linq.Expressions;

namespace SIRGA.Application.Interfaces;

public interface IRepository<T> where T : class
{
  IQueryable<T> Query();
  Task<T?> GetByIdAsync(int id);
  Task<IEnumerable<T>> GetAllAsync();
  Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
  Task AddAsync(T entity);
  Task UpdateAsync(T entity);
  Task RemoveAsync(T entity);
  Task<TResult> RunQueryAsync<TResult>(Func<IQueryable<T>, Task<TResult>> query);
}

public interface IUnitOfWork : IDisposable
{
  IRepository<Employee> Employees { get; }
  IRepository<Company> Companies { get; }
  IRepository<Union> Unions { get; }
  IRepository<DeclarationRegistry> Declarations { get; }
  IRepository<SindicataryAssignment> Assignments { get; }
  IRepository<SalaryScale> SalaryScales { get; }
  IRepository<AccountingStudio> AccountingStudios { get; }
  IRepository<InterestRate> InterestRates { get; }
  IRepository<PaymentTransaction> PaymentTransactions { get; }
  IRepository<CompanyBalance> CompanyBalances { get; }
  IRepository<BalanceHistory> BalanceHistories { get; }
  IRepository<CompanyAssociationRequest> AssociationRequests { get; }
  IRepository<EmployeeCategory> EmployeeCategories { get; }
  IRepository<AuditLog> AuditLogs { get; }
  IRepository<InspectionAct> InspectionActs { get; }
  IRepository<PaymentPlan> PaymentPlans { get; }
  IRepository<Inspector> Inspectors { get; }
  IRepository<InspectionVisit> InspectionVisits { get; }
  IRepository<PrejudicialNotice> PrejudicialNotices { get; }
  IRepository<Province> Provinces { get; }
  IRepository<City> Cities { get; }
  IRepository<UnionSubscription> UnionSubscriptions { get; }
  IRepository<UnionSubscriptionPayment> UnionSubscriptionPayments { get; }
  IRepository<UnionSubscriptionRequest> UnionSubscriptionRequests { get; }
  Task<int> CompleteAsync();
}
