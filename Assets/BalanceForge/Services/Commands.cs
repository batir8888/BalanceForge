using BalanceForge.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace BalanceForge.Services
{
    /// <summary>
    /// Интерфейс для команд поддерживающих операции Undo/Redo.
    /// Каждая команда инкапсулирует действие которое можно выполнить и отменить.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Выполняет команду и применяет изменения к таблице.
        /// </summary>
        void Execute();
        
        /// <summary>
        /// Отменяет команду восстанавливая предыдущее состояние.
        /// </summary>
        void Undo();
        
        /// <summary>
        /// Получает описание команды для отображения в UI (например, в истории Undo/Redo).
        /// </summary>
        /// <returns>Строка с кратким описанием действия команды.</returns>
        string GetDescription();
    }
    
    /// <summary>
    /// Команда для добавления новой строки в таблицу баланса.
    /// Реализует интерфейс ICommand для поддержки Undo/Redo операций.
    /// </summary>
    public class AddRowCommand : ICommand
    {
        /// <summary>
        /// Таблица баланса в которую добавляется строка.
        /// </summary>
        private BalanceTable table;
        
        /// <summary>
        /// Строка которая будет добавлена.
        /// </summary>
        private BalanceRow row;
        
        /// <summary>
        /// Инициализирует новый экземпляр команды добавления строки.
        /// </summary>
        /// <param name="table">Таблица баланса для добавления строки.</param>
        /// <param name="row">Строка для добавления.</param>
        public AddRowCommand(BalanceTable table, BalanceRow row)
        {
            this.table = table;
            this.row = row;
        }
        
        /// <summary>
        /// Выполняет добавление строки в таблицу если она еще не содержится там.
        /// </summary>
        public void Execute()
        {
            if (!table.Rows.Contains(row))
                table.Rows.Add(row);
        }
        
        /// <summary>
        /// Отменяет добавление удаляя строку из таблицы.
        /// </summary>
        public void Undo()
        {
            table.Rows.Remove(row);
        }
        
        /// <summary>
        /// Получает описание команды.
        /// </summary>
        /// <returns>Строка "Add Row".</returns>
        public string GetDescription()
        {
            return "Add Row";
        }
    }
    
    /// <summary>
    /// Команда для редактирования значения ячейки в таблице баланса.
    /// Сохраняет старое и новое значение для поддержки Undo/Redo.
    /// Реализует интерфейс ICommand.
    /// </summary>
    public class EditCellCommand : ICommand
    {
        /// <summary>
        /// Таблица баланса содержащая редактируемую ячейку.
        /// </summary>
        private BalanceTable table;
        
        /// <summary>
        /// ID строки содержащей редактируемую ячейку.
        /// </summary>
        private string rowId;
        
        /// <summary>
        /// ID столбца содержащего редактируемую ячейку.
        /// </summary>
        private string columnId;
        
        /// <summary>
        /// Значение ячейки до редактирования.
        /// </summary>
        private object oldValue;
        
        /// <summary>
        /// Новое значение ячейки после редактирования.
        /// </summary>
        private object newValue;
        
        /// <summary>
        /// Кэшированная ссылка на строку для оптимизации поиска.
        /// </summary>
        private BalanceRow targetRow;
        
        /// <summary>
        /// Инициализирует новый экземпляр команды редактирования ячейки.
        /// </summary>
        /// <param name="table">Таблица баланса содержащая ячейку.</param>
        /// <param name="rowId">ID строки редактируемой ячейки.</param>
        /// <param name="columnId">ID столбца редактируемой ячейки.</param>
        /// <param name="oldValue">Старое значение ячейки.</param>
        /// <param name="newValue">Новое значение ячейки.</param>
        public EditCellCommand(BalanceTable table, string rowId, string columnId, object oldValue, object newValue)
        {
            this.table = table;
            this.rowId = rowId;
            this.columnId = columnId;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.targetRow = table.Rows.Find(r => r.RowId == rowId);
        }
        
        /// <summary>
        /// Выполняет редактирование устанавливая новое значение в ячейку.
        /// </summary>
        public void Execute()
        {
            if (targetRow != null)
                targetRow.SetValue(columnId, newValue);
        }
        
        /// <summary>
        /// Отменяет редактирование восстанавливая старое значение в ячейку.
        /// </summary>
        public void Undo()
        {
            if (targetRow != null)
                targetRow.SetValue(columnId, oldValue);
        }
        
        /// <summary>
        /// Получает описание команды с ID столбца.
        /// </summary>
        /// <returns>Строка в формате "Edit Cell [columnId]".</returns>
        public string GetDescription()
        {
            return $"Edit Cell [{columnId}]";
        }
    }
    
    /// <summary>
    /// Команда для удаления одной строки из таблицы баланса.
    /// Сохраняет индекс строки для восстановления исходной позиции при Undo.
    /// Реализует интерфейс ICommand.
    /// </summary>
    public class DeleteRowCommand : ICommand
    {
        /// <summary>
        /// Таблица баланса из которой удаляется строка.
        /// </summary>
        private BalanceTable table;
        
        /// <summary>
        /// Удаляемая строка.
        /// </summary>
        private BalanceRow row;
        
        /// <summary>
        /// Индекс строки до удаления, используется для восстановления при Undo.
        /// </summary>
        private int rowIndex;
        
        /// <summary>
        /// Инициализирует новый экземпляр команды удаления строки.
        /// </summary>
        /// <param name="table">Таблица баланса из которой удаляется строка.</param>
        /// <param name="row">Строка для удаления.</param>
        public DeleteRowCommand(BalanceTable table, BalanceRow row)
        {
            this.table = table;
            this.row = row;
            this.rowIndex = table.Rows.IndexOf(row);
        }
        
        /// <summary>
        /// Выполняет удаление удаляя строку из таблицы.
        /// </summary>
        public void Execute()
        {
            table.Rows.Remove(row);
        }
        
        /// <summary>
        /// Отменяет удаление восстанавливая строку в исходную позицию.
        /// Если исходный индекс больше не валиден, добавляет строку в конец таблицы.
        /// </summary>
        public void Undo()
        {
            if (rowIndex >= 0 && rowIndex <= table.Rows.Count)
                table.Rows.Insert(rowIndex, row);
            else
                table.Rows.Add(row);
        }
        
        /// <summary>
        /// Получает описание команды.
        /// </summary>
        /// <returns>Строка "Delete Row".</returns>
        public string GetDescription()
        {
            return "Delete Row";
        }
    }
    
    /// <summary>
    /// Команда для удаления нескольких строк из таблицы баланса одновременно.
    /// Сохраняет индексы всех удаляемых строк для точного восстановления порядка при Undo.
    /// Реализует интерфейс ICommand.
    /// </summary>
    public class MultiDeleteCommand : ICommand
    {
        /// <summary>
        /// Таблица баланса из которой удаляются строки.
        /// </summary>
        private BalanceTable table;
        
        /// <summary>
        /// Список удаляемых строк.
        /// </summary>
        private List<BalanceRow> rows;
        
        /// <summary>
        /// Словарь сохраняющий исходный индекс каждой удаляемой строки.
        /// Ключ это строка, значение это её индекс до удаления.
        /// </summary>
        private Dictionary<BalanceRow, int> rowIndices;
        
        /// <summary>
        /// Инициализирует новый экземпляр команды удаления нескольких строк.
        /// Сохраняет индексы всех строк для восстановления при Undo.
        /// </summary>
        /// <param name="table">Таблица баланса из которой удаляются строки.</param>
        /// <param name="rows">Список строк для удаления.</param>
        public MultiDeleteCommand(BalanceTable table, List<BalanceRow> rows)
        {
            this.table = table;
            this.rows = new List<BalanceRow>(rows);
            this.rowIndices = new Dictionary<BalanceRow, int>();
            
            foreach (var row in rows)
            {
                rowIndices[row] = table.Rows.IndexOf(row);
            }
        }
        
        /// <summary>
        /// Выполняет удаление всех строк из таблицы.
        /// </summary>
        public void Execute()
        {
            foreach (var row in rows)
            {
                table.Rows.Remove(row);
            }
        }
        
        /// <summary>
        /// Отменяет удаление восстанавливая все строки в их исходные позиции в правильном порядке.
        /// Сортирует строки по их исходным индексам перед вставкой для восстановления исходного порядка.
        /// </summary>
        public void Undo()
        {
            var sortedRows = rows.OrderBy(r => rowIndices[r]).ToList();
            foreach (var row in sortedRows)
            {
                int index = rowIndices[row];
                if (index >= 0 && index <= table.Rows.Count)
                    table.Rows.Insert(index, row);
                else
                    table.Rows.Add(row);
            }
        }
        
        /// <summary>
        /// Получает описание команды с количеством удаленных строк.
        /// </summary>
        /// <returns>Строка в формате "Delete N Rows" где N это количество удаленных строк.</returns>
        public string GetDescription()
        {
            return $"Delete {rows.Count} Rows";
        }
    }
}