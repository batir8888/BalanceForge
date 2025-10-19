using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    [Serializable]
    public class BalanceRow
    {
        [SerializeField] private string rowId;
        [SerializeField] private SerializableDictionary<string, string> cellValuesSerialized;
        [SerializeField] private long createdAtTicks;
        
        [NonSerialized] private Dictionary<string, object> cellValues;
        [NonSerialized] private bool isDeserialized = false;
        
        public string RowId => rowId;
        public DateTime CreatedAt => new DateTime(createdAtTicks);
        
        public BalanceRow()
        {
            rowId = Guid.NewGuid().ToString();
            cellValues = new Dictionary<string, object>();
            cellValuesSerialized = new SerializableDictionary<string, string>();
            createdAtTicks = DateTime.Now.Ticks;
            isDeserialized = true;
        }
        
        public object GetValue(string columnId)
        {
            // Lazy deserialization
            if (!isDeserialized)
            {
                DeserializeCellValues();
                isDeserialized = true;
            }
            
            return cellValues != null && cellValues.TryGetValue(columnId, out var value) ? value : null;
        }
        
        public void SetValue(string columnId, object value)
        {
            if (cellValues == null)
            {
                cellValues = new Dictionary<string, object>();
                isDeserialized = true;
            }
            
            cellValues[columnId] = value;
            
            // Update serialized version
            if (cellValuesSerialized == null)
                cellValuesSerialized = new SerializableDictionary<string, string>();
            
            cellValuesSerialized[columnId] = value?.ToString() ?? string.Empty;
        }
        
        public BalanceRow Clone()
        {
            var clone = new BalanceRow
            {
                rowId = Guid.NewGuid().ToString(),
                createdAtTicks = DateTime.Now.Ticks
            };
            
            // Clone from serialized data to avoid unnecessary deserialization
            if (cellValuesSerialized != null)
            {
                clone.cellValuesSerialized = new SerializableDictionary<string, string>();
                foreach (var kvp in cellValuesSerialized)
                {
                    clone.cellValuesSerialized[kvp.Key] = kvp.Value;
                }
            }
            
            return clone;
        }
        
        private void DeserializeCellValues()
        {
            if (cellValues == null)
                cellValues = new Dictionary<string, object>();
            else
                cellValues.Clear();
            
            if (cellValuesSerialized != null)
            {
                foreach (var kvp in cellValuesSerialized)
                {
                    cellValues[kvp.Key] = kvp.Value;
                }
            }
        }
        
        // Optimization: Pre-warm deserialization for batch operations
        public void EnsureDeserialized()
        {
            if (!isDeserialized)
            {
                DeserializeCellValues();
                isDeserialized = true;
            }
        }
    }
}