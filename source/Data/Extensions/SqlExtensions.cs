using System;
using System.Data;
using System.Linq.Expressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Common.Data.Models;

namespace Common.Data.Extensions;

public static class SqlExtensions
{
    public static DbContextOptions<TContext> GetDbContextOptions<TContext>(
        string connectionString) where TContext : DbContext =>
        new DbContextOptionsBuilder<TContext>()
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSqlServer(connectionString, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                //o.EnableRetryOnFailure(retryCount); // doesn't work with transactions
            }).Options;

    public static IServiceCollection AddConfigureDb<T>(
        this IServiceCollection services, IConfiguration configuration) where T : SimInfoDbContext
    {
        return services.AddDbContext<SimInfoDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(SimInfoDbContext.ConfigName);
            CheckConnectionString(connectionString, SimInfoDbContext.DbName, SimInfoDbContext.ConfigName);
            options.UseSqlServer(connectionString, o =>
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }

    private static void CheckConnectionString(string connectionString, string dbName = "DB", string configName = "DB")
    {
        string errorMessage =
            $"{dbName} connection string configuration value {{0}}. " +
            $"Add 'ConnectionStrings:{configName}' to 'appsettings.json' or as an Environment variable .";

        if (string.IsNullOrEmpty(connectionString))
            throw new NullReferenceException(string.Format(errorMessage, "not found"));
        else if (!connectionString.StartsWith("Server="))
            throw new ArgumentException(string.Format(errorMessage, "is encrypted"));
        else
            Debug.WriteLine("{0} connection string set to {1}", dbName, connectionString);
    }
    
    public static IQueryable<T> GetAsNoTracking<T>(this DbContext context) where T : class
    {
        return context.Set<T>().AsNoTracking();
    }

    public static IEnumerable<T> GetAsNoTracking<T>(this DbContext context, Expression<Func<T, bool>> predicate) where T : class
    {
        return context.GetAsNoTracking<T>().Where(predicate);
    }

    public static void DetachLocal<T>(this DbContext context, T t, Func<T, bool> predicate) where T : class
    {
        var local = context.Set<T>().Local.FirstOrDefault(predicate);
        if (local != null)
            context.Entry(local).State = EntityState.Detached;
        context.Entry(t).State = EntityState.Modified;
    }

    internal static DbContext GetContext<T>(this DbSet<T> dbSet) where T : class
    {
        return dbSet.GetService<ICurrentDbContext>().Context;
    }

    internal static string GetTableName<T>(this IEntityType? entityType) where T : class
    {
        var tableNameAnnotation = entityType?.GetAnnotation("Relational:TableName");
        return tableNameAnnotation?.Value?.ToString() ?? typeof(T).Name;
    }

    internal static async Task<int> TruncateAsync<T>(this DbContext context, string schema = "dbo", CancellationToken ct = default) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        string tableName = context.Set<T>().EntityType.GetTableName() ?? typeof(T).Name;
        System.Diagnostics.Trace.TraceInformation("TRUNCATE TABLE {0}.{1};", schema, tableName);
        return await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {schema}.{tableName};", ct);
    }

    internal static async Task<int> DeleteAsync<T>(this DbContext context, string schema = "dbo", CancellationToken ct = default) where T : class
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        string tableName = context.Set<T>().EntityType.GetTableName() ?? typeof(T).Name;
        System.Diagnostics.Trace.TraceInformation("DELETE FROM {0}.{1};", schema, tableName);
        return await context.Database.ExecuteSqlRawAsync($"DELETE FROM {schema}.{tableName};", ct);
    }

    internal static void Clear<T>(this DbSet<T> dbSet) where T : class
    {
        if (!dbSet.IsNull())
            dbSet.RemoveRange(dbSet.ToList());
    }

    internal static void Refresh<T>(this DbContext context, ref DbSet<T> set) where T : class
    {
        if (set == null)
            set = context.Set<T>();
    }

    public static async Task<int> SaveAsync<T>(
        this DbContext context, T data, Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : class
    {
        int updateCount = await context.UpdateOrAddAsync(data, predicate, ct);
        _ = await context.SaveChangesAsync(ct);
        return updateCount;
    }

    public static async Task<int> SaveAsync<T>(
        this DbContext context, IEnumerable<T> data, IQueryable<T> existingItems, CancellationToken ct = default) where T : class
    {
        int updateCount = await context.UpdateOrAddAsync(data, existingItems, ct);
        _ = await context.SaveChangesAsync(ct);
        return updateCount;
    }

    public static async Task<int> UpdateOrAddAsync<T>(
        this DbContext context, T data, Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : class
    {
        var existingItems = context.GetAsNoTracking(predicate);
        return await context.UpdateOrAddAsync(data, existingItems, ct);
    }

    public static async Task<int> UpdateOrAddAsync<T>(
        this DbContext context, IEnumerable<T> data, IQueryable<T> existingItems, CancellationToken ct = default) where T : class
    {
        int updateCount = 0;
        if (data != null)
        {
            foreach (var item in data)
            {
                updateCount += await context.UpdateOrAddAsync(item, existingItems, ct);
            }
        }
        return updateCount;
    }

    public static async Task<int> UpdateOrAddAsync<T>(
        this DbContext context, T data, IQueryable<T>? existingItems, CancellationToken ct = default) where T : class
    {
        int updateCount = 0;
        if (existingItems != null)
        {
            foreach (var item in existingItems)
            {
                await context.UpdateOrAddAsync(data, item, ct);
                updateCount++;
            }
        }
        else if (data != null)
        {
            await context.AddAsync(data, ct);
            updateCount++;
        }
        return updateCount;
    }

    public static async Task UpdateOrAddAsync<T>(
        this DbContext context, T data, T? existing = null, CancellationToken ct = default) where T : class
    {
        if (context != null && data != null)
        {
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(data);
            }
            else
            {
                await context.AddAsync(data, ct);
            }
        }
    }

    //public static void AddOrUpdate<T>(
    //    this DbSet<T> set, T entity) where T : class
    //{
    //    if (set != null && entity != null)
    //    {
    //        var context = set.GetContext();
    //        if (context != null &&
    //            context.Entry(entity).IsKeySet)
    //            set.Update(entity);
    //        else
    //            set.Add(entity);
    //    }
    //}

    public static async Task<int> AddOrUpdateAsync<T>(
        this DbContext context, T data, Expression<Func<T, bool>> predicate, CancellationToken ct = default) where T : EntityDates
    {
        int updateCount = 0;
        var items = context.GetAsNoTracking(predicate);
        if (items != null)
        {
            foreach (var item in items)
            {
                await context.AddOrUpdateAsync(data, item, ct);
                updateCount++;
            }
        }
        else
        {
            await context.AddAsync(data, ct);
            updateCount++;
        }
        return updateCount;
    }

    public static async Task AddOrUpdateAsync<T>(
        this DbContext context, T data, T? existing = null, CancellationToken ct = default) where T : EntityDates
    {
        if (context != null && data != null)
        {
            data.ModifiedOn = DateTime.Now;
            if (existing != null)
            {
                context.Entry(existing).CurrentValues.SetValues(data);
            }
            else
            {
                data.CreatedOn = DateTime.Now;
                await context.AddAsync(data, ct);
            }
        }
    }

    public static async Task<int> AddTruncateAsync<T>(
        this DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
    {
        int updateCount = 0;
        if (entities != null)
        {
            int totalCount = entities.Count();
            var task = context.TruncateAsync<T>("dbo") // faster but requires ALTER permissions
                .ContinueWith((t) => context.AddRangeAsync(entities));
            updateCount = await context.ExecuteTransactionAsync<T>(task, totalCount);
        }
        return updateCount;
    }

    public static async Task<int> AddOverwriteAsync<T>(
        this DbContext context, IEnumerable<T> entities, CancellationToken ct = default) where T : class
    {
        int updateCount = 0;
        if (entities != null)
        {
            int totalCount = entities.Count();
            var task = context.DeleteAsync<T>("dbo") // slower but only requires DELETE permissions
                .ContinueWith((t) => context.AddRangeAsync(entities));
            updateCount = await context.ExecuteTransactionAsync<T>(task, totalCount);
        }
        return updateCount;
    }

    internal static async Task<int> ExecuteTransactionAsync<T>(
        this DbContext context, Task addOrUpdateTask, int totalEntityCount, Task? safeAddOrUpdate = null, CancellationToken ct = default) where T : class
    {
        int updateCount = 0;
        if (!ct.IsCancellationRequested)
        {
            string tableName = typeof(T).Name;
            try
            {
                using var transaction = context.Database.BeginTransaction();
                await addOrUpdateTask.ConfigureAwait(false);
                int changeCount = await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                System.Diagnostics.Trace.TraceInformation("Added {0} of {1} records to {2} ({3} changed)",
                    updateCount, totalEntityCount, tableName, changeCount);
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Trace.TraceInformation("Database table {0} transaction cancelled.", tableName);
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Trace.TraceWarning("Database connection to {0} disposed prematurely. {1}", ex, tableName);
            }
            catch (Exception ex)
            {
                var e = ex.InnerException ?? ex;
                System.Diagnostics.Trace.TraceError("Failed to update {0} record in the database. " +
                    "Transaction has been rolled back automatically on disposal. {1}", tableName, e);
                System.Diagnostics.Debugger.Break();
                if (safeAddOrUpdate != null)
                    await safeAddOrUpdate;
            }
        }
        return updateCount;
    }
}