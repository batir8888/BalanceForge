using System.Collections.Generic;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Представляет результат валидации таблицы баланса.
    /// Содержит список обнаруженных ошибок и предупреждений, позволяет определить прошла ли валидация успешно.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Флаг общего статуса валидации.
        /// </summary>
        private bool isValid;
        
        /// <summary>
        /// Список всех найденных ошибок и предупреждений валидации.
        /// </summary>
        private List<ValidationError> errors;
        
        /// <summary>
        /// Получает значение указывающее прошла ли валидация успешно.
        /// Возвращает true только если флаг isValid истинен И список ошибок пуст.
        /// </summary>
        public bool IsValid => isValid && errors.Count == 0;
        
        /// <summary>
        /// Получает список всех найденных ошибок и предупреждений валидации.
        /// </summary>
        public List<ValidationError> Errors => errors;
        
        /// <summary>
        /// Инициализирует новый экземпляр класса ValidationResult.
        /// Создает пустой список ошибок и устанавливает статус как валидный.
        /// </summary>
        public ValidationResult()
        {
            isValid = true;
            errors = new List<ValidationError>();
        }
        
        /// <summary>
        /// Добавляет ошибку или предупреждение в результат валидации.
        /// Устанавливает флаг isValid в false при добавлении любой ошибки.
        /// </summary>
        /// <param name="error">Объект ValidationError содержащий информацию об ошибке.</param>
        public void AddError(ValidationError error)
        {
            errors.Add(error);
            isValid = false;
        }
        
        /// <summary>
        /// Проверяет присутствуют ли ошибки в результате валидации.
        /// </summary>
        /// <returns>true если список ошибок содержит хотя бы одну ошибку, иначе false.</returns>
        public bool HasErrors()
        {
            return errors.Count > 0;
        }
    }
    
    /// <summary>
    /// Представляет одну ошибку или предупреждение валидации для конкретной ячейки таблицы.
    /// Содержит информацию о местоположении ошибки (строка и столбец) и её описание.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Получает или устанавливает идентификатор строки, в которой обнаружена ошибка.
        /// </summary>
        public string RowId { get; set; }
        
        /// <summary>
        /// Получает или устанавливает идентификатор столбца, в котором обнаружена ошибка.
        /// </summary>
        public string ColumnId { get; set; }
        
        /// <summary>
        /// Получает или устанавливает описание ошибки или предупреждения.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Получает или устанавливает уровень серьезности ошибки.
        /// </summary>
        public ErrorSeverity Severity { get; set; }
        
        /// <summary>
        /// Инициализирует новый экземпляр класса ValidationError.
        /// </summary>
        /// <param name="rowId">Идентификатор строки с ошибкой.</param>
        /// <param name="columnId">Идентификатор столбца с ошибкой.</param>
        /// <param name="message">Описание ошибки.</param>
        /// <param name="severity">Уровень серьезности ошибки (по умолчанию ErrorSeverity.Error).</param>
        public ValidationError(string rowId, string columnId, string message, ErrorSeverity severity = ErrorSeverity.Error)
        {
            RowId = rowId;
            ColumnId = columnId;
            Message = message;
            Severity = severity;
        }
    }
    
    /// <summary>
    /// Перечисление уровней серьезности ошибок валидации.
    /// Определяет как критичной является найденная проблема.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>Предупреждение - потенциальная проблема, но данные могут быть использованы.</summary>
        Warning,
        
        /// <summary>Ошибка - нарушение требований, данные не должны использоваться в продакшене.</summary>
        Error,
        
        /// <summary>Критическая ошибка - серьезное нарушение целостности данных, использование невозможно.</summary>
        Critical
    }
}