using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    public interface IExporter
    {
        bool Export(BalanceTable table, string filePath);
    }
}