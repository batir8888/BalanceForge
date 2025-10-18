using System.Collections.Generic;
using System.Linq;
using BalanceForge.Core.Data;

namespace BalanceForge.Data.Operations
{
    public interface IFilter
    {
        List<BalanceRow> Apply(List<BalanceRow> rows);
    }
    
    public enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        Contains,
        StartsWith,
        EndsWith
    }
    
    public class FilterCondition
    {
        public string ColumnId { get; set; }
        public FilterOperator Operator { get; set; }
        public object Value { get; set; }
        
        public bool Matches(BalanceRow row)
        {
            var cellValue = row.GetValue(ColumnId);
            if (cellValue == null) return false;
            
            string cellStr = cellValue.ToString();
            string valueStr = Value?.ToString() ?? "";
            
            switch (Operator)
            {
                case FilterOperator.Equals:
                    return cellStr.Equals(valueStr, System.StringComparison.OrdinalIgnoreCase);
                case FilterOperator.NotEquals:
                    return !cellStr.Equals(valueStr, System.StringComparison.OrdinalIgnoreCase);
                case FilterOperator.Contains:
                    return cellStr.IndexOf(valueStr, System.StringComparison.OrdinalIgnoreCase) >= 0;
                case FilterOperator.StartsWith:
                    return cellStr.StartsWith(valueStr, System.StringComparison.OrdinalIgnoreCase);
                case FilterOperator.EndsWith:
                    return cellStr.EndsWith(valueStr, System.StringComparison.OrdinalIgnoreCase);
                case FilterOperator.GreaterThan:
                    if (float.TryParse(cellStr, out float cellFloat) && float.TryParse(valueStr, out float valueFloat))
                        return cellFloat > valueFloat;
                    return cellStr.CompareTo(valueStr) > 0;
                case FilterOperator.LessThan:
                    if (float.TryParse(cellStr, out float cellFloat2) && float.TryParse(valueStr, out float valueFloat2))
                        return cellFloat2 < valueFloat2;
                    return cellStr.CompareTo(valueStr) < 0;
                default:
                    return false;
            }
        }
    }
    
    public class ColumnFilter : IFilter
    {
        private FilterCondition condition;
        
        public ColumnFilter(FilterCondition condition)
        {
            this.condition = condition;
        }
        
        public List<BalanceRow> Apply(List<BalanceRow> rows)
        {
            return rows.Where(row => condition.Matches(row)).ToList();
        }
    }
    
    public enum LogicalOperator
    {
        And,
        Or
    }
    
    public class CompositeFilter : IFilter
    {
        private List<IFilter> filters = new List<IFilter>();
        private LogicalOperator logicalOp;
        
        public CompositeFilter(LogicalOperator op)
        {
            logicalOp = op;
        }
        
        public void AddFilter(IFilter filter)
        {
            filters.Add(filter);
        }
        
        public List<BalanceRow> Apply(List<BalanceRow> rows)
        {
            if (filters.Count == 0) return rows;
            
            if (logicalOp == LogicalOperator.And)
            {
                var result = rows;
                foreach (var filter in filters)
                {
                    result = filter.Apply(result);
                }
                return result;
            }
            else // OR
            {
                var resultSet = new HashSet<BalanceRow>();
                foreach (var filter in filters)
                {
                    var filtered = filter.Apply(rows);
                    foreach (var row in filtered)
                    {
                        resultSet.Add(row);
                    }
                }
                return resultSet.ToList();
            }
        }
    }
}