using HardwareStore.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HardwareStore.Common.Infrastructure.Audit;

public static class AuditRegistration
{
    /// <summary>
    /// The one way a module registers its business DbContext: PostgreSQL plus the audit interceptor. Without a request-aware
    /// <see cref="IAuditActor"/> (Host registers one) every change is logged as made by the system.
    /// </summary>
    public static IServiceCollection AddAuditedDbContextFactory<TContext>(this IServiceCollection services, string connectionString)
        where TContext : DbContext
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAuditActor, SystemAuditActor>();
        services.TryAddSingleton<AuditSaveChangesInterceptor>();
        return services.AddDbContextFactory<TContext>((provider, options) =>
            options.UseNpgsql(connectionString).AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>()));
    }

    /// <summary>Allows putting back earlier values of <typeparamref name="TEntity"/> from the audit log, for exactly these fields.</summary>
    public static IServiceCollection AddAuditRestoreTarget<TContext, TEntity>(this IServiceCollection services, params string[] restorableFields)
        where TContext : DbContext
        where TEntity : class =>
        services.AddSingleton<IAuditRestoreTarget>(provider =>
            new DbContextAuditRestoreTarget<TContext, TEntity>(provider.GetRequiredService<IDbContextFactory<TContext>>(), restorableFields));
}
