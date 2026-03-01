using KCharge.Faults;
using KCharge.Notifications;
using KCharge.Payments;
using KCharge.Sessions;
using KCharge.Stations;
using KCharge.Tariffs;
using KCharge.Users;
using KCharge.Vehicles;
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

namespace KCharge.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class KChargeDbContext :
    AbpDbContext<KChargeDbContext>,
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

    #region KCharge Entities

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

    // Faults
    public DbSet<Fault> Faults { get; set; }

    // Notifications
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<Alert> Alerts { get; set; }

    // Users
    public DbSet<AppUser> AppUsers { get; set; }

    #endregion

    public KChargeDbContext(DbContextOptions<KChargeDbContext> options)
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

        // Configure KCharge entities
        builder.ConfigureKChargeEntities();
    }
}

public static class KChargeDbContextModelCreatingExtensions
{
    public static void ConfigureKChargeEntities(this ModelBuilder builder)
    {
        // ChargingStation
        builder.Entity<ChargingStation>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "ChargingStations", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.StationCode).IsRequired().HasMaxLength(50);
            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Address).IsRequired().HasMaxLength(500);
            b.Property(x => x.FirmwareVersion).HasMaxLength(50);
            b.Property(x => x.Model).HasMaxLength(100);
            b.Property(x => x.Vendor).HasMaxLength(100);
            b.Property(x => x.SerialNumber).HasMaxLength(100);

            b.HasIndex(x => x.StationCode).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StationGroupId);
            b.HasIndex(x => x.IsEnabled);

            b.HasMany(x => x.Connectors)
                .WithOne(x => x.Station)
                .HasForeignKey(x => x.StationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Connector
        builder.Entity<Connector>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "Connectors", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.MaxPowerKw).HasPrecision(10, 2);

            b.HasIndex(x => new { x.StationId, x.ConnectorNumber }).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.IsEnabled);
        });

        // StationGroup
        builder.Entity<StationGroup>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "StationGroups", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Region).HasMaxLength(100);

            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.IsActive);
        });

        // StatusChangeLog
        builder.Entity<StatusChangeLog>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "StatusChangeLogs", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "ChargingSessions", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.TotalEnergyKwh).HasPrecision(10, 3);
            b.Property(x => x.TotalCost).HasPrecision(18, 0);
            b.Property(x => x.RatePerKwh).HasPrecision(18, 2);
            b.Property(x => x.StopReason).HasMaxLength(200);
            b.Property(x => x.IdTag).HasMaxLength(50);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.StationId);
            b.HasIndex(x => x.OcppTransactionId);
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StartTime);
            b.HasIndex(x => x.EndTime);

            b.HasMany(x => x.MeterValues)
                .WithOne()
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MeterValue
        builder.Entity<MeterValue>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "MeterValues", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.EnergyKwh).HasPrecision(10, 3);
            b.Property(x => x.CurrentAmps).HasPrecision(10, 2);
            b.Property(x => x.VoltageVolts).HasPrecision(10, 2);
            b.Property(x => x.PowerKw).HasPrecision(10, 3);
            b.Property(x => x.SocPercent).HasPrecision(5, 2);

            b.HasIndex(x => x.SessionId);
            b.HasIndex(x => x.Timestamp);
        });

        // TariffPlan
        builder.Entity<TariffPlan>(b =>
        {
            b.ToTable(KChargeConsts.DbTablePrefix + "TariffPlans", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "Vehicles", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "PaymentTransactions", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "Invoices", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "EInvoices", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "Faults", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "Notifications", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "Alerts", KChargeConsts.DbSchema);
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
            b.ToTable(KChargeConsts.DbTablePrefix + "AppUsers", KChargeConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
            b.Property(x => x.PhoneNumber).HasMaxLength(20);
            b.Property(x => x.Email).HasMaxLength(256);
            b.Property(x => x.AvatarUrl).HasMaxLength(500);
            b.Property(x => x.PreferredLanguage).HasMaxLength(10);
            b.Property(x => x.FcmToken).HasMaxLength(500);
            b.Property(x => x.WalletBalance).HasPrecision(18, 0);

            b.HasIndex(x => x.IdentityUserId).IsUnique();
            b.HasIndex(x => x.PhoneNumber);
            b.HasIndex(x => x.Email);
            b.HasIndex(x => x.IsActive);
        });
    }
}
