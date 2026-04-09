using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using KLC.Enums;
using KLC.Payments;
using KLC.Permissions;
using KLC.Sessions;
using KLC.Stations;
using KLC.Notifications;
using KLC.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace KLC.EInvoices;

[Authorize(KLCPermissions.EInvoices.Default)]
public class EInvoiceAppService : KLCAppService, IEInvoiceAppService
{
    private readonly IRepository<EInvoice, Guid> _eInvoiceRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;
    private readonly IRepository<AppUser, Guid> _appUserRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly IPushNotificationService _pushNotificationService;

    public EInvoiceAppService(
        IRepository<EInvoice, Guid> eInvoiceRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository,
        IRepository<AppUser, Guid> appUserRepository,
        IAsyncQueryableExecuter asyncExecuter,
        IPushNotificationService pushNotificationService)
    {
        _eInvoiceRepository = eInvoiceRepository;
        _invoiceRepository = invoiceRepository;
        _paymentRepository = paymentRepository;
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
        _appUserRepository = appUserRepository;
        _asyncExecuter = asyncExecuter;
        _pushNotificationService = pushNotificationService;
    }

    public async Task<EInvoiceDetailDto> GetAsync(Guid id)
    {
        var eInvoice = await _eInvoiceRepository.GetAsync(id);
        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.Id == eInvoice.InvoiceId);

