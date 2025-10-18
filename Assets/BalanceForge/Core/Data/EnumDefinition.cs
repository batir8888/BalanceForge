using System;
using System.Collections.Generic;
using UnityEngine;

namespace BalanceForge.Core.Data
{
    [Serializable]
    public class EnumDefinition
    {
        [SerializeField] private string enumName;
        [SerializeField] private List<string> values = new List<string>();
        
        public string EnumName => enumName;
        public List<string> Values => values;
        
        public EnumDefinition(string name)
        {
            enumName = name;
        }
        
        public void AddValue(string value)
        {
            if (!values.Contains(value))
                values.Add(value);
        }
        
        public void RemoveValue(string value)
        {
            values.Remove(value);
        }
        
        public int GetIndex(string value)
        {
            return values.IndexOf(value);
        }
    }
}