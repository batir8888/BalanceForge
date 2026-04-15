#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BalanceForge.Core.Data;
using BalanceForge.Data.Operations;
using BalanceForge.ImportExport;
using BalanceForge.Services;
using BalanceForge.Editor.CodeGen;
// Disambiguate from UnityEngine.UIElements.SortDirection
using BFSortDirection = BalanceForge.Data.Operations.SortDirection;

namespace BalanceForge.Editor.UI
{
    public class BalanceTableEditorWindow : EditorWindow
    {
        // ── State ────────────────────────────────────────────────
        private BalanceTable currentTable;
        private readonly UndoRedoService undoRedoService = new UndoRedoService();
        private List<BalanceRow> displayedRows = new List<BalanceRow>();
        private readonly SortingState sortingState = new SortingState();
        private readonly List<FilterCondition> filterConditions = new List<FilterCondition>();
        private LogicalOperator filterLogicalOp = LogicalOperator.And;
        private string quickSearchText = "";
        private bool filterPanelVisible = false;

        // ── UI references ────────────────────────────────────────
        private MultiColumnListView tableView;
        private VisualElement tableContainer;
        private VisualElement filterPanel;
        private VisualElement validationBanner;
        private Label statusLabel;
        private ToolbarButton undoButton;
        private ToolbarButton redoButton;
        private ToolbarButton filterButton;
        private ToolbarButton generateCodeButton;
        private ObjectField tableField;

        private const string StyleSheetPath = "Assets/BalanceForge/Editor/UI/BalanceForgeEditor.uss";

        // ── Entry point ──────────────────────────────────────────
        [MenuItem("Window/BalanceForge/Table Editor")]
        public static void ShowWindow()
        {
            var w = GetWindow<BalanceTableEditorWindow>("Balance Table Editor");
            w.minSize = new Vector2(700, 450);
        }

