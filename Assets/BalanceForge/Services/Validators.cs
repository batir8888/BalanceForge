using System.Globalization;
using System.Text.RegularExpressions;

namespace BalanceForge.Services
{
    /// <summary>
    /// Валидатор для проверки что значение находится в указанном диапазоне.
    /// Поддерживает целые числа и числа с плавающей точкой.
    /// Реализует интерфейс IValidator.
    /// </summary>
    public class RangeValidator : IValidator
    {
        /// <summary>
        /// Минимальное допустимое значение включительно.
        /// </summary>
        private float minValue;
        
        /// <summary>
        /// Максимальное допустимое значение включительно.
        /// </summary>
        private float maxValue;
        
        /// <summary>
        /// Инициализирует новый экземпляр RangeValidator с указанными границами диапазона.
        /// </summary>
        /// <param name="min">Минимальное допустимое значение (включительно).</param>
        /// <param name="max">Максимальное допустимое значение (включительно).</param>
        public RangeValidator(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }
        
        /// <summary>
        /// Проверяет находится ли значение в допустимом диапазоне [min, max].
        /// Пытается преобразовать значение в float для числовой проверки.
        /// Null значения считаются невалидными.
        /// </summary>
        /// <param name="value">Значение для проверки.</param>
        /// <returns>true если значение числовое и находится в диапазоне, иначе false.</returns>
        public bool Validate(object value)
        {
            if (value == null) return false;
            
            if (float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return floatValue >= minValue && floatValue <= maxValue;
            }
            
            return false;
        }
        
        /// <summary>
        /// Получает сообщение об ошибке с информацией о диапазоне.
        /// </summary>
        /// <returns>Строка с описанием допустимого диапазона значений.</returns>
        public string GetErrorMessage()
        {
            return $"Value must be between {minValue} and {maxValue}";
        }
    }
    
    /// <summary>
    /// Валидатор для проверки, что значение соответствует регулярному выражению.
    /// Используется для валидации строк по сложным шаблонам (email, URL, специальные форматы и т.д.).
    /// Реализует интерфейс IValidator.
    /// </summary>
    public class RegexValidator : IValidator
    {
        /// <summary>
        /// Строка с регулярным выражением для проверки.
        /// </summary>
        private string pattern;
        
        /// <summary>
        /// Скомпилированный объект Regex для эффективного повторного использования.
        /// </summary>
        private Regex regex;
        
        /// <summary>
        /// Инициализирует новый экземпляр RegexValidator с указанным регулярным выражением.
        /// Компилирует регулярное выражение при создании для оптимизации последующих проверок.
        /// </summary>
        /// <param name="pattern">Регулярное выражение для проверки значений.</param>
        public RegexValidator(string pattern)
        {
            this.pattern = pattern;
            this.regex = new Regex(pattern, RegexOptions.Compiled);
        }
        
        /// <summary>
        /// Проверяет, соответствует ли значение регулярному выражению.
        /// Преобразует значение в строку перед проверкой.
        /// Null значения считаются невалидными.
        /// </summary>
        /// <param name="value">Значение для проверки.</param>
        /// <returns>true если значение соответствует регулярному выражению, иначе false.</returns>
        public bool Validate(object value)
        {
            if (value == null) return false;
            return regex.IsMatch(value.ToString());
        }
        
        /// <summary>
        /// Получает сообщение об ошибке с информацией о требуемом шаблоне.
        /// </summary>
        /// <returns>Строка с описанием регулярного выражения которому должно соответствовать значение.</returns>
        public string GetErrorMessage()
        {
            return $"Value must match pattern: {pattern}";
        }
    }
    
    /// <summary>
    /// Валидатор для проверки, что значение не пусто.
    /// Проверяет что значение не null и его строковое представление не пусто.
    /// Используется для обязательных полей в таблице баланса.
    /// Реализует интерфейс IValidator.
    /// </summary>
    public class RequiredValidator : IValidator
    {
        /// <summary>
        /// Проверяет что значение не пусто и не null.
        /// </summary>
        /// <param name="value">Значение для проверки.</param>
        /// <returns>true если значение не null и не пусто, иначе false.</returns>
        public bool Validate(object value)
        {
            return value != null && !string.IsNullOrEmpty(value.ToString());
        }
        
        /// <summary>
        /// Получает сообщение об ошибке для обязательного поля.
        /// </summary>
        /// <returns>Строка "This field is required".</returns>
        public string GetErrorMessage()
        {
            return "This field is required";
        }
    }
}