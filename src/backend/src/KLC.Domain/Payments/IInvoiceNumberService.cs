using System.Threading.Tasks;

namespace KLC.Payments;

/// <summary>
/// Generates unique, collision-free invoice numbers using a database sequence.
/// All implementations must guarantee monotonically increasing numbers with no gaps
/// under concurrent load.
/// </summary>
public interface IInvoiceNumberService
{
    Task<string> GenerateNextAsync();
}
