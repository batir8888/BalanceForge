using System.Collections.Generic;

namespace BalanceForge.Core.Data
{
    public class ValidationResult
    {
        private bool isValid;
        private List<ValidationError> errors;
        
        public bool IsValid => isValid && errors.Count == 0;
        public List<ValidationError> Errors => errors;
        
        public ValidationResult()
        {
            isValid = true;
            errors = new List<ValidationError>();
        }
        
        public void AddError(ValidationError error)
        {
            errors.Add(error);
            isValid = false;
        }
        
        public bool HasErrors()
        {
            return errors.Count > 0;
        }
    }
    
    public class ValidationError
    {
        public string RowId { get; set; }
        public string ColumnId { get; set; }
        public string Message { get; set; }
        public ErrorSeverity Severity { get; set; }
        
        public ValidationError(string rowId, string columnId, string message, ErrorSeverity severity = ErrorSeverity.Error)
        {
            RowId = rowId;
            ColumnId = columnId;
            Message = message;
            Severity = severity;
        }
    }
    
    public enum ErrorSeverity
    {
        Warning,
        Error,
        Critical
    }
}