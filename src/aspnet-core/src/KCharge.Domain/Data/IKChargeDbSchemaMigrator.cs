using System.Threading.Tasks;

namespace KCharge.Data;

public interface IKChargeDbSchemaMigrator
{
    Task MigrateAsync();
}
