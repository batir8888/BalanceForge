#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using BalanceForge.Core.Data;

namespace BalanceForge.Editor.CodeGen
{
    public static class BalanceTableCodeGenerator
    {
        // ── C# reserved keywords ─────────────────────────────────────────────────
        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked",
            "class","const","continue","decimal","default","delegate","do","double","else",
            "enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is","lock",
            "long","namespace","new","null","object","operator","out","override","params",
            "private","protected","public","readonly","ref","return","sbyte","sealed",
            "short","sizeof","stackalloc","static","string","struct","switch","this",
            "throw","true","try","typeof","uint","ulong","unchecked","unsafe","ushort",
            "using","virtual","void","volatile","while"
        };

        // ── SessionState keys ─────────────────────────────────────────────────────
        private const string KeyTablePath  = "BalanceForge.PendingBake.TablePath";
        private const string KeyAssetPath  = "BalanceForge.PendingBake.AssetPath";
        internal const string KeyOutputDir = "BalanceForge.CodeGen.OutputDir.";

        // ── Post-reload hook ──────────────────────────────────────────────────────
        [InitializeOnLoadMethod]
        private static void OnAfterDomainReload()
        {
            string tablePath = SessionState.GetString(KeyTablePath, "");
            string assetPath = SessionState.GetString(KeyAssetPath, "");
            if (string.IsNullOrEmpty(tablePath)) return;

            SessionState.EraseString(KeyTablePath);
            SessionState.EraseString(KeyAssetPath);

            EditorApplication.delayCall += () =>
            {
                var table = AssetDatabase.LoadAssetAtPath<BalanceTable>(tablePath);
                if (table == null)
                {
                    Debug.LogError($"[BalanceForge CodeGen] Cannot load table at '{tablePath}' for baking.");
                    return;
                }
                BakeData(table, assetPath);
            };
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a strongly-typed C# source file (Struct of Arrays layout) for
        /// the given BalanceTable. Writes to outputDir/{ClassName}Data.cs and returns
        /// the written path. Does NOT call AssetDatabase.Refresh() — the caller controls that.
        /// </summary>
        public static string GenerateCode(BalanceTable table, string outputDir)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            if (string.IsNullOrEmpty(outputDir)) throw new ArgumentException("outputDir must not be empty.", nameof(outputDir));

            outputDir = outputDir.Replace('\\', '/').TrimEnd('/') + "/";

            string className = ToIdentifier(table.TableName);
            if (string.IsNullOrEmpty(className)) className = "BalanceTable";

            var code = BuildSourceCode(table, className);

            // Convert Assets-relative path to OS path for Directory.CreateDirectory / File.WriteAllText
            string osDir = ToOsPath(outputDir);
            Directory.CreateDirectory(osDir);

            string filePath   = outputDir + className + "Data.cs";
            string osFilePath = ToOsPath(filePath);
            File.WriteAllText(osFilePath, code, Encoding.UTF8);

            return filePath;
        }

