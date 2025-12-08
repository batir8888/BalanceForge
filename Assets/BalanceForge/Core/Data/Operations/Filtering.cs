using System.Collections.Generic;
using System.Linq;
using BalanceForge.Core.Data;

namespace BalanceForge.Data.Operations
{
    /// <summary>
    /// Интерфейс для применения фильтров к списку строк баланса.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Применяет фильтр к списку строк.
        /// </summary>
        /// <param name="rows">Список строк баланса для фильтрации.</param>
        /// <returns>Отфильтрованный список строк баланса.</returns>
        List<BalanceRow> Apply(List<BalanceRow> rows);
    }
    
    /// <summary>
    /// Перечисление операторов для фильтрации данных.
    /// </summary>
    public enum FilterOperator
    {
        /// <summary>Равно.</summary>
        Equals,
        /// <summary>Не равно.</summary>
        NotEquals,
        /// <summary>Больше чем.</summary>
        GreaterThan,
        /// <summary>Меньше чем.</summary>
        LessThan,
        /// <summary>Содержит.</summary>
        Contains,
        /// <summary>Начинается с.</summary>
        StartsWith,
        /// <summary>Заканчивается на.</summary>
        EndsWith
    }
    
    /// <summary>
    /// Представляет условие фильтрации для одного столбца.
    /// </summary>
    public class FilterCondition
    {
        /// <summary>
        /// Получает или устанавливает идентификатор столбца для фильтрации.
        /// </summary>
        public string ColumnId { get; set; }
        
        /// <summary>
        /// Получает или устанавливает оператор сравнения.
        /// </summary>
        public FilterOperator Operator { get; set; }
        
        /// <summary>
        /// Получает или устанавливает значение для сравнения.
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Проверяет, соответствует ли строка условию фильтра.
        /// </summary>
        /// <param name="row">Строка баланса для проверки.</param>
        /// <returns>true если строка соответствует условию, иначе false.</returns>
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
    
    /// <summary>
    /// Фильтр для одного столбца на основе одного условия.
    /// Реализует интерфейс IFilter для применения фильтра к списку строк.
    /// </summary>
    public class ColumnFilter : IFilter
    {
        private FilterCondition condition;
        
        /// <summary>
        /// Инициализирует новый экземпляр класса ColumnFilter.
        /// </summary>
        /// <param name="condition">Условие фильтрации для применения.</param>
        public ColumnFilter(FilterCondition condition)
        {
            this.condition = condition;
        }
        
        /// <summary>
        /// Применяет условие фильтра к списку строк.
        /// </summary>
        /// <param name="rows">Список строк баланса для фильтрации.</param>
        /// <returns>Список строк, соответствующих условию фильтра.</returns>
        public List<BalanceRow> Apply(List<BalanceRow> rows)
        {
            return rows.Where(row => condition.Matches(row)).ToList();
        }
    }
    
    /// <summary>
    /// Перечисление логических операторов для комбинирования фильтров.
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>Логическое И (AND) - все условия должны быть истинны.</summary>
        And,
        /// <summary>Логическое ИЛИ (OR) - хотя бы одно условие должно быть истинно.</summary>
        Or
    }
    
    /// <summary>
    /// Составной фильтр, который комбинирует несколько фильтров с помощью логических операторов.
    /// Позволяет создавать сложные условия фильтрации (И/ИЛИ).
    /// </summary>
    public class CompositeFilter : IFilter
    {
        private List<IFilter> filters = new List<IFilter>();
        private LogicalOperator logicalOp;
        
        /// <summary>
        /// Инициализирует новый экземпляр класса CompositeFilter с указанным логическим оператором.
        /// </summary>
        /// <param name="op">Логический оператор (And или Or) для комбинирования фильтров.</param>
        public CompositeFilter(LogicalOperator op)
        {
            logicalOp = op;
        }
        
        /// <summary>
        /// Добавляет фильтр в композитный фильтр.
        /// </summary>
        /// <param name="filter">Фильтр для добавления.</param>
        public void AddFilter(IFilter filter)
        {
            filters.Add(filter);
        }
        
        /// <summary>
        /// Применяет все добавленные фильтры к списку строк с использованием логического оператора.
        /// Для оператора And фильтры применяются последовательно (каждый фильтр уменьшает результат).
        /// Для оператора Or собираются все строки, соответствующие хотя бы одному фильтру.
        /// </summary>
        /// <param name="rows">Список строк баланса для фильтрации.</param>
        /// <returns>Список отфильтрованных строк.</returns>
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