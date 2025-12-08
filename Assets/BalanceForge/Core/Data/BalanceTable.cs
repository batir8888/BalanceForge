using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// ScriptableObject для управления таблицей баланса, содержащей определения столбцов и строки данных.
    /// Поддерживает операции создания, удаления и валидации строк и столбцов.
    /// Автоматически отслеживает время последнего изменения.
    /// Доступен для создания через Unity Editor меню: BalanceForge > Balance Table.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBalanceTable", menuName = "BalanceForge/Balance Table")]
    public class BalanceTable : ScriptableObject
    {
        /// <summary>
        /// Человекочитаемое имя таблицы.
        /// </summary>
        [SerializeField] private string tableName;
        
        /// <summary>
        /// Уникальный идентификатор таблицы, автоматически генерируется при первом включении.
        /// </summary>
        [SerializeField] private string tableId;
        
        /// <summary>
        /// Список определений столбцов таблицы.
        /// </summary>
        [SerializeField] private List<ColumnDefinition> columns = new List<ColumnDefinition>();
        
        /// <summary>
        /// Список строк данных таблицы баланса.
        /// </summary>
        [SerializeField] private List<BalanceRow> rows = new List<BalanceRow>();
        
        /// <summary>
        /// Timestamp последнего изменения таблицы в формате Ticks для Unity сериализации.
        /// </summary>
        [SerializeField] private long lastModifiedTicks;
        
        /// <summary>
        /// Получает или устанавливает человекочитаемое имя таблицы.
        /// Установка значения автоматически обновляет временную метку последнего изменения.
        /// </summary>
        public string TableName 
        { 
            get => tableName;
            set 
            {
                tableName = value;
                UpdateLastModified();
            }
        }
        
        /// <summary>
        /// Получает уникальный идентификатор таблицы.
        /// </summary>
        public string TableId => tableId;
        
        /// <summary>
        /// Получает список определений столбцов таблицы.
        /// </summary>
        public List<ColumnDefinition> Columns => columns;
        
        /// <summary>
        /// Получает список строк данных таблицы.
        /// </summary>
        public List<BalanceRow> Rows => rows;
        
        /// <summary>
        /// Получает дату и время последнего изменения таблицы.
        /// </summary>
        public DateTime LastModified => new DateTime(lastModifiedTicks);
        
        /// <summary>
        /// Вызывается Unity когда объект включается.
        /// Генерирует уникальный ID таблицы если он еще не установлен.
        /// </summary>
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(tableId))
                tableId = Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Добавляет новый столбец в таблицу.
        /// Инициализирует значение по умолчанию для всех существующих строк.
        /// Обновляет временную метку последнего изменения.
        /// </summary>
        /// <param name="column">Определение столбца для добавления.</param>
        public void AddColumn(ColumnDefinition column)
        {
            columns.Add(column);
            
            // Add default values to existing rows
            foreach (var row in rows)
            {
                row.SetValue(column.ColumnId, column.DefaultValue);
            }
            
            UpdateLastModified();
        }
        
        /// <summary>
        /// Удаляет столбец из таблицы по его идентификатору.
        /// Очищает значения этого столбца во всех строках (устанавливает null).
        /// Обновляет временную метку последнего изменения.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца для удаления.</param>
        public void RemoveColumn(string columnId)
        {
            var column = columns.FirstOrDefault(c => c.ColumnId == columnId);
            if (column != null)
            {
                columns.Remove(column);
                
                // Remove values from existing rows
                foreach (var row in rows)
                {
                    row.SetValue(columnId, null);
                }
                
                UpdateLastModified();
            }
        }
        
        /// <summary>
        /// Добавляет новую строку в таблицу.
        /// Инициализирует все значения ячеек значениями по умолчанию из определений столбцов.
        /// Обновляет временную метку последнего изменения.
        /// </summary>
        /// <returns>Новая добавленная строка баланса.</returns>
        public BalanceRow AddRow()
        {
            var row = new BalanceRow();
            
            // Initialize with default values
            foreach (var column in columns)
            {
                row.SetValue(column.ColumnId, column.DefaultValue);
            }
            
            rows.Add(row);
            UpdateLastModified();
            return row;
        }
        
        /// <summary>
        /// Удаляет строку из таблицы по её идентификатору.
        /// Обновляет временную метку последнего изменения при успешном удалении.
        /// </summary>
        /// <param name="rowId">Идентификатор строки для удаления.</param>
        /// <returns>true если строка была найдена и удалена, иначе false.</returns>
        public bool RemoveRow(string rowId)
        {
            var row = rows.FirstOrDefault(r => r.RowId == rowId);
            if (row != null)
            {
                rows.Remove(row);
                UpdateLastModified();
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Получает строку по индексу в списке.
        /// </summary>
        /// <param name="index">Индекс строки (0-based).</param>
        /// <returns>Строка баланса по указанному индексу или null если индекс выходит за границы списка.</returns>
        public BalanceRow GetRow(int index)
        {
            if (index >= 0 && index < rows.Count)
                return rows[index];
            return null;
        }
        
        /// <summary>
        /// Получает определение столбца по его идентификатору.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца для поиска.</param>
        /// <returns>Определение столбца или null если столбец с таким ID не найден.</returns>
        public ColumnDefinition GetColumn(string columnId)
        {
            return columns.FirstOrDefault(c => c.ColumnId == columnId);
        }
        
        /// <summary>
        /// Проверяет все данные таблицы на соответствие правилам валидации определенным в столбцах.
        /// Создает детальный отчет об ошибках и предупреждениях валидации.
        /// </summary>
        /// <returns>Объект ValidationResult, содержащий список найденных ошибок и статус валидации.</returns>
        public ValidationResult ValidateData()
        {
            var result = new ValidationResult();
            
            foreach (var row in rows)
            {
                foreach (var column in columns)
                {
                    var value = row.GetValue(column.ColumnId);
                    if (!column.Validate(value))
                    {
                        result.AddError(new ValidationError(
                            row.RowId,
                            column.ColumnId,
                            $"Validation failed for column {column.DisplayName}",
                            column.IsRequired ? ErrorSeverity.Error : ErrorSeverity.Warning
                        ));
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверяет соответствие структуры таблицы списку имен столбцов.
        /// Сравнивает количество столбцов и их отображаемые имена в том же порядке.
        /// </summary>
        /// <param name="columnNames">Список ожидаемых отображаемых имен столбцов в правильном порядке.</param>
        /// <returns>true если таблица имеет точно такую же структуру столбцов, иначе false.</returns>
        public bool HasStructure(List<string> columnNames)
        {
            if (columns.Count != columnNames.Count)
                return false;
                
            for (int i = 0; i < columns.Count; i++)
            {
                if (columns[i].DisplayName != columnNames[i])
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Обновляет временную метку последнего изменения на текущее время.
        /// В редакторе Unity отмечает объект как измененный для сохранения.
        /// </summary>
        private void UpdateLastModified()
        {
            lastModifiedTicks = DateTime.Now.Ticks;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}