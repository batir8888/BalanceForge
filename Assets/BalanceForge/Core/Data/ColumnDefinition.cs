using System;
using UnityEngine;
using BalanceForge.Services;

namespace BalanceForge.Core.Data
{
    [Serializable]
    public class ColumnDefinition
    {
        [SerializeField] private string columnId;
        [SerializeField] private string displayName;
        [SerializeField] private ColumnType dataType;
        [SerializeField] private bool isRequired;
        [SerializeField] private string defaultValueSerialized;
        [SerializeField] private EnumDefinition enumDefinition;
        [SerializeField] private string assetType; // For AssetReference columns
        
        [NonSerialized] private IValidator validator;
        
        public string ColumnId => columnId;
        public string DisplayName => displayName;
        public ColumnType DataType => dataType;
        public bool IsRequired => isRequired;
        public EnumDefinition EnumDefinition => enumDefinition;
        public string AssetType => assetType;
        
        public object DefaultValue
        {
            get => DeserializeValue(defaultValueSerialized);
            set => defaultValueSerialized = SerializeValue(value);
        }
        
        public IValidator Validator
        {
            get => validator;
            set => validator = value;
        }
        
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
        
        public void SetAssetType(Type type)
        {
            assetType = type?.AssemblyQualifiedName ?? typeof(UnityEngine.Object).AssemblyQualifiedName;
        }
        
        public Type GetAssetType()
        {
            if (string.IsNullOrEmpty(assetType))
                return typeof(UnityEngine.Object);
            return Type.GetType(assetType) ?? typeof(UnityEngine.Object);
        }
        
        public bool Validate(object value)
        {
            if (isRequired && (value == null || string.IsNullOrEmpty(value.ToString())))
                return false;
                
            return validator?.Validate(value) ?? true;
        }
        
        public string GetTypeName()
        {
            return dataType.ToString();
        }
        
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
        
        private object DeserializeValue(string serialized)
        {
            if (string.IsNullOrEmpty(serialized)) return GetDefaultForType();
            
            try
            {
                switch (dataType)
                {
                    case ColumnType.Integer:
                        return int.Parse(serialized);
                    case ColumnType.Float:
                        return float.Parse(serialized);
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
                case ColumnType.Enum: return enumDefinition?.Values.Count > 0 ? enumDefinition.Values[0] : "";
                default: return string.Empty;
            }
        }
    }
}