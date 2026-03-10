using System;
using KLC.Enums;
using KLC.Payments;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace KLC.EInvoices;

/// <summary>
/// Tests for EInvoice and Invoice domain entity behavior.
/// Validates status transitions, retry logic, and invoice calculations.
/// </summary>
public class EInvoiceTests
{
    private static EInvoice CreateTestEInvoice(
        EInvoiceProvider provider = EInvoiceProvider.MISA)
    {
        return new EInvoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            provider);
    }

    [Fact]
    public void Create_EInvoice_Should_Set_Default_Values()
    {
        var invoiceId = Guid.NewGuid();
        var eInvoice = new EInvoice(Guid.NewGuid(), invoiceId, EInvoiceProvider.Viettel);

        eInvoice.InvoiceId.ShouldBe(invoiceId);
        eInvoice.Provider.ShouldBe(EInvoiceProvider.Viettel);
        eInvoice.Status.ShouldBe(EInvoiceStatus.Pending);
        eInvoice.RetryCount.ShouldBe(0);
        eInvoice.ExternalInvoiceId.ShouldBeNull();
        eInvoice.EInvoiceNumber.ShouldBeNull();
        eInvoice.ViewUrl.ShouldBeNull();
        eInvoice.PdfUrl.ShouldBeNull();
        eInvoice.SignatureHash.ShouldBeNull();
        eInvoice.IssuedAt.ShouldBeNull();
        eInvoice.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void MarkProcessing_Should_Transition_From_Pending()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.Status.ShouldBe(EInvoiceStatus.Pending);

        eInvoice.MarkProcessing();

        eInvoice.Status.ShouldBe(EInvoiceStatus.Processing);
    }

    [Fact]
    public void MarkIssued_Should_Set_All_Fields()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkProcessing();

        eInvoice.MarkIssued(
            externalInvoiceId: "EXT-001",
            eInvoiceNumber: "EINV-2026-000001",
            viewUrl: "https://einvoice.vn/view/001",
            pdfUrl: "https://einvoice.vn/pdf/001",
            signatureHash: "abc123hash");

        eInvoice.Status.ShouldBe(EInvoiceStatus.Issued);
        eInvoice.ExternalInvoiceId.ShouldBe("EXT-001");
        eInvoice.EInvoiceNumber.ShouldBe("EINV-2026-000001");
        eInvoice.ViewUrl.ShouldBe("https://einvoice.vn/view/001");
        eInvoice.PdfUrl.ShouldBe("https://einvoice.vn/pdf/001");
        eInvoice.SignatureHash.ShouldBe("abc123hash");
        eInvoice.IssuedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkFailed_Should_Increment_RetryCount_And_Set_Error()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkProcessing();

        eInvoice.MarkFailed("Connection timeout");

        eInvoice.Status.ShouldBe(EInvoiceStatus.Failed);
        eInvoice.RetryCount.ShouldBe(1);
        eInvoice.ErrorMessage.ShouldBe("Connection timeout");
    }

    [Fact]
    public void MarkCancelled_Should_Set_Cancelled_Status()
    {
        var eInvoice = CreateTestEInvoice();

        eInvoice.MarkCancelled();

        eInvoice.Status.ShouldBe(EInvoiceStatus.Cancelled);
    }

    [Fact]
    public void CanRetry_Should_Return_True_When_Failed_Under_MaxRetries()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkFailed("Error 1");

        eInvoice.CanRetry().ShouldBeTrue();
        eInvoice.RetryCount.ShouldBe(1);
    }

    [Fact]
    public void CanRetry_Should_Return_False_After_Three_Failures()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkFailed("Error 1");
        eInvoice.ResetForRetry();
        eInvoice.MarkFailed("Error 2");
        eInvoice.ResetForRetry();
        eInvoice.MarkFailed("Error 3");

        eInvoice.CanRetry().ShouldBeFalse();
        eInvoice.RetryCount.ShouldBe(3);
    }

    [Fact]
    public void ResetForRetry_Should_Reset_Status_To_Pending()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkFailed("Transient error");

        eInvoice.ResetForRetry();

        eInvoice.Status.ShouldBe(EInvoiceStatus.Pending);
        eInvoice.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ResetForRetry_Should_Throw_When_MaxRetries_Exceeded()
    {
        var eInvoice = CreateTestEInvoice();
        eInvoice.MarkFailed("Error 1");
        eInvoice.ResetForRetry();
        eInvoice.MarkFailed("Error 2");
        eInvoice.ResetForRetry();
        eInvoice.MarkFailed("Error 3");

        var ex = Should.Throw<BusinessException>(() => eInvoice.ResetForRetry());
        ex.Code.ShouldBe(KLCDomainErrorCodes.EInvoiceCannotRetry);
    }

    [Fact]
    public void Invoice_Constructor_Should_Calculate_Amounts_Correctly()
    {
        var invoice = new Invoice(
            Guid.NewGuid(),
            paymentTransactionId: Guid.NewGuid(),
            invoiceNumber: "INV-2026-000001",
            energyKwh: 25.5m,
            ratePerKwh: 3500m,
            taxRatePercent: 10m);

        // BaseAmount = 25.5 * 3500 = 89250
        invoice.BaseAmount.ShouldBe(89250m);
        // TaxAmount = 89250 * 0.10 = 8925
        invoice.TaxAmount.ShouldBe(8925m);
        // TotalAmount = 89250 + 8925 = 98175
        invoice.TotalAmount.ShouldBe(98175m);
        invoice.EnergyKwh.ShouldBe(25.5m);
        invoice.RatePerKwh.ShouldBe(3500m);
        invoice.TaxRatePercent.ShouldBe(10m);
    }

    [Fact]
    public void Invoice_GenerateInvoiceNumber_Should_Follow_Format()
    {
        var invoiceNumber = Invoice.GenerateInvoiceNumber(42);

        invoiceNumber.ShouldStartWith("INV-");
        invoiceNumber.ShouldEndWith("-000042");
        invoiceNumber.Length.ShouldBe(15); // INV-YYYY-NNNNNN
    }

    [Fact]
    public void Invoice_Should_Set_IssuedAt_On_Creation()
    {
        var before = DateTime.UtcNow;

        var invoice = new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-2026-000001",
            10m, 2000m, 8m);

        invoice.IssuedAt.ShouldBeGreaterThanOrEqualTo(before);
        invoice.IssuedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    public void EInvoice_Full_Lifecycle_Pending_Processing_Issued()
    {
        var eInvoice = CreateTestEInvoice(EInvoiceProvider.VNPT);

        eInvoice.Status.ShouldBe(EInvoiceStatus.Pending);

        eInvoice.MarkProcessing();
        eInvoice.Status.ShouldBe(EInvoiceStatus.Processing);

        eInvoice.MarkIssued("EXT-999", "EINV-2026-999999");
        eInvoice.Status.ShouldBe(EInvoiceStatus.Issued);
        eInvoice.ExternalInvoiceId.ShouldBe("EXT-999");
        eInvoice.EInvoiceNumber.ShouldBe("EINV-2026-999999");
    }

    [Fact]
    public void EInvoice_Retry_Lifecycle_Failed_Reset_Processing_Issued()
    {
        var eInvoice = CreateTestEInvoice();

        eInvoice.MarkFailed("Network error");
        eInvoice.Status.ShouldBe(EInvoiceStatus.Failed);
        eInvoice.RetryCount.ShouldBe(1);

        eInvoice.ResetForRetry();
        eInvoice.Status.ShouldBe(EInvoiceStatus.Pending);

        eInvoice.MarkProcessing();
        eInvoice.MarkIssued("EXT-RETRY", "EINV-RETRY-001");
        eInvoice.Status.ShouldBe(EInvoiceStatus.Issued);
        eInvoice.RetryCount.ShouldBe(1); // RetryCount preserved
    }
}
