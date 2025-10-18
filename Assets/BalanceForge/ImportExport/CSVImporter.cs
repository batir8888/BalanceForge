using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    public class CSVImporter : IImporter
    {
        public BalanceTable Import(string filePath)
        {
            if (!CanImport(filePath))
                return null;
                
            var content = File.ReadAllText(filePath);
            var data = ParseCSV(content);
            
            if (data.Count < 2) // Need at least header and one row
                return null;
                
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = Path.GetFileNameWithoutExtension(filePath);
            
            // Create columns from header
            var header = data[0];
            var columns = InferColumnTypes(data);
            
            foreach (var column in columns)
            {
                table.AddColumn(column);
            }
            
            // Add rows
            for (int i = 1; i < data.Count; i++)
            {
                var row = new BalanceRow();
                for (int j = 0; j < Math.Min(header.Length, data[i].Length); j++)
                {
                    row.SetValue(columns[j].ColumnId, ParseValue(data[i][j], columns[j].DataType));
                }
                table.Rows.Add(row);
            }
            
            return table;
        }
        
        public bool CanImport(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".csv";
        }
        
        private List<string[]> ParseCSV(string content)
        {
            var result = new List<string[]>();
            var lines = content.Split('\n');
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.Add(line.Split(',').Select(s => s.Trim()).ToArray());
                }
            }
            
            return result;
        }
        
        private List<ColumnDefinition> InferColumnTypes(List<string[]> data)
        {
            var columns = new List<ColumnDefinition>();
            if (data.Count == 0) return columns;
            
            var header = data[0];
            
            for (int i = 0; i < header.Length; i++)
            {
                var columnType = ColumnType.String; // Default
                
                // Try to infer type from data
                if (data.Count > 1)
                {
                    bool allInts = true;
                    bool allFloats = true;
                    bool allBools = true;
                    
                    for (int j = 1; j < data.Count; j++)
                    {
                        if (i < data[j].Length)
                        {
                            var value = data[j][i];
                            
                            if (!int.TryParse(value, out _))
                                allInts = false;
                            if (!float.TryParse(value, out _))
                                allFloats = false;
                            if (!bool.TryParse(value, out _))
                                allBools = false;
                        }
                    }
                    
                    if (allBools)
                        columnType = ColumnType.Boolean;
                    else if (allInts)
                        columnType = ColumnType.Integer;
                    else if (allFloats)
                        columnType = ColumnType.Float;
                }
                
                var column = new ColumnDefinition(
                    $"col_{i}",
                    header[i].Trim(),
                    columnType,
                    false,
                    null
                );
                
                columns.Add(column);
            }
            
            return columns;
        }
        
        private object ParseValue(string value, ColumnType type)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
                
            try
            {
                switch (type)
                {
                    case ColumnType.Integer:
                        return int.Parse(value);
                    case ColumnType.Float:
                        return float.Parse(value);
                    case ColumnType.Boolean:
                        return bool.Parse(value);
                    default:
                        return value.Trim();
                }
            }
            catch
            {
                return value.Trim();
            }
        }
    }
    
    public interface IImporter
    {
        BalanceTable Import(string filePath);
        bool CanImport(string filePath);
    }
}
