using System.IO;
using System.Text;
using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    /// <summary>
    /// Экспортер таблицы баланса в формат CSV (Comma-Separated Values).
    /// Преобразует данные таблицы в текстовый формат CSV с заголовками столбцов и строками данных.
    /// Реализует интерфейс IExporter.
    /// </summary>
    public class CSVExporter : IExporter
    {
        /// <summary>
        /// Экспортирует таблицу баланса в CSV файл.
        /// </summary>
        /// <param name="table">Таблица баланса для экспорта.</param>
        /// <param name="filePath">Полный путь к файлу где сохранить CSV.</param>
        /// <returns>true если экспорт выполнен успешно, иначе false при ошибке.</returns>
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
        
        /// <summary>
        /// Преобразует таблицу баланса в строку CSV формата.
        /// Первая строка содержит названия столбцов (DisplayName), остальные строки содержат данные.
        /// Значения разделены запятыми, null значения экспортируются как пустые строки.
        /// </summary>
        /// <param name="table">Таблица баланса для преобразования.</param>
        /// <returns>Строка в формате CSV с заголовками и данными.</returns>
        private string ConvertToCSV(BalanceTable table)
        {
            var sb = new StringBuilder();

            // Header
            for (int i = 0; i < table.Columns.Count; i++)
            {
                sb.Append(EscapeCSVField(table.Columns[i].DisplayName));
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
                    sb.Append(EscapeCSVField(value?.ToString() ?? ""));
                    if (i < table.Columns.Count - 1)
                        sb.Append(",");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Экранирует значение поля по стандарту RFC 4180.
        /// Оборачивает в кавычки если поле содержит запятую, кавычку или перенос строки.
        /// Удваивает кавычки внутри поля.
        /// </summary>
        private static string EscapeCSVField(string field)
        {
            if (field.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            return field;
        }
    }
    
    /// <summary>
    /// Интерфейс для экспортеров данных таблиц баланса в различные форматы файлов.
    /// Позволяет расширять функциональность добавлением новых экспортеров (JSON, XML и т.д.).
    /// </summary>
    public interface IExporter
    {
        /// <summary>
        /// Экспортирует таблицу баланса в файл.
        /// </summary>
        /// <param name="table">Таблица баланса для экспорта.</param>
        /// <param name="filePath">Путь к файлу для сохранения экспортированных данных.</param>
        /// <returns>true если экспорт выполнен успешно, иначе false.</returns>
        bool Export(BalanceTable table, string filePath);
    }
}