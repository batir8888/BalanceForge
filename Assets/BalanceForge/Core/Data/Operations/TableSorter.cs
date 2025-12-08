using System;
using System.Collections.Generic;
using BalanceForge.Core.Data;

namespace BalanceForge.Data.Operations
{
    /// <summary>
    /// Перечисление направлений сортировки для столбцов таблицы.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>Сортировка отсутствует.</summary>
        None,
        /// <summary>Возрастающий порядок (А-Я, 0-9).</summary>
        Ascending,
        /// <summary>Убывающий порядок (Я-А, 9-0).</summary>
        Descending
    }
    
    /// <summary>
    /// Управляет состоянием сортировки таблицы, отслеживая активный столбец и направление сортировки.
    /// Предоставляет функциональность переключения направления сортировки при клике на столбец.
    /// </summary>
    public class SortingState
    {
        /// <summary>
        /// Получает или устанавливает идентификатор столбца, по которому выполняется сортировка.
        /// </summary>
        public string SortColumnId { get; set; }
        
        /// <summary>
        /// Получает или устанавливает направление сортировки.
        /// </summary>
        public SortDirection Direction { get; set; }
        
        /// <summary>
        /// Переключает направление сортировки для указанного столбца.
        /// Если сортировка по другому столбцу, переключается на новый столбец в режиме Ascending.
        /// Если сортировка по этому же столбцу, циклически переключается: None → Ascending → Descending → None.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца для переключения сортировки.</param>
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
    
    /// <summary>
    /// Оптимизированный сортировщик строк баланса с поддержкой различных типов данных.
    /// Использует Array.Sort для высокой производительности вместо LINQ OrderBy.
    /// </summary>
    public class TableSorter
    {
        /// <summary>
        /// Сортирует список строк баланса по указанному столбцу в заданном направлении.
        /// Операция оптимизирована для работы с большими наборами данных благодаря использованию Array.Sort.
        /// </summary>
        /// <param name="rows">Список строк баланса для сортировки.</param>
        /// <param name="columnId">Идентификатор столбца для сортировки.</param>
        /// <param name="direction">Направление сортировки (Ascending, Descending или None).</param>
        /// <param name="columnType">Тип данных столбца для правильного сравнения значений.</param>
        /// <returns>Отсортированный список строк. Если direction равен None или список содержит 1 или менее элементов, возвращается исходный список.</returns>
        public static List<BalanceRow> Sort(List<BalanceRow> rows, string columnId, SortDirection direction, ColumnType columnType)
        {
            if (direction == SortDirection.None || rows.Count <= 1)
                return rows;
            
            // Convert to array for faster sorting
            var array = rows.ToArray();
            
            // Create comparer based on column type
            var comparer = CreateComparer(columnId, columnType, direction);
            
            // Use Array.Sort - much faster than LINQ OrderBy
            Array.Sort(array, comparer);
            
            return new List<BalanceRow>(array);
        }
        
        /// <summary>
        /// Создает компаратор для сравнения строк по указанному столбцу и типу данных.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца.</param>
        /// <param name="columnType">Тип данных столбца.</param>
        /// <param name="direction">Направление сортировки.</param>
        /// <returns>Компаратор для сравнения объектов BalanceRow.</returns>
        private static IComparer<BalanceRow> CreateComparer(string columnId, ColumnType columnType, SortDirection direction)
        {
            return new RowComparer(columnId, columnType, direction);
        }
        
        /// <summary>
        /// Внутренний класс для сравнения строк баланса.
        /// Поддерживает сравнение различных типов данных (Integer, Float, Boolean) с правильной типизацией.
        /// </summary>
        private class RowComparer : IComparer<BalanceRow>
        {
            private readonly string columnId;
            private readonly ColumnType columnType;
            private readonly int directionMultiplier;
            
            /// <summary>
            /// Инициализирует новый экземпляр класса RowComparer.
            /// </summary>
            /// <param name="columnId">Идентификатор столбца для сравнения.</param>
            /// <param name="columnType">Тип данных столбца для выбора правильного способа сравнения.</param>
            /// <param name="direction">Направление сортировки для определения множителя направления.</param>
            public RowComparer(string columnId, ColumnType columnType, SortDirection direction)
            {
                this.columnId = columnId;
                this.columnType = columnType;
                this.directionMultiplier = direction == SortDirection.Ascending ? 1 : -1;
            }
            
            /// <summary>
            /// Сравнивает две строки баланса по значению указанного столбца.
            /// Корректно обрабатывает null значения, размещая их в начале списка.
            /// </summary>
            /// <param name="x">Первая строка для сравнения.</param>
            /// <param name="y">Вторая строка для сравнения.</param>
            /// <returns>Отрицательное число если x &lt; y, ноль если x == y, положительное если x &gt; y. Учитывается направление сортировки.</returns>
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
            
            /// <summary>
            /// Сравнивает два значения с учетом типа столбца.
            /// Пытается преобразовать значения в указанный тип (Integer, Float, Boolean).
            /// Если преобразование не удается, выполняет строковое сравнение без учета регистра.
            /// </summary>
            /// <param name="x">Первое значение для сравнения.</param>
            /// <param name="y">Второе значение для сравнения.</param>
            /// <returns>Результат сравнения: отрицательное число если x &lt; y, ноль если x == y, положительное если x &gt; y.</returns>
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