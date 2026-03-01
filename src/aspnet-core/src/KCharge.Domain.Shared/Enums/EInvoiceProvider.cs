namespace KCharge.Enums;

/// <summary>
/// Represents the e-invoice provider for Vietnamese tax compliance.
/// </summary>
public enum EInvoiceProvider
{
    /// <summary>
    /// MISA e-invoice service.
    /// </summary>
    MISA = 0,

    /// <summary>
    /// Viettel e-invoice service.
    /// </summary>
    Viettel = 1,

    /// <summary>
    /// VNPT e-invoice service.
    /// </summary>
    VNPT = 2
}
