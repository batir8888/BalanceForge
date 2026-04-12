using System;
using System.Globalization;
using UnityEngine;
using BalanceForge.Services;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Определяет схему столбца таблицы баланса, включая тип данных, валидацию и значение по умолчанию.
    /// Поддерживает сериализацию/десериализацию различных типов (примитивы, Vector, Color, Enum, Asset References).
    /// Может содержать кастомный валидатор для проверки значений.
    /// </summary>
    [Serializable]
    public class ColumnDefinition
    {
        /// <summary>
        /// Уникальный идентификатор столбца.
        /// </summary>
        [SerializeField] private string columnId;
        
        /// <summary>
        /// Человекочитаемое имя столбца, отображаемое в UI.
        /// </summary>
        [SerializeField] private string displayName;
        
        /// <summary>
        /// Тип данных столбца.
        /// </summary>
        [SerializeField] private ColumnType dataType;
        
        /// <summary>
        /// Определяет является ли столбец обязательным (не может быть пустым).
        /// </summary>
        [SerializeField] private bool isRequired;
        
        /// <summary>
        /// Сериализованное значение по умолчанию для новых строк.
        /// </summary>
        [SerializeField] private string defaultValueSerialized;
        
        /// <summary>
        /// Определение перечисления для столбцов типа Enum.
        /// </summary>
        [SerializeField] private EnumDefinition enumDefinition;
        
        /// <summary>
        /// Assembly-qualified имя типа для Asset Reference столбцов.
        /// Используется при десериализации для восстановления правильного типа Unity Object.
        /// </summary>
        [SerializeField] private string assetType;
        
        /// <summary>
        /// Runtime валидатор для проверки значений ячеек этого столбца.
        /// </summary>
        [NonSerialized] private IValidator validator;
        
        /// <summary>
        /// Получает уникальный идентификатор столбца.
        /// </summary>
        public string ColumnId => columnId;
        
        /// <summary>
        /// Получает человекочитаемое имя столбца.
        /// </summary>
        public string DisplayName => displayName;
        
        /// <summary>
        /// Получает тип данных столбца.
        /// </summary>
        public ColumnType DataType => dataType;
        
        /// <summary>
        /// Получает значение указывающее является ли столбец обязательным.
        /// </summary>
        public bool IsRequired => isRequired;
        
        /// <summary>
        /// Получает определение перечисления для столбцов типа Enum.
        /// </summary>
        public EnumDefinition EnumDefinition => enumDefinition;
        
        /// <summary>
        /// Получает тип Unity Object для Asset Reference столбцов.
        /// </summary>
        public string AssetType => assetType;
        
        /// <summary>
        /// Получает или устанавливает значение по умолчанию для столбца.
        /// При получении десериализует сохраненное значение в правильный тип.
        /// При установке сериализует значение в строку для хранения.
        /// </summary>
        public object DefaultValue
        {
            get => DeserializeValue(defaultValueSerialized);
            set => defaultValueSerialized = SerializeValue(value);
        }
        
        /// <summary>
        /// Получает или устанавливает кастомный валидатор для проверки значений ячеек в этом столбце.
        /// </summary>
        public IValidator Validator
        {
            get => validator;
            set => validator = value;
        }
        
        /// <summary>
        /// Инициализирует новый экземпляр класса ColumnDefinition.
        /// </summary>
        /// <param name="id">Уникальный идентификатор столбца.</param>
        /// <param name="name">Человекочитаемое имя столбца для отображения в UI.</param>
        /// <param name="type">Тип данных столбца.</param>
        /// <param name="required">Является ли столбец обязательным (по умолчанию false).</param>
        /// <param name="defaultValue">Значение по умолчанию для новых строк (по умолчанию null).</param>
        public ColumnDefinition(string id, string name, ColumnType type, bool required = false, object defaultValue = null)
        {
            columnId = id;
            displayName = name;
            dataType = type;
            isRequired = required;
            DefaultValue = defaultValue;
            
            if (type == ColumnType.Enum)
            {
                enumDefinition = new EnumDefinition(name + "_Enum");
            }
        }
        
        /// <summary>
        /// Устанавливает тип Unity Object для Asset Reference столбцов.
        /// Сохраняет assembly-qualified имя типа для восстановления при десериализации.
        /// </summary>
        /// <param name="type">Тип Union Object (например, Prefab, Material, Texture2D). Если null, используется базовый UnityEngine.Object.</param>
        public void SetAssetType(Type type)
        {
            assetType = type?.AssemblyQualifiedName ?? typeof(UnityEngine.Object).AssemblyQualifiedName;
        }
        
        /// <summary>
        /// Получает тип Unity Object для Asset Reference столбцов.
        /// Десериализует сохраненное assembly-qualified имя типа.
        /// </summary>
        /// <returns>Тип для Asset Reference или UnityEngine.Object если тип не удалось восстановить.</returns>
        public Type GetAssetType()
        {
            if (string.IsNullOrEmpty(assetType))
                return typeof(UnityEngine.Object);
            return Type.GetType(assetType) ?? typeof(UnityEngine.Object);
        }
        
        /// <summary>
        /// Проверяет соответствие значения правилам валидации столбца.
        /// Если столбец обязательный, проверяет что значение не null и не пусто.
        /// Если установлен кастомный валидатор, также выполняет его проверку.
        /// </summary>
        /// <param name="value">Значение для проверки.</param>
        /// <returns>true если значение проходит всю валидацию, иначе false.</returns>
        public bool Validate(object value)
        {
            if (isRequired && (value == null || string.IsNullOrEmpty(value.ToString())))
                return false;
                
            return validator?.Validate(value) ?? true;
        }
        
        /// <summary>
        /// Получает строковое представление типа данных столбца.
        /// </summary>
        /// <returns>Строка с именем типа данных (String, Integer, Float, Vector2 и т.д.).</returns>
        public string GetTypeName()
        {
            return dataType.ToString();
        }
        
        /// <summary>
        /// Сериализует значение в строку в зависимости от типа столбца.
        /// Для сложных типов (Vector, Color) использует JsonUtility.
        /// Для примитивов и строк использует ToString().
        /// </summary>
        /// <param name="value">Значение для сериализации.</param>
        /// <returns>Строковое представление значения или пустая строка если значение null.</returns>
        private string SerializeValue(object value)
        {
            if (value == null) return string.Empty;
            
            switch (dataType)
            {
                case ColumnType.Vector2:
                    return JsonUtility.ToJson((Vector2)value);
                case ColumnType.Vector3:
                    return JsonUtility.ToJson((Vector3)value);
                case ColumnType.Color:
                    return JsonUtility.ToJson((Color)value);
                default:
                    return value.ToString();
            }
        }
        
        /// <summary>
        /// Десериализует строку в объект правильного типа согласно типу столбца.
        /// Обрабатывает ошибки парсинга, возвращая значение по умолчанию для типа.
        /// </summary>
        /// <param name="serialized">Строковое представление значения.</param>
        /// <returns>Десериализованное значение в правильном типе или значение по умолчанию если парсинг не удался.</returns>
        private object DeserializeValue(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) return GetDefaultForType();
            
            try
            {
                switch (dataType)
                {
                    case ColumnType.Integer:
                        return int.Parse(serialized, CultureInfo.InvariantCulture);
                    case ColumnType.Float:
                        return float.Parse(serialized, CultureInfo.InvariantCulture);
                    case ColumnType.Boolean:
                        return bool.Parse(serialized);
                    case ColumnType.Vector2:
                        return JsonUtility.FromJson<Vector2>(serialized);
                    case ColumnType.Vector3:
                        return JsonUtility.FromJson<Vector3>(serialized);
                    case ColumnType.Color:
                        return JsonUtility.FromJson<Color>(serialized);
                    case ColumnType.Enum:
                        return serialized;
                    default:
                        return serialized;
                }
            }
            catch
            {
                return GetDefaultForType();
            }
        }
        
        /// <summary>
        /// Получает значение по умолчанию для типа данных столбца.
        /// Используется при инициализации новых строк если не задано другое значение.
        /// </summary>
        /// <returns>Значение по умолчанию: 0 для Integer, 0f для Float, false для Boolean, zero для Vector, white для Color, первое значение enum или пустая строка.</returns>
        private object GetDefaultForType()
        {
            switch (dataType)
            {
                case ColumnType.Integer: return 0;
                case ColumnType.Float: return 0f;
                case ColumnType.Boolean: return false;
                case ColumnType.Vector2: return Vector2.zero;
                case ColumnType.Vector3: return Vector3.zero;
                case ColumnType.Color: return Color.white;
                case ColumnType.Enum: return (enumDefinition != null && enumDefinition.Values != null && enumDefinition.Values.Count > 0) ? enumDefinition.Values[0] : "";
                default: return string.Empty;
            }
        }
    }
}