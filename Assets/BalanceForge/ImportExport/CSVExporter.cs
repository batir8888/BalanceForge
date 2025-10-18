using System.IO;
using System.Text;
using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    public class CSVExporter : IExporter
    {
        public bool Export(BalanceTable table, string filePath)
        {
            try
            {
                var csv = ConvertToCSV(table);
                File.WriteAllText(filePath, csv);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private string ConvertToCSV(BalanceTable table)
        {
            var sb = new StringBuilder();
            
            // Header
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(table.Columns[i].DisplayName);
                if (i < table.Columns.Count - 1)
                    sb.Append(",");
            }
            sb.AppendLine();
            
            // Rows
            foreach (var row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    var value = row.GetValue(table.Columns[i].ColumnId);
                    sb.Append(value?.ToString() ?? "");
                    if (i < table.Columns.Count - 1)
                        sb.Append(",");
                }
                sb.AppendLine();
            }
            
            return sb.ToString();
        }
    }
    
    public interface IExporter
    {
        bool Export(BalanceTable table, string filePath);
    }
}