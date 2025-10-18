#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using BalanceForge.Core.Data;
using BalanceForge.Services;
using BalanceForge.Data.Operations;
using BalanceForge.ImportExport;
using System.Collections.Generic;
using System.Linq;

namespace BalanceForge.Editor.UI
{
    public class BalanceTableEditorWindow : EditorWindow
    {
        private BalanceTable currentTable;
        private UndoRedoService undoRedoService;
        private Vector2 scrollPosition;
        private bool showValidation = false;
        private ValidationResult validationResult;
        
        // Sorting
        private SortingState sortingState = new SortingState();
        private List<BalanceRow> displayedRows;
        
        // Filtering
        private bool showFilterPanel = false;
        private List<FilterCondition> filterConditions = new List<FilterCondition>();
        private LogicalOperator filterLogicalOp = LogicalOperator.And;
        
        // Selection
        private HashSet<string> selectedRowIds = new HashSet<string>();
        private string focusedCellRowId;
        private string focusedCellColumnId;
        
        // Context Menu
        private GenericMenu contextMenu;
        
        [MenuItem("Window/BalanceForge/Table Editor")]
        public static void ShowWindow()
        {
            GetWindow<BalanceTableEditorWindow>("Balance Table Editor");
        }
        
        private void OnEnable()
        {
            undoRedoService = new UndoRedoService();
            displayedRows = new List<BalanceRow>();
        }
        
        public void LoadTable(BalanceTable table)
        {
            currentTable = table;
            RefreshDisplayedRows();
            undoRedoService.Clear();
        }
        
        private void OnGUI()
        {
            // Handle keyboard shortcuts
            HandleKeyboardShortcuts();
            
            DrawToolbar();
            
            EditorGUILayout.Space();
            
            if (currentTable != null)
            {
                // Check if file is editable
                if (!IsFileEditable())
                {
                    EditorGUILayout.HelpBox("This file is read-only. You cannot make changes.", MessageType.Warning);
                }
                
                if (showFilterPanel)
                {
                    DrawFilterPanel();
                }
                
                DrawTable();
                DrawStatusBar();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Balance Table to edit or create a new one from Assets menu", MessageType.Info);
            }
        }
        
