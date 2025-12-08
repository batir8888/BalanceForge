using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Определяет набор возможных значений для столбца типа Enum.
    /// Управляет списком доступных опций для выбора в ячейках столбца перечисления.
    /// </summary>
    [Serializable]
    public class EnumDefinition
    {
        /// <summary>
        /// Имя перечисления, используется для идентификации и отображения в UI.
        /// </summary>
        [SerializeField] private string enumName;
        
        /// <summary>
        /// Список строковых значений доступных для выбора в этом перечислении.
        /// </summary>
        [SerializeField] private List<string> values = new List<string>();
        
        /// <summary>
        /// Получает имя перечисления.
        /// </summary>
        public string EnumName => enumName;
        
        /// <summary>
        /// Получает список всех доступных значений перечисления.
        /// </summary>
        public List<string> Values => values;
        
        /// <summary>
        /// Инициализирует новый экземпляр класса EnumDefinition с указанным именем.
        /// </summary>
        /// <param name="name">Имя перечисления для идентификации.</param>
        public EnumDefinition(string name)
        {
            enumName = name;
        }
        
        /// <summary>
        /// Добавляет новое значение в перечисление.
        /// Не добавляет значение если оно уже присутствует в списке (избегает дубликатов).
        /// </summary>
        /// <param name="value">Строковое значение для добавления.</param>
        public void AddValue(string value)
        {
            if (!values.Contains(value))
                values.Add(value);
        }
        
        /// <summary>
        /// Удаляет значение из перечисления.
        /// Не выбрасывает исключение если значение не найдено.
        /// </summary>
        /// <param name="value">Строковое значение для удаления.</param>
        public void RemoveValue(string value)
        {
            values.Remove(value);
        }
        
        /// <summary>
        /// Получает индекс значения в списке доступных опций перечисления.
        /// </summary>
        /// <param name="value">Строковое значение для поиска.</param>
        /// <returns>Индекс значения (0-based) или -1 если значение не найдено в списке.</returns>
        public int GetIndex(string value)
        {
            return values.IndexOf(value);
        }
    }
}