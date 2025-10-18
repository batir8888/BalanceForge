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
        
        public string RowId => rowId;
        public DateTime CreatedAt => new DateTime(createdAtTicks);
        
        public BalanceRow()
        {
            rowId = Guid.NewGuid().ToString();
            cellValues = new Dictionary<string, object>();
            cellValuesSerialized = new SerializableDictionary<string, string>();
            createdAtTicks = DateTime.Now.Ticks;
        }
        
        public object GetValue(string columnId)
        {
            if (cellValues == null)
                DeserializeCellValues();
                
            return cellValues.TryGetValue(columnId, out var value) ? value : null;
        }
        
        public void SetValue(string columnId, object value)
        {
            if (cellValues == null)
                cellValues = new Dictionary<string, object>();
                
            cellValues[columnId] = value;
            cellValuesSerialized[columnId] = value?.ToString() ?? string.Empty;
        }
        
        public BalanceRow Clone()
        {
            var clone = new BalanceRow
            {
                rowId = Guid.NewGuid().ToString(),
                createdAtTicks = DateTime.Now.Ticks
            };
            
            if (cellValues != null)
            {
                foreach (var kvp in cellValues)
                {
                    clone.SetValue(kvp.Key, kvp.Value);
                }
            }
            
            return clone;
        }
        
        private void DeserializeCellValues()
        {
            cellValues = new Dictionary<string, object>();
            foreach (var kvp in cellValuesSerialized)
            {
                cellValues[kvp.Key] = kvp.Value;
            }
        }
    }
}