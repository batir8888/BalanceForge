#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BalanceForge.Core.Data;

namespace BalanceForge.Editor.Windows
{
    public class CreateTableWizard : EditorWindow
    {
        private string tableName = "NewBalanceTable";
        private readonly List<ColumnDefinition> columns = new List<ColumnDefinition>();

        private ScrollView columnsScroll;
        private Button createButton;

        private const string StyleSheetPath = "Assets/BalanceForge/Editor/UI/BalanceForgeEditor.uss";

        [MenuItem("Assets/Create/BalanceForge/Balance Table Wizard")]
        public static void ShowWindow()
        {
            var w = GetWindow<CreateTableWizard>("Create Balance Table");
            w.minSize = new Vector2(460, 520);
        }

        private void CreateGUI()
        {
            var ss = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (ss != null) rootVisualElement.styleSheets.Add(ss);

            if (columns.Count == 0)
                columns.Add(new ColumnDefinition("id", "ID", ColumnType.String, true));

            var root = rootVisualElement;
            root.style.flexDirection = FlexDirection.Column;
            root.style.flexGrow = 1;
            root.style.paddingTop    = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft   = 10;
            root.style.paddingRight  = 10;

            // ── Title ────────────────────────────────────────────
            var title = new Label("Create New Balance Table");
            title.AddToClassList("bf-wizard-title");
            title.style.fontSize = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(title);

            // ── Table name ───────────────────────────────────────
            var nameField = new TextField("Table Name") { value = tableName, isDelayed = true };
            nameField.style.marginTop    = 6;
            nameField.style.marginBottom = 10;
            nameField.RegisterValueChangedCallback(evt =>
            {
                tableName = evt.newValue;
                RefreshCreateButton();
            });
            root.Add(nameField);

            // ── Columns header ───────────────────────────────────
            var colHeader = new VisualElement();
            colHeader.style.flexDirection  = FlexDirection.Row;
            colHeader.style.alignItems     = Align.Center;
            colHeader.style.marginBottom   = 4;

            var colsLabel = new Label("Columns");
            colsLabel.style.flexGrow = 1;
            colsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            colHeader.Add(colsLabel);

            var addColBtn = new Button(AddColumn) { text = "+ Add Column" };
            colHeader.Add(addColBtn);
            root.Add(colHeader);

            // ── Scroll view for columns ──────────────────────────
            columnsScroll = new ScrollView();
            columnsScroll.style.flexGrow    = 1;
            columnsScroll.style.marginBottom = 6;
            root.Add(columnsScroll);

            RebuildColumnList();

            // ── Footer ───────────────────────────────────────────
            var footer = new VisualElement();
            footer.AddToClassList("bf-wizard-footer");

            var cancelBtn = new Button(Close) { text = "Cancel" };

            createButton = new Button(CreateTable) { text = "Create Table", name = "btn-create" };
            createButton.AddToClassList("bf-btn-primary");

            footer.Add(cancelBtn);
            footer.Add(createButton);
            root.Add(footer);

            RefreshCreateButton();
        }

        // ── Column list ──────────────────────────────────────────
        private void AddColumn()
        {
            columns.Add(new ColumnDefinition(
                Guid.NewGuid().ToString("N"),
                $"Column {columns.Count + 1}",
                ColumnType.String,
                false));
            RebuildColumnList();
        }

        private void RebuildColumnList()
        {
            columnsScroll.Clear();
            for (int i = 0; i < columns.Count; i++)
                columnsScroll.Add(BuildColumnRow(i));
            RefreshCreateButton();
        }

        private VisualElement BuildColumnRow(int idx)
        {
            var col = columns[idx];
            var box = new VisualElement();
            box.AddToClassList("bf-wizard-col-box");

            // Header: index label + remove button
            var header = new VisualElement();
            header.AddToClassList("bf-wizard-col-header");

            var indexLbl = new Label($"Column {idx + 1}");
            indexLbl.AddToClassList("bf-wizard-col-index");
            header.Add(indexLbl);

            if (columns.Count > 1)
            {
                var removeBtn = new Button(() => { columns.RemoveAt(idx); RebuildColumnList(); }) { text = "✕ Remove" };
                removeBtn.AddToClassList("bf-wizard-remove");
                header.Add(removeBtn);
            }
            box.Add(header);

            // Display name
            var nameField = new TextField("Display Name") { value = col.DisplayName, isDelayed = true };
            nameField.RegisterValueChangedCallback(evt =>
            {
                ReplaceColumn(idx, displayName: evt.newValue);
                RebuildColumnList();
            });
            box.Add(nameField);

            // Type
            var typeField = new EnumField("Type", col.DataType);
            typeField.RegisterValueChangedCallback(evt =>
            {
                ReplaceColumn(idx, dataType: (ColumnType)evt.newValue);
                RebuildColumnList();
            });
            box.Add(typeField);

            // Required toggle
            var reqToggle = new Toggle("Required") { value = col.IsRequired };
            reqToggle.RegisterValueChangedCallback(evt =>
            {
                ReplaceColumn(idx, isRequired: evt.newValue);
                // No need to rebuild whole list just for required toggle
            });
            box.Add(reqToggle);

            // Enum value editor
            if (col.DataType == ColumnType.Enum && col.EnumDefinition != null)
                box.Add(BuildEnumSection(idx, col));

            return box;
        }

        private VisualElement BuildEnumSection(int colIdx, ColumnDefinition col)
        {
            var section = new VisualElement();
            section.AddToClassList("bf-wizard-enum-section");

            var hdr = new VisualElement();
            hdr.AddToClassList("bf-wizard-enum-header");
            hdr.Add(new Label("Enum Values") { style = { flexGrow = 1 } });
            hdr.Add(new Button(() =>
            {
                col.EnumDefinition.AddValue($"Value_{col.EnumDefinition.Values.Count}");
                RebuildColumnList();
            }) { text = "+ Add Value" });
            section.Add(hdr);

            for (int j = 0; j < col.EnumDefinition.Values.Count; j++)
            {
                var vi = j; // capture
                var row = new VisualElement();
                row.AddToClassList("bf-wizard-enum-row");

                var valField = new TextField { value = col.EnumDefinition.Values[j], isDelayed = true };
                valField.RegisterValueChangedCallback(evt => col.EnumDefinition.Values[vi] = evt.newValue);

                var delBtn = new Button(() => { col.EnumDefinition.Values.RemoveAt(vi); RebuildColumnList(); }) { text = "✕" };
                delBtn.style.width = 26;

                row.Add(valField);
                row.Add(delBtn);
                section.Add(row);
            }

            return section;
        }

        // ── Helpers ──────────────────────────────────────────────
        private void ReplaceColumn(int idx,
            string displayName = null,
            ColumnType? dataType = null,
            bool? isRequired = null)
        {
            var old = columns[idx];
            columns[idx] = new ColumnDefinition(
                old.ColumnId,
                displayName ?? old.DisplayName,
                dataType    ?? old.DataType,
                isRequired  ?? old.IsRequired);

            // Preserve enum values if type stays Enum
            if ((dataType ?? old.DataType) == ColumnType.Enum &&
                old.EnumDefinition != null &&
                columns[idx].EnumDefinition != null)
            {
                foreach (var v in old.EnumDefinition.Values)
                    columns[idx].EnumDefinition.AddValue(v);
            }
        }

        private void RefreshCreateButton()
        {
            if (createButton == null) return;
            createButton.SetEnabled(!string.IsNullOrWhiteSpace(tableName) && columns.Count > 0);
        }

        // ── Create ───────────────────────────────────────────────
        private void CreateTable()
        {
            if (string.IsNullOrWhiteSpace(tableName) || columns.Count == 0) return;

            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = tableName;
            foreach (var col in columns)
                table.AddColumn(col);

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Balance Table", tableName, "asset",
                "Choose where to save the balance table");

            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(table, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = table;

            Close();

            var editor = GetWindow<UI.BalanceTableEditorWindow>();
            editor.LoadTable(table);
        }
    }
}
#endif
