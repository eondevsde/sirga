using Microsoft.EntityFrameworkCore;
using SIRGA.Application.Interfaces;
using System.Linq.Expressions;

namespace SIRGA.Infrastructure.Persistence;

public class Repository<T> : IRepository<T> where T : class
{
  protected readonly SIRG_DbContext _context;
  private readonly SemaphoreSlim _dbLock;

  public Repository(SIRG_DbContext context, SemaphoreSlim dbLock)
  {
    _context = context;
    _dbLock = dbLock;
  }

  private async Task<T> WithLockAsync(Func<Task<T>> action)
  {
    await _dbLock.WaitAsync();
    try { return await action(); }
    finally { _dbLock.Release(); }
  }

  private async Task<IEnumerable<T>> WithLockAsync(Func<Task<IEnumerable<T>>> action)
  {
    await _dbLock.WaitAsync();
    try { return await action(); }
    finally { _dbLock.Release(); }
  }

  private async Task WithLockAsync(Func<Task> action)
  {
    await _dbLock.WaitAsync();
    try { await action(); }
    finally { _dbLock.Release(); }
  }

  public IQueryable<T> Query() => _context.Set<T>();

  public async Task<T?> GetByIdAsync(int id) => await WithLockAsync(() => _context.Set<T>().FindAsync(id).AsTask());

  public async Task<IEnumerable<T>> GetAllAsync() => await WithLockAsync(async () => await _context.Set<T>().ToListAsync());

  public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
      => await WithLockAsync(async () => await _context.Set<T>().Where(predicate).ToListAsync());

  public async Task AddAsync(T entity) => await WithLockAsync(() => _context.Set<T>().AddAsync(entity).AsTask());

  public async Task UpdateAsync(T entity) => await WithLockAsync(() => Task.FromResult(_context.Set<T>().Update(entity)));

  public async Task RemoveAsync(T entity) => await WithLockAsync(() => Task.FromResult(_context.Set<T>().Remove(entity)));

  public async Task<TResult> RunQueryAsync<TResult>(Func<IQueryable<T>, Task<TResult>> query)
  {
    await _dbLock.WaitAsync();
    try { return await query(_context.Set<T>()); }
    finally { _dbLock.Release(); }
  }
}
