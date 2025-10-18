using System.Text.RegularExpressions;

namespace BalanceForge.Services
{
    public class RangeValidator : IValidator
    {
        private float minValue;
        private float maxValue;
        
        public RangeValidator(float min, float max)
        {
            minValue = min;
            maxValue = max;
        }
        
        public bool Validate(object value)
        {
            if (value == null) return false;
            
            if (float.TryParse(value.ToString(), out float floatValue))
            {
                return floatValue >= minValue && floatValue <= maxValue;
            }
            
            return false;
        }
        
        public string GetErrorMessage()
        {
            return $"Value must be between {minValue} and {maxValue}";
        }
    }
    
    public class RegexValidator : IValidator
    {
        private string pattern;
        private Regex regex;
        
        public RegexValidator(string pattern)
        {
            this.pattern = pattern;
            this.regex = new Regex(pattern);
        }
        
        public bool Validate(object value)
        {
            if (value == null) return false;
            return regex.IsMatch(value.ToString());
        }
        
        public string GetErrorMessage()
        {
            return $"Value must match pattern: {pattern}";
        }
    }
    
    public class RequiredValidator : IValidator
    {
        public bool Validate(object value)
        {
            return value != null && !string.IsNullOrEmpty(value.ToString());
        }
        
        public string GetErrorMessage()
        {
            return "This field is required";
        }
    }
}