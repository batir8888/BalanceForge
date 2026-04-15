#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using BalanceForge.Core.Data;

namespace BalanceForge.Editor.CodeGen
{
    /// <summary>
    /// Automatically re-generates and re-bakes a BalanceTable's typed data class
    /// whenever the source .asset is saved — but only if the user has already
    /// generated code for that table (opt-in via the presence of the .cs file).
    /// </summary>
    internal sealed class BalanceTablePostprocessor : AssetPostprocessor
    {
        // Guard against recursive triggering: GenerateCode writes a .cs which
        // itself triggers OnPostprocessAllAssets again.
        private static bool _regenerating;

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_regenerating) return;

            foreach (var path in importedAssets)
            {
                if (!path.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var table = AssetDatabase.LoadAssetAtPath<BalanceTable>(path);
                if (table == null) continue;

                // Look up the output directory that was used when the user first generated
                string outputDir = SessionState.GetString(
                    BalanceTableCodeGenerator.KeyOutputDir + table.TableId,
                    "Assets/BalanceForge/Generated/");

                outputDir = outputDir.Replace('\\', '/').TrimEnd('/') + "/";

                string className  = BalanceTableCodeGenerator.ToIdentifier(table.TableName);
                string csOsPath   = ToOsPath(outputDir + className + "Data.cs");

                // Opt-in: only re-generate if the .cs file already exists
                if (!File.Exists(csOsPath)) continue;

                try
                {
                    _regenerating = true;

                    BalanceTableCodeGenerator.GenerateCode(table, outputDir);

                    // Schedule bake for after domain reload
                    SessionState.SetString("BalanceForge.PendingBake.TablePath", path);
                    SessionState.SetString("BalanceForge.PendingBake.AssetPath",
                        outputDir + className + "Data.asset");

                    AssetDatabase.Refresh();
                }
                finally
                {
                    _regenerating = false;
                }
            }
        }

        private static string ToOsPath(string assetsRelativePath)
        {
            string projectRoot = Application.dataPath.Replace('\\', '/');
            projectRoot = projectRoot.Substring(0, projectRoot.Length - "Assets".Length);
            return projectRoot + assetsRelativePath;
        }
    }
}
#endif
