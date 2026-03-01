using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KCharge.Enums;
using KCharge.Permissions;
using KCharge.Sessions;
using KCharge.Stations;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace KCharge.Payments;

[Authorize]
public class PaymentAppService : KChargeAppService, IPaymentAppService
{
    private readonly IRepository<PaymentTransaction, Guid> _paymentRepository;
    private readonly IRepository<Invoice, Guid> _invoiceRepository;
    private readonly IRepository<UserPaymentMethod, Guid> _paymentMethodRepository;
    private readonly IRepository<ChargingSession, Guid> _sessionRepository;
    private readonly IRepository<ChargingStation, Guid> _stationRepository;

    public PaymentAppService(
        IRepository<PaymentTransaction, Guid> paymentRepository,
        IRepository<Invoice, Guid> invoiceRepository,
        IRepository<UserPaymentMethod, Guid> paymentMethodRepository,
        IRepository<ChargingSession, Guid> sessionRepository,
        IRepository<ChargingStation, Guid> stationRepository)
    {
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _paymentMethodRepository = paymentMethodRepository;
        _sessionRepository = sessionRepository;
        _stationRepository = stationRepository;
    }

    public async Task<PaymentResultDto> ProcessPaymentAsync(ProcessPaymentDto input)
    {
        var userId = CurrentUser.GetId();
        var session = await _sessionRepository.GetAsync(input.SessionId);

        // Validate session belongs to user
        if (session.UserId != userId)
        {
            throw new BusinessException("MOD_008_001");
        }

        // Check session is completed
        if (session.Status != SessionStatus.Completed)
        {
            throw new BusinessException("MOD_008_001")
                .WithData("reason", "Session not completed");
        }

        // Check for existing payment
        var existingPayment = await _paymentRepository.FirstOrDefaultAsync(
            p => p.SessionId == input.SessionId &&
                 (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing || p.Status == PaymentStatus.Completed));

        if (existingPayment != null)
        {
            if (existingPayment.Status == PaymentStatus.Completed)
            {
                throw new BusinessException("MOD_008_005");
            }
            return new PaymentResultDto
            {
                PaymentId = existingPayment.Id,
                Status = existingPayment.Status,
                ReferenceCode = existingPayment.ReferenceCode
            };
        }

        // Create payment transaction
        var payment = new PaymentTransaction(
            GuidGenerator.Create(),
            input.SessionId,
            userId,
            input.Gateway,
            session.TotalCost
        );

        await _paymentRepository.InsertAsync(payment);

        // TODO: Call actual payment gateway API
        // For now, simulate gateway redirect URL
        var redirectUrl = input.Gateway switch
        {
            PaymentGateway.ZaloPay => $"https://zalopay.vn/pay?ref={payment.ReferenceCode}",
            PaymentGateway.MoMo => $"https://momo.vn/pay?ref={payment.ReferenceCode}",
            PaymentGateway.OnePay => $"https://onepay.vn/pay?ref={payment.ReferenceCode}",
            _ => null
        };

        return new PaymentResultDto
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            GatewayRedirectUrl = redirectUrl,
            ReferenceCode = payment.ReferenceCode
        };
    }

    public async Task<PaymentTransactionDto> GetAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var payment = await _paymentRepository.GetAsync(id);

        // Check ownership (unless admin)
        if (payment.UserId != userId && !await AuthorizationService.IsGrantedAsync(KChargePermissions.Payments.ViewAll))
        {
            throw new BusinessException("MOD_008_001");
        }

        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToDto(payment, station.Name, session.TotalEnergyKwh);
    }

    public async Task<PagedResultDto<PaymentListDto>> GetHistoryAsync(GetPaymentListDto input)
    {
        var userId = CurrentUser.GetId();
        var query = await _paymentRepository.GetQueryableAsync();

        // Filter by user unless admin
        if (!await AuthorizationService.IsGrantedAsync(KChargePermissions.Payments.ViewAll))
        {
            query = query.Where(p => p.UserId == userId);
        }

        if (input.Status.HasValue)
        {
            query = query.Where(p => p.Status == input.Status.Value);
        }

        if (input.Gateway.HasValue)
        {
            query = query.Where(p => p.Gateway == input.Gateway.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(p => p.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(p => p.CreationTime <= input.ToDate.Value);
        }

        if (input.Cursor.HasValue)
        {
            query = query.Where(p => p.Id.CompareTo(input.Cursor.Value) > 0);
        }

        query = query.OrderByDescending(p => p.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(query);
        var payments = await AsyncExecuter.ToListAsync(query.Take(input.MaxResultCount));

        // Get station names
        var sessionIds = payments.Select(p => p.SessionId).Distinct().ToList();
        var sessions = await _sessionRepository.GetListAsync(s => sessionIds.Contains(s.Id));
        var stationIds = sessions.Select(s => s.StationId).Distinct().ToList();
        var stations = await _stationRepository.GetListAsync(st => stationIds.Contains(st.Id));

        var sessionMap = sessions.ToDictionary(s => s.Id);
        var stationMap = stations.ToDictionary(st => st.Id, st => st.Name);

        var dtos = payments.Select(p =>
        {
            var session = sessionMap.TryGetValue(p.SessionId, out var s) ? s : null;
            var stationName = session != null && stationMap.TryGetValue(session.StationId, out var name) ? name : null;
            return new PaymentListDto
            {
                Id = p.Id,
                SessionId = p.SessionId,
                Gateway = p.Gateway,
                Amount = p.Amount,
                Status = p.Status,
                ReferenceCode = p.ReferenceCode,
                CreationTime = p.CreationTime,
                StationName = stationName
            };
        }).ToList();

        return new PagedResultDto<PaymentListDto>(totalCount, dtos);
    }

    public async Task<PaymentMethodDto> AddPaymentMethodAsync(CreatePaymentMethodDto input)
    {
        var userId = CurrentUser.GetId();

        // If setting as default, remove default from other methods
        if (input.IsDefault)
        {
            var existingMethods = await _paymentMethodRepository.GetListAsync(
                m => m.UserId == userId && m.IsDefault);
            foreach (var method in existingMethods)
            {
                method.RemoveDefault();
            }
            if (existingMethods.Any())
            {
                await _paymentMethodRepository.UpdateManyAsync(existingMethods);
            }
        }

        var paymentMethod = new UserPaymentMethod(
            GuidGenerator.Create(),
            userId,
            input.Gateway,
            input.DisplayName,
            input.TokenReference
        );

        if (input.IsDefault)
        {
            paymentMethod.SetAsDefault();
        }

        await _paymentMethodRepository.InsertAsync(paymentMethod);

        return MapToMethodDto(paymentMethod);
    }

    public async Task<List<PaymentMethodDto>> GetPaymentMethodsAsync()
    {
        var userId = CurrentUser.GetId();
        var methods = await _paymentMethodRepository.GetListAsync(
            m => m.UserId == userId && m.IsActive);

        return methods.Select(MapToMethodDto).ToList();
    }

    public async Task DeletePaymentMethodAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var method = await _paymentMethodRepository.GetAsync(id);

        if (method.UserId != userId)
        {
            throw new BusinessException("MOD_008_004");
        }

        method.Deactivate();
        await _paymentMethodRepository.UpdateAsync(method);
    }

    public async Task SetDefaultPaymentMethodAsync(Guid id)
    {
        var userId = CurrentUser.GetId();
        var method = await _paymentMethodRepository.GetAsync(id);

        if (method.UserId != userId)
        {
            throw new BusinessException("MOD_008_004");
        }

        // Remove default from other methods
        var otherMethods = await _paymentMethodRepository.GetListAsync(
            m => m.UserId == userId && m.IsDefault && m.Id != id);
        foreach (var other in otherMethods)
        {
            other.RemoveDefault();
        }
        if (otherMethods.Any())
        {
            await _paymentMethodRepository.UpdateManyAsync(otherMethods);
        }

        method.SetAsDefault();
        await _paymentMethodRepository.UpdateAsync(method);
    }

    public async Task<InvoiceDto> GetInvoiceAsync(Guid id)
    {
        var invoice = await _invoiceRepository.GetAsync(id);
        var payment = await _paymentRepository.GetAsync(invoice.PaymentTransactionId);
        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToInvoiceDto(invoice, station.Name, session.StartTime, session.EndTime);
    }

    public async Task<InvoiceDto?> GetInvoiceByPaymentAsync(Guid paymentId)
    {
        var invoice = await _invoiceRepository.FirstOrDefaultAsync(i => i.PaymentTransactionId == paymentId);
        if (invoice == null) return null;

        var payment = await _paymentRepository.GetAsync(paymentId);
        var session = await _sessionRepository.GetAsync(payment.SessionId);
        var station = await _stationRepository.GetAsync(session.StationId);

        return MapToInvoiceDto(invoice, station.Name, session.StartTime, session.EndTime);
    }

    [AllowAnonymous]
    public async Task HandleCallbackAsync(string gateway, PaymentCallbackDto callback)
    {
        // Find payment by reference code
        var payment = await _paymentRepository.FirstOrDefaultAsync(
            p => p.ReferenceCode == callback.ReferenceCode);

        if (payment == null)
        {
            throw new BusinessException("MOD_008_001")
                .WithData("referenceCode", callback.ReferenceCode);
        }

        // TODO: Validate callback signature based on gateway

        if (callback.Status == "success" || callback.Status == "completed")
        {
            payment.MarkCompleted(callback.TransactionId ?? "");

            // Generate invoice
            var session = await _sessionRepository.GetAsync(payment.SessionId);
            var invoiceNumber = Invoice.GenerateInvoiceNumber(await GetNextInvoiceSequenceAsync());
            var invoice = new Invoice(
                GuidGenerator.Create(),
                payment.Id,
                invoiceNumber,
                session.TotalEnergyKwh,
                session.RatePerKwh,
                10m // 10% VAT
            );

            await _invoiceRepository.InsertAsync(invoice);
        }
        else if (callback.Status == "failed" || callback.Status == "error")
        {
            payment.MarkFailed(callback.ErrorCode ?? "Payment failed");
        }

        await _paymentRepository.UpdateAsync(payment);
    }

    private async Task<int> GetNextInvoiceSequenceAsync()
    {
        var count = await _invoiceRepository.CountAsync();
        return (int)count + 1;
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction payment, string? stationName, decimal energyKwh)
    {
        return new PaymentTransactionDto
        {
            Id = payment.Id,
            SessionId = payment.SessionId,
            UserId = payment.UserId,
            Gateway = payment.Gateway,
            Amount = payment.Amount,
            Status = payment.Status,
            GatewayTransactionId = payment.GatewayTransactionId,
            ReferenceCode = payment.ReferenceCode,
            ErrorMessage = payment.ErrorMessage,
            CreationTime = payment.CreationTime,
            CompletedAt = payment.CompletedAt,
            StationName = stationName,
            EnergyKwh = energyKwh
        };
    }

    private static PaymentMethodDto MapToMethodDto(UserPaymentMethod method)
    {
        return new PaymentMethodDto
        {
            Id = method.Id,
            Gateway = method.Gateway,
            DisplayName = method.DisplayName,
            IsDefault = method.IsDefault,
            LastFourDigits = method.LastFourDigits,
            CreationTime = method.CreationTime
        };
    }

    private static InvoiceDto MapToInvoiceDto(Invoice invoice, string? stationName, DateTime? startTime, DateTime? endTime)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            PaymentTransactionId = invoice.PaymentTransactionId,
            InvoiceNumber = invoice.InvoiceNumber,
            EnergyKwh = invoice.EnergyKwh,
            BaseAmount = invoice.BaseAmount,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            TaxRatePercent = invoice.TaxRatePercent,
            RatePerKwh = invoice.RatePerKwh,
            IssuedAt = invoice.IssuedAt,
            StationName = stationName,
            SessionStartTime = startTime,
            SessionEndTime = endTime
        };
    }
}
