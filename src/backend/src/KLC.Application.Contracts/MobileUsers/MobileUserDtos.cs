using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KLC.Enums;

namespace KLC.MobileUsers;

public class MobileUserListDto
{
    public Guid Id { get; set; }
    public Guid IdentityUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public decimal WalletBalance { get; set; }
    public MembershipTier MembershipTier { get; set; }
    public bool IsPhoneVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileUserDetailDto
{
    public Guid Id { get; set; }
    public Guid IdentityUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public decimal WalletBalance { get; set; }
    public MembershipTier MembershipTier { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastTopUpAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SessionCount { get; set; }
    public decimal TotalSpent { get; set; }
}

public class MobileUserSessionDto
{
    public Guid Id { get; set; }
    public Guid StationId { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal TotalEnergyKwh { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileUserTransactionDto
{
    public Guid Id { get; set; }
    public WalletTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
    public string? ReferenceCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MobileUserStatisticsDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Suspended { get; set; }
}

public class GetMobileUserListDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class GetMobileUserSessionsDto
{
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class GetMobileUserTransactionsDto
{
    public Guid? Cursor { get; set; }

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

public class WalletAdjustDto
{
    [Required]
    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}

public class WalletAdjustResultDto
{
    public decimal NewBalance { get; set; }
    public Guid TransactionId { get; set; }
}

public class CursorPagedResultDto<T>
{
    public List<T> Data { get; set; } = new();
    public CursorPaginationDto Pagination { get; set; } = new();
}

public class CursorPaginationDto
{
    public Guid? NextCursor { get; set; }
    public bool HasMore { get; set; }
    public int PageSize { get; set; }
}
