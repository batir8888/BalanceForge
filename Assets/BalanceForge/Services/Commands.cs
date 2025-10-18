using BalanceForge.Core.Data;

namespace BalanceForge.Services
{
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
}