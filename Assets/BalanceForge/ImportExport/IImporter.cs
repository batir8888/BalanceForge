using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    public interface IImporter
    {
        BalanceTable Import(string filePath);
        bool CanImport(string filePath);
    }
}