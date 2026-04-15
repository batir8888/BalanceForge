#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
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
        private TextField tableNameField;

        // ── ScriptableObject import state ────────────────────────
        private ScriptableObject importSource;
        private bool appendOnImport;

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
            tableNameField = new TextField("Table Name") { value = tableName, isDelayed = true };
            tableNameField.style.marginTop    = 6;
            tableNameField.style.marginBottom = 6;
            tableNameField.RegisterValueChangedCallback(evt =>
            {
                tableName = evt.newValue;
                RefreshCreateButton();
            });
            root.Add(tableNameField);

            // ── Import from ScriptableObject ─────────────────────
            root.Add(BuildImportSection());

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

        // ── Import from ScriptableObject ─────────────────────────

        private VisualElement BuildImportSection()
        {
            var foldout = new Foldout { text = "Import Structure from ScriptableObject", value = false };
            foldout.style.marginBottom = 8;

            var hint = new Label("Reflects public and [SerializeField] fields and maps them to columns.");
            hint.style.fontSize  = 10;
            hint.style.color     = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            hint.style.marginBottom = 4;
            hint.style.whiteSpace   = WhiteSpace.Normal;
            foldout.Add(hint);

            var soField = new ObjectField("ScriptableObject") { objectType = typeof(ScriptableObject) };
            soField.RegisterValueChangedCallback(evt =>
            {
                importSource = evt.newValue as ScriptableObject;

                // Auto-fill table name when it is still the default
                if (importSource != null && tableName == "NewBalanceTable")
                {
                    tableName = importSource.GetType().Name;
                    if (tableNameField != null) tableNameField.SetValueWithoutNotify(tableName);
                    RefreshCreateButton();
                }
            });
            foldout.Add(soField);

            var appendToggle = new Toggle("Append to existing columns") { value = false };
            appendToggle.RegisterValueChangedCallback(evt => appendOnImport = evt.newValue);
            appendToggle.style.marginTop = 2;
            foldout.Add(appendToggle);

            var importBtn = new Button(ExecuteImport) { text = "Import Columns" };
            importBtn.style.marginTop = 6;
            foldout.Add(importBtn);

            return foldout;
        }

        private void ExecuteImport()
        {
            if (importSource == null)
            {
                EditorUtility.DisplayDialog("Import Structure",
                    "Drag a ScriptableObject into the field above first.", "OK");
                return;
            }

            var imported = ReflectColumns(importSource.GetType());

            if (imported.Count == 0)
            {
                EditorUtility.DisplayDialog("Import Structure",
                    $"No mappable serialized fields found on '{importSource.GetType().Name}'.\n\n" +
                    "Supported field types: string, int, float, bool, " +
                    "Vector2, Vector3, Color, enum, UnityEngine.Object subclasses.",
                    "OK");
                return;
            }

            if (!appendOnImport)
                columns.Clear();

            foreach (var col in imported)
                columns.Add(col);

            RebuildColumnList();
        }

        // ── Reflection helpers ────────────────────────────────────

        private static List<ColumnDefinition> ReflectColumns(Type type)
        {
            var result = new List<ColumnDefinition>();
            var seenIds = new HashSet<string>();

            // Collect public instance fields + private fields marked [SerializeField]
            var fields = new List<FieldInfo>();
            fields.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.Instance));
            foreach (var f in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                if (f.IsDefined(typeof(SerializeField), false))
                    fields.Add(f);

            foreach (var field in fields)
            {
                // Skip explicitly hidden or non-serialized fields
                if (field.IsDefined(typeof(HideInInspectorAttribute), false)) continue;
                if (field.IsDefined(typeof(NonSerializedAttribute),    false)) continue;
                // Skip arrays and generic collections (no direct BalanceForge mapping)
                if (field.FieldType.IsArray)        continue;
                if (field.FieldType.IsGenericType)  continue;

                ColumnType? colType = MapFieldType(field.FieldType);
                if (colType == null) continue;

                // Deduplicate column IDs
                string colId = field.Name;
                if (!seenIds.Add(colId))
                {
                    int n = 2;
                    while (!seenIds.Add(colId + "_" + n)) n++;
                    colId = colId + "_" + (n - 1);
                }

                var col = new ColumnDefinition(
                    colId,
                    FormatFieldName(field.Name),
                    colType.Value,
                    false);

                // Populate enum values from the C# enum definition
                if (colType == ColumnType.Enum && col.EnumDefinition != null)
                {
                    foreach (var enumName in Enum.GetNames(field.FieldType))
                        col.EnumDefinition.AddValue(enumName);
                }

                result.Add(col);
            }

            return result;
        }

        /// <summary>
        /// Maps a C# field type to the closest BalanceForge ColumnType.
        /// Returns null for unmappable types (complex classes, collections, etc.).
        /// </summary>
        private static ColumnType? MapFieldType(Type t)
        {
            if (t == typeof(string))                                    return ColumnType.String;
            if (t == typeof(int)   || t == typeof(long)  ||
                t == typeof(short) || t == typeof(byte)  ||
                t == typeof(uint)  || t == typeof(ulong))               return ColumnType.Integer;
            if (t == typeof(float) || t == typeof(double))              return ColumnType.Float;
            if (t == typeof(bool))                                      return ColumnType.Boolean;
            if (t == typeof(Vector2))                                   return ColumnType.Vector2;
            if (t == typeof(Vector3))                                   return ColumnType.Vector3;
            if (t == typeof(Color) || t == typeof(Color32))             return ColumnType.Color;
            if (t.IsEnum)                                               return ColumnType.Enum;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t))         return ColumnType.AssetReference;
            return null;
        }

        /// <summary>
        /// Converts a camelCase or _prefixed field name to a readable display name.
        /// Examples: "maxHp" → "Max Hp",  "m_moveSpeed" → "Move Speed"
        /// </summary>
        private static string FormatFieldName(string name)
        {
            // Strip leading m_ or _ (common Unity serialization convention)
            if (name.StartsWith("m_", StringComparison.Ordinal)) name = name.Substring(2);
            else if (name.Length > 0 && name[0] == '_')          name = name.Substring(1);

            if (name.Length == 0) return "Column";

            var sb = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                // Insert space before an uppercase letter that follows a lowercase letter
                if (i > 0 && char.IsUpper(c) && char.IsLower(name[i - 1]))
                    sb.Append(' ');
                sb.Append(i == 0 ? char.ToUpperInvariant(c) : c);
            }
            return sb.ToString();
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
