using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using KLC.ChargingStations;
using KLC.ChargingSessions;
using KLC.Authorization;
using KLC.Faults;

namespace KLC.EntityFrameworkCore;

public class KlcDbContext : AbpDbContext<KlcDbContext>
{
    public DbSet<ChargingStation> ChargingStations { get; set; }
    public DbSet<Connector> Connectors { get; set; }
    public DbSet<ChargingSession> ChargingSessions { get; set; }
    public DbSet<MeterValue> MeterValues { get; set; }
    public DbSet<IdTag> IdTags { get; set; }
    public DbSet<Fault> Faults { get; set; }

    public KlcDbContext(DbContextOptions<KlcDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(KlcDbContext).Assembly);
    }
}