        // ── Public API ───────────────────────────────────────────
        public void LoadTable(BalanceTable table)
        {
            currentTable = table;
            undoRedoService.Clear();
            filterConditions.Clear();
            quickSearchText = "";
            sortingState.SortColumnId = null;
            sortingState.Direction = BFSortDirection.None;

            if (tableField != null && tableField.value != table)
                tableField.SetValueWithoutNotify(table);

            RefreshDisplayedRows();
            RebuildTable();
            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        // ── CreateGUI ────────────────────────────────────────────
        private void CreateGUI()
        {
            var root = rootVisualElement;
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (ss != null) root.styleSheets.Add(ss);

            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;

            root.Add(BuildToolbar());

            filterPanel = BuildFilterPanel();
            filterPanel.style.display = DisplayStyle.None;
            filterPanel.style.flexShrink = 0;
            root.Add(filterPanel);

            validationBanner = BuildValidationBanner();
            validationBanner.style.display = DisplayStyle.None;
            validationBanner.style.flexShrink = 0;
            root.Add(validationBanner);

            tableContainer = new VisualElement();
            tableContainer.style.flexGrow = 1;
            tableContainer.style.flexShrink = 1;
            tableContainer.style.overflow = Overflow.Hidden;
            root.Add(tableContainer);

            root.Add(BuildStatusBar());

            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            RebuildTable();
        }

        // ── Toolbar ──────────────────────────────────────────────
        private Toolbar BuildToolbar()
        {
            var tb = new Toolbar();
            tb.AddToClassList("bf-toolbar");

            tableField = new ObjectField { objectType = typeof(BalanceTable) };
            tableField.style.width = 220;
            tableField.RegisterValueChangedCallback(evt => LoadTable(evt.newValue as BalanceTable));
            tb.Add(tableField);

            tb.Add(new ToolbarSpacer());

            var addBtn = new ToolbarButton(HandleAddRow) { text = "+ Row" };
            addBtn.tooltip = "Add a new row  (Ctrl+Enter)";
            tb.Add(addBtn);

            var delBtn = new ToolbarButton(HandleDeleteSelected) { text = "✕ Delete" };
            delBtn.tooltip = "Delete selected rows  (Delete)";
            delBtn.name = "btn-delete";
            tb.Add(delBtn);

            tb.Add(new ToolbarSpacer());

            var searchField = new ToolbarSearchField();
            searchField.style.width = 170;
            searchField.tooltip = "Quick search across all columns";
            searchField.RegisterValueChangedCallback(evt =>
            {
                quickSearchText = evt.newValue;
                RefreshDisplayedRows();
                UpdateStatusBar();
            });
            tb.Add(searchField);

            tb.Add(new ToolbarSpacer());

            filterButton = new ToolbarButton(ToggleFilterPanel) { text = "⚙ Filters", name = "filter-toggle" };
            filterButton.tooltip = "Toggle advanced filter panel";
            tb.Add(filterButton);

            var validateBtn = new ToolbarButton(HandleValidate) { text = "✓ Validate" };
            validateBtn.tooltip = "Check all cell values against column rules";
            tb.Add(validateBtn);

            var ioBtn = new ToolbarButton(ShowImportExportMenu) { text = "Import / Export" };
            tb.Add(ioBtn);

            generateCodeButton = new ToolbarButton(HandleGenerateCode) { text = "<> Generate" };
            generateCodeButton.tooltip = "Generate strongly-typed C# row class and bake data asset";
            tb.Add(generateCodeButton);

            var saveBtn = new ToolbarButton(SaveTable) { text = "💾 Save" };
            saveBtn.tooltip = "Save asset to disk  (Ctrl+S)";
            tb.Add(saveBtn);

            tb.Add(new ToolbarSpacer());

            undoButton = new ToolbarButton(HandleUndo) { text = "↩ Undo" };
            undoButton.tooltip = "Undo last action  (Ctrl+Z)";
            undoButton.SetEnabled(false);
            tb.Add(undoButton);

            redoButton = new ToolbarButton(HandleRedo) { text = "↪ Redo" };
            redoButton.tooltip = "Redo  (Ctrl+Y / Ctrl+Shift+Z)";
            redoButton.SetEnabled(false);
            tb.Add(redoButton);

            return tb;
        }

        // ── Filter panel ─────────────────────────────────────────
        private VisualElement BuildFilterPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList("bf-filter-panel");
            panel.name = "filter-panel";

            var header = new VisualElement();
            header.AddToClassList("bf-filter-header");
            header.Add(new Label("Advanced Filters"));

            header.Add(new Label("Combine:"));

            var opField = new EnumField(filterLogicalOp);
            opField.style.width = 60;
            opField.style.minWidth = 60;
            opField.RegisterValueChangedCallback(evt =>
            {
                filterLogicalOp = (LogicalOperator)evt.newValue;
                RefreshDisplayedRows();
                UpdateStatusBar();
            });
            header.Add(opField);

            var addCondBtn = new Button(() => AddFilterCondition(panel)) { text = "+ Condition" };
            header.Add(addCondBtn);

            var clearBtn = new Button(() =>
            {
                filterConditions.Clear();
                RebuildFilterRows(panel);
                RefreshDisplayedRows();
                UpdateStatusBar();
            }) { text = "Clear all" };
            header.Add(clearBtn);

            panel.Add(header);

            var rows = new VisualElement();
            rows.name = "filter-rows";
            panel.Add(rows);

            return panel;
        }

        private void AddFilterCondition(VisualElement panel)
        {
            if (currentTable == null || currentTable.Columns.Count == 0) return;
            var firstCol = currentTable.Columns[0];
            var ops = GetOperatorsForType(firstCol.DataType);
            filterConditions.Add(new FilterCondition
            {
                ColumnId = firstCol.ColumnId,
                Operator = ops[0].op,
                Value    = ""
            });
            RebuildFilterRows(panel);
        }

