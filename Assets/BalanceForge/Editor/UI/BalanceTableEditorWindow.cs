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
        
        // Virtual scrolling optimization
        private float rowHeight = 22f;
        private float columnWidth = 150f;
        private float headerHeight = 40f;
        private int visibleStartIndex = 0;
        private int visibleEndIndex = 0;
        private Rect scrollViewRect;
        
        // OPTIMIZATIONS
        private const int CACHE_SIZE_LIMIT = 500;
        private const int VISIBLE_ROW_BUFFER = 5;
        private const float REPAINT_THROTTLE = 0.05f;
        
        private Dictionary<string, object> cellValueCache = new Dictionary<string, object>();
        private Dictionary<string, GUIContent> guiContentCache = new Dictionary<string, GUIContent>();
        
        private int lastCacheFrame = -1;
        private double lastRepaintTime = 0;
        private bool isDirty = false;
        
        // Cached styles
        private GUIStyle cellStyle;
        private GUIStyle headerStyle;
        private bool stylesInitialized = false;
        
        private int frameCounter = 0;
        
        [MenuItem("Window/BalanceForge/Table Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<BalanceTableEditorWindow>("Balance Table Editor");
            window.minSize = new Vector2(600, 400);
        }
        
        private void OnEnable()
        {
            undoRedoService = new UndoRedoService();
            displayedRows = new List<BalanceRow>();
            frameCounter = 0;
            InitializeStyles();
        }
        
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            
            cellStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(4, 4, 2, 2),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };
            
            headerStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            
            stylesInitialized = true;
        }
        
        private void OnInspectorUpdate()
        {
            if (isDirty && EditorApplication.timeSinceStartup - lastRepaintTime > REPAINT_THROTTLE)
            {
                Repaint();
                isDirty = false;
                lastRepaintTime = EditorApplication.timeSinceStartup;
            }
        }
        
        public void LoadTable(BalanceTable table)
        {
            currentTable = table;
            RefreshDisplayedRows();
            undoRedoService.Clear();
            ClearAllCaches();
        }
        
        private void ClearAllCaches()
        {
            cellValueCache.Clear();
            guiContentCache.Clear();
            lastCacheFrame = -1;
        }
        
        private void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
            {
                frameCounter++;
                if (frameCounter != lastCacheFrame)
                {
                    cellValueCache.Clear();
                    lastCacheFrame = frameCounter;
                    
                    if (guiContentCache.Count > CACHE_SIZE_LIMIT)
                    {
                        guiContentCache.Clear();
                    }
                }
            }
            
            HandleKeyboardShortcuts();
            DrawToolbar();
            EditorGUILayout.Space();
            
            if (currentTable != null)
            {
                if (!IsFileEditable())
                {
                    EditorGUILayout.HelpBox("This file is read-only. You cannot make changes.", MessageType.Warning);
                }
                
                // Debug info
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Total: {currentTable.Rows.Count}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Displayed: {displayedRows?.Count ?? 0}", GUILayout.Width(100));
                if (displayedRows != null && displayedRows.Count > 0)
                {
                    EditorGUILayout.LabelField($"Visible: {visibleEndIndex - visibleStartIndex + 1}", GUILayout.Width(100));
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(3);
                
                if (showFilterPanel)
                {
                    DrawFilterPanel();
                }
                
                if (showValidation && validationResult != null && validationResult.HasErrors())
                {
                    EditorGUILayout.HelpBox($"Found {validationResult.Errors.Count} validation errors", MessageType.Warning);
                    if (GUILayout.Button("Hide Validation"))
                    {
                        showValidation = false;
                    }
                    EditorGUILayout.Space();
                }
                
                DrawOptimizedTable();
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
                bool handled = false;
                
                if ((e.control || e.command) && e.keyCode == KeyCode.C)
                {
                    HandleCopy();
                    handled = true;
                }
                else if ((e.control || e.command) && e.keyCode == KeyCode.V)
                {
                    HandlePaste();
                    handled = true;
                }
                else if ((e.control || e.command) && e.keyCode == KeyCode.Z && !e.shift)
                {
                    if (undoRedoService.CanUndo())
                    {
                        undoRedoService.Undo();
                        RefreshDisplayedRows();
                        isDirty = true;
                    }
                    handled = true;
                }
                else if (((e.control || e.command) && e.shift && e.keyCode == KeyCode.Z) ||
                         ((e.control || e.command) && e.keyCode == KeyCode.Y))
                {
                    if (undoRedoService.CanRedo())
                    {
                        undoRedoService.Redo();
                        RefreshDisplayedRows();
                        isDirty = true;
                    }
                    handled = true;
                }
                else if (e.keyCode == KeyCode.Delete)
                {
                    HandleDeleteSelected();
                    handled = true;
                }
                
                if (handled)
                {
                    e.Use();
                }
            }
        }
        
        // ИСПРАВЛЕНО: Используем только GUI методы без смешивания с GUILayout
        private void DrawOptimizedTable()
        {
            if (displayedRows == null || displayedRows.Count == 0)
            {
                EditorGUILayout.HelpBox("No data to display. Add rows or adjust filters.", MessageType.Info);
                
                if (currentTable != null && GUILayout.Button("Add 10 Test Rows", GUILayout.Height(30)))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var row = currentTable.AddRow();
                        foreach (var column in currentTable.Columns)
                        {
                            object defaultValue = column.DataType switch
                            {
                                ColumnType.String => $"Value_{i}",
                                ColumnType.Integer => i * 100,
                                ColumnType.Float => i * 10.5f,
                                ColumnType.Boolean => i % 2 == 0,
                                _ => $"Data_{i}"
                            };
                            row.SetValue(column.ColumnId, defaultValue);
                        }
                    }
                    RefreshDisplayedRows();
                    Repaint();
                }
                return;
            }
            
            scrollViewRect = GUILayoutUtility.GetRect(
                GUIContent.none, 
                GUIStyle.none, 
                GUILayout.ExpandWidth(true), 
                GUILayout.ExpandHeight(true),
                GUILayout.MinHeight(200)
            );
            
            float contentHeight = headerHeight + displayedRows.Count * rowHeight;
            float contentWidth = Mathf.Max(scrollViewRect.width, 20 + columnWidth * currentTable.Columns.Count);
            
            scrollPosition = GUI.BeginScrollView(
                scrollViewRect, 
                scrollPosition, 
                new Rect(0, 0, contentWidth, contentHeight)
            );
            
            DrawTableHeaderDirect();
            
            int rawStart = Mathf.FloorToInt((scrollPosition.y - headerHeight) / rowHeight);
            int rawEnd = Mathf.CeilToInt((scrollPosition.y + scrollViewRect.height - headerHeight) / rowHeight);
            
            visibleStartIndex = Mathf.Max(0, rawStart - VISIBLE_ROW_BUFFER);
            visibleEndIndex = Mathf.Min(displayedRows.Count - 1, rawEnd + VISIBLE_ROW_BUFFER);
            
            // Draw visible rows using direct GUI
            for (int i = visibleStartIndex; i <= visibleEndIndex && i < displayedRows.Count; i++)
            {
                DrawTableRowDirect(i, displayedRows[i]);
            }
            
            GUI.EndScrollView();
        }
        
        // ИСПРАВЛЕНО: Прямая отрисовка заголовка без GUILayout внутри
        private void DrawTableHeaderDirect()
        {
            float contentWidth = Mathf.Max(scrollViewRect.width, 20 + columnWidth * currentTable.Columns.Count);
            var headerRect = new Rect(0, 0, contentWidth, headerHeight);
            
            EditorGUI.DrawRect(headerRect, new Color(0.22f, 0.22f, 0.22f, 1f));
            
            float xPos = 5;
            float yPos = (headerHeight - 20) / 2;
            
            // Select all checkbox
            bool allSelected = displayedRows.Count > 0 && displayedRows.All(r => selectedRowIds.Contains(r.RowId));
            var checkRect = new Rect(xPos, yPos, 20, 20);
            bool newAllSelected = EditorGUI.Toggle(checkRect, allSelected);
            
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
            
            xPos += 25;
            
            // Column headers
            foreach (var column in currentTable.Columns)
            {
                string headerText = column.DisplayName;
                if (sortingState.SortColumnId == column.ColumnId)
                {
                    headerText += sortingState.Direction == SortDirection.Ascending ? " ▲" : " ▼";
                }
                
                string cacheKey = $"header_{column.ColumnId}_{headerText}";
                GUIContent content;
                if (!guiContentCache.TryGetValue(cacheKey, out content))
                {
                    content = new GUIContent(headerText);
                    guiContentCache[cacheKey] = content;
                }
                
                var buttonRect = new Rect(xPos, 2, columnWidth, headerHeight - 4);
                if (GUI.Button(buttonRect, content, headerStyle))
                {
                    sortingState.Toggle(column.ColumnId);
                    RefreshDisplayedRows();
                    isDirty = true;
                }
                
                xPos += columnWidth;
            }
            
            // Separator
            var separatorRect = new Rect(0, headerHeight - 2, contentWidth, 1);
            EditorGUI.DrawRect(separatorRect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
        
        // ИСПРАВЛЕНО: Прямая отрисовка строки без GUILayout внутри
        private void DrawTableRowDirect(int index, BalanceRow row)
        {
            float yPos = headerHeight + index * rowHeight;
            float contentWidth = Mathf.Max(scrollViewRect.width, 20 + columnWidth * currentTable.Columns.Count);
            var rowRect = new Rect(0, yPos, contentWidth, rowHeight);
            
            // Alternate row colors
            if (index % 2 == 0)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.22f, 0.22f, 0.22f, 1f));
            }
            else
            {
                EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.25f, 0.25f, 1f));
            }
            
            float xPos = 5;
            
            // Selection checkbox
            bool isSelected = selectedRowIds.Contains(row.RowId);
            var checkRect = new Rect(xPos, yPos + 1, 20, 20);
            bool newSelected = EditorGUI.Toggle(checkRect, isSelected);
            
            if (newSelected != isSelected)
            {
                if (newSelected)
                    selectedRowIds.Add(row.RowId);
                else
                    selectedRowIds.Remove(row.RowId);
                isDirty = true;
            }
            
            xPos += 25;
            
            // Draw cells
            GUI.enabled = IsFileEditable();
            foreach (var column in currentTable.Columns)
            {
                DrawCellDirect(row, column, new Rect(xPos, yPos + 1, columnWidth, rowHeight - 2));
                xPos += columnWidth;
            }
            GUI.enabled = true;
        }
        
        private void DrawCellDirect(BalanceRow row, ColumnDefinition column, Rect cellRect)
        {
            string cacheKey = $"{row.RowId}_{column.ColumnId}";
            object value;
            
            if (!cellValueCache.TryGetValue(cacheKey, out value))
            {
                value = row.GetValue(column.ColumnId);
                cellValueCache[cacheKey] = value;
            }
            
            bool isFocused = row.RowId == focusedCellRowId && column.ColumnId == focusedCellColumnId;
            
            if (Event.current.type == EventType.MouseDown && cellRect.Contains(Event.current.mousePosition))
            {
                focusedCellRowId = row.RowId;
                focusedCellColumnId = column.ColumnId;
                isDirty = true;
                Event.current.Use();
            }
            
            if (isFocused)
            {
                EditorGUI.DrawRect(cellRect, new Color(0.3f, 0.5f, 0.8f, 0.4f));
            }
            
            object newValue = DrawCellByType(cellRect, column, value);
            
            if (!Equals(value, newValue))
            {
                var command = new EditCellCommand(currentTable, row.RowId, column.ColumnId, value, newValue);
                undoRedoService.ExecuteCommand(command);
                cellValueCache[cacheKey] = newValue;
                
                if (!column.Validate(newValue))
                {
                    EditorUtility.DisplayDialog("Validation Error", 
                        $"Invalid value for {column.DisplayName}", "OK");
                }
            }
            
            if (Event.current.type == EventType.ContextClick && cellRect.Contains(Event.current.mousePosition))
            {
                ShowCellContextMenu(column.ColumnId, value);
                Event.current.Use();
            }
        }
        
        private object DrawCellByType(Rect position, ColumnDefinition column, object value)
        {
            switch (column.DataType)
            {
                case ColumnType.String:
                    return EditorGUI.TextField(position, value?.ToString() ?? "");
                    
                case ColumnType.Integer:
                    int intVal = value != null ? System.Convert.ToInt32(value) : 0;
                    return EditorGUI.IntField(position, intVal);
                    
                case ColumnType.Float:
                    float floatVal = value != null ? System.Convert.ToSingle(value) : 0f;
                    return EditorGUI.FloatField(position, floatVal);
                    
                case ColumnType.Boolean:
                    bool boolVal = value != null && System.Convert.ToBoolean(value);
                    return EditorGUI.Toggle(position, boolVal);
                    
                case ColumnType.Color:
                    Color colorVal = value is Color ? (Color)value : Color.white;
                    return EditorGUI.ColorField(position, colorVal);
                    
                case ColumnType.Vector2:
                    Vector2 vec2Val = value is Vector2 ? (Vector2)value : Vector2.zero;
                    return EditorGUI.Vector2Field(position, "", vec2Val);
                    
                case ColumnType.Vector3:
                    Vector3 vec3Val = value is Vector3 ? (Vector3)value : Vector3.zero;
                    return EditorGUI.Vector3Field(position, "", vec3Val);
                    
                case ColumnType.AssetReference:
                    return EditorGUI.ObjectField(position,
                        value as UnityEngine.Object,
                        column.GetAssetType(),
                        false);
                        
                case ColumnType.Enum:
                    if (column.EnumDefinition != null && column.EnumDefinition.Values.Count > 0)
                    {
                        var currentIndex = column.EnumDefinition.GetIndex(value?.ToString() ?? "");
                        if (currentIndex < 0) currentIndex = 0;
                        var newIndex = EditorGUI.Popup(position, currentIndex, column.EnumDefinition.Values.ToArray());
                        return column.EnumDefinition.Values[newIndex];
                    }
                    return EditorGUI.TextField(position, value?.ToString() ?? "");
                    
                default:
                    return value;
            }
        }
        
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            var newTable = (BalanceTable)EditorGUILayout.ObjectField(
                currentTable, typeof(BalanceTable), false, GUILayout.Width(200));
            
            if (newTable != currentTable)
            {
                currentTable = newTable;
                RefreshDisplayedRows();
                undoRedoService.Clear();
                ClearAllCaches();
            }
            
            GUILayout.FlexibleSpace();
            
            if (currentTable != null)
            {
                GUI.enabled = IsFileEditable();
                
                if (GUILayout.Button("Add Row", EditorStyles.toolbarButton))
                {
                    var newRow = currentTable.AddRow();
                    var command = new AddRowCommand(currentTable, newRow);
                    undoRedoService.ExecuteCommand(command);
                    RefreshDisplayedRows();
                }
                
                if (GUILayout.Button("Delete Selected", EditorStyles.toolbarButton))
                {
                    HandleDeleteSelected();
                }
                
                GUI.enabled = true;
                
                GUILayout.Space(10);
                
                bool newShowFilter = GUILayout.Toggle(showFilterPanel, "Filter", EditorStyles.toolbarButton);
                if (newShowFilter != showFilterPanel)
                {
                    showFilterPanel = newShowFilter;
                }
                
                if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
                {
                    validationResult = currentTable.ValidateData();
                    showValidation = true;
                }
                
                if (GUILayout.Button("Import/Export", EditorStyles.toolbarButton))
                {
                    ShowImportExportMenu();
                }
                
                if (GUILayout.Button("Save", EditorStyles.toolbarButton))
                {
                    SaveTable();
                }
                
                GUILayout.Space(10);
                
                GUI.enabled = undoRedoService.CanUndo();
                if (GUILayout.Button("Undo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Undo();
                    RefreshDisplayedRows();
                    isDirty = true;
                }
                GUI.enabled = undoRedoService.CanRedo();
                if (GUILayout.Button("Redo", EditorStyles.toolbarButton))
                {
                    undoRedoService.Redo();
                    RefreshDisplayedRows();
                    isDirty = true;
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
                
                var columnNames = currentTable.Columns.Select(c => c.DisplayName).ToArray();
                var columnIndex = System.Array.FindIndex(columnNames, name => 
                    currentTable.Columns.FirstOrDefault(c => c.DisplayName == name)?.ColumnId == condition.ColumnId);
                
                var newColumnIndex = EditorGUILayout.Popup(columnIndex >= 0 ? columnIndex : 0, columnNames, GUILayout.Width(150));
                if (newColumnIndex != columnIndex && newColumnIndex >= 0)
                {
                    condition.ColumnId = currentTable.Columns[newColumnIndex].ColumnId;
                }
                
                condition.Operator = (FilterOperator)EditorGUILayout.EnumPopup(condition.Operator, GUILayout.Width(100));
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
        
        private void HandleCopy()
        {
            if (!string.IsNullOrEmpty(focusedCellRowId) && !string.IsNullOrEmpty(focusedCellColumnId))
            {
                var row = currentTable.Rows.FirstOrDefault(r => r.RowId == focusedCellRowId);
                if (row != null)
                {
                    var value = row.GetValue(focusedCellColumnId);
                    ClipboardService.Copy(focusedCellColumnId, value);
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
                }
            }
        }
        
        private void ShowCellContextMenu(string columnId, object value)
        {
            contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Copy"), false, () => ClipboardService.Copy(columnId, value));
            
            if (ClipboardService.CanPaste(columnId))
            {
                contextMenu.AddItem(new GUIContent("Paste"), false, HandlePaste);
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
                        var importedColumns = importedTable.Columns.Select(c => c.DisplayName).ToList();
                        if (!currentTable.HasStructure(importedColumns))
                        {
                            bool proceed = EditorUtility.DisplayDialog("Warning", 
                                "The CSV structure doesn't match the current table. Some data may be lost. Continue?", 
                                "Continue", "Cancel");
                            
                            if (!proceed) return;
                        }
                        
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
            if (!IsFileEditable() || selectedRowIds.Count == 0) return;
            
            bool confirm = EditorUtility.DisplayDialog("Confirm Delete",
                $"Are you sure you want to delete {selectedRowIds.Count} row(s)?",
                "Delete", "Cancel");
            
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
            
            if (filterConditions.Count > 0)
            {
                var filter = new CompositeFilter(filterLogicalOp);
                foreach (var condition in filterConditions)
                {
                    filter.AddFilter(new ColumnFilter(condition));
                }
                displayedRows = filter.Apply(displayedRows);
            }
            
            if (sortingState.Direction != SortDirection.None && !string.IsNullOrEmpty(sortingState.SortColumnId))
            {
                var column = currentTable.GetColumn(sortingState.SortColumnId);
                if (column != null)
                {
                    displayedRows = TableSorter.Sort(
                        displayedRows, 
                        sortingState.SortColumnId, 
                        sortingState.Direction, 
                        column.DataType
                    );
                }
            }
            
            ClearAllCaches();
        }
        
        private bool IsFileEditable()
        {
            if (currentTable == null) return false;
            
            string path = AssetDatabase.GetAssetPath(currentTable);
            if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/")) return false;
            
            return !AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.ForceUpdate) || 
                   AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.UseCachedIfPossible);
        }
        
        private void SaveTable()
        {
            if (currentTable)
            {
                EditorUtility.SetDirty(currentTable);
                AssetDatabase.SaveAssets();
                Debug.Log($"Table '{currentTable.TableName}' saved successfully!");
            }
        }
        
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Total: {currentTable.Rows.Count} | Displayed: {displayedRows.Count} | Selected: {selectedRowIds.Count} | Visible: {visibleEndIndex - visibleStartIndex + 1}");
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Modified: {currentTable.LastModified:yyyy-MM-dd HH:mm:ss}");
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif