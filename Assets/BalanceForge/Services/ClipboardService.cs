using UnityEngine;
using System.Collections.Generic;

namespace BalanceForge.Services
{
    /// <summary>
    /// Статический сервис управления буфером обмена для таблицы баланса.
    /// Поддерживает копирование и вставку отдельных значений и групп значений.
    /// Интегрирует внутренний буфер обмена с системным буфером обмена Unity через GUIUtility.systemCopyBuffer.
    /// </summary>
    public static class ClipboardService
    {
        /// <summary>
        /// Внутренний буфер обмена для хранения значений по ID столбца.
        /// </summary>
        private static Dictionary<string, object> clipboard = new Dictionary<string, object>();
        
        /// <summary>
        /// ID столбца последнего скопированного значения.
        /// Используется для определения типа данных при вставке.
        /// </summary>
        private static string clipboardColumnId;
        
        /// <summary>
        /// Флаг указывающий что буфер обмена содержит данные.
        /// </summary>
        private static bool hasData = false;
        
        /// <summary>
        /// Копирует одно значение в буфер обмена.
        /// Сохраняет значение во внутренний буфер обмена и также копирует его строковое представление в системный буфер обмена.
        /// Предыдущие данные в буфере очищаются.
        /// </summary>
        /// <param name="columnId">ID столбца для которого копируется значение. Используется для валидации при вставке.</param>
        /// <param name="value">Значение для копирования. Null значения копируются как пустая строка в системный буфер.</param>
        public static void Copy(string columnId, object value)
        {
            clipboard.Clear();
            clipboard[columnId] = value;
            clipboardColumnId = columnId;
            hasData = true;
            
            // Also copy to system clipboard as string
            GUIUtility.systemCopyBuffer = value?.ToString() ?? "";
        }
        
        /// <summary>
        /// Копирует несколько значений разных столбцов в буфер обмена.
        /// Сохраняет все значения во внутренний буфер обмена и копирует их в системный буфер как табуляцией разделенные значения.
        /// Полезно для операций с выбранными ячейками из разных столбцов.
        /// </summary>
        /// <param name="values">Словарь где ключи это ID столбцов, а значения это объекты для копирования.</param>
        public static void CopyMultiple(Dictionary<string, object> values)
        {
            clipboard = new Dictionary<string, object>(values);
            hasData = true;
            
            // Copy as tab-separated values to system clipboard
            var text = string.Join("\t", values.Values);
            GUIUtility.systemCopyBuffer = text;
        }
        
        /// <summary>
        /// Проверяет может ли быть вставлено значение для указанного столбца.
        /// Возвращает true если буфер обмена содержит данные и либо есть значение для этого столбца, 
        /// либо есть данные в системном буфере обмена.
        /// </summary>
        /// <param name="columnId">ID столбца для проверки возможности вставки.</param>
        /// <returns>true если есть данные для вставки в этот столбец, иначе false.</returns>
        public static bool CanPaste(string columnId)
        {
            return hasData && (clipboard.ContainsKey(columnId) || !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer));
        }
        
        /// <summary>
        /// Вставляет значение из буфера обмена для указанного столбца.
        /// Сначала проверяет внутренний буфер обмена, затем система буфер обмена.
        /// Если значение не найдено в буфере обмена, вставляет содержимое системного буфера обмена как строку.
        /// </summary>
        /// <param name="columnId">ID столбца для вставки значения.</param>
        /// <returns>Значение из внутреннего буфера или содержимое системного буфера обмена как строка.</returns>
        public static object Paste(string columnId)
        {
            if (clipboard.ContainsKey(columnId))
                return clipboard[columnId];
            
            // Try to paste from system clipboard
            return GUIUtility.systemCopyBuffer;
        }
        
        /// <summary>
        /// Вставляет все значения из буфера обмена.
        /// Возвращает копию внутреннего буфера обмена с всеми сохраненными значениями.
        /// Полезно для вставки нескольких значений в разные столбцы одновременно.
        /// </summary>
        /// <returns>Копия словаря внутреннего буфера обмена со всеми значениями.</returns>
        public static Dictionary<string, object> PasteMultiple()
        {
            return new Dictionary<string, object>(clipboard);
        }
        
        /// <summary>
        /// Очищает буфер обмена удаляя все сохраненные данные.
        /// Не влияет на системный буфер обмена, только на внутренний.
        /// </summary>
        public static void Clear()
        {
            clipboard.Clear();
            hasData = false;
        }
    }
}