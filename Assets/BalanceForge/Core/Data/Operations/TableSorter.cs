using System;
using System.Collections.Generic;
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
        // Optimized sort using Array.Sort instead of LINQ
        public static List<BalanceRow> Sort(List<BalanceRow> rows, string columnId, SortDirection direction, ColumnType columnType)
        {
            if (direction == SortDirection.None || rows.Count <= 1)
                return rows;
            
            // Pre-warm deserialization for all rows
            foreach (var row in rows)
            {
                row.EnsureDeserialized();
            }
            
            // Convert to array for faster sorting
            var array = rows.ToArray();
            
            // Create comparer based on column type
            var comparer = CreateComparer(columnId, columnType, direction);
            
            // Use Array.Sort - much faster than LINQ OrderBy
            Array.Sort(array, comparer);
            
            return new List<BalanceRow>(array);
        }
        
        private static IComparer<BalanceRow> CreateComparer(string columnId, ColumnType columnType, SortDirection direction)
        {
            return new RowComparer(columnId, columnType, direction);
        }
        
        private class RowComparer : IComparer<BalanceRow>
        {
            private readonly string columnId;
            private readonly ColumnType columnType;
            private readonly int directionMultiplier;
            
            public RowComparer(string columnId, ColumnType columnType, SortDirection direction)
            {
                this.columnId = columnId;
                this.columnType = columnType;
                this.directionMultiplier = direction == SortDirection.Ascending ? 1 : -1;
            }
            
            public int Compare(BalanceRow x, BalanceRow y)
            {
                var valueX = x.GetValue(columnId);
                var valueY = y.GetValue(columnId);
                
                if (valueX == null && valueY == null) return 0;
                if (valueX == null) return -directionMultiplier;
                if (valueY == null) return directionMultiplier;
                
                int result = CompareValues(valueX, valueY);
                return result * directionMultiplier;
            }
            
            private int CompareValues(object x, object y)
            {
                switch (columnType)
                {
                    case ColumnType.Integer:
                        if (int.TryParse(x.ToString(), out int intX) && int.TryParse(y.ToString(), out int intY))
                            return intX.CompareTo(intY);
                        break;
                        
                    case ColumnType.Float:
                        if (float.TryParse(x.ToString(), out float floatX) && float.TryParse(y.ToString(), out float floatY))
                            return floatX.CompareTo(floatY);
                        break;
                        
                    case ColumnType.Boolean:
                        if (bool.TryParse(x.ToString(), out bool boolX) && bool.TryParse(y.ToString(), out bool boolY))
                            return boolX.CompareTo(boolY);
                        break;
                }
                
                return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}