        /// <summary>
        /// Uses reflection to instantiate the generated ScriptableObject type,
        /// fills each column array (Struct of Arrays layout) from the source
        /// BalanceTable, and saves a .asset file.
        /// Must be called after the generated .cs has been compiled (post domain-reload).
        /// </summary>
        public static void BakeData(BalanceTable source, string generatedAssetPath)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(generatedAssetPath)) throw new ArgumentException("generatedAssetPath must not be empty.", nameof(generatedAssetPath));

            string className = ToIdentifier(source.TableName);
            if (string.IsNullOrEmpty(className)) className = "BalanceTable";

            Type dataType = FindGeneratedType(className + "Data");
            if (dataType == null)
            {
                Debug.LogError($"[BalanceForge CodeGen] Cannot find generated type '{className}Data'. " +
                               "Make sure the code was generated and Unity has finished compiling.");
                return;
            }

            // Delete existing asset to allow re-baking
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(generatedAssetPath) != null)
                AssetDatabase.DeleteAsset(generatedAssetPath);

            var dataInstance = ScriptableObject.CreateInstance(dataType);
            int rowCount     = source.Rows.Count;

            // SoA: for each column, build a typed array and fill it by iterating rows
            var usedNames = new HashSet<string>();
            foreach (var col in source.Columns)
            {
                string fieldName = ToIdentifier(col.ColumnId);
                fieldName = Deduplicate(fieldName, usedNames);

                var field = dataType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
                if (field == null) continue;                // column may have been renamed post-generation

                // field.FieldType is e.g. int[], float[], string[]
                Type elementType = field.FieldType.GetElementType();
                if (elementType == null) continue;          // not an array field — skip

                Array array = Array.CreateInstance(elementType, rowCount);
                for (int i = 0; i < rowCount; i++)
                {
                    object raw     = source.Rows[i].GetValue(col.ColumnId);
                    object coerced = CoerceValue(raw, elementType);
                    array.SetValue(coerced, i);
                }

                field.SetValue(dataInstance, array);
            }

            // Ensure output directory exists as an asset folder
            string assetDir = generatedAssetPath.Substring(0, generatedAssetPath.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(assetDir))
                Directory.CreateDirectory(ToOsPath(assetDir + "/"));

            AssetDatabase.CreateAsset(dataInstance, generatedAssetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[BalanceForge CodeGen] Baked {rowCount} rows → {generatedAssetPath}");
        }

        /// <summary>
        /// Converts a column ID or table name into a valid PascalCase C# identifier.
        /// Public so it can be used by CodeGenDialog and BalanceTablePostprocessor.
        /// </summary>
        public static string ToIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Column";

            // Split on non-alphanumeric characters, PascalCase each token
            string[] tokens = Regex.Split(name, @"[^a-zA-Z0-9]+");
            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                if (token.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(token[0]));
                if (token.Length > 1) sb.Append(token.Substring(1));
            }

            string result = sb.ToString();
            if (result.Length == 0) return "Column";

            // Prefix _ if starts with digit
            if (char.IsDigit(result[0])) result = "_" + result;

            // Suffix Value if it is a C# keyword
            if (CSharpKeywords.Contains(result)) result += "Value";

            return result;
        }

        // ── Code builder (Struct of Arrays) ──────────────────────────────────────

        private static string BuildSourceCode(BalanceTable table, string className)
        {
            var sb = new StringBuilder();

            sb.AppendLine("// AUTO-GENERATED by BalanceForge \u2014 do not edit manually.");
            sb.AppendLine($"// Table: {table.TableName}  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("// Layout: Struct of Arrays \u2014 one typed array per column.");
            sb.AppendLine("using System;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine("namespace BalanceForge.Generated");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className}Data : ScriptableObject");
            sb.AppendLine("    {");

            // One typed array field per column
            var usedNames = new HashSet<string>();
            foreach (var col in table.Columns)
            {
                string fieldName  = ToIdentifier(col.ColumnId);
                fieldName = Deduplicate(fieldName, usedNames);
                string elemType   = ToCSharpType(col.DataType);
                string emptyArray = $"System.Array.Empty<{elemType}>()";
                sb.AppendLine($"        [SerializeField] public {elemType}[] {fieldName} = {emptyArray};");
            }

            sb.AppendLine();

            // Count property — derived from the first column that was emitted
            if (table.Columns.Count > 0)
            {
                // Recompute the first field name with a fresh deduplicate pass
                string firstField = ToIdentifier(table.Columns[0].ColumnId);
                sb.AppendLine($"        /// <summary>Number of rows baked into this asset.</summary>");
                sb.AppendLine($"        public int Count => {firstField}?.Length ?? 0;");
                sb.AppendLine();
            }

            // FindIndex — lets callers search without exposing row objects
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Returns the first row index for which <paramref name=\"predicate\"/> returns true,");
            sb.AppendLine("        /// or -1 if no row matches.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public int FindIndex(Predicate<int> predicate)");
            sb.AppendLine("        {");
            sb.AppendLine("            int n = Count;");
            sb.AppendLine("            for (int i = 0; i < n; i++)");
            sb.AppendLine("                if (predicate(i)) return i;");
            sb.AppendLine("            return -1;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        // ── Type helpers ──────────────────────────────────────────────────────────

        private static string ToCSharpType(ColumnType t)
        {
            switch (t)
            {
                case ColumnType.String:         return "string";
                case ColumnType.Integer:        return "int";
                case ColumnType.Float:          return "float";
                case ColumnType.Boolean:        return "bool";
                case ColumnType.Vector2:        return "Vector2";
                case ColumnType.Vector3:        return "Vector3";
                case ColumnType.Color:          return "Color";
                case ColumnType.Enum:           return "string";
                case ColumnType.AssetReference: return "UnityEngine.Object";
                default:                        return "string";
            }
        }

        // ── Value coercion ────────────────────────────────────────────────────────

        private static object CoerceValue(object raw, Type targetType)
        {
            if (raw == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            if (targetType.IsAssignableFrom(raw.GetType()))
                return raw;

            // Common numeric narrowing from BalanceRow's boxed storage
            if (targetType == typeof(float) && raw is int intVal)   return (float)intVal;
            if (targetType == typeof(int)   && raw is float floatVal) return (int)floatVal;

            // String fallback
            if (targetType == typeof(string)) return raw.ToString();

            // Value type default if all else fails
            if (targetType.IsValueType) return Activator.CreateInstance(targetType);

            return null;
        }

        // ── Reflection helpers ────────────────────────────────────────────────────

        private static Type FindGeneratedType(string typeName)
        {
            string qualified = "BalanceForge.Generated." + typeName;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(qualified);
                if (t != null) return t;
            }
            return null;
        }

        // ── Misc helpers ──────────────────────────────────────────────────────────

        private static string Deduplicate(string name, HashSet<string> used)
        {
            if (used.Add(name)) return name;
            int n = 2;
            string candidate;
            do { candidate = name + "_" + n++; }
            while (!used.Add(candidate));
            return candidate;
        }

        /// <summary>
        /// Converts an Assets-relative path ("Assets/Foo/") to an OS absolute path.
        /// </summary>
        private static string ToOsPath(string assetsRelativePath)
        {
            // Application.dataPath ends with "Assets"
            string projectRoot = Application.dataPath.Replace('\\', '/');
            projectRoot = projectRoot.Substring(0, projectRoot.Length - "Assets".Length);
            return projectRoot + assetsRelativePath;
        }
    }
}
#endif
