using System.Collections.Generic;
using UnityEngine;
using BalanceForge.Core.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BalanceForge.Services
{
    public class TableManager
    {
        private Dictionary<string, BalanceTable> loadedTables = new Dictionary<string, BalanceTable>();
        private static TableManager instance;
        
        public static TableManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new TableManager();
                return instance;
            }
        }
        
        public BalanceTable LoadTable(string assetPath)
        {
            #if UNITY_EDITOR
            var table = AssetDatabase.LoadAssetAtPath<BalanceTable>(assetPath);
            if (table != null)
            {
                loadedTables[table.TableId] = table;
            }
            return table;
            #else
            return null;
            #endif
        }
        
        public void SaveTable(BalanceTable table)
        {
            #if UNITY_EDITOR
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            #endif
        }
        
        public BalanceTable CreateTable(string name, List<ColumnDefinition> columns)
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.name = name;
            
            foreach (var column in columns)
            {
                table.Columns.Add(column);
            }
            
            #if UNITY_EDITOR
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Balance Table",
                name,
                "asset",
                "Choose where to save the balance table"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(table, path);
                AssetDatabase.SaveAssets();
                loadedTables[table.TableId] = table;
                return table;
            }
            #endif
            
            return null;
        }
        
        public bool DeleteTable(string tableId)
        {
            if (loadedTables.TryGetValue(tableId, out var table))
            {
                loadedTables.Remove(tableId);
                #if UNITY_EDITOR
                string path = AssetDatabase.GetAssetPath(table);
                AssetDatabase.DeleteAsset(path);
                #endif
                return true;
            }
            return false;
        }
    }
}