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
            createdAtTicks = DateTime.UtcNow.Ticks;
        }
        
        public object GetValue(string columnId)
        {
            // КРИТИЧНО: Всегда десериализуем из сохранённых данных
            if (cellValues == null || cellValues.Count == 0)
                DeserializeCellValues();
                
            return cellValues.TryGetValue(columnId, out var value) ? value : null;
        }
        
        public void SetValue(string columnId, object value)
        {
            if (cellValues == null)
                cellValues = new Dictionary<string, object>();
            
            if (cellValuesSerialized == null)
                cellValuesSerialized = new SerializableDictionary<string, string>();
            
            // КРИТИЧНО: Обновляем ОБА словаря!
            cellValues[columnId] = value;
            
            // Сериализуем значение для сохранения
            string serializedValue = SerializeValue(value);
            cellValuesSerialized[columnId] = serializedValue;
        }
        
        public BalanceRow Clone()
        {
            var clone = new BalanceRow
            {
                rowId = Guid.NewGuid().ToString(),
                createdAtTicks = DateTime.UtcNow.Ticks
            };
            
            // Клонируем сериализованные значения
            if (cellValuesSerialized != null)
            {
                foreach (var kvp in cellValuesSerialized)
                {
                    clone.cellValuesSerialized[kvp.Key] = kvp.Value;
                }
            }
            
            // Десериализуем в runtime словарь
            clone.DeserializeCellValues();
            
            return clone;
        }
        
        private void DeserializeCellValues()
        {
            cellValues = new Dictionary<string, object>();
            
            if (cellValuesSerialized == null)
                return;
            
            foreach (var kvp in cellValuesSerialized)
            {
                // Десериализуем из строки обратно в object
                cellValues[kvp.Key] = kvp.Value;
            }
        }
        
        private string SerializeValue(object value)
        {
            if (value == null) 
                return string.Empty;
            
            // Для базовых типов просто ToString()
            if (value is string || value is int || value is float || value is bool)
                return value.ToString();
            
            // Для Unity типов используем JsonUtility
            if (value is Vector2 || value is Vector3 || value is Color)
                return JsonUtility.ToJson(value);
            
            // Для UnityEngine.Object сохраняем только имя (ссылку нельзя сериализовать)
            if (value is UnityEngine.Object unityObj)
                return unityObj != null ? unityObj.name : string.Empty;
            
            return value.ToString();
        }
        
        // Unity вызывает это перед сохранением
        public void OnBeforeSerialize()
        {
            // Убеждаемся что cellValuesSerialized актуален
            if (cellValues != null && cellValuesSerialized != null)
            {
                // Синхронизируем все значения
                foreach (var kvp in cellValues)
                {
                    if (!cellValuesSerialized.ContainsKey(kvp.Key))
                    {
                        cellValuesSerialized[kvp.Key] = SerializeValue(kvp.Value);
                    }
                }
            }
        }
    }
}