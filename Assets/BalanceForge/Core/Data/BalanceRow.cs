using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Представляет строку таблицы баланса с поддержкой различных типов данных и Unity Object ссылок.
    /// Обеспечивает сериализацию и десериализацию значений ячеек с сохранением информации о типах.
    /// Поддерживает примитивные типы (int, float, bool), Vector2/3, Color и Asset References.
    /// </summary>
    [Serializable]
    public class BalanceRow : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Уникальный идентификатор строки.
        /// </summary>
        [SerializeField] private string rowId;
        
        /// <summary>
        /// Сериализованные значения ячеек в формате строк для хранения.
        /// </summary>
        [SerializeField] private SerializableDictionary<string, string> cellValuesSerialized;
        
        /// <summary>
        /// Карта типов данных для каждого столбца, используется при десериализации для правильного восстановления типов.
        /// </summary>
        [SerializeField] private SerializableDictionary<string, string> cellTypesMap;
        
        /// <summary>
        /// Список ссылок на Unity Objects (префабы, материалы, текстуры и т.д.).
        /// </summary>
        [SerializeField] private List<UnityEngine.Object> assetReferences = new List<UnityEngine.Object>();
        
        /// <summary>
        /// Ключи для соответствия между ячейками и asset references.
        /// </summary>
        [SerializeField] private List<string> assetReferenceKeys = new List<string>();
        
        /// <summary>
        /// Timestamp создания строки в формате Ticks для Unity сериализации.
        /// </summary>
        [SerializeField] private long createdAtTicks;
        
        /// <summary>
        /// Runtime словарь значений ячеек. Используется для быстрого доступа и не сохраняется.
        /// </summary>
        [NonSerialized] private Dictionary<string, object> cellValues;

        /// <summary>
        /// Флаг, указывающий что десериализация из сохранённых данных уже была выполнена.
        /// Предотвращает повторную десериализацию при каждом обращении к пустым строкам.
        /// </summary>
        [NonSerialized] private bool _isDeserialized;
        
        /// <summary>
        /// Получает уникальный идентификатор строки.
        /// </summary>
        public string RowId => rowId;
        
        /// <summary>
        /// Получает дату и время создания строки в UTC.
        /// </summary>
        public DateTime CreatedAt => new DateTime(createdAtTicks);
        
        /// <summary>
        /// Инициализирует новый экземпляр класса BalanceRow.
        /// Создает уникальный ID и инициализирует необходимые словари.
        /// </summary>
        public BalanceRow()
        {
            rowId = Guid.NewGuid().ToString();
            cellValues = new Dictionary<string, object>();
            cellValuesSerialized = new SerializableDictionary<string, string>();
            cellTypesMap = new SerializableDictionary<string, string>();
            createdAtTicks = DateTime.UtcNow.Ticks;
        }
        
        /// <summary>
        /// Получает значение ячейки по идентификатору столбца.
        /// Автоматически десериализует значения из сохраненных данных при необходимости.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца.</param>
        /// <returns>Значение ячейки в правильном типе или null если значение не найдено.</returns>
        public object GetValue(string columnId)
        {
            if (cellValues == null || !_isDeserialized)
                DeserializeCellValues();

            return cellValues.TryGetValue(columnId, out var value) ? value : null;
        }
        
        /// <summary>
        /// Устанавливает значение ячейки для указанного столбца.
        /// Автоматически определяет тип, сериализует значение и обновляет оба словаря (runtime и сохраненный).
        /// </summary>
        /// <param name="columnId">Идентификатор столбца.</param>
        /// <param name="value">Значение для установки (поддерживаются int, float, bool, string, Vector2, Vector3, Color, UnityEngine.Object).</param>
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
            string serializedValue = SerializeValue(value, typeName, columnId);
            cellValuesSerialized[columnId] = serializedValue;
        }
        
        /// <summary>
        /// Создает глубокую копию строки с новым ID и текущей временной меткой.
        /// Копирует все значения ячеек, типы и asset references.
        /// </summary>
        /// <returns>Новый клон строки с независимыми данными.</returns>
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
        
        /// <summary>
        /// Десериализует все значения ячеек из сохраненного формата в runtime словарь.
        /// Учитывает информацию о типах для правильного восстановления объектов.
        /// </summary>
        private void DeserializeCellValues()
        {
            cellValues = new Dictionary<string, object>();
            _isDeserialized = true;

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
        
        /// <summary>
        /// Определяет строковое представление типа для переданного значения.
        /// </summary>
        /// <param name="value">Объект для определения типа.</param>
        /// <returns>Строка с именем типа (String, Integer, Float, Boolean, Vector2, Vector3, Color, AssetReference).</returns>
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
        
        /// <summary>
        /// Сериализует значение в строку в зависимости от его типа.
        /// Для Unity Objects сохраняет ссылку в списки assetReferences и assetReferenceKeys.
        /// Для Vector и Color использует JsonUtility.
        /// </summary>
        /// <param name="value">Значение для сериализации.</param>
        /// <param name="typeName">Тип значения для выбора правильного способа сериализации.</param>
        /// <returns>Строковое представление значения.</returns>
        private string SerializeValue(object value, string typeName, string columnId = null)
        {
            if (value == null) 
                return string.Empty;
            
            switch (typeName)
            {
                case "String":
                case "Boolean":
                    return value.ToString();
                case "Integer":
                    return Convert.ToInt32(value).ToString(CultureInfo.InvariantCulture);
                case "Float":
                    return Convert.ToSingle(value).ToString("R", CultureInfo.InvariantCulture);
                    
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
                        string key = GetAssetKey(columnId, unityObj);
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
        
        /// <summary>
        /// Десериализует строку в объект правильного типа.
        /// Обрабатывает ошибки парсинга, возвращая значение по умолчанию для типа.
        /// </summary>
        /// <param name="serialized">Строковое представление значения.</param>
        /// <param name="typeName">Тип для десериализации.</param>
        /// <param name="columnId">Идентификатор столбца (для логирования ошибок).</param>
        /// <returns>Десериализованное значение или значение по умолчанию для типа.</returns>
        private object DeserializeValue(string serialized, string typeName, string columnId)
        {
            if (string.IsNullOrEmpty(serialized))
                return GetDefaultForType(typeName);
            
            try
            {
                switch (typeName)
                {
                    case "Integer":
                        return int.Parse(serialized, CultureInfo.InvariantCulture);

                    case "Float":
                        return float.Parse(serialized, CultureInfo.InvariantCulture);
                        
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
        
        /// <summary>
        /// Получает значение по умолчанию для указанного типа данных.
        /// </summary>
        /// <param name="typeName">Тип для которого требуется значение по умолчанию.</param>
        /// <returns>Значение по умолчанию: 0 для Integer, 0f для Float, false для Boolean, zero вектора/цвета, null для AssetReference, пустая строка для String.</returns>
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
        
        /// <summary>
        /// Генерирует уникальный ключ для asset reference на основе ID столбца и Instance ID объекта.
        /// </summary>
        /// <param name="columnId">Идентификатор столбца.</param>
        /// <param name="obj">Unity Object для получения Instance ID.</param>
        /// <returns>Уникальный ключ в формате "columnId_instanceId".</returns>
        private string GetAssetKey(string columnId, UnityEngine.Object obj)
        {
            return $"{columnId}_{obj.GetInstanceID()}";
        }
        
        /// <summary>
        /// Вызывается Unity перед сохранением для синхронизации runtime словаря с сериализованным словарем.
        /// Гарантирует что все значения из cellValues будут сохранены с правильными типами.
        /// </summary>
        public void OnBeforeSerialize()
        {
            if (cellValues == null || cellValuesSerialized == null)
                return;

            // Синхронизируем ВСЕ значения, включая обновлённые
            foreach (var kvp in cellValues)
            {
                string typeName = GetTypeName(kvp.Value);
                cellTypesMap[kvp.Key] = typeName;
                cellValuesSerialized[kvp.Key] = SerializeValue(kvp.Value, typeName, kvp.Key);
            }
        }

        /// <summary>
        /// Вызывается Unity после десериализации для сброса флага, чтобы runtime-словарь
        /// был перестроен из актуальных сериализованных данных при первом обращении.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _isDeserialized = false;
            cellValues = null;
        }
    }
    
    /// <summary>
    /// Вспомогательная структура для сериализации Vector2 с использованием JsonUtility.
    /// Необходима так как Unity не сериализует Vector2 напрямую в JSON.
    /// </summary>
    [Serializable]
    public struct Vector2Serializable
    {
        /// <summary>X компонент вектора.</summary>
        public float x;
        /// <summary>Y компонент вектора.</summary>
        public float y;
        
        /// <summary>
        /// Преобразует структуру обратно в Vector2.
        /// </summary>
        /// <returns>Vector2 с компонентами из структуры.</returns>
        public Vector2 ToVector2() => new Vector2(x, y);
        
        /// <summary>
        /// Создает серализуемую структуру из Vector2.
        /// </summary>
        /// <param name="v">Исходный Vector2.</param>
        /// <returns>Vector2Serializable с скопированными компонентами.</returns>
        public static Vector2Serializable FromVector2(Vector2 v) => new Vector2Serializable { x = v.x, y = v.y };
    }
    
    /// <summary>
    /// Вспомогательная структура для сериализации Vector3 с использованием JsonUtility.
    /// Необходима так как Unity не сериализует Vector3 напрямую в JSON.
    /// </summary>
    [Serializable]
    public struct Vector3Serializable
    {
        /// <summary>X компонент вектора.</summary>
        public float x;
        /// <summary>Y компонент вектора.</summary>
        public float y;
        /// <summary>Z компонент вектора.</summary>
        public float z;
        
        /// <summary>
        /// Преобразует структуру обратно в Vector3.
        /// </summary>
        /// <returns>Vector3 с компонентами из структуры.</returns>
        public Vector3 ToVector3() => new Vector3(x, y, z);
        
        /// <summary>
        /// Создает сериализуемую структуру из Vector3.
        /// </summary>
        /// <param name="v">Исходный Vector3.</param>
        /// <returns>Vector3Serializable с скопированными компонентами.</returns>
        public static Vector3Serializable FromVector3(Vector3 v) => new Vector3Serializable { x = v.x, y = v.y, z = v.z };
    }
    
    /// <summary>
    /// Вспомогательная структура для сериализации Color с использованием JsonUtility.
    /// Необходима так как Unity не сериализует Color напрямую в JSON.
    /// </summary>
    [Serializable]
    public struct ColorSerializable
    {
        /// <summary>Красный компонент цвета (0-1).</summary>
        public float r;
        /// <summary>Зеленый компонент цвета (0-1).</summary>
        public float g;
        /// <summary>Синий компонент цвета (0-1).</summary>
        public float b;
        /// <summary>Альфа (прозрачность) компонент цвета (0-1).</summary>
        public float a;
        
        /// <summary>
        /// Преобразует структуру обратно в Color.
        /// </summary>
        /// <returns>Color с компонентами из структуры.</returns>
        public Color ToColor() => new Color(r, g, b, a);
        
        /// <summary>
        /// Создает сериализуемую структуру из Color.
        /// </summary>
        /// <param name="c">Исходный Color.</param>
        /// <returns>ColorSerializable с скопированными компонентами.</returns>
        public static ColorSerializable FromColor(Color c) => new ColorSerializable { r = c.r, g = c.g, b = c.b, a = c.a };
    }
}