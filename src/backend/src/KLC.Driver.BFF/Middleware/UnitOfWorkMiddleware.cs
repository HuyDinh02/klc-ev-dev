using Volo.Abp.Uow;

namespace KLC.Driver.Middleware;

/// <summary>
/// Wraps every BFF request in an ABP Unit of Work scope.
/// Application services injected into the BFF use ABP IRepository which requires
/// an active UoW — without this middleware, repository operations throw
/// ObjectDisposedException because the BFF (Minimal API) does not create a UoW
/// scope by default (unlike ABP's own HTTP pipeline).
/// </summary>
public class UnitOfWorkMiddleware
{
    private readonly RequestDelegate _next;

    public UnitOfWorkMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var unitOfWorkManager = context.RequestServices.GetRequiredService<IUnitOfWorkManager>();

        using var uow = unitOfWorkManager.Begin(new AbpUnitOfWorkOptions
        {
            IsTransactional = true
        });

        await _next(context);

        await uow.CompleteAsync();
    }
}
