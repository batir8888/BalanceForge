using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    /// <summary>
    /// Сериализуемый словарь для Unity, поддерживающий сохранение и загрузку из сцен и ScriptableObjects.
    /// Наследует от Dictionary&lt;TKey, TValue&gt; и реализует ISerializationCallbackReceiver для интеграции с системой сериализации Unity.
    /// Преобразует словарь в два отдельных списка (ключи и значения) для сериализации, так как Unity не может напрямую сериализовать Dictionary.
    /// </summary>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Список ключей словаря, сохраняется для сериализации.
        /// </summary>
        [SerializeField] private List<TKey> keys = new List<TKey>();
        
        /// <summary>
        /// Список значений словаря, сохраняется для сериализации.
        /// Индексы в этом списке соответствуют индексам в списке ключей.
        /// </summary>
        [SerializeField] private List<TValue> values = new List<TValue>();
        
        /// <summary>
        /// Вызывается Unity перед сохранением объекта.
        /// Копирует все пары ключ-значение из словаря в списки keys и values для сериализации.
        /// </summary>
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }
        
        /// <summary>
        /// Вызывается Unity после загрузки сериализованных данных.
        /// Восстанавливает словарь из списков keys и values, восстанавливая исходные пары ключ-значение.
        /// Использует Math.Min для защиты от несовпадения размеров списков.
        /// </summary>
        public void OnAfterDeserialize()
        {
            Clear();
            
            for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
            {
                this[keys[i]] = values[i];
            }
        }
    }
}