        private void RebuildFilterRows(VisualElement panel)
        {
            var container = panel.Q("filter-rows");
            container.Clear();

            if (currentTable == null) return;

            var colNames = currentTable.Columns.Select(c => c.DisplayName).ToList();
            var colIds   = currentTable.Columns.Select(c => c.ColumnId).ToList();

            for (int i = 0; i < filterConditions.Count; i++)
            {
                var cond   = filterConditions[i];
                var idx    = i; // capture

                // Resolve column type for this condition
                int colIdx = colIds.IndexOf(cond.ColumnId);
                if (colIdx < 0) colIdx = 0;
                var colType = currentTable.Columns[colIdx].DataType;

                // Build operator list for this column type
                var opList   = GetOperatorsForType(colType);
                var opLabels = opList.Select(o => o.label).ToList();

                // If current operator is not valid for the column type, reset it
                if (!opList.Any(o => o.op == cond.Operator))
                    cond.Operator = opList[0].op;

                int opIdx = opList.FindIndex(o => o.op == cond.Operator);
                if (opIdx < 0) opIdx = 0;

                // ── Row container ────────────────────────────────────
                var row = new VisualElement();
                row.AddToClassList("bf-filter-row");

                // Column picker
                var colDrop = new DropdownField(colNames, colIdx);
                colDrop.RegisterValueChangedCallback(evt =>
                {
                    int ci = colNames.IndexOf(evt.newValue);
                    if (ci < 0) return;

                    var prevType = currentTable.Columns.FirstOrDefault(c => c.ColumnId == filterConditions[idx].ColumnId)?.DataType;
                    filterConditions[idx].ColumnId = colIds[ci];
                    var newType = currentTable.Columns[ci].DataType;

                    // If type changed, reset operator to first valid one and rebuild UI
                    if (prevType != newType)
                    {
                        var newOps = GetOperatorsForType(newType);
                        filterConditions[idx].Operator = newOps[0].op;
                        RebuildFilterRows(panel);
                        return;
                    }
                    RefreshDisplayedRows();
                    UpdateStatusBar();
                });

                // Operator picker (type-filtered)
                var opDrop = new DropdownField(opLabels, opIdx);
                opDrop.style.width    = 130;
                opDrop.style.minWidth = 130;
                opDrop.RegisterValueChangedCallback(evt =>
                {
                    int oi = opLabels.IndexOf(evt.newValue);
                    if (oi >= 0)
                    {
                        filterConditions[idx].Operator = opList[oi].op;
                        // Update regex validation on the value field
                        var vf = row.Q<TextField>("filter-val");
                        if (vf != null) UpdateRegexValidation(vf, filterConditions[idx].Operator, vf.value);
                    }
                    RefreshDisplayedRows();
                    UpdateStatusBar();
                });

                // Value field
                var valField = new TextField { value = cond.Value?.ToString() ?? "", isDelayed = true, name = "filter-val" };
                valField.RegisterValueChangedCallback(evt =>
                {
                    filterConditions[idx].Value = evt.newValue;
                    UpdateRegexValidation(valField, filterConditions[idx].Operator, evt.newValue);
                    RefreshDisplayedRows();
                    UpdateStatusBar();
                });
                // Validate on initial build
                UpdateRegexValidation(valField, cond.Operator, cond.Value?.ToString() ?? "");

                // Remove button
                var removeBtn = new Button(() =>
                {
                    filterConditions.RemoveAt(idx);
                    RebuildFilterRows(panel);
                    RefreshDisplayedRows();
                    UpdateStatusBar();
                }) { text = "✕" };
                removeBtn.AddToClassList("bf-filter-remove");

                row.Add(colDrop);
                row.Add(opDrop);
                row.Add(valField);
                row.Add(removeBtn);
                container.Add(row);
            }
        }

