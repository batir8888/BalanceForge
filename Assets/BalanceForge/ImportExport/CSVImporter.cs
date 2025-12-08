using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BalanceForge.Core.Data;

namespace BalanceForge.ImportExport
{
    /// <summary>
    /// Импортер таблицы баланса из CSV файлов (Comma-Separated Values).
    /// Автоматически определяет типы столбцов на основе анализа данных (Integer, Float, Boolean или String).
    /// Создает новый ScriptableObject BalanceTable на основе содержимого CSV файла.
    /// Реализует интерфейс IImporter.
    /// </summary>
    public class CSVImporter : IImporter
    {
        /// <summary>
        /// Импортирует CSV файл и создает новую таблицу баланса.
        /// Первая строка файла интерпретируется как заголовки столбцов.
        /// Остальные строки интерпретируются как данные строк таблицы.
        /// Типы столбцов определяются автоматически на основе анализа значений.
        /// </summary>
        /// <param name="filePath">Путь к CSV файлу для импорта.</param>
        /// <returns>Новый объект BalanceTable с импортированными данными или null если импорт не удался.</returns>
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
        
        /// <summary>
        /// Проверяет может ли импортер обработать файл.
        /// Проверяет что расширение файла равно .csv (без учета регистра).
        /// </summary>
        /// <param name="filePath">Путь к файлу для проверки.</param>
        /// <returns>true если файл имеет расширение .csv, иначе false.</returns>
        public bool CanImport(string filePath)
        {
            return Path.GetExtension(filePath).ToLower() == ".csv";
        }
        
        /// <summary>
        /// Парсит содержимое CSV файла в список массивов строк.
        /// Каждая строка файла преобразуется в массив значений разделенных запятыми.
        /// Пустые строки пропускаются, значения обрезаются от пробелов.
        /// </summary>
        /// <param name="content">Содержимое CSV файла в виде строки.</param>
        /// <returns>Список массивов строк, где каждый массив представляет одну строку CSV.</returns>
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
        
        /// <summary>
        /// Определяет типы данных для столбцов на основе анализа значений в CSV файле.
        /// Анализирует все значения в каждом столбце и пытается определить если это Boolean, Integer, Float или String.
        /// Порядок проверки: Boolean → Integer → Float → String (по умолчанию).
        /// Создает ColumnDefinition объекты с определенными типами и заголовками из первой строки.
        /// </summary>
        /// <param name="data">Распарсенные данные CSV в виде списка массивов строк.</param>
        /// <returns>Список ColumnDefinition объектов с определенными типами для каждого столбца.</returns>
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
        
        /// <summary>
        /// Парсит строковое значение в объект правильного типа.
        /// Пустые или пробельные значения возвращаются как null.
        /// При ошибке парсинга возвращается исходная строка обрезанная от пробелов.
        /// </summary>
        /// <param name="value">Строка для парсинга.</param>
        /// <param name="type">Целевой тип данных.</param>
        /// <returns>Распарсенное значение нужного типа или исходная строка при ошибке.</returns>
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
    
    /// <summary>
    /// Интерфейс для импортеров данных из различных форматов файлов в таблицы баланса.
    /// Позволяет расширять функциональность добавлением новых импортеров (JSON, XML, Excel и т.д.).
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// Импортирует файл и создает новую таблицу баланса.
        /// </summary>
        /// <param name="filePath">Путь к файлу для импорта.</param>
        /// <returns>Новый объект BalanceTable с импортированными данными или null если импорт не удался.</returns>
        BalanceTable Import(string filePath);
        
        /// <summary>
        /// Проверяет может ли импортер обработать файл по его расширению или другим критериям.
        /// </summary>
        /// <param name="filePath">Путь к файлу для проверки поддержки.</param>
        /// <returns>true если импортер может обработать этот файл, иначе false.</returns>
        bool CanImport(string filePath);
    }
}