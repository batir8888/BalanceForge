using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    [CreateAssetMenu(fileName = "NewBalanceTable", menuName = "BalanceForge/Balance Table")]
    public class BalanceTable : ScriptableObject
    {
        [SerializeField] private string tableName;
        [SerializeField] private string tableId;
        [SerializeField] private List<ColumnDefinition> columns = new List<ColumnDefinition>();
        [SerializeField] private List<BalanceRow> rows = new List<BalanceRow>();
        [SerializeField] private long lastModifiedTicks;
        
        public string TableName => tableName;
        public string TableId => tableId;
        public List<ColumnDefinition> Columns => columns;
        public List<BalanceRow> Rows => rows;
        public DateTime LastModified => new DateTime(lastModifiedTicks);
        
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(tableId))
                tableId = Guid.NewGuid().ToString();
        }
        
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
        
        public BalanceRow GetRow(int index)
        {
            if (index >= 0 && index < rows.Count)
                return rows[index];
            return null;
        }
        
        public ColumnDefinition GetColumn(string columnId)
        {
            return columns.FirstOrDefault(c => c.ColumnId == columnId);
        }
        
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
        
        private void UpdateLastModified()
        {
            lastModifiedTicks = DateTime.Now.Ticks;
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}