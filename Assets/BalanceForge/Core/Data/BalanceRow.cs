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
        [SerializeField] private SerializableDictionary<string, string> cellTypesMap; // НОВОЕ: Храним типы для правильной десериализации
        [SerializeField] private List<UnityEngine.Object> assetReferences = new List<UnityEngine.Object>(); // НОВОЕ: Для Unity Objects
        [SerializeField] private List<string> assetReferenceKeys = new List<string>(); // НОВОЕ: Ключи для asset references
        [SerializeField] private long createdAtTicks;
        
        [NonSerialized] private Dictionary<string, object> cellValues;
        
        public string RowId => rowId;
        public DateTime CreatedAt => new DateTime(createdAtTicks);
        
        public BalanceRow()
        {
            rowId = Guid.NewGuid().ToString();
            cellValues = new Dictionary<string, object>();
            cellValuesSerialized = new SerializableDictionary<string, string>();
            cellTypesMap = new SerializableDictionary<string, string>();
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
                
            if (cellTypesMap == null)
                cellTypesMap = new SerializableDictionary<string, string>();
            
            // КРИТИЧНО: Обновляем ОБА словаря!
            cellValues[columnId] = value;
            
            // Сохраняем тип для правильной десериализации
            string typeName = GetTypeName(value);
            cellTypesMap[columnId] = typeName;
            
            // Сериализуем значение для сохранения
            string serializedValue = SerializeValue(value, typeName);
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
            
            // Клонируем карту типов
            if (cellTypesMap != null)
            {
                foreach (var kvp in cellTypesMap)
                {
                    clone.cellTypesMap[kvp.Key] = kvp.Value;
                }
            }
            
            // Клонируем asset references
            if (assetReferences != null && assetReferenceKeys != null)
            {
                for (int i = 0; i < assetReferenceKeys.Count; i++)
                {
                    clone.assetReferenceKeys.Add(assetReferenceKeys[i]);
                    if (i < assetReferences.Count)
                        clone.assetReferences.Add(assetReferences[i]);
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
            
            // Инициализируем карту типов если её нет (для старых данных)
            if (cellTypesMap == null)
                cellTypesMap = new SerializableDictionary<string, string>();
            
            foreach (var kvp in cellValuesSerialized)
            {
                string columnId = kvp.Key;
                string serializedValue = kvp.Value;
                
                // Получаем тип данных
                string typeName = cellTypesMap.ContainsKey(columnId) ? cellTypesMap[columnId] : "String";
                
                // ИСПРАВЛЕНО: Десериализуем с учётом типа
                cellValues[columnId] = DeserializeValue(serializedValue, typeName, columnId);
            }
        }
        
        private string GetTypeName(object value)
        {
            if (value == null) return "String";
            
            if (value is string) return "String";
            if (value is int) return "Integer";
            if (value is float) return "Float";
            if (value is bool) return "Boolean";
            if (value is Vector2) return "Vector2";
            if (value is Vector3) return "Vector3";
            if (value is Color) return "Color";
            if (value is UnityEngine.Object) return "AssetReference";
            
            return "String";
        }
        
        private string SerializeValue(object value, string typeName)
        {
            if (value == null) 
                return string.Empty;
            
            switch (typeName)
            {
                case "String":
                case "Integer":
                case "Float":
                case "Boolean":
                    return value.ToString();
                    
                case "Vector2":
                    return JsonUtility.ToJson(Vector2Serializable.FromVector2((Vector2)value));
                    
                case "Vector3":
                    return JsonUtility.ToJson(Vector3Serializable.FromVector3((Vector3)value));
                    
                case "Color":
                    return JsonUtility.ToJson(ColorSerializable.FromColor((Color)value));
                    
                case "AssetReference":
                    // Unity Objects сохраняем отдельно
                    if (value is UnityEngine.Object unityObj)
                    {
                        string key = GetAssetKey(typeName, unityObj);
                        int index = assetReferenceKeys.IndexOf(key);
                        if (index == -1)
                        {
                            assetReferenceKeys.Add(key);
                            assetReferences.Add(unityObj);
                            index = assetReferences.Count - 1;
                        }
                        return index.ToString();
                    }
                    return string.Empty;
                    
                default:
                    return value.ToString();
            }
        }
        
        private object DeserializeValue(string serialized, string typeName, string columnId)
        {
            if (string.IsNullOrEmpty(serialized))
                return GetDefaultForType(typeName);
            
            try
            {
                switch (typeName)
                {
                    case "Integer":
                        return int.Parse(serialized);
                        
                    case "Float":
                        return float.Parse(serialized);
                        
                    case "Boolean":
                        return bool.Parse(serialized);
                        
                    case "Vector2":
                        // ИСПРАВЛЕНО: Парсим JSON обратно в Vector2
                        return JsonUtility.FromJson<Vector2Serializable>(serialized).ToVector2();
                        
                    case "Vector3":
                        // ИСПРАВЛЕНО: Парсим JSON обратно в Vector3
                        return JsonUtility.FromJson<Vector3Serializable>(serialized).ToVector3();
                        
                    case "Color":
                        // ИСПРАВЛЕНО: Парсим JSON обратно в Color
                        return JsonUtility.FromJson<ColorSerializable>(serialized).ToColor();
                        
                    case "AssetReference":
                        // ИСПРАВЛЕНО: Восстанавливаем Unity Object из списка
                        if (int.TryParse(serialized, out int index))
                        {
                            if (index >= 0 && index < assetReferences.Count)
                                return assetReferences[index];
                        }
                        return null;
                        
                    case "String":
                    default:
                        return serialized;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize value for column {columnId}: {ex.Message}");
                return GetDefaultForType(typeName);
            }
        }
        
        private object GetDefaultForType(string typeName)
        {
            switch (typeName)
            {
                case "Integer": return 0;
                case "Float": return 0f;
                case "Boolean": return false;
                case "Vector2": return Vector2.zero;
                case "Vector3": return Vector3.zero;
                case "Color": return Color.white;
                case "AssetReference": return null;
                default: return string.Empty;
            }
        }
        
        private string GetAssetKey(string columnId, UnityEngine.Object obj)
        {
            return $"{columnId}_{obj.GetInstanceID()}";
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
                        string typeName = GetTypeName(kvp.Value);
                        cellTypesMap[kvp.Key] = typeName;
                        cellValuesSerialized[kvp.Key] = SerializeValue(kvp.Value, typeName);
                    }
                }
            }
        }
    }
    
    // Вспомогательные классы для сериализации Unity типов
    [Serializable]
    public struct Vector2Serializable
    {
        public float x;
        public float y;
        
        public Vector2 ToVector2() => new Vector2(x, y);
        public static Vector2Serializable FromVector2(Vector2 v) => new Vector2Serializable { x = v.x, y = v.y };
    }
    
    [Serializable]
    public struct Vector3Serializable
    {
        public float x;
        public float y;
        public float z;
        
        public Vector3 ToVector3() => new Vector3(x, y, z);
        public static Vector3Serializable FromVector3(Vector3 v) => new Vector3Serializable { x = v.x, y = v.y, z = v.z };
    }
    
    [Serializable]
    public struct ColorSerializable
    {
        public float r;
        public float g;
        public float b;
        public float a;
        
        public Color ToColor() => new Color(r, g, b, a);
        public static ColorSerializable FromColor(Color c) => new ColorSerializable { r = c.r, g = c.g, b = c.b, a = c.a };
    }
}