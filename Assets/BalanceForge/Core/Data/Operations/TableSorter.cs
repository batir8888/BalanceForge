using System;
using System.Collections.Generic;
using System.Linq;
using BalanceForge.Core.Data;

namespace BalanceForge.Data.Operations
{
    public enum SortDirection
    {
        None,
        Ascending,
        Descending
    }
    
    public class SortingState
    {
        public string SortColumnId { get; set; }
        public SortDirection Direction { get; set; }
        
        public void Toggle(string columnId)
        {
            if (SortColumnId != columnId)
            {
                SortColumnId = columnId;
                Direction = SortDirection.Ascending;
            }
            else
            {
                Direction = Direction switch
                {
                    SortDirection.None => SortDirection.Ascending,
                    SortDirection.Ascending => SortDirection.Descending,
                    SortDirection.Descending => SortDirection.None,
                    _ => SortDirection.None
                };
            }
        }
    }
    
    public class TableSorter
    {
        public static List<BalanceRow> Sort(List<BalanceRow> rows, string columnId, SortDirection direction, ColumnType columnType)
        {
            if (direction == SortDirection.None)
                return rows;
            
            var sorted = rows.OrderBy<BalanceRow, object>(row =>
            {
                var value = row.GetValue(columnId);
                if (value == null) return null;
                
                switch (columnType)
                {
                    case ColumnType.Integer:
                        return int.TryParse(value.ToString(), out int intVal) ? intVal : 0;
                    case ColumnType.Float:
                        return float.TryParse(value.ToString(), out float floatVal) ? floatVal : 0f;
                    case ColumnType.Boolean:
                        return bool.TryParse(value.ToString(), out bool boolVal) && boolVal ? 1 : 0;
                    default:
                        return value.ToString();
                }
            }).ToList();
            
            if (direction == SortDirection.Descending)
                sorted.Reverse();
            
            return sorted;
        }
    }
}