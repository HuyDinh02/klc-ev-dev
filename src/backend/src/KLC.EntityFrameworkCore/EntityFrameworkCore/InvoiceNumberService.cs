using System;
using System.Threading.Tasks;
using KLC.Payments;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace KLC.EntityFrameworkCore;

/// <summary>
/// Generates invoice numbers using a PostgreSQL sequence.
/// The sequence guarantees uniqueness and monotonic ordering even under
/// concurrent load, unlike the previous COUNT(*)+1 approach which had
/// a race window that produced duplicate invoice numbers.
/// </summary>
public class InvoiceNumberService : IInvoiceNumberService, ITransientDependency
{
    private readonly KLCDbContext _dbContext;

    public InvoiceNumberService(KLCDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> GenerateNextAsync()
    {
        // nextval() is atomic at the database level — safe under any concurrency.
        var seq = await _dbContext.Database
            .SqlQueryRaw<long>("SELECT nextval('klc_invoice_seq')")
            .FirstAsync();

        return $"INV-{DateTime.UtcNow:yyyy}-{seq:D6}";
    }
}
