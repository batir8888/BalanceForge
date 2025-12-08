namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Перечисление поддерживаемых типов данных для столбцов таблицы баланса.
    /// Определяет какие типы значений могут храниться в ячейках и как они будут сериализоваться/десериализоваться.
    /// </summary>
    public enum ColumnType
    {
        /// <summary>Текстовая строка.</summary>
        String,
        
        /// <summary>Целое число (int).</summary>
        Integer,
        
        /// <summary>Число с плавающей точкой (float).</summary>
        Float,
        
        /// <summary>Логическое значение (bool) - true или false.</summary>
        Boolean,
        
        /// <summary>Перечисление - выбор из предопределенного списка значений.</summary>
        Enum,
        
        /// <summary>Ссылка на Unity Asset (Prefab, Material, Texture2D и т.д.).</summary>
        AssetReference,
        
        /// <summary>Цвет в формате RGBA (Color).</summary>
        Color,
        
        /// <summary>Двумерный вектор (Vector2) с компонентами x и y.</summary>
        Vector2,
        
        /// <summary>Трехмерный вектор (Vector3) с компонентами x, y и z.</summary>
        Vector3
    }
}