        if (invoice == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.InvoiceNotFound);
        }

        var payment = await _paymentRepository.FirstOrDefaultAsync(p => p.Id == invoice.PaymentTransactionId);
        ChargingSession? session = null;
        ChargingStation? station = null;
        AppUser? user = null;

        if (payment != null)
        {
            session = await _sessionRepository.FirstOrDefaultAsync(s => s.Id == payment.SessionId);
            if (session != null)
            {
                station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == session.StationId);
            }
            user = await _appUserRepository.FirstOrDefaultAsync(u => u.IdentityUserId == payment.UserId);
        }

        return new EInvoiceDetailDto
        {
            Id = eInvoice.Id,
            InvoiceId = eInvoice.InvoiceId,
            Provider = eInvoice.Provider,
            ExternalInvoiceId = eInvoice.ExternalInvoiceId,
            EInvoiceNumber = eInvoice.EInvoiceNumber,
            Status = eInvoice.Status,
            ViewUrl = eInvoice.ViewUrl,
            PdfUrl = eInvoice.PdfUrl,
            SignatureHash = eInvoice.SignatureHash,
            IssuedAt = eInvoice.IssuedAt,
            ErrorMessage = eInvoice.ErrorMessage,
            RetryCount = eInvoice.RetryCount,
            CreationTime = eInvoice.CreationTime,
            InvoiceNumber = invoice.InvoiceNumber,
            TotalAmount = invoice.TotalAmount,
            StationName = station?.Name,
            BuyerName = user?.FullName,
            BuyerEmail = user?.Email,
            BuyerPhone = user?.PhoneNumber,
            SessionId = session?.Id,
            SessionStartTime = session?.StartTime,
            SessionEndTime = session?.EndTime,
            EnergyKwh = session?.TotalEnergyKwh,
            RatePerKwh = session?.RatePerKwh,
            BaseAmount = invoice.BaseAmount,
            TaxAmount = invoice.TaxAmount,
            TaxRatePercent = invoice.TaxRatePercent
        };
    }

    public async Task<EInvoiceDto?> GetByInvoiceIdAsync(Guid invoiceId)
    {
        var eInvoice = await _eInvoiceRepository.FirstOrDefaultAsync(e => e.InvoiceId == invoiceId);
        if (eInvoice == null)
        {
            return null;
        }

        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.Id == invoiceId);
        var payment = invoice != null
            ? await _paymentRepository.FirstOrDefaultAsync(p => p.Id == invoice.PaymentTransactionId)
            : null;
        ChargingSession? session = null;
        ChargingStation? station = null;

        if (payment != null)
        {
            session = await _sessionRepository.FirstOrDefaultAsync(s => s.Id == payment.SessionId);
            if (session != null)
            {
                station = await _stationRepository.FirstOrDefaultAsync(s => s.Id == session.StationId);
            }
        }

        return new EInvoiceDto
        {
            Id = eInvoice.Id,
            InvoiceId = eInvoice.InvoiceId,
            Provider = eInvoice.Provider,
            ExternalInvoiceId = eInvoice.ExternalInvoiceId,
            EInvoiceNumber = eInvoice.EInvoiceNumber,
            Status = eInvoice.Status,
            ViewUrl = eInvoice.ViewUrl,
            PdfUrl = eInvoice.PdfUrl,
            SignatureHash = eInvoice.SignatureHash,
            IssuedAt = eInvoice.IssuedAt,
            ErrorMessage = eInvoice.ErrorMessage,
            RetryCount = eInvoice.RetryCount,
            CreationTime = eInvoice.CreationTime,
            InvoiceNumber = invoice?.InvoiceNumber,
            TotalAmount = invoice?.TotalAmount,
            StationName = station?.Name
        };
    }

    public async Task<PagedResultDto<EInvoiceListDto>> GetListAsync(GetEInvoiceListDto input)
    {
        var query = await _eInvoiceRepository.GetQueryableAsync();

        // Apply filters
        if (input.Status.HasValue)
        {
            query = query.Where(e => e.Status == input.Status.Value);
        }

        if (input.Provider.HasValue)
        {
            query = query.Where(e => e.Provider == input.Provider.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(e => e.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(e => e.CreationTime <= input.ToDate.Value);
        }

        // Cursor-based pagination
        if (input.Cursor.HasValue)
        {
            query = query.Where(e => e.Id.CompareTo(input.Cursor.Value) > 0);
        }

        var totalCount = await _asyncExecuter.CountAsync(query);

        var eInvoices = await _asyncExecuter.ToListAsync(
            query
                .OrderByDescending(e => e.CreationTime)
                .Take(input.MaxResultCount));

        var invoiceIds = eInvoices.Select(e => e.InvoiceId).ToList();
        var invoiceQuery = await _invoiceRepository.GetQueryableAsync();
        var invoices = await _asyncExecuter.ToListAsync(
            invoiceQuery.Where(i => invoiceIds.Contains(i.Id)));

        var dtos = eInvoices.Select(eInvoice =>
        {
            var invoice = invoices.FirstOrDefault(i => i.Id == eInvoice.InvoiceId);
            return new EInvoiceListDto
            {
                Id = eInvoice.Id,
                InvoiceId = eInvoice.InvoiceId,
                InvoiceNumber = invoice?.InvoiceNumber,
                Provider = eInvoice.Provider,
                EInvoiceNumber = eInvoice.EInvoiceNumber,
                Status = eInvoice.Status,
                IssuedAt = eInvoice.IssuedAt,
                RetryCount = eInvoice.RetryCount,
                TotalAmount = invoice?.TotalAmount,
                CreationTime = eInvoice.CreationTime
            };
        }).ToList();

        return new PagedResultDto<EInvoiceListDto>(totalCount, dtos);
    }

    [Authorize(KLCPermissions.EInvoices.Generate)]
    public async Task<EInvoiceResultDto> GenerateAsync(CreateEInvoiceDto input)
    {
        // Check if invoice exists
        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.Id == input.InvoiceId);
        if (invoice == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.InvoiceNotFound);
        }

        // Check if e-invoice already exists
        var existingEInvoice = await _eInvoiceRepository.FirstOrDefaultAsync(e => e.InvoiceId == input.InvoiceId);
        if (existingEInvoice != null)
        {
            throw new BusinessException(KLCDomainErrorCodes.EInvoiceAlreadyExists)
                .WithData("InvoiceId", input.InvoiceId);
        }

        // Create new e-invoice
        var eInvoice = new EInvoice(
            GuidGenerator.Create(),
            input.InvoiceId,
            input.Provider);

        // Mark as processing
        eInvoice.MarkProcessing();

        // In a real implementation, this would call the e-invoice provider API
        // For now, we simulate the process
        try
        {
            // Simulate API call to e-invoice provider
            var result = await SimulateEInvoiceGeneration(eInvoice, invoice);

            if (result.Success)
            {
                eInvoice.MarkIssued(
                    result.ExternalId!,
                    result.EInvoiceNumber!,
                    result.ViewUrl,
                    result.PdfUrl,
                    result.SignatureHash);
            }
            else
            {
                eInvoice.MarkFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            eInvoice.MarkFailed(ex.Message);
        }

        await _eInvoiceRepository.InsertAsync(eInvoice);

        // Send push notification when e-invoice is successfully issued
        if (eInvoice.Status == EInvoiceStatus.Issued)
        {
            try
            {
                var payment = await _paymentRepository.FirstOrDefaultAsync(p => p.Id == invoice.PaymentTransactionId);
                if (payment != null)
                {
                    await _pushNotificationService.SendToUserAsync(
                        payment.UserId,
                        "Hóa đơn điện tử đã sẵn sàng",
                        $"Hóa đơn #{eInvoice.EInvoiceNumber} cho phiên sạc của bạn đã sẵn sàng xem.",
                        new Dictionary<string, string>
                        {
                            ["type"] = "einvoice_ready",
                            ["eInvoiceId"] = eInvoice.Id.ToString(),
                            ["viewUrl"] = eInvoice.ViewUrl ?? string.Empty
                        },
                        NotificationType.EInvoiceReady);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to send EInvoiceReady push notification for e-invoice {EInvoiceId}", eInvoice.Id);
            }
        }

        return new EInvoiceResultDto
        {
            EInvoiceId = eInvoice.Id,
            Status = eInvoice.Status,
            EInvoiceNumber = eInvoice.EInvoiceNumber,
            ViewUrl = eInvoice.ViewUrl,
            PdfUrl = eInvoice.PdfUrl,
            ErrorMessage = eInvoice.ErrorMessage
        };
    }

    [Authorize(KLCPermissions.EInvoices.Retry)]
    public async Task<EInvoiceResultDto> RetryAsync(Guid id)
    {
        var eInvoice = await _eInvoiceRepository.GetAsync(id);

        if (!eInvoice.CanRetry())
        {
            throw new BusinessException(KLCDomainErrorCodes.EInvoiceCannotRetry)
                .WithData("RetryCount", eInvoice.RetryCount);
        }

        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.Id == eInvoice.InvoiceId);
        if (invoice == null)
        {
            throw new BusinessException(KLCDomainErrorCodes.InvoiceNotFound);
        }

        // Reset and retry
        eInvoice.ResetForRetry();
        eInvoice.MarkProcessing();

        try
        {
            var result = await SimulateEInvoiceGeneration(eInvoice, invoice);

            if (result.Success)
            {
                eInvoice.MarkIssued(
                    result.ExternalId!,
                    result.EInvoiceNumber!,
                    result.ViewUrl,
                    result.PdfUrl,
                    result.SignatureHash);
            }
            else
            {
                eInvoice.MarkFailed(result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            eInvoice.MarkFailed(ex.Message);
        }

        await _eInvoiceRepository.UpdateAsync(eInvoice);

        return new EInvoiceResultDto
        {
            EInvoiceId = eInvoice.Id,
            Status = eInvoice.Status,
            EInvoiceNumber = eInvoice.EInvoiceNumber,
            ViewUrl = eInvoice.ViewUrl,
            PdfUrl = eInvoice.PdfUrl,
            ErrorMessage = eInvoice.ErrorMessage
        };
    }

    [Authorize(KLCPermissions.EInvoices.Cancel)]
    public async Task CancelAsync(Guid id)
    {
        var eInvoice = await _eInvoiceRepository.GetAsync(id);

        if (eInvoice.Status == EInvoiceStatus.Cancelled)
        {
            throw new BusinessException(KLCDomainErrorCodes.EInvoiceAlreadyCancelled);
        }

        if (eInvoice.Status == EInvoiceStatus.Issued)
        {
            // In a real implementation, this would call the provider's cancellation API
            // For now, we just mark it as cancelled
        }

        eInvoice.MarkCancelled();
        await _eInvoiceRepository.UpdateAsync(eInvoice);
    }

    public async Task<string?> GetPdfUrlAsync(Guid id)
    {
        var eInvoice = await _eInvoiceRepository.GetAsync(id);
        return eInvoice.PdfUrl;
    }

    /// <summary>
    /// Simulates e-invoice generation. In production, this would call the actual provider API.
    /// </summary>
    private Task<EInvoiceGenerationResult> SimulateEInvoiceGeneration(EInvoice eInvoice, Invoice invoice)
    {
        // Simulate different providers
        var providerPrefix = eInvoice.Provider switch
        {
            EInvoiceProvider.MISA => "MISA",
            EInvoiceProvider.Viettel => "VT",
            EInvoiceProvider.VNPT => "VNPT",
            _ => "UNK"
        };

        var result = new EInvoiceGenerationResult
        {
            Success = true,
            ExternalId = $"{providerPrefix}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}",
            EInvoiceNumber = $"EINV-{DateTime.UtcNow:yyyy}-{new Random().Next(100000, 999999):D6}",
            ViewUrl = $"https://einvoice.{providerPrefix.ToLower()}.vn/view/{Guid.NewGuid()}",
            PdfUrl = $"https://einvoice.{providerPrefix.ToLower()}.vn/pdf/{Guid.NewGuid()}",
            SignatureHash = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        };

        return Task.FromResult(result);
    }

    private class EInvoiceGenerationResult
    {
        public bool Success { get; set; }
        public string? ExternalId { get; set; }
        public string? EInvoiceNumber { get; set; }
        public string? ViewUrl { get; set; }
        public string? PdfUrl { get; set; }
        public string? SignatureHash { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
