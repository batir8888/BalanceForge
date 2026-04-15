#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using BalanceForge.Core.Data;

namespace BalanceForge.Editor.CodeGen
{
    /// <summary>
    /// Small dialog that lets the user choose an output directory, then
    /// triggers code generation + schedules an asset bake after compilation.
    /// </summary>
    internal sealed class CodeGenDialog : EditorWindow
    {
        private BalanceTable _table;
        private string       _outputDir = "Assets/BalanceForge/Generated/";
        private Label        _statusLabel;
        private TextField    _dirField;

        internal static void Show(BalanceTable table)
        {
            var w = GetWindow<CodeGenDialog>(true, "Generate Typed Code", true);
            w._table = table;
            w.minSize = new Vector2(480, 150);
            w.maxSize = new Vector2(480, 150);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop    = 12;
            root.style.paddingBottom = 12;
            root.style.paddingLeft   = 14;
            root.style.paddingRight  = 14;

            // ── Directory row ─────────────────────────────────────────
            var dirRow = new VisualElement();
            dirRow.style.flexDirection = FlexDirection.Row;
            dirRow.style.marginBottom  = 8;

            _dirField = new TextField("Output folder");
            _dirField.value = _outputDir;
            _dirField.isDelayed = true;
            _dirField.style.flexGrow = 1;
            _dirField.labelElement.style.minWidth = 90;
            _dirField.RegisterValueChangedCallback(evt => _outputDir = evt.newValue);

            var browseBtn = new Button(BrowseDirectory) { text = "\u2026" };
            browseBtn.style.width     = 28;
            browseBtn.style.marginLeft = 4;

            dirRow.Add(_dirField);
            dirRow.Add(browseBtn);
            root.Add(dirRow);

            // ── Status label ──────────────────────────────────────────
            _statusLabel = new Label("");
            _statusLabel.style.marginBottom = 10;
            _statusLabel.style.color        = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            _statusLabel.style.whiteSpace   = WhiteSpace.Normal;
            root.Add(_statusLabel);

            // ── Footer buttons ────────────────────────────────────────
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;

            var cancelBtn = new Button(Close) { text = "Cancel" };
            cancelBtn.style.marginRight = 6;

            var genBtn = new Button(OnGenerate) { text = "Generate + Bake" };

            footer.Add(cancelBtn);
            footer.Add(genBtn);
            root.Add(footer);
        }

        private void BrowseDirectory()
        {
            string startDir = _outputDir.StartsWith("Assets/")
                ? System.IO.Path.GetFullPath(Application.dataPath + "/../" + _outputDir)
                : Application.dataPath;

            string chosen = EditorUtility.OpenFolderPanel("Choose output folder", startDir, "");
            if (string.IsNullOrEmpty(chosen)) return;

            chosen = chosen.Replace('\\', '/');

            // Convert absolute OS path to Assets-relative path
            string projectRoot = Application.dataPath.Replace('\\', '/');
            projectRoot = projectRoot.Substring(0, projectRoot.Length - "Assets".Length);
            if (chosen.StartsWith(projectRoot))
                chosen = chosen.Substring(projectRoot.Length);

            if (!chosen.StartsWith("Assets/") && !chosen.StartsWith("Assets\\"))
            {
                _statusLabel.text = "Output folder must be inside the project's Assets folder.";
                return;
            }

            _outputDir = chosen.TrimEnd('/') + "/";
            _dirField?.SetValueWithoutNotify(_outputDir);
            _statusLabel.text = "";
        }

        private void OnGenerate()
        {
            if (_table == null)
            {
                _statusLabel.text = "No table selected.";
                return;
            }

            _outputDir = _outputDir.Replace('\\', '/').TrimEnd('/') + "/";

            if (!_outputDir.StartsWith("Assets/"))
            {
                _statusLabel.text = "Output folder must be inside the Assets folder (must start with \"Assets/\").";
                return;
            }

            try
            {
                string csPath = BalanceTableCodeGenerator.GenerateCode(_table, _outputDir);

                string tableAssetPath = AssetDatabase.GetAssetPath(_table);
                string className      = BalanceTableCodeGenerator.ToIdentifier(_table.TableName);
                if (string.IsNullOrEmpty(className)) className = "BalanceTable";

                string dataAssetPath = _outputDir + className + "Data.asset";

                // Persist bake parameters across the coming domain reload
                SessionState.SetString("BalanceForge.PendingBake.TablePath", tableAssetPath);
                SessionState.SetString("BalanceForge.PendingBake.AssetPath", dataAssetPath);
                SessionState.SetString(
                    BalanceTableCodeGenerator.KeyOutputDir + _table.TableId,
                    _outputDir);

                // Show a brief message before the window is destroyed by the reload
                _statusLabel.text = $"Written: {csPath}\nBaking after compile\u2026";

                AssetDatabase.Refresh();
                // Domain reload happens here — the window is destroyed.
            }
            catch (System.Exception ex)
            {
                _statusLabel.text = $"Error: {ex.Message}";
                Debug.LogException(ex);
            }
        }
    }
}
#endif
