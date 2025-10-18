#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BalanceForge.Core.Data;

namespace BalanceForge.Editor.Windows
{
    public class CreateTableWizard : EditorWindow
    {
        private string tableName = "NewBalanceTable";
        private List<ColumnDefinition> columns = new List<ColumnDefinition>();
        private Vector2 scrollPosition;
        
        [MenuItem("Assets/Create/BalanceForge/Balance Table Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<CreateTableWizard>("Create Balance Table");
            window.minSize = new Vector2(500, 400);
        }
        
        private void OnEnable()
        {
            if (columns.Count == 0)
            {
                // Add default column
                columns.Add(new ColumnDefinition("id", "ID", ColumnType.String, true));
            }
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Create New Balance Table", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            tableName = EditorGUILayout.TextField("Table Name", tableName);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Columns:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < columns.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"Column {i + 1}", GUILayout.Width(70));
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    columns.RemoveAt(i);
                    i--;
                    continue;
                }
                
                EditorGUILayout.EndHorizontal();
                
                var col = columns[i];
                var newName = EditorGUILayout.TextField("Name", col.DisplayName);
                var newType = (ColumnType)EditorGUILayout.EnumPopup("Type", col.DataType);
                var newRequired = EditorGUILayout.Toggle("Required", col.IsRequired);
                
                // Update column if changed
                if (newName != col.DisplayName || newType != col.DataType || newRequired != col.IsRequired)
                {
                    columns[i] = new ColumnDefinition(
                        $"col_{i}",
                        newName,
                        newType,
                        newRequired
                    );
                    
                    // If enum type, setup enum values
                    if (newType == ColumnType.Enum && columns[i].EnumDefinition != null)
                    {
                        EditorGUILayout.LabelField("Enum Values:");
                        EditorGUI.indentLevel++;
                        
                        var enumDef = columns[i].EnumDefinition;
                        for (int j = 0; j < enumDef.Values.Count; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            enumDef.Values[j] = EditorGUILayout.TextField(enumDef.Values[j]);
                            if (GUILayout.Button("-", GUILayout.Width(20)))
                            {
                                enumDef.Values.RemoveAt(j);
                                j--;
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        if (GUILayout.Button("Add Enum Value", GUILayout.Width(120)))
                        {
                            enumDef.AddValue($"Value_{enumDef.Values.Count}");
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    // If asset reference, select asset type
                    if (newType == ColumnType.AssetReference)
                    {
                        EditorGUILayout.LabelField("Asset Type: GameObject/ScriptableObject/etc");
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Add Column"))
            {
                columns.Add(new ColumnDefinition(
                    $"col_{columns.Count}",
                    $"Column {columns.Count + 1}",
                    ColumnType.String,
                    false
                ));
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(tableName) && columns.Count > 0;
            if (GUILayout.Button("Create Table", GUILayout.Height(30)))
            {
                CreateTable();
            }
            GUI.enabled = true;
            
            if (GUILayout.Button("Cancel", GUILayout.Height(30)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void CreateTable()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = tableName;
            
            foreach (var column in columns)
            {
                table.AddColumn(column);
            }
            
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Balance Table",
                tableName,
                "asset",
                "Choose where to save the balance table"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(table, path);
                AssetDatabase.SaveAssets();
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = table;
                
                Close();
                
                // Open in editor
                var editorWindow = GetWindow<UI.BalanceTableEditorWindow>();
                editorWindow.LoadTable(table);
            }
        }
    }
}
#endif