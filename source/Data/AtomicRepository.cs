using System;
using System.Linq;
//using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Common.Data.Extensions;

namespace Common.Data;

public interface IAsyncRepository<T>
{
    Task<IList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<int> SaveAsync(T Item, Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<int> SaveAsync(IEnumerable<T> Items, Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
}

/// <summary>
/// This Repository Removes Unit of Work from the Repository and forces actions to be 
/// atomic.  When there are not business reasons to do multiple actions in a single transaction
/// this allows the business layer to ignore the database layer
/// </summary>
/// <typeparam name="T"></typeparam>
public class AtomicRepository<T> : IAsyncRepository<T> where T : class
{
    static string _tableName = typeof(T).Name;

    protected DbContext _context;
    public AtomicRepository(DbContext context)
    {
        _context = context;
    }

    private IQueryable<T> Get()
    {
        return _context.Set<T>().AsNoTracking();
    }

    public virtual async Task<T?> Get(int id, CancellationToken ct = default)
    {
        return await _context.Set<T>().FindAsync(id, ct).ConfigureAwait(false);
    }

    public virtual async Task<IList<T>> GetAll(CancellationToken ct = default)
    {
        return await Get().ToListAsync(ct).ConfigureAwait(false);
    }

    public virtual IAsyncEnumerable<T> GetAsyncEnumerable(Expression<Func<T, bool>> predicate)
    {
        return Get().Where(predicate).AsAsyncEnumerable();
    }

    public virtual async Task<IList<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await Get().Where(predicate).ToListAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<IList<T>> Get<T2>(Expression<Func<T, bool>> predicate, Expression<Func<T, T2>> order, CancellationToken ct = default)
    {
        return await Get().Where(predicate).OrderBy(order).ToListAsync(ct).ConfigureAwait(false);
    }

    public virtual async Task<T?> GetSingleExisting(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await Get().Where(predicate).SingleOrDefaultAsync(ct);
    }

    protected async Task<bool> AddOrUpdate(T item, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        bool isAdded = false;
        if (item != null)
        {
            var count = await _context.UpdateOrAddAsync(item, predicate, ct);
            isAdded = count > 0;
        }
        return isAdded;
    }

    protected async Task<int> AddOrUpdateRange(IEnumerable<T> items, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        int added = 0;
        if (items != null)
            foreach (T item in items)
                if (await AddOrUpdate(item, predicate, ct))
                    added++;
        return added;
    }

    protected async Task<int> SaveTransaction(Task addOrUpdateTask, CancellationToken ct = default)
    {
        int changeCount = 0;
        try
        {
            using var transaction = _context.Database.BeginTransaction();
            await addOrUpdateTask;
            changeCount = await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Trace.TraceWarning("Database transaction canceled");
        }
        catch (InvalidOperationException ex)
        {
            System.Diagnostics.Trace.TraceWarning("Database connection disposed prematurely. {0}", ex);
        }
        catch (Exception ex)
        {
            var e = ex.InnerException ?? ex;
            System.Diagnostics.Trace.TraceError("Failed to update {0} record in the database. " +
                "Transaction has been rolled back automatically on disposal. {1}", _tableName, e);
            System.Diagnostics.Debugger.Break();
            throw;
        }
        return changeCount;
    }

    public virtual async Task<int> SaveAsync(T item, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var task = AddOrUpdate(item, predicate, ct);
        return await SaveTransaction(task, ct);
    }

    //public virtual async Task<int> Save(IEnumerable<T> items)
    //{
    //    var task = AddOrUpdateRange(items);
    //    int changeCount = await SaveTransaction(task);
    //    int updateCount = changeCount > 0 ? items.Count() : 0;
    //    return updateCount;
    //}

    //public virtual async Task<int> SaveRange(IEnumerable<T> items, Expression<Func<T, bool>> predicate)
    //{
    //    int updateCount = 0;
    //    if (items != null)
    //    {
    //        int totalItemCount = items.Count();
    //        try
    //        {
    //            using var transaction = _context.Database.BeginTransaction();
    //            int addCount = await AddOrUpdateRange(items, predicate);
    //            int changeCount = await _context.SaveChangesAsync();
    //            await transaction.CommitAsync();
    //            updateCount = totalItemCount;
    //            _logger.LogDebug("Added {0} of {1} records to {2} ({3} changed)",
    //                addCount, totalItemCount, _tableName, changeCount);
    //        }
    //        catch (Exception ex)
    //        {
    //            var e = ex.InnerException ?? ex;
    //            _logger.LogWarning(e, "Failed to update {0} record in the database. " +
    //                "Transaction has been rolled back automatically on disposal. " +
    //                "Trying again individually.", _tableName);
    //            updateCount = await Save(items);
    //        }
    //    }
    //    return updateCount;
    //}

    public virtual async Task<int> SaveRange(IEnumerable<T> items, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        int updateCount = 0;
        if (items != null)
        {
            int totalItemCount = items.Count();
            try
            {
                using var transaction = _context.Database.BeginTransaction();
                int addCount = await AddOrUpdateRange(items, predicate, ct);
                int changeCount = await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                updateCount = totalItemCount;
                System.Diagnostics.Trace.TraceInformation("Added {0} of {1} records to {2} ({3} changed)",
                    addCount, totalItemCount, _tableName, changeCount);
            }
            catch (Exception ex)
            {
                var e = ex.InnerException ?? ex;
                System.Diagnostics.Trace.TraceWarning("Failed to update {0} record in the database. " +
                    "Transaction has been rolled back automatically on disposal. " +
                    "Trying again individually. {1}", _tableName, e);
                updateCount = await SaveAsync(items, predicate, ct);
            }
        }
        return updateCount;
    }

    public virtual async Task<int> SaveAsync(IEnumerable<T> items, Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        int updateCount = 0;
        if (items != null)
        {
            int totalItemCount = items.Count();
            foreach (T item in items)
            {
                try
                {
                    using var transaction = _context.Database.BeginTransaction();
                    bool isAdded = await AddOrUpdate(item, predicate, ct);
                    int changeCount = await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    updateCount++;
                    System.Diagnostics.Trace.TraceInformation("{0} {1} of {2} records to {3} ({4} change)",
                        isAdded ? "Added" : "Updated", updateCount, totalItemCount, _tableName, changeCount);
                }
                catch (Exception ex)
                {
                    var e = ex.InnerException ?? ex;
                    System.Diagnostics.Trace.TraceError("Failed to update {0} record in the database. " +
                        "Transaction has been rolled back automatically on disposal. {1}", _tableName, e);
                    System.Diagnostics.Debugger.Break();
                    throw;
                }
            }
        }
        return updateCount;
    }

    public virtual async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        var itemsToDelete = await GetAsync(predicate, ct);
        _context.RemoveRange(itemsToDelete, ct);
        return await _context.SaveChangesAsync(ct);
    }
}