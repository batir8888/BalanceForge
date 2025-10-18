#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BalanceForge.Core.Data;
using BalanceForge.Services;
using System.Collections.Generic;

namespace BalanceForge.Editor.UI
{
    public class BalanceTableEditorWindow : EditorWindow
    {
        private BalanceTable currentTable;
        private TableViewController viewController;
        private UndoRedoService undoRedoService;
        private Vector2 scrollPosition;
        private bool showValidation = false;
        private ValidationResult validationResult;
        
        [MenuItem("Window/BalanceForge/Table Editor")]
        public static void ShowWindow()
        {
            GetWindow<BalanceTableEditorWindow>("Balance Table Editor");
        }
        
        private void OnEnable()
        {
            undoRedoService = new UndoRedoService();
            viewController = new TableViewController();
        }
        
        private void OnGUI()
        {
            DrawToolbar();
            
            EditorGUILayout.Space();
            
            if (currentTable != null)
            {
                DrawTable();
                DrawStatusBar();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Balance Table to edit", MessageType.Info);
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            // Table selection
            var newTable = (BalanceTable)EditorGUILayout.ObjectField(
                currentTable, 
                typeof(BalanceTable), 
                false,
                GUILayout.Width(200)
            );
            
            if (newTable != currentTable)
            {
                currentTable = newTable;
                viewController.SetTable(currentTable);
                undoRedoService.Clear();
            }
            
            GUILayout.FlexibleSpace();
            
            if (currentTable != null)
            {
                // Add Row
                if (GUILayout.Button("Add Row", EditorStyles.toolbarButton))
                {
                    var newRow = currentTable.AddRow();
                    var command = new AddRowCommand(currentTable, newRow);
                    undoRedoService.ExecuteCommand(command);
                }
                
                // Delete Selected Rows
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton))
                {
                    var selectedRows = viewController.GetSelectedRows();
                    foreach (var row in selectedRows)
                    {
                        var command = new DeleteRowCommand(currentTable, row);
                        undoRedoService.ExecuteCommand(command);
                    }
                    viewController.ClearSelection();
                }
                
                GUILayout.Space(10);
                
                // Validate
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    validationResult = currentTable.ValidateData();
                    showValidation = true;
                }
                
                // Save
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    TableManager.Instance.SaveTable(currentTable);
                    Debug.Log($"Table '{currentTable.TableName}' saved successfully!");
                }
                
                GUILayout.Space(10);
                
                // Undo/Redo
                GUI.enabled = undoRedoService.CanUndo();
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Undo();
                    Repaint();
                }
                GUI.enabled = undoRedoService.CanRedo();
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Redo();
                    Repaint();
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTable()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            if (showValidation && validationResult != null && validationResult.HasErrors())
            {
                EditorGUILayout.HelpBox($"Found {validationResult.Errors.Count} validation errors", MessageType.Warning);
                if (GUILayout.Button("Hide Validation"))
                {
                    showValidation = false;
                }
                EditorGUILayout.Space();
            }
            
            viewController.Draw(undoRedoService);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Rows: {currentTable.Rows.Count} | Columns: {currentTable.Columns.Count}");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Last Modified: {currentTable.LastModified:yyyy-MM-dd HH:mm}");
            EditorGUILayout.EndHorizontal();
        }
    }
    
    public class TableViewController
    {
        private BalanceTable table;
        private HashSet<string> selectedRowIds = new HashSet<string>();
        private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
        
        public void SetTable(BalanceTable newTable)
        {
            table = newTable;
            selectedRowIds.Clear();
            foldouts.Clear();
        }
        
        public void Draw(UndoRedoService undoRedoService)
        {
            if (table == null) return;
            
            // Draw header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Select", GUILayout.Width(50));
            
            foreach (var column in table.Columns)
            {
                GUILayout.Label(column.DisplayName, GUILayout.Width(150));
            }
            EditorGUILayout.EndHorizontal();
            
            // Draw separator
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Draw rows
            foreach (var row in table.Rows)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Selection checkbox
                bool isSelected = selectedRowIds.Contains(row.RowId);
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(50));
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        selectedRowIds.Add(row.RowId);
                    else
                        selectedRowIds.Remove(row.RowId);
                }
                
                // Draw cells
                foreach (var column in table.Columns)
                {
                    var oldValue = row.GetValue(column.ColumnId);
                    var newValue = DrawCell(column, oldValue);
                    
                    if (!Equals(oldValue, newValue))
                    {
                        var command = new EditCellCommand(table, row.RowId, column.ColumnId, oldValue, newValue);
                        undoRedoService.ExecuteCommand(command);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private object DrawCell(ColumnDefinition column, object value)
        {
            object newValue = value;
            
            switch (column.DataType)
            {
                case ColumnType.String:
                    newValue = EditorGUILayout.TextField(value?.ToString() ?? "", GUILayout.Width(150));
                    break;
                case ColumnType.Integer:
                    int intVal = value != null ? System.Convert.ToInt32(value) : 0;
                    newValue = EditorGUILayout.IntField(intVal, GUILayout.Width(150));
                    break;
                case ColumnType.Float:
                    float floatVal = value != null ? System.Convert.ToSingle(value) : 0f;
                    newValue = EditorGUILayout.FloatField(floatVal, GUILayout.Width(150));
                    break;
                case ColumnType.Boolean:
                    bool boolVal = value != null && System.Convert.ToBoolean(value);
                    newValue = EditorGUILayout.Toggle(boolVal, GUILayout.Width(150));
                    break;
                case ColumnType.Color:
                    Color colorVal = value is Color ? (Color)value : Color.white;
                    newValue = EditorGUILayout.ColorField(colorVal, GUILayout.Width(150));
                    break;
                case ColumnType.Vector2:
                    Vector2 vec2Val = value is Vector2 ? (Vector2)value : Vector2.zero;
                    newValue = EditorGUILayout.Vector2Field("", vec2Val, GUILayout.Width(150));
                    break;
                case ColumnType.Vector3:
                    Vector3 vec3Val = value is Vector3 ? (Vector3)value : Vector3.zero;
                    newValue = EditorGUILayout.Vector3Field("", vec3Val, GUILayout.Width(150));
                    break;
                case ColumnType.AssetReference:
                    newValue = EditorGUILayout.ObjectField(
                        value as UnityEngine.Object,
                        typeof(UnityEngine.Object),
                        false,
                        GUILayout.Width(150)
                    );
                    break;
            }
            
            return newValue;
        }
        
        public List<BalanceRow> GetSelectedRows()
        {
            var selected = new List<BalanceRow>();
            foreach (var rowId in selectedRowIds)
            {
                var row = table.Rows.Find(r => r.RowId == rowId);
                if (row != null)
                    selected.Add(row);
            }
            return selected;
        }
        
        public void ClearSelection()
        {
            selectedRowIds.Clear();
        }
    }
}
#endif