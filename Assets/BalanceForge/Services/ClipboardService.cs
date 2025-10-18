using UnityEngine;
using System.Collections.Generic;

namespace BalanceForge.Services
{
    public static class ClipboardService
    {
        private static Dictionary<string, object> clipboard = new Dictionary<string, object>();
        private static string clipboardColumnId;
        private static bool hasData = false;
        
        public static void Copy(string columnId, object value)
        {
            clipboard.Clear();
            clipboard[columnId] = value;
            clipboardColumnId = columnId;
            hasData = true;
            
            // Also copy to system clipboard as string
            GUIUtility.systemCopyBuffer = value?.ToString() ?? "";
        }
        
        public static void CopyMultiple(Dictionary<string, object> values)
        {
            clipboard = new Dictionary<string, object>(values);
            hasData = true;
            
            // Copy as tab-separated values to system clipboard
            var text = string.Join("\t", values.Values);
            GUIUtility.systemCopyBuffer = text;
        }
        
        public static bool CanPaste(string columnId)
        {
            return hasData && (clipboard.ContainsKey(columnId) || !string.IsNullOrEmpty(GUIUtility.systemCopyBuffer));
        }
        
        public static object Paste(string columnId)
        {
            if (clipboard.ContainsKey(columnId))
                return clipboard[columnId];
            
            // Try to paste from system clipboard
            return GUIUtility.systemCopyBuffer;
        }
        
        public static Dictionary<string, object> PasteMultiple()
        {
            return new Dictionary<string, object>(clipboard);
        }
        
        public static void Clear()
        {
            clipboard.Clear();
            hasData = false;
        }
    }
}