        // Returns (display label, FilterOperator) pairs valid for the given column type.
        private static List<(string label, FilterOperator op)> GetOperatorsForType(ColumnType type)
        {
            switch (type)
            {
                case ColumnType.Integer:
                case ColumnType.Float:
                    return new List<(string, FilterOperator)>
                    {
                        ("= Equals",        FilterOperator.Equals),
                        ("≠ Not Equals",    FilterOperator.NotEquals),
                        ("> Greater Than",  FilterOperator.GreaterThan),
                        ("< Less Than",     FilterOperator.LessThan),
                    };
                case ColumnType.Boolean:
                case ColumnType.Enum:
                    return new List<(string, FilterOperator)>
                    {
                        ("= Equals",     FilterOperator.Equals),
                        ("≠ Not Equals", FilterOperator.NotEquals),
                    };
                default: // String and everything else
                    return new List<(string, FilterOperator)>
                    {
                        ("∋ Contains",    FilterOperator.Contains),
                        ("= Equals",      FilterOperator.Equals),
                        ("≠ Not Equals",  FilterOperator.NotEquals),
                        ("⊏ Starts With", FilterOperator.StartsWith),
                        ("⊐ Ends With",   FilterOperator.EndsWith),
                        ("~ Regex",       FilterOperator.Regex),
                    };
            }
        }

        // Adds/removes the invalid-regex CSS class on the TextField.
        private static void UpdateRegexValidation(TextField field, FilterOperator op, string value)
        {
            if (op != FilterOperator.Regex || string.IsNullOrEmpty(value))
            {
                field.RemoveFromClassList("bf-filter-value--regex-invalid");
                return;
            }
            try
            {
                System.Text.RegularExpressions.Regex.IsMatch("", value);
                field.RemoveFromClassList("bf-filter-value--regex-invalid");
            }
            catch
            {
                field.AddToClassList("bf-filter-value--regex-invalid");
            }
        }

        // ── Validation banner ────────────────────────────────────
        private VisualElement BuildValidationBanner()
        {
            var banner = new VisualElement();
            banner.AddToClassList("bf-validation-banner");

            var lbl = new Label();
            lbl.name = "validation-label";
            banner.Add(lbl);

            var closeBtn = new Button(() => banner.style.display = DisplayStyle.None) { text = "✕" };
            closeBtn.AddToClassList("bf-validation-close");
            banner.Add(closeBtn);

            return banner;
        }

        // ── Status bar ───────────────────────────────────────────
        private VisualElement BuildStatusBar()
        {
            var bar = new VisualElement();
            bar.AddToClassList("bf-status-bar");
            statusLabel = new Label("No table loaded");
            bar.Add(statusLabel);
            return bar;
        }

        // ── Table view ───────────────────────────────────────────
        private void RebuildTable()
        {
            tableContainer.Clear();
            tableView = null;

            if (currentTable == null)
            {
                tableContainer.Add(MakeEmptyLabel(
                    "Select a Balance Table in the toolbar,\nor create one via Assets › Create › BalanceForge."));
                return;
            }

            if (currentTable.Columns.Count == 0)
            {
                tableContainer.Add(MakeEmptyLabel(
                    "This table has no columns.\nUse Assets › Create › BalanceForge › Balance Table Wizard to set up columns."));
                return;
            }

            tableView = new MultiColumnListView();
            tableView.style.flexGrow = 1;
            tableView.itemsSource = displayedRows;
            tableView.selectionType = SelectionType.Multiple;
            tableView.reorderable = true;
            tableView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            tableView.showBorder = true;
            tableView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            tableView.fixedItemHeight = 22;

            // Row-number column
            var numCol = new Column
            {
                name        = "__num",
                title       = "#",
                width       = new Length(36, LengthUnit.Pixel),
                minWidth    = new Length(36, LengthUnit.Pixel),
                maxWidth    = new Length(36, LengthUnit.Pixel),
                resizable   = false,
                sortable    = false,
                stretchable = false,
            };
            numCol.makeCell = () =>
            {
                var l = new Label();
                l.AddToClassList("bf-cell-rownum");
                return l;
            };
            numCol.bindCell = (el, i) => ((Label)el).text = (i + 1).ToString();
            tableView.columns.Add(numCol);

            // Data columns
            foreach (var colDef in currentTable.Columns)
                tableView.columns.Add(BuildColumn(colDef));

            tableView.sortingMode = ColumnSortingMode.Custom;
            tableView.columnSortingChanged += OnColumnSortingChanged;

            tableContainer.Add(tableView);

            // Empty-state overlay (shown when rows list is empty)
            var emptyOverlay = MakeEmptyLabel("No rows match the current search or filter.\nPress \"+ Row\" or Ctrl+Enter to add one.");
            emptyOverlay.name = "empty-overlay";
            tableContainer.Add(emptyOverlay);

            UpdateEmptyOverlay();
        }

