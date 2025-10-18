using BalanceForge.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace BalanceForge.Services
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        string GetDescription();
    }
    
    public class AddRowCommand : ICommand
    {
        private BalanceTable table;
        private BalanceRow row;
        
        public AddRowCommand(BalanceTable table, BalanceRow row)
        {
            this.table = table;
            this.row = row;
        }
        
        public void Execute()
        {
            if (!table.Rows.Contains(row))
                table.Rows.Add(row);
        }
        
        public void Undo()
        {
            table.Rows.Remove(row);
        }
        
        public string GetDescription()
        {
            return "Add Row";
        }
    }
    
    public class EditCellCommand : ICommand
    {
        private BalanceTable table;
        private string rowId;
        private string columnId;
        private object oldValue;
        private object newValue;
        private BalanceRow targetRow;
        
        public EditCellCommand(BalanceTable table, string rowId, string columnId, object oldValue, object newValue)
        {
            this.table = table;
            this.rowId = rowId;
            this.columnId = columnId;
            this.oldValue = oldValue;
            this.newValue = newValue;
            this.targetRow = table.Rows.Find(r => r.RowId == rowId);
        }
        
        public void Execute()
        {
            if (targetRow != null)
                targetRow.SetValue(columnId, newValue);
        }
        
        public void Undo()
        {
            if (targetRow != null)
                targetRow.SetValue(columnId, oldValue);
        }
        
        public string GetDescription()
        {
            return $"Edit Cell [{columnId}]";
        }
    }
    
    public class DeleteRowCommand : ICommand
    {
        private BalanceTable table;
        private BalanceRow row;
        private int rowIndex;
        
        public DeleteRowCommand(BalanceTable table, BalanceRow row)
        {
            this.table = table;
            this.row = row;
            this.rowIndex = table.Rows.IndexOf(row);
        }
        
        public void Execute()
        {
            table.Rows.Remove(row);
        }
        
        public void Undo()
        {
            if (rowIndex >= 0 && rowIndex <= table.Rows.Count)
                table.Rows.Insert(rowIndex, row);
            else
                table.Rows.Add(row);
        }
        
        public string GetDescription()
        {
            return "Delete Row";
        }
    }
    
    public class MultiDeleteCommand : ICommand
    {
        private BalanceTable table;
        private List<BalanceRow> rows;
        private Dictionary<BalanceRow, int> rowIndices;
        
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
        
        public void Execute()
        {
            foreach (var row in rows)
            {
                table.Rows.Remove(row);
            }
        }
        
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
        
        public string GetDescription()
        {
            return $"Delete {rows.Count} Rows";
        }
    }
}