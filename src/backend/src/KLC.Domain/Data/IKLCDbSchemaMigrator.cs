using System.Threading.Tasks;

namespace KLC.Data;

public interface IKLCDbSchemaMigrator
{
    Task MigrateAsync();
}