        private static Label MakeEmptyLabel(string text)
        {
            var l = new Label(text);
            l.AddToClassList("bf-empty-message");
            return l;
        }

        private void UpdateEmptyOverlay()
        {
            if (tableContainer == null) return;
            bool empty = displayedRows == null || displayedRows.Count == 0;
            tableView?.SetDisplay(!empty);
            tableContainer.Q("empty-overlay")?.SetDisplay(empty);
        }

        private Column BuildColumn(ColumnDefinition colDef)
        {
            var col = new Column
            {
                name        = colDef.ColumnId,
                title       = colDef.DisplayName,
                width    = new Length(ColumnWidth(colDef.DataType), LengthUnit.Pixel),
                minWidth = new Length(50, LengthUnit.Pixel),
                resizable   = true,
                sortable    = true,
                stretchable = false,
            };

            col.makeHeader = () =>
            {
                var lbl = new Label(colDef.DisplayName);
                lbl.AddToClassList("bf-column-header");
                lbl.tooltip = $"{colDef.DataType}" + (colDef.IsRequired ? "  (required)" : "");
                return lbl;
            };

            col.makeCell   = () => MakeCellElement(colDef);
            col.bindCell   = (el, i) => BindCell(el, i, colDef);

            return col;
        }

        private static float ColumnWidth(ColumnType t) => t switch
        {
            ColumnType.Boolean       => 60,
            ColumnType.Integer       => 80,
            ColumnType.Float         => 90,
            ColumnType.Color         => 80,
            ColumnType.Vector2       => 180,
            ColumnType.Vector3       => 230,
            ColumnType.AssetReference => 200,
            _                        => 150,
        };