        private void HandleKeyboardShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Copy (Ctrl/Cmd + C)
                if ((e.control || e.command) && e.keyCode == KeyCode.C)
                {
                    HandleCopy();
                    e.Use();
                }
                // Paste (Ctrl/Cmd + V)
                else if ((e.control || e.command) && e.keyCode == KeyCode.V)
                {
                    HandlePaste();
                    e.Use();
                }
                // Undo (Ctrl/Cmd + Z)
                else if ((e.control || e.command) && e.keyCode == KeyCode.Z && !e.shift)
                {
                    if (undoRedoService.CanUndo())
                    {
                        undoRedoService.Undo();
                        RefreshDisplayedRows();
                        Repaint();
                    }
                    e.Use();
                }
                // Redo (Ctrl/Cmd + Shift + Z or Ctrl/Cmd + Y)
                else if (((e.control || e.command) && e.shift && e.keyCode == KeyCode.Z) ||
                         ((e.control || e.command) && e.keyCode == KeyCode.Y))
                {
                    if (undoRedoService.CanRedo())
                    {
                        undoRedoService.Redo();
                        RefreshDisplayedRows();
                        Repaint();
                    }
                    e.Use();
                }
                // Delete
                else if (e.keyCode == KeyCode.Delete)
                {
                    HandleDeleteSelected();
                    e.Use();
                }
            }
        }
        
        private void HandleCopy()
        {
            if (!string.IsNullOrEmpty(focusedCellRowId) && !string.IsNullOrEmpty(focusedCellColumnId))
            {
                var row = currentTable.Rows.FirstOrDefault(r => r.RowId == focusedCellRowId);
                if (row != null)
                {
                    var value = row.GetValue(focusedCellColumnId);
                    ClipboardService.Copy(focusedCellColumnId, value);
                    Debug.Log($"Copied value from {focusedCellColumnId}");
                }
            }
        }
        
        private void HandlePaste()
        {
            if (!IsFileEditable()) return;
            
            if (!string.IsNullOrEmpty(focusedCellRowId) && !string.IsNullOrEmpty(focusedCellColumnId))
            {
                var row = currentTable.Rows.FirstOrDefault(r => r.RowId == focusedCellRowId);
                if (row != null && ClipboardService.CanPaste(focusedCellColumnId))
                {
                    var oldValue = row.GetValue(focusedCellColumnId);
                    var newValue = ClipboardService.Paste(focusedCellColumnId);
                    
                    var command = new EditCellCommand(currentTable, focusedCellRowId, focusedCellColumnId, oldValue, newValue);
                    undoRedoService.ExecuteCommand(command);
                    RefreshDisplayedRows();
                    Debug.Log($"Pasted value to {focusedCellColumnId}");
                }
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
                RefreshDisplayedRows();
                undoRedoService.Clear();
            }
            
            GUILayout.FlexibleSpace();
            
            if (currentTable != null)
            {
                GUI.enabled = IsFileEditable();
                
                // Add Row
                if (GUILayout.Button("Add Row", EditorStyles.toolbarButton))
                {
                    var newRow = currentTable.AddRow();
                    var command = new AddRowCommand(currentTable, newRow);
                    undoRedoService.ExecuteCommand(command);
                    RefreshDisplayedRows();
                }
                
                // Delete Selected Rows
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton))
                {
                    HandleDeleteSelected();
                }
                
                GUI.enabled = true;
                
                GUILayout.Space(10);
                
                // Filter
                bool newShowFilter = GUILayout.Toggle(showFilterPanel, "Filter", EditorStyles.toolbarButton);
                if (newShowFilter != showFilterPanel)
                {
                    showFilterPanel = newShowFilter;
                }
                
                // Validate
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    validationResult = currentTable.ValidateData();
                    showValidation = true;
                }
                
                // Import/Export
                if (GUILayout.Button("Import/Export", EditorStyles.toolbarButton))
                {
                    ShowImportExportMenu();
                }
                
                // Save
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveTable();
                }
                
                GUILayout.Space(10);
                
                // Undo/Redo
                GUI.enabled = undoRedoService.CanUndo();
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Undo();
                    RefreshDisplayedRows();
                    Repaint();
                }
                GUI.enabled = undoRedoService.CanRedo();
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Redo();
                    RefreshDisplayedRows();
                    Repaint();
                }
                GUI.enabled = true;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilterPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Filters", EditorStyles.boldLabel);
            
            filterLogicalOp = (LogicalOperator)EditorGUILayout.EnumPopup("Combine with", filterLogicalOp);
            
            for (int i = 0; i < filterConditions.Count; i++)
            {
                var condition = filterConditions[i];
                EditorGUILayout.BeginHorizontal();
                
                // Column selection
                var columnNames = currentTable.Columns.Select(c => c.DisplayName).ToArray();
                var columnIndex = System.Array.FindIndex(columnNames, name => 
                    currentTable.Columns.FirstOrDefault(c => c.DisplayName == name)?.ColumnId == condition.ColumnId);
                
                var newColumnIndex = EditorGUILayout.Popup(columnIndex >= 0 ? columnIndex : 0, columnNames, GUILayout.Width(150));
                if (newColumnIndex != columnIndex && newColumnIndex >= 0)
                {
                    condition.ColumnId = currentTable.Columns[newColumnIndex].ColumnId;
                }
                
                // Operator
                condition.Operator = (FilterOperator)EditorGUILayout.EnumPopup(condition.Operator, GUILayout.Width(100));
                
                // Value
                condition.Value = EditorGUILayout.TextField(condition.Value?.ToString() ?? "", GUILayout.Width(150));
                
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    filterConditions.RemoveAt(i);
                    RefreshDisplayedRows();
                    i--;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Filter"))
            {
                filterConditions.Add(new FilterCondition 
                { 
                    ColumnId = currentTable.Columns[0].ColumnId,
                    Operator = FilterOperator.Contains,
                    Value = ""
                });
            }
            
            if (GUILayout.Button("Clear All"))
            {
                filterConditions.Clear();
                RefreshDisplayedRows();
            }
            
            if (GUILayout.Button("Apply"))
            {
                RefreshDisplayedRows();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        private void DrawTable()
        {
            if (showValidation && validationResult != null && validationResult.HasErrors())
            {
                EditorGUILayout.HelpBox($"Found {validationResult.Errors.Count} validation errors", MessageType.Warning);
                if (GUILayout.Button("Hide Validation"))
                {
                    showValidation = false;
                }
                EditorGUILayout.Space();
            }
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Draw header
            EditorGUILayout.BeginHorizontal();
            
            // Select all checkbox
            bool allSelected = displayedRows.Count > 0 && displayedRows.All(r => selectedRowIds.Contains(r.RowId));
            bool newAllSelected = EditorGUILayout.Toggle(allSelected, GUILayout.Width(20));
            if (newAllSelected != allSelected)
            {
                if (newAllSelected)
                {
                    foreach (var row in displayedRows)
                        selectedRowIds.Add(row.RowId);
                }
                else
                {
                    selectedRowIds.Clear();
                }
            }
            
            // Column headers (clickable for sorting)
            foreach (var column in currentTable.Columns)
            {
                string headerText = column.DisplayName;
                if (sortingState.SortColumnId == column.ColumnId)
                {
                    headerText += sortingState.Direction == SortDirection.Ascending ? " ▲" : " ▼";
                }
                
                if (GUILayout.Button(headerText, EditorStyles.toolbarButton, GUILayout.Width(150)))
                {
                    sortingState.Toggle(column.ColumnId);
                    RefreshDisplayedRows();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Draw separator
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // Draw rows
            foreach (var row in displayedRows)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Selection checkbox
                bool isSelected = selectedRowIds.Contains(row.RowId);
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        selectedRowIds.Add(row.RowId);
                    else
                        selectedRowIds.Remove(row.RowId);
                }
                
                // Draw cells
                foreach (var column in currentTable.Columns)
                {
                    GUI.enabled = IsFileEditable();
                    
                    // Track focused cell
                    if (Event.current.type == EventType.MouseDown && 
                        GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                    {
                        focusedCellRowId = row.RowId;
                        focusedCellColumnId = column.ColumnId;
                    }
                    
                    var oldValue = row.GetValue(column.ColumnId);
                    var newValue = DrawCell(column, oldValue, row.RowId == focusedCellRowId && column.ColumnId == focusedCellColumnId);
                    
                    if (!Equals(oldValue, newValue))
                    {
                        var command = new EditCellCommand(currentTable, row.RowId, column.ColumnId, oldValue, newValue);
                        undoRedoService.ExecuteCommand(command);
                        
                        // Auto-validate on change
                        var columnDef = currentTable.GetColumn(column.ColumnId);
                        if (!columnDef.Validate(newValue))
                        {
                            EditorUtility.DisplayDialog("Validation Error", 
                                $"Invalid value for {column.DisplayName}", "OK");
                        }
                    }
                    
                    GUI.enabled = true;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private object DrawCell(ColumnDefinition column, object value, bool isFocused)
        {
            object newValue = value;
            
            // Highlight focused cell
            if (isFocused)
            {
                GUI.backgroundColor = Color.cyan * 0.3f;
            }
            
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
                        column.GetAssetType(),
                        false,
                        GUILayout.Width(150)
                    );
                    break;
                case ColumnType.Enum:
                    if (column.EnumDefinition != null && column.EnumDefinition.Values.Count > 0)
                    {
                        var currentIndex = column.EnumDefinition.GetIndex(value?.ToString() ?? "");
                        if (currentIndex < 0) currentIndex = 0;
                        var newIndex = EditorGUILayout.Popup(currentIndex, column.EnumDefinition.Values.ToArray(), GUILayout.Width(150));
                        newValue = column.EnumDefinition.Values[newIndex];
                    }
                    else
                    {
                        newValue = EditorGUILayout.TextField(value?.ToString() ?? "", GUILayout.Width(150));
                    }
                    break;
            }
            
            GUI.backgroundColor = Color.white;
            
            // Right-click context menu for cell
            if (Event.current.type == EventType.ContextClick && 
                GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
            {
                ShowCellContextMenu(column.ColumnId, value);
                Event.current.Use();
            }
            
            return newValue;
        }
        
        private void ShowCellContextMenu(string columnId, object value)
        {
            contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Copy"), false, () =>
            {
                ClipboardService.Copy(columnId, value);
            });
            
            if (ClipboardService.CanPaste(columnId))
            {
                contextMenu.AddItem(new GUIContent("Paste"), false, () =>
                {
                    HandlePaste();
                });
            }
            else
            {
                contextMenu.AddDisabledItem(new GUIContent("Paste"));
            }
            
            contextMenu.ShowAsContext();
        }
        
        private void ShowImportExportMenu()
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Export to CSV"), false, () =>
            {
                var path = EditorUtility.SaveFilePanel("Export Balance Table", "", currentTable.TableName + ".csv", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    var exporter = new CSVExporter();
                    if (exporter.Export(currentTable, path))
                    {
                        EditorUtility.DisplayDialog("Success", "Table exported successfully!", "OK");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to export table.", "OK");
                    }
                }
            });
            
            menu.AddItem(new GUIContent("Import from CSV"), false, () =>
            {
                var path = EditorUtility.OpenFilePanel("Import CSV", "", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    var importer = new CSVImporter();
                    var importedTable = importer.Import(path);
                    
                    if (importedTable != null)
                    {
                        // Check structure compatibility
                        var importedColumns = importedTable.Columns.Select(c => c.DisplayName).ToList();
                        if (!currentTable.HasStructure(importedColumns))
                        {
                            bool proceed = EditorUtility.DisplayDialog("Warning", 
                                "The CSV structure doesn't match the current table. Some data may be lost. Continue?", 
                                "Continue", "Cancel");
                            
                            if (!proceed) return;
                        }
                        
                        // Import rows
                        currentTable.Rows.Clear();
                        foreach (var row in importedTable.Rows)
                        {
                            currentTable.Rows.Add(row);
                        }
                        
                        RefreshDisplayedRows();
                        EditorUtility.DisplayDialog("Success", "Data imported successfully!", "OK");
                    }
                }
            });
            
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Export to JSON"), false, () =>
            {
                EditorUtility.DisplayDialog("Info", "JSON export coming soon!", "OK");
            });
            
            menu.ShowAsContext();
        }
        
        private void HandleDeleteSelected()
        {
            if (!IsFileEditable()) return;
            
            if (selectedRowIds.Count == 0) return;
            
            bool confirm = EditorUtility.DisplayDialog(
                "Confirm Delete",
                $"Are you sure you want to delete {selectedRowIds.Count} row(s)?",
                "Delete",
                "Cancel"
            );
            
            if (confirm)
            {
                var rowsToDelete = currentTable.Rows.Where(r => selectedRowIds.Contains(r.RowId)).ToList();
                var command = new MultiDeleteCommand(currentTable, rowsToDelete);
                undoRedoService.ExecuteCommand(command);
                selectedRowIds.Clear();
                RefreshDisplayedRows();
            }
        }
        
        private void RefreshDisplayedRows()
        {
            if (currentTable == null)
            {
                displayedRows = new List<BalanceRow>();
                return;
            }
            
            displayedRows = new List<BalanceRow>(currentTable.Rows);
            
            // Apply filters
            if (filterConditions.Count > 0)
            {
                var filter = new CompositeFilter(filterLogicalOp);
                foreach (var condition in filterConditions)
                {
                    filter.AddFilter(new ColumnFilter(condition));
                }
                displayedRows = filter.Apply(displayedRows);
            }
            
            // Apply sorting
            if (sortingState.Direction != SortDirection.None && !string.IsNullOrEmpty(sortingState.SortColumnId))
            {
                var column = currentTable.GetColumn(sortingState.SortColumnId);
                if (column != null)
                {
                    displayedRows = TableSorter.Sort(displayedRows, sortingState.SortColumnId, sortingState.Direction, column.DataType);
                }
            }
        }
        
        private bool IsFileEditable()
        {
            if (currentTable == null) return false;
            
            string path = AssetDatabase.GetAssetPath(currentTable);
            if (string.IsNullOrEmpty(path)) return false;
            
            // Check if file is in Packages folder (read-only)
            if (path.StartsWith("Packages/")) return false;
            
            // Check version control status if needed
            // This is a simplified check - you might want to integrate with your VCS
            return !AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.ForceUpdate) || 
                   AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.UseCachedIfPossible);
        }
        
        private void SaveTable()
        {
            if (currentTable != null)
            {
                EditorUtility.SetDirty(currentTable);
                AssetDatabase.SaveAssets();
                Debug.Log($"Table '{currentTable.TableName}' saved successfully!");
            }
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Total Rows: {currentTable.Rows.Count} | Displayed: {displayedRows.Count} | Selected: {selectedRowIds.Count}");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Last Modified: {currentTable.LastModified:yyyy-MM-dd HH:mm:ss}");
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif