using KLC.Faults;
using KLC.Fleets;
using KLC.Maintenance;
using KLC.Marketing;
using KLC.Notifications;
using KLC.Ocpp;
using KLC.Operators;
using KLC.Payments;
using KLC.PowerSharing;
using KLC.Sessions;
using KLC.Stations;
using KLC.Support;
using KLC.Tariffs;
using KLC.Users;
using KLC.Vehicles;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace KLC.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class KLCDbContext :
    AbpDbContext<KLCDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    #region ABP Identity Entities

    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    #region KLC Entities

    // Stations
    public DbSet<ChargingStation> ChargingStations { get; set; }
    public DbSet<Connector> Connectors { get; set; }
    public DbSet<StationGroup> StationGroups { get; set; }
    public DbSet<StatusChangeLog> StatusChangeLogs { get; set; }

    // Sessions
    public DbSet<ChargingSession> ChargingSessions { get; set; }
    public DbSet<MeterValue> MeterValues { get; set; }

    // Tariffs
    public DbSet<TariffPlan> TariffPlans { get; set; }

    // Vehicles
    public DbSet<Vehicle> Vehicles { get; set; }

    // Payments
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<EInvoice> EInvoices { get; set; }
    public DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }

    // Faults
    public DbSet<Fault> Faults { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Alert> Alerts { get; set; }

    // Users
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<UserIdTag> UserIdTags { get; set; }
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    // Stations (extended)
    public DbSet<FavoriteStation> FavoriteStations { get; set; }
    public DbSet<StationAmenity> StationAmenities { get; set; }
    public DbSet<StationPhoto> StationPhotos { get; set; }

    // Wallet
    public DbSet<WalletTransaction> WalletTransactions { get; set; }

    // Notifications (extended)
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }

    // Marketing
    public DbSet<Voucher> Vouchers { get; set; }
    public DbSet<UserVoucher> UserVouchers { get; set; }
    public DbSet<Promotion> Promotions { get; set; }

    // Support
    public DbSet<UserFeedback> UserFeedbacks { get; set; }

    // OCPP
    public DbSet<OcppRawEvent> OcppRawEvents { get; set; }

    // Maintenance
    public DbSet<MaintenanceTask> MaintenanceTasks { get; set; }

    // Power Sharing
    public DbSet<PowerSharingGroup> PowerSharingGroups { get; set; }
    public DbSet<PowerSharingGroupMember> PowerSharingGroupMembers { get; set; }
    public DbSet<SiteLoadProfile> SiteLoadProfiles { get; set; }

    // Operators
    public DbSet<Operator> Operators { get; set; }
    public DbSet<OperatorStation> OperatorStations { get; set; }
    public DbSet<OperatorWebhookLog> OperatorWebhookLogs { get; set; }

    // Fleets
    public DbSet<Fleet> Fleets { get; set; }
    public DbSet<FleetVehicle> FleetVehicles { get; set; }
    public DbSet<FleetChargingSchedule> FleetChargingSchedules { get; set; }
    public DbSet<FleetAllowedStation> FleetAllowedStations { get; set; }

    #endregion

    public KLCDbContext(DbContextOptions<KLCDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ABP modules
        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();

        // Configure KLC entities
        builder.ConfigureKLCEntities();

        // PostGIS Point column — only supported on PostgreSQL; ignore on SQLite (tests)
        if (Database.IsNpgsql())
        {
            builder.Entity<ChargingStation>(b =>
            {
                b.Property(x => x.Location).HasColumnType("geography (point, 4326)");
                b.HasIndex(x => x.Location).HasMethod("gist");
            });
        }
        else
        {
            builder.Entity<ChargingStation>().Ignore(x => x.Location);
        }
    }
}

public static class KLCDbContextModelCreatingExtensions
{
    public static void ConfigureKLCEntities(this ModelBuilder builder)
    {
        // ChargingStation
        builder.Entity<ChargingStation>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "ChargingStations", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.StationCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Address).IsRequired().HasMaxLength(500);
            b.Property(x => x.FirmwareVersion).HasMaxLength(50);
            b.Property(x => x.Model).HasMaxLength(100);
            b.Property(x => x.Vendor).HasMaxLength(100);
            b.Property(x => x.SerialNumber).HasMaxLength(100);
            b.Property(x => x.VendorProfileName).HasMaxLength(100);
            b.Property(x => x.FirmwareUpdateStatus).HasMaxLength(50);
            b.Property(x => x.DiagnosticsStatus).HasMaxLength(50);

            b.HasIndex(x => x.StationCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StationGroupId);
            b.HasIndex(x => x.IsEnabled);

            b.HasMany(x => x.Connectors)
                .WithOne(x => x.Station)
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Amenities)
                .WithOne()
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Photos)
                .WithOne()
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Connector
        builder.Entity<Connector>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Connectors", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.MaxPowerKw).HasPrecision(10, 2);
            b.Property(x => x.QrCodeData).HasMaxLength(500);

            b.HasIndex(x => new { x.StationId, x.ConnectorNumber }).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.IsEnabled);
        });

        // StationGroup
        builder.Entity<StationGroup>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "StationGroups", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Region).HasMaxLength(100);

            b.HasOne(x => x.ParentGroup)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentGroupId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.GroupType);
            b.HasIndex(x => x.ParentGroupId);
        });

        // StatusChangeLog
        builder.Entity<StatusChangeLog>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "StatusChangeLogs", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PreviousStatus).IsRequired().HasMaxLength(50);
            b.Property(x => x.NewStatus).IsRequired().HasMaxLength(50);
            b.Property(x => x.Source).IsRequired().HasMaxLength(50);
            b.Property(x => x.Details).HasMaxLength(500);

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.Timestamp);
        });

        // ChargingSession
        builder.Entity<ChargingSession>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "ChargingSessions", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TotalEnergyKwh).HasPrecision(10, 3);
            b.Property(x => x.TotalCost).HasPrecision(18, 0);
            b.Property(x => x.RatePerKwh).HasPrecision(18, 2);
            b.Property(x => x.StopReason).HasMaxLength(200);
            b.Property(x => x.IdTag).HasMaxLength(50);

            b.HasIndex(x => new { x.UserId, x.Status }); // PERF-2: avoid full scans on active-session queries
            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.OcppTransactionId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StartTime);
            b.HasIndex(x => x.EndTime);

            // BUG-5: Prevent concurrent session starts — DB enforces one active session per user.
            // Statuses 0=Pending, 1=Starting, 2=InProgress map to their int values.
            b.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("\"Status\" IN (0, 1, 2)")
                .HasDatabaseName("IX_AppChargingSessions_UserId_Active");

            b.HasMany(x => x.MeterValues)
                .WithOne()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MeterValue
        builder.Entity<MeterValue>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "MeterValues", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EnergyKwh).HasPrecision(10, 3);
            b.Property(x => x.CurrentAmps).HasPrecision(10, 2);
            b.Property(x => x.VoltageVolts).HasPrecision(10, 2);
            b.Property(x => x.PowerKw).HasPrecision(10, 3);
            b.Property(x => x.SocPercent).HasPrecision(5, 2);

            b.HasIndex(x => new { x.SessionId, x.Timestamp }) // PERF-2: cover ORDER BY Timestamp DESC queries
                .HasDatabaseName("IX_AppMeterValues_SessionId_Timestamp");
            b.HasIndex(x => x.Timestamp);
        });

        // TariffPlan
        builder.Entity<TariffPlan>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "TariffPlans", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.BaseRatePerKwh).HasPrecision(18, 2);
            b.Property(x => x.TaxRatePercent).HasPrecision(5, 2);

            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.IsDefault);
            b.HasIndex(x => x.EffectiveFrom);
        });

        // Vehicle
        builder.Entity<Vehicle>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Vehicles", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Make).IsRequired().HasMaxLength(100);
            b.Property(x => x.Model).IsRequired().HasMaxLength(100);
            b.Property(x => x.LicensePlate).HasMaxLength(20);
            b.Property(x => x.Color).HasMaxLength(50);
            b.Property(x => x.Nickname).HasMaxLength(100);
            b.Property(x => x.BatteryCapacityKwh).HasPrecision(10, 2);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.LicensePlate);
            b.HasIndex(x => x.IsActive);
        });

        // PaymentTransaction
        builder.Entity<PaymentTransaction>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "PaymentTransactions", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasPrecision(18, 0);
            b.Property(x => x.GatewayTransactionId).HasMaxLength(100);
            b.Property(x => x.ReferenceCode).HasMaxLength(50);
            b.Property(x => x.ErrorMessage).HasMaxLength(500);

            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ReferenceCode);
            b.HasIndex(x => x.GatewayTransactionId);

            b.HasOne(x => x.Invoice)
                .WithOne(x => x.PaymentTransaction)
                .HasForeignKey<Invoice>(x => x.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Invoice
        builder.Entity<Invoice>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Invoices", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.InvoiceNumber).IsRequired().HasMaxLength(50);
            b.Property(x => x.EnergyKwh).HasPrecision(10, 3);
            b.Property(x => x.BaseAmount).HasPrecision(18, 0);
            b.Property(x => x.TaxAmount).HasPrecision(18, 0);
            b.Property(x => x.TotalAmount).HasPrecision(18, 0);
            b.Property(x => x.TaxRatePercent).HasPrecision(5, 2);
            b.Property(x => x.RatePerKwh).HasPrecision(18, 2);

            b.HasIndex(x => x.InvoiceNumber).IsUnique();
            b.HasIndex(x => x.IssuedAt);

            b.HasOne(x => x.EInvoice)
                .WithOne(x => x.Invoice)
                .HasForeignKey<EInvoice>(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EInvoice
        builder.Entity<EInvoice>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "EInvoices", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ExternalInvoiceId).HasMaxLength(100);
            b.Property(x => x.EInvoiceNumber).HasMaxLength(50);
            b.Property(x => x.ViewUrl).HasMaxLength(500);
            b.Property(x => x.PdfUrl).HasMaxLength(500);
            b.Property(x => x.SignatureHash).HasMaxLength(500);
            b.Property(x => x.ErrorMessage).HasMaxLength(500);

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ExternalInvoiceId);
        });

        // Fault
        builder.Entity<Fault>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Faults", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ErrorCode).IsRequired().HasMaxLength(100);
            b.Property(x => x.ErrorInfo).HasMaxLength(500);
            b.Property(x => x.VendorErrorCode).HasMaxLength(100);
            b.Property(x => x.ResolutionNotes).HasMaxLength(1000);

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.DetectedAt);
            b.HasIndex(x => x.Priority);
        });

        // Notification
        builder.Entity<Notification>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Notifications", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Body).IsRequired().HasMaxLength(1000);
            b.Property(x => x.Data).HasMaxLength(2000);
            b.Property(x => x.ActionUrl).HasMaxLength(500);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.IsRead);
            b.HasIndex(x => x.CreationTime);
        });

        // Alert
        builder.Entity<Alert>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Alerts", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Message).IsRequired().HasMaxLength(500);
            b.Property(x => x.ResolutionNotes).HasMaxLength(1000);
            b.Property(x => x.Data).HasMaxLength(2000);

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Type);
            b.HasIndex(x => x.Priority);
            b.HasIndex(x => x.CreationTime);
        });

        // AppUser
        builder.Entity<AppUser>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "AppUsers", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.PhoneNumber).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.AvatarUrl).HasMaxLength(500);
            b.Property(x => x.PreferredLanguage).HasMaxLength(10);
            b.Property(x => x.FcmToken).HasMaxLength(500);
            b.Property(x => x.WalletBalance).HasPrecision(18, 0);
            b.Property(x => x.Gender).HasMaxLength(1);

            // Optimistic concurrency on WalletBalance: uses PostgreSQL's built-in
            // xmin system column (updated on every row write — no extra column needed).
            // EF Core will include it in UPDATE WHERE clauses; a stale read throws
            // DbUpdateConcurrencyException, which callers should retry.
            b.Property<uint>("xmin").HasColumnType("xid").IsRowVersion().HasDefaultValue(0u);

            b.HasIndex(x => x.IdentityUserId).IsUnique();
            b.HasIndex(x => x.PhoneNumber).IsUnique().HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.Email);
            b.HasIndex(x => x.IsActive);
        });

        // UserIdTag
        builder.Entity<UserIdTag>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "UserIdTags", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.IdTag).IsRequired().HasMaxLength(50);
            b.Property(x => x.FriendlyName).HasMaxLength(100);

            b.HasIndex(x => x.IdTag).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.IsActive);
        });

        // UserPaymentMethod
        builder.Entity<UserPaymentMethod>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "UserPaymentMethods", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
            b.Property(x => x.TokenReference).IsRequired().HasMaxLength(500);
            b.Property(x => x.LastFourDigits).HasMaxLength(4);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => new { x.UserId, x.IsDefault }).HasFilter("\"IsDefault\" = true");
        });

        // DeviceToken
        builder.Entity<DeviceToken>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "DeviceTokens", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Token).IsRequired().HasMaxLength(500);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Token).IsUnique();
            b.HasIndex(x => x.IsActive);
        });

        // NotificationPreference
        builder.Entity<NotificationPreference>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "NotificationPreferences", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.UserId).IsUnique();
        });

        // WalletTransaction
        builder.Entity<WalletTransaction>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "WalletTransactions", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Amount).HasPrecision(18, 0);
            b.Property(x => x.BalanceAfter).HasPrecision(18, 0);
            b.Property(x => x.GatewayTransactionId).HasMaxLength(100);
            b.Property(x => x.ReferenceCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Type);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ReferenceCode);
            b.HasIndex(x => x.CreationTime);
            b.HasIndex(x => x.RelatedSessionId);
        });

        // FavoriteStation
        builder.Entity<FavoriteStation>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "FavoriteStations", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.UserId, x.StationId }).IsUnique();
            b.HasIndex(x => x.UserId);
        });

        // StationAmenity
        builder.Entity<StationAmenity>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "StationAmenities", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => new { x.StationId, x.AmenityType }).IsUnique();
        });

        // StationPhoto
        builder.Entity<StationPhoto>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "StationPhotos", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Url).IsRequired().HasMaxLength(500);
            b.Property(x => x.ThumbnailUrl).HasMaxLength(500);

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => new { x.StationId, x.IsPrimary }).HasFilter("\"IsPrimary\" = true");
        });

        // Voucher
        builder.Entity<Voucher>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Vouchers", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Code).IsRequired().HasMaxLength(50);
            b.Property(x => x.Value).HasPrecision(18, 2);
            b.Property(x => x.MinOrderAmount).HasPrecision(18, 0);
            b.Property(x => x.MaxDiscountAmount).HasPrecision(18, 0);
            b.Property(x => x.Description).HasMaxLength(500);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.ExpiryDate);
            b.HasIndex(x => x.PromotionId);
        });

        // UserVoucher
        builder.Entity<UserVoucher>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "UserVouchers", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.UserId, x.VoucherId }).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.VoucherId);
        });

        // Promotion
        builder.Entity<Promotion>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Promotions", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.ImageUrl).HasMaxLength(500);

            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.StartDate);
            b.HasIndex(x => x.EndDate);
            b.HasIndex(x => x.Type);
        });

        // UserFeedback
        builder.Entity<UserFeedback>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "UserFeedbacks", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Subject).IsRequired().HasMaxLength(200);
            b.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            b.Property(x => x.AdminResponse).HasMaxLength(2000);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Type);
            b.HasIndex(x => x.CreationTime);
        });

        // OcppRawEvent
        builder.Entity<OcppRawEvent>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "OcppRawEvents", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.ChargePointId).IsRequired().HasMaxLength(64);
            b.Property(x => x.Action).IsRequired().HasMaxLength(50);
            b.Property(x => x.UniqueId).IsRequired().HasMaxLength(50);
            b.Property(x => x.Payload).IsRequired().HasColumnType("jsonb");

            b.HasIndex(x => x.ChargePointId);
            b.HasIndex(x => x.Action);
            b.HasIndex(x => x.ReceivedAt);
            b.HasIndex(x => new { x.ChargePointId, x.ReceivedAt });
        });

        // MaintenanceTask
        builder.Entity<MaintenanceTask>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "MaintenanceTasks", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.AssignedTo).IsRequired().HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(2000);

            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.ScheduledDate);
        });

        // PowerSharingGroup
        builder.Entity<PowerSharingGroup>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "PowerSharingGroups", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.MaxCapacityKw).HasPrecision(10, 2);
            b.Property(x => x.MinPowerPerConnectorKw).HasPrecision(10, 2);

            b.HasIndex(x => x.IsActive);
            b.HasIndex(x => x.Mode);
            b.HasIndex(x => x.StationGroupId);

            b.HasMany(x => x.Members)
                .WithOne(x => x.Group)
                .HasForeignKey(x => x.PowerSharingGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PowerSharingGroupMember
        builder.Entity<PowerSharingGroupMember>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "PowerSharingGroupMembers", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.AllocatedPowerKw).HasPrecision(10, 2);

            b.HasIndex(x => x.PowerSharingGroupId);
            b.HasIndex(x => x.ConnectorId);
            b.HasIndex(x => x.StationId);
            b.HasIndex(x => new { x.PowerSharingGroupId, x.ConnectorId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        });

        // SiteLoadProfile
        builder.Entity<SiteLoadProfile>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "SiteLoadProfiles", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TotalLoadKw).HasPrecision(10, 2);
            b.Property(x => x.AvailableCapacityKw).HasPrecision(10, 2);
            b.Property(x => x.PeakLoadKw).HasPrecision(10, 2);

            b.HasIndex(x => x.PowerSharingGroupId);
            b.HasIndex(x => x.Timestamp);
            b.HasIndex(x => new { x.PowerSharingGroupId, x.Timestamp });
        });

        // Operator
        builder.Entity<Operator>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Operators", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.ApiKeyHash).IsRequired().HasMaxLength(128);
            b.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
            b.Property(x => x.WebhookUrl).HasMaxLength(500);
            b.Property(x => x.Description).HasMaxLength(1000);

            b.HasIndex(x => x.Name).IsUnique();
            b.HasIndex(x => x.ApiKeyHash);
            b.HasIndex(x => x.IsActive);

            b.HasMany(x => x.AllowedStations)
                .WithOne()
                .HasForeignKey(x => x.OperatorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // OperatorStation
        builder.Entity<OperatorStation>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "OperatorStations", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.OperatorId, x.StationId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.OperatorId);
            b.HasIndex(x => x.StationId);
        });

        // OperatorWebhookLog
        builder.Entity<OperatorWebhookLog>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "OperatorWebhookLogs", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.PayloadJson).IsRequired();
            b.Property(x => x.ErrorMessage).HasMaxLength(1000);

            b.HasIndex(x => x.OperatorId);
            b.HasIndex(x => x.EventType);
            b.HasIndex(x => x.CreationTime);
        });

        // Fleet
        builder.Entity<Fleet>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "Fleets", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.MaxMonthlyBudgetVnd).HasPrecision(18, 2);
            b.Property(x => x.CurrentMonthSpentVnd).HasPrecision(18, 2);

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.OperatorUserId);
            b.HasIndex(x => x.IsActive);

            b.HasMany(x => x.Vehicles)
                .WithOne()
                .HasForeignKey(x => x.FleetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FleetVehicle
        builder.Entity<FleetVehicle>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "FleetVehicles", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.DailyChargingLimitKwh).HasPrecision(18, 2);
            b.Property(x => x.CurrentDayEnergyKwh).HasPrecision(18, 2);
            b.Property(x => x.CurrentMonthEnergyKwh).HasPrecision(18, 2);

            b.HasIndex(x => new { x.FleetId, x.VehicleId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.FleetId);
            b.HasIndex(x => x.VehicleId);
            b.HasIndex(x => x.DriverUserId);
            b.HasIndex(x => x.IsActive);
        });

        // FleetChargingSchedule
        builder.Entity<FleetChargingSchedule>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "FleetChargingSchedules", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.FleetId, x.DayOfWeek });
            b.HasIndex(x => x.FleetId);
        });

        // FleetAllowedStation
        builder.Entity<FleetAllowedStation>(b =>
        {
            b.ToTable(KLCConsts.DbTablePrefix + "FleetAllowedStations", KLCConsts.DbSchema);
            b.ConfigureByConvention();

            b.HasIndex(x => new { x.FleetId, x.StationGroupId }).IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            b.HasIndex(x => x.FleetId);
            b.HasIndex(x => x.StationGroupId);
        });
    }
}