        // ── Cell factory ─────────────────────────────────────────
        // makeCell registers a permanent callback that reads userData set by bindCell.
        private VisualElement MakeCellElement(ColumnDefinition colDef)
        {
            var container = new VisualElement();
            container.AddToClassList("bf-cell");

            VisualElement field;

            switch (colDef.DataType)
            {
                case ColumnType.Integer:
                {
                    var f = new IntegerField { isDelayed = true };
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Float:
                {
                    var f = new FloatField { isDelayed = true };
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Boolean:
                {
                    var f = new Toggle();
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Color:
                {
                    var f = new ColorField();
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Vector2:
                {
                    var f = new Vector2Field();
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Vector3:
                {
                    var f = new Vector3Field();
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.AssetReference:
                {
                    var f = new ObjectField { objectType = colDef.GetAssetType(), allowSceneObjects = false };
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                case ColumnType.Enum when colDef.EnumDefinition?.Values?.Count > 0:
                {
                    var choices = new List<string>(colDef.EnumDefinition.Values);
                    var f = new DropdownField(choices, 0);
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
                default:
                {
                    var f = new TextField { isDelayed = true };
                    f.RegisterValueChangedCallback(evt => CommitCell(container, evt.newValue));
                    field = f;
                    break;
                }
            }

            field.style.flexGrow = 1;
            field.AddToClassList("bf-cell-field");
            container.Add(field);
            return container;
        }

        // bindCell: set value silently, store row+col in userData for the commit callback.
        private void BindCell(VisualElement container, int index, ColumnDefinition colDef)
        {
            if (index < 0 || index >= displayedRows.Count) return;

            var row   = displayedRows[index];
            var value = row.GetValue(colDef.ColumnId);

            container.userData = (row, colDef);

            var field = container.Q(className: "bf-cell-field");
            SetValueWithoutNotify(field, colDef, value);

            container.EnableInClassList("bf-cell--invalid", !colDef.Validate(value));
            container.SetEnabled(IsFileEditable());
        }

        private void CommitCell(VisualElement container, object newValue)
        {
            if (container.userData is not (BalanceRow row, ColumnDefinition colDef)) return;
            if (!IsFileEditable()) return;

            var oldValue = row.GetValue(colDef.ColumnId);
            if (Equals(oldValue, newValue)) return;

            var cmd = new EditCellCommand(currentTable, row.RowId, colDef.ColumnId, oldValue, newValue);
            undoRedoService.ExecuteCommand(cmd);

            container.EnableInClassList("bf-cell--invalid", !colDef.Validate(newValue));

            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        private static void SetValueWithoutNotify(VisualElement field, ColumnDefinition colDef, object value)
        {
            try
            {
                switch (colDef.DataType)
                {
                    case ColumnType.Integer when field is IntegerField iF:
                        iF.SetValueWithoutNotify(value is int iv ? iv : 0);
                        break;
                    case ColumnType.Float when field is FloatField fF:
                        fF.SetValueWithoutNotify(value is float fv ? fv : 0f);
                        break;
                    case ColumnType.Boolean when field is Toggle tog:
                        tog.SetValueWithoutNotify(value is bool bv && bv);
                        break;
                    case ColumnType.Color when field is ColorField cf:
                        cf.SetValueWithoutNotify(value is Color cv ? cv : Color.white);
                        break;
                    case ColumnType.Vector2 when field is Vector2Field v2F:
                        v2F.SetValueWithoutNotify(value is Vector2 v2v ? v2v : Vector2.zero);
                        break;
                    case ColumnType.Vector3 when field is Vector3Field v3F:
                        v3F.SetValueWithoutNotify(value is Vector3 v3v ? v3v : Vector3.zero);
                        break;
                    case ColumnType.AssetReference when field is ObjectField oF:
                        oF.SetValueWithoutNotify(value as Object);
                        break;
                    case ColumnType.Enum when field is DropdownField dd:
                        var sv = value?.ToString() ?? "";
                        dd.SetValueWithoutNotify(dd.choices.Contains(sv) ? sv : (dd.choices.Count > 0 ? dd.choices[0] : ""));
                        break;
                    default:
                        if (field is TextField tf)
                            tf.SetValueWithoutNotify(value?.ToString() ?? "");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BalanceForge] SetValueWithoutNotify: {ex.Message}");
            }
        }

        // ── Sorting ──────────────────────────────────────────────
        private void OnColumnSortingChanged()
        {
            var descs = tableView?.sortColumnDescriptions;
            if (descs == null || descs.Count == 0)
            {
                sortingState.SortColumnId = null;
                sortingState.Direction = BFSortDirection.None;
            }
            else
            {
                sortingState.SortColumnId = descs[0].columnName;
                sortingState.Direction = descs[0].direction == UnityEngine.UIElements.SortDirection.Ascending
                    ? BFSortDirection.Ascending
                    : BFSortDirection.Descending;
            }

            RefreshDisplayedRows();
            UpdateStatusBar();
        }

        // ── Data refresh ─────────────────────────────────────────
        private void RefreshDisplayedRows()
        {
            if (currentTable == null)
            {
                displayedRows = new List<BalanceRow>();
                return;
            }

            displayedRows = new List<BalanceRow>(currentTable.Rows);

            // Quick search (all columns, case-insensitive)
            if (!string.IsNullOrEmpty(quickSearchText))
            {
                var term = quickSearchText.ToLowerInvariant();
                displayedRows = displayedRows.Where(row =>
                    currentTable.Columns.Any(c =>
                    {
                        var v = row.GetValue(c.ColumnId);
                        return v != null && v.ToString().ToLowerInvariant().Contains(term);
                    })).ToList();
            }

            // Advanced filters
            if (filterConditions.Count > 0)
            {
                var composite = new CompositeFilter(filterLogicalOp);
                foreach (var cond in filterConditions)
                    composite.AddFilter(new ColumnFilter(cond));
                displayedRows = composite.Apply(displayedRows);
            }

            // Sorting
            if (sortingState.Direction != BFSortDirection.None &&
                !string.IsNullOrEmpty(sortingState.SortColumnId))
            {
                var col = currentTable.GetColumn(sortingState.SortColumnId);
                if (col != null)
                    displayedRows = TableSorter.Sort(displayedRows, sortingState.SortColumnId,
                        sortingState.Direction, col.DataType);
            }

            if (tableView != null)
            {
                tableView.itemsSource = displayedRows;
                tableView.RefreshItems();
            }

            UpdateEmptyOverlay();
        }

        // ── Actions ──────────────────────────────────────────────
        private void HandleAddRow()
        {
            if (currentTable == null || !IsFileEditable()) return;

            var row = currentTable.AddRow();
            var cmd = new AddRowCommand(currentTable, row);
            undoRedoService.ExecuteCommand(cmd);

            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
            RefreshDisplayedRows();

            // Scroll to the new row
            if (displayedRows.Count > 0)
                tableView?.ScrollToItem(displayedRows.Count - 1);

            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        private void HandleDeleteSelected()
        {
            if (currentTable == null || !IsFileEditable()) return;
            var selected = tableView?.selectedItems?.OfType<BalanceRow>().ToList();
            if (selected == null || selected.Count == 0) return;

            if (!EditorUtility.DisplayDialog("Confirm Delete",
                    $"Delete {selected.Count} selected row(s)?", "Delete", "Cancel")) return;

            var cmd = new MultiDeleteCommand(currentTable, selected);
            undoRedoService.ExecuteCommand(cmd);

            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
            RefreshDisplayedRows();
            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        private void HandleUndo()
        {
            if (!undoRedoService.CanUndo()) return;
            undoRedoService.Undo();
            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
            RefreshDisplayedRows();
            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        private void HandleRedo()
        {
            if (!undoRedoService.CanRedo()) return;
            undoRedoService.Redo();
            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
            RefreshDisplayedRows();
            UpdateUndoRedoButtons();
            UpdateStatusBar();
        }

        private void HandleValidate()
        {
            if (currentTable == null) return;
            var result = currentTable.ValidateData();
            var lbl    = validationBanner.Q<Label>("validation-label");

            if (!result.HasErrors())
            {
                lbl.text = "✓  All values are valid.";
                validationBanner.RemoveFromClassList("bf-validation-banner--error");
                validationBanner.AddToClassList("bf-validation-banner--ok");
            }
            else
            {
                lbl.text = $"⚠  {result.Errors.Count} validation error(s). Cells with issues are highlighted in red.";
                validationBanner.AddToClassList("bf-validation-banner--error");
                validationBanner.RemoveFromClassList("bf-validation-banner--ok");
                tableView?.RefreshItems(); // re-bind to show red borders
            }

            validationBanner.style.display = DisplayStyle.Flex;
        }

        private void SaveTable()
        {
            if (currentTable == null) return;
            EditorUtility.SetDirty(currentTable);
            AssetDatabase.SaveAssets();
        }

        private void ShowImportExportMenu()
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Export to CSV"), false, () =>
            {
                var path = EditorUtility.SaveFilePanel("Export Balance Table", "",
                    currentTable.TableName + ".csv", "csv");
                if (string.IsNullOrEmpty(path)) return;
                if (new CSVExporter().Export(currentTable, path))
                    EditorUtility.DisplayDialog("Export", "Table exported successfully.", "OK");
            });

            menu.AddItem(new GUIContent("Import from CSV"), false, () =>
            {
                var path = EditorUtility.OpenFilePanel("Import CSV", "", "csv");
                if (string.IsNullOrEmpty(path)) return;

                var imported = new CSVImporter().Import(path);
                if (imported == null) { EditorUtility.DisplayDialog("Import", "Failed to parse the CSV file.", "OK"); return; }

                var importedCols = imported.Columns.Select(c => c.DisplayName).ToList();
                if (!currentTable.HasStructure(importedCols))
                {
                    if (!EditorUtility.DisplayDialog("Structure mismatch",
                            "CSV columns don't match this table. Some data may be lost. Continue?",
                            "Continue", "Cancel")) return;
                }

                currentTable.Rows.Clear();
                foreach (var r in imported.Rows) currentTable.Rows.Add(r);
                EditorUtility.SetDirty(currentTable);
                AssetDatabase.SaveAssets();
                RefreshDisplayedRows();
                EditorUtility.DisplayDialog("Import", "Data imported successfully.", "OK");
            });

            menu.AddSeparator("");
            menu.AddDisabledItem(new GUIContent("Export to JSON  (coming soon)"));
            menu.ShowAsContext();
        }

        private void HandleGenerateCode()
        {
            if (currentTable == null)
            {
                EditorUtility.DisplayDialog("Generate Code", "Load a Balance Table first.", "OK");
                return;
            }
            CodeGenDialog.Show(currentTable);
        }

        // ── UI helpers ───────────────────────────────────────────
        private void ToggleFilterPanel()
        {
            filterPanelVisible = !filterPanelVisible;
            SetFilterPanelVisible(filterPanelVisible);
        }

        private void SetFilterPanelVisible(bool visible)
        {
            filterPanelVisible = visible;
            if (filterPanel != null)
                filterPanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            // Подсветка кнопки как активной/неактивной
            filterButton?.EnableInClassList("unity-button--active", visible);
        }

        private void UpdateUndoRedoButtons()
        {
            undoButton?.SetEnabled(undoRedoService.CanUndo());
            redoButton?.SetEnabled(undoRedoService.CanRedo());
        }

        private void UpdateStatusBar()
        {
            if (statusLabel == null) return;

            if (currentTable == null)
            {
                statusLabel.text = "No table loaded";
                return;
            }

            int total     = currentTable.Rows.Count;
            int displayed = displayedRows?.Count ?? 0;

            var parts = new List<string> { currentTable.TableName };
            parts.Add(displayed < total ? $"Showing {displayed} / {total} rows" : $"{total} rows");
            parts.Add($"Modified {currentTable.LastModified:yyyy-MM-dd HH:mm}");

            statusLabel.text = string.Join("   |   ", parts);
        }

        private bool IsFileEditable()
        {
            if (currentTable == null) return false;
            var path = AssetDatabase.GetAssetPath(currentTable);
            if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/")) return false;
            return !AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.ForceUpdate) ||
                    AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.UseCachedIfPossible);
        }

        // ── Keyboard shortcuts ───────────────────────────────────
        private void OnKeyDown(KeyDownEvent evt)
        {
            if (currentTable == null) return;
            bool ctrl = evt.ctrlKey || evt.commandKey;

            if (ctrl && !evt.shiftKey && evt.keyCode == KeyCode.Z)
            { HandleUndo(); evt.StopPropagation(); }
            else if ((ctrl && evt.shiftKey && evt.keyCode == KeyCode.Z) || (ctrl && evt.keyCode == KeyCode.Y))
            { HandleRedo(); evt.StopPropagation(); }
            else if (ctrl && evt.keyCode == KeyCode.Return)
            { HandleAddRow(); evt.StopPropagation(); }
            else if (evt.keyCode == KeyCode.Delete && !ctrl)
            { HandleDeleteSelected(); evt.StopPropagation(); }
            else if (ctrl && evt.keyCode == KeyCode.S)
            { SaveTable(); evt.StopPropagation(); }
        }
    }

    // ── Small extension helper ───────────────────────────────────
    internal static class VisualElementExtensions
    {
        internal static void SetDisplay(this VisualElement el, bool visible) =>
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
#endif
