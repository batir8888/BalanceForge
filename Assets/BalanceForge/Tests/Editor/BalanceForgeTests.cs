using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

// Direct namespace imports - no assembly references needed
using BalanceForge.Core.Data;
using BalanceForge.Data.Operations;
using BalanceForge.Services;
using BalanceForge.ImportExport;
using BalanceForge.Editor.CodeGen;

namespace BalanceForge.Tests
{
    /// <summary>
    /// Simplified unit tests for BalanceForge - No assembly definition required
    /// Place this file in Assets/Tests/Editor/ folder
    /// </summary>
    public class BalanceForgeTests
    {
        #region BalanceRow Core Tests
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test01_BalanceRow_StringValue_Success()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act
            row.SetValue("testCol", "TestValue");
            var result = row.GetValue("testCol");
            
            // Assert
            Assert.AreEqual("TestValue", result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test02_BalanceRow_IntegerValue_Success()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act
            row.SetValue("intCol", 42);
            var result = row.GetValue("intCol");
            
            // Assert
            Assert.AreEqual(42, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test03_BalanceRow_FloatValue_Success()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act
            row.SetValue("floatCol", 3.14f);
            var result = row.GetValue("floatCol");
            
            // Assert
            Assert.AreEqual(3.14f, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test04_BalanceRow_BooleanValue_Success()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act
            row.SetValue("boolCol", true);
            var result = row.GetValue("boolCol");
            
            // Assert
            Assert.AreEqual(true, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test05_BalanceRow_Vector2Value_Success()
        {
            // Arrange
            var row = new BalanceRow();
            var expected = new Vector2(1.5f, 2.5f);
            
            // Act
            row.SetValue("vec2Col", expected);
            var result = row.GetValue("vec2Col");
            
            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test06_BalanceRow_Vector3Value_Success()
        {
            // Arrange
            var row = new BalanceRow();
            var expected = new Vector3(1f, 2f, 3f);
            
            // Act
            row.SetValue("vec3Col", expected);
            var result = row.GetValue("vec3Col");
            
            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test07_BalanceRow_ColorValue_Success()
        {
            // Arrange
            var row = new BalanceRow();
            var expected = new Color(1f, 0.5f, 0.25f, 1f);
            
            // Act
            row.SetValue("colorCol", expected);
            var result = row.GetValue("colorCol");
            
            // Assert
            Assert.AreEqual(expected, result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test08_BalanceRow_NonExistentColumn_ReturnsNull()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act
            var result = row.GetValue("nonExistent");
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test09_BalanceRow_NullValue_HandlesCorrectly()
        {
            // Arrange
            var row = new BalanceRow();
    
            // Act
            row.SetValue("col", null);
            var result = row.GetValue("col");
    
            // Assert
            // Принимаем оба допустимых результата:
            if (result == null)
            {
                Assert.Pass("Null value correctly returns null");
            }
            else
            {
                Assert.AreEqual(string.Empty, result);
            }
        }
        
        [Test, Category("BalanceRow Core Tests")]
        public void Test10_BalanceRow_Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new BalanceRow();
            original.SetValue("col1", "value1");
            original.SetValue("col2", 42);
            
            // Act
            var clone = original.Clone();
            clone.SetValue("col1", "modified");
            
            // Assert
            Assert.AreNotEqual(original.RowId, clone.RowId);
            Assert.AreEqual("value1", original.GetValue("col1"));
            Assert.AreEqual("modified", clone.GetValue("col1"));
            Assert.AreEqual(42, clone.GetValue("col2"));
        }
        
        #endregion
        
        #region BalanceTable Core Tests
        
        [Test]
        public void Test11_BalanceTable_AddColumn_Succeeds()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            var column = new ColumnDefinition("col1", "Column 1", ColumnType.String);
            
            // Act
            table.AddColumn(column);
            
            // Assert
            Assert.AreEqual(1, table.Columns.Count);
            Assert.AreEqual("Column 1", table.Columns[0].DisplayName);
        }
        
        [Test]
        public void Test12_BalanceTable_AddRow_Succeeds()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String, false, "default"));
            
            // Act
            var row = table.AddRow();
            
            // Assert
            Assert.IsNotNull(row);
            Assert.AreEqual(1, table.Rows.Count);
            Assert.AreEqual("default", row.GetValue("col1"));
        }
        
        [Test]
        public void Test13_BalanceTable_RemoveRow_Succeeds()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String));
            var row = table.AddRow();
            
            // Act
            var removed = table.RemoveRow(row.RowId);
            
            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(0, table.Rows.Count);
        }
        
        [Test]
        public void Test14_BalanceTable_RemoveNonExistentRow_ReturnsFalse()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            
            // Act
            var removed = table.RemoveRow("non-existent");
            
            // Assert
            Assert.IsFalse(removed);
        }
        
        [Test]
        public void Test15_BalanceTable_ValidateRequiredField_Fails()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Required", ColumnType.String, true));
            var row = table.AddRow();
            row.SetValue("col1", "");
            
            // Act
            var result = table.ValidateData();
            
            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.Greater(result.Errors.Count, 0);
        }
        
        #endregion
        
        #region Sorting Tests
        
        [Test]
        public void Test16_TableSorter_IntegerAscending_CorrectOrder()
        {
            // Arrange
            var rows = new List<BalanceRow>();
            var values = new[] { 3, 1, 4, 2, 5 };
            foreach (var val in values)
            {
                var row = new BalanceRow();
                row.SetValue("intCol", val);
                rows.Add(row);
            }
            
            // Act
            var sorted = TableSorter.Sort(rows, "intCol", SortDirection.Ascending, ColumnType.Integer);
            
            // Assert
            for (int i = 0; i < sorted.Count; i++)
            {
                Assert.AreEqual(i + 1, sorted[i].GetValue("intCol"));
            }
        }
        
        [Test]
        public void Test17_TableSorter_IntegerDescending_CorrectOrder()
        {
            // Arrange
            var rows = new List<BalanceRow>();
            var values = new[] { 3, 1, 4, 2, 5 };
            foreach (var val in values)
            {
                var row = new BalanceRow();
                row.SetValue("intCol", val);
                rows.Add(row);
            }
            
            // Act
            var sorted = TableSorter.Sort(rows, "intCol", SortDirection.Descending, ColumnType.Integer);
            
            // Assert
            for (int i = 0; i < sorted.Count; i++)
            {
                Assert.AreEqual(5 - i, sorted[i].GetValue("intCol"));
            }
        }
        
        [Test]
        public void Test18_TableSorter_StringAscending_CorrectOrder()
        {
            // Arrange
            var rows = new List<BalanceRow>();
            var values = new[] { "Gamma", "Alpha", "Omega", "Beta" };
            foreach (var val in values)
            {
                var row = new BalanceRow();
                row.SetValue("strCol", val);
                rows.Add(row);
            }
            
            // Act
            var sorted = TableSorter.Sort(rows, "strCol", SortDirection.Ascending, ColumnType.String);
            
            // Assert
            Assert.AreEqual("Alpha", sorted[0].GetValue("strCol").ToString());
            Assert.AreEqual("Beta", sorted[1].GetValue("strCol").ToString());
            Assert.AreEqual("Gamma", sorted[2].GetValue("strCol").ToString());
            Assert.AreEqual("Omega", sorted[3].GetValue("strCol").ToString());
        }
        
        [Test]
        public void Test19_TableSorter_NullValues_HandlesGracefully()
        {
            // Arrange
            var rows = new List<BalanceRow>();
            
            var row1 = new BalanceRow();
            row1.SetValue("col", 5);
            rows.Add(row1);
            
            var row2 = new BalanceRow();
            row2.SetValue("col", null);
            rows.Add(row2);
            
            var row3 = new BalanceRow();
            row3.SetValue("col", 3);
            rows.Add(row3);
            
            // Act & Assert - Should not throw
            var sorted = TableSorter.Sort(rows, "col", SortDirection.Ascending, ColumnType.Integer);
            Assert.IsNotNull(sorted);
            Assert.AreEqual(3, sorted.Count);
        }
        
        #endregion
        
        #region Filtering Tests
        
        [Test]
        public void Test20_ColumnFilter_Equals_FiltersCorrectly()
        {
            // Arrange
            var rows = CreateTestRows();
            var condition = new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Equals,
                Value = "Item2"
            };
            var filter = new ColumnFilter(condition);
            
            // Act
            var filtered = filter.Apply(rows);
            
            // Assert
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Item2", filtered[0].GetValue("name"));
        }
        
        [Test]
        public void Test21_ColumnFilter_Contains_FiltersCorrectly()
        {
            // Arrange
            var rows = CreateTestRows();
            var condition = new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Contains,
                Value = "Item"
            };
            var filter = new ColumnFilter(condition);
            
            // Act
            var filtered = filter.Apply(rows);
            
            // Assert
            Assert.AreEqual(3, filtered.Count);
        }
        
        [Test]
        public void Test22_ColumnFilter_GreaterThan_FiltersCorrectly()
        {
            // Arrange
            var rows = CreateTestRows();
            var condition = new FilterCondition
            {
                ColumnId = "value",
                Operator = FilterOperator.GreaterThan,
                Value = 20
            };
            var filter = new ColumnFilter(condition);
            
            // Act
            var filtered = filter.Apply(rows);
            
            // Assert
            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(30, filtered[0].GetValue("value"));
        }
        
        [Test]
        public void Test23_CompositeFilter_And_CombinesCorrectly()
        {
            // Arrange
            var rows = CreateTestRows();
            var filter = new CompositeFilter(LogicalOperator.And);
            filter.AddFilter(new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Contains,
                Value = "Item"
            }));
            filter.AddFilter(new ColumnFilter(new FilterCondition
            {
                ColumnId = "value",
                Operator = FilterOperator.GreaterThan,
                Value = 15
            }));
            
            // Act
            var filtered = filter.Apply(rows);
            
            // Assert
            Assert.AreEqual(2, filtered.Count);
        }
        
        #endregion
        
        #region Command Pattern Tests
        
        [Test]
        public void Test24_UndoRedoService_ExecuteCommand_CanUndo()
        {
            // Arrange
            var service = new UndoRedoService();
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String));
            var row = table.AddRow();
            var command = new EditCellCommand(table, row.RowId, "col1", "old", "new");
            
            // Act
            service.ExecuteCommand(command);
            
            // Assert
            Assert.IsTrue(service.CanUndo());
            Assert.IsFalse(service.CanRedo());
        }
        
        [Test]
        public void Test25_UndoRedoService_Undo_RestoresPreviousState()
        {
            // Arrange
            var service = new UndoRedoService();
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("col1", "original");
            
            var command = new EditCellCommand(table, row.RowId, "col1", "original", "modified");
            service.ExecuteCommand(command);
            
            // Act
            service.Undo();
            
            // Assert
            Assert.AreEqual("original", row.GetValue("col1"));
            Assert.IsTrue(service.CanRedo());
        }
        
        [Test]
        public void Test26_UndoRedoService_Redo_ReappliesCommand()
        {
            // Arrange
            var service = new UndoRedoService();
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("col1", "original");
            
            var command = new EditCellCommand(table, row.RowId, "col1", "original", "modified");
            service.ExecuteCommand(command);
            service.Undo();
            
            // Act
            service.Redo();
            
            // Assert
            Assert.AreEqual("modified", row.GetValue("col1"));
        }
        
        #endregion
        
        #region Validation Tests
        
        [Test]
        public void Test27_RangeValidator_ValueInRange_ReturnsTrue()
        {
            // Arrange
            var validator = new RangeValidator(0f, 100f);
            
            // Act
            var result = validator.Validate(50f);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Test28_RangeValidator_ValueOutOfRange_ReturnsFalse()
        {
            // Arrange
            var validator = new RangeValidator(0f, 100f);
            
            // Act
            var resultBelow = validator.Validate(-10f);
            var resultAbove = validator.Validate(150f);
            
            // Assert
            Assert.IsFalse(resultBelow);
            Assert.IsFalse(resultAbove);
        }
        
        [Test]
        public void Test29_RequiredValidator_NonEmptyValue_ReturnsTrue()
        {
            // Arrange
            var validator = new RequiredValidator();
            
            // Act
            var result = validator.Validate("some value");
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Test30_RequiredValidator_EmptyValue_ReturnsFalse()
        {
            // Arrange
            var validator = new RequiredValidator();
            
            // Act
            var resultNull = validator.Validate(null);
            var resultEmpty = validator.Validate("");
            
            // Assert
            Assert.IsFalse(resultNull);
            Assert.IsFalse(resultEmpty);
        }
        
        [Test]
        public void Test31_RegexValidator_MatchingPattern_ReturnsTrue()
        {
            // Arrange
            var validator = new RegexValidator(@"^\d{3}-\d{3}-\d{4}$");
            
            // Act
            var result = validator.Validate("123-456-7890");
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Test32_RegexValidator_NonMatchingPattern_ReturnsFalse()
        {
            // Arrange
            var validator = new RegexValidator(@"^\d{3}-\d{3}-\d{4}$");
            
            // Act
            var result = validator.Validate("invalid");
            
            // Assert
            Assert.IsFalse(result);
        }
        
        #endregion
        
        #region Performance Tests
        
        [Test]
        public void Test33_BalanceTable_LargeDataset_PerformsWell()
        {
            // Arrange
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Column 1", ColumnType.String));
            table.AddColumn(new ColumnDefinition("col2", "Column 2", ColumnType.Integer));
            
            var startTime = System.DateTime.Now;
            
            // Act - Add 1000 rows
            for (int i = 0; i < 1000; i++)
            {
                var row = table.AddRow();
                row.SetValue("col1", $"Value_{i}");
                row.SetValue("col2", i);
            }
            
            var duration = (System.DateTime.Now - startTime).TotalMilliseconds;
            
            // Assert
            Assert.AreEqual(1000, table.Rows.Count);
            Assert.Less(duration, 5000, "Should complete in less than 5 seconds");
        }
        
        [Test]
        public void Test34_BalanceRow_RapidUpdates_MaintainsConsistency()
        {
            // Arrange
            var row = new BalanceRow();
            
            // Act - Rapid updates
            for (int i = 0; i < 100; i++)
            {
                row.SetValue("testCol", i);
            }
            
            // Assert
            Assert.AreEqual(99, row.GetValue("testCol"));
        }
        
        [Test]
        public void Test35_SerializableDictionary_LargeDataset_HandlesCorrectly()
        {
            // Arrange
            var dict = new SerializableDictionary<string, string>();
            
            // Act
            for (int i = 0; i < 1000; i++)
            {
                dict[$"key_{i}"] = $"value_{i}";
            }
            
            dict.OnBeforeSerialize();
            dict.Clear();
            dict.OnAfterDeserialize();
            
            // Assert
            Assert.AreEqual(1000, dict.Count);
            Assert.AreEqual("value_500", dict["key_500"]);
        }
        
        #endregion
        
        #region Additional Filtering Tests

        [Test]
        public void Test36_ColumnFilter_NotEquals_FiltersCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.NotEquals,
                Value = "Item2"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(2, filtered.Count);
            Assert.IsFalse(filtered.Exists(r => r.GetValue("name").ToString() == "Item2"));
        }

        [Test]
        public void Test37_ColumnFilter_LessThan_FiltersCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "value",
                Operator = FilterOperator.LessThan,
                Value = 20
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual(10, filtered[0].GetValue("value"));
        }

        [Test]
        public void Test38_ColumnFilter_StartsWith_FiltersCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.StartsWith,
                Value = "Item"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(3, filtered.Count);
        }

        [Test]
        public void Test39_ColumnFilter_EndsWith_FiltersCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.EndsWith,
                Value = "3"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(1, filtered.Count);
            Assert.AreEqual("Item3", filtered[0].GetValue("name").ToString());
        }

        [Test]
        public void Test40_ColumnFilter_Regex_FiltersCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Regex,
                Value = @"^Item[12]$"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(2, filtered.Count);
        }

        [Test]
        public void Test41_ColumnFilter_Regex_InvalidPattern_ReturnsNoRows()
        {
            var rows = CreateTestRows();
            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Regex,
                Value = "[invalid"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(0, filtered.Count);
        }

        [Test]
        public void Test42_ColumnFilter_NullCellValue_ExcludesRow()
        {
            var rows = new List<BalanceRow>();
            var rowWithNull = new BalanceRow();
            // do NOT set "name" — GetValue returns null
            rows.Add(rowWithNull);

            var filter = new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Contains,
                Value = "Item"
            });

            var filtered = filter.Apply(rows);

            Assert.AreEqual(0, filtered.Count);
        }

        [Test]
        public void Test43_CompositeFilter_Or_CombinesCorrectly()
        {
            var rows = CreateTestRows();
            var filter = new CompositeFilter(LogicalOperator.Or);
            filter.AddFilter(new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Equals,
                Value = "Item1"
            }));
            filter.AddFilter(new ColumnFilter(new FilterCondition
            {
                ColumnId = "name",
                Operator = FilterOperator.Equals,
                Value = "Item3"
            }));

            var filtered = filter.Apply(rows);

            Assert.AreEqual(2, filtered.Count);
        }

        [Test]
        public void Test44_CompositeFilter_Empty_ReturnsAllRows()
        {
            var rows = CreateTestRows();
            var filter = new CompositeFilter(LogicalOperator.And);

            var filtered = filter.Apply(rows);

            Assert.AreEqual(3, filtered.Count);
        }

        #endregion

        #region Command Tests

        [Test]
        public void Test45_AddRowCommand_Execute_AddsRow()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var row = new BalanceRow();
            var command = new AddRowCommand(table, row);

            command.Execute();

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsTrue(table.Rows.Contains(row));
        }

        [Test]
        public void Test46_AddRowCommand_Undo_RemovesRow()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var row = new BalanceRow();
            var command = new AddRowCommand(table, row);

            command.Execute();
            command.Undo();

            Assert.AreEqual(0, table.Rows.Count);
        }

        [Test]
        public void Test47_DeleteRowCommand_Execute_RemovesRow()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var row = table.AddRow();
            var command = new DeleteRowCommand(table, row);

            command.Execute();

            Assert.AreEqual(0, table.Rows.Count);
        }

        [Test]
        public void Test48_DeleteRowCommand_Undo_RestoresRowAtOriginalIndex()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var rowA = table.AddRow(); rowA.SetValue("col1", "A");
            var rowB = table.AddRow(); rowB.SetValue("col1", "B");
            var rowC = table.AddRow(); rowC.SetValue("col1", "C");

            // Delete the middle row (index 1)
            var command = new DeleteRowCommand(table, rowB);
            command.Execute();
            command.Undo();

            Assert.AreEqual(3, table.Rows.Count);
            Assert.AreEqual("B", table.Rows[1].GetValue("col1").ToString());
        }

        [Test]
        public void Test49_MultiDeleteCommand_Execute_RemovesAllRows()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var rowA = table.AddRow();
            var rowB = table.AddRow();
            var rowC = table.AddRow();

            var command = new MultiDeleteCommand(table, new List<BalanceRow> { rowA, rowC });
            command.Execute();

            Assert.AreEqual(1, table.Rows.Count);
            Assert.IsTrue(table.Rows.Contains(rowB));
        }

        [Test]
        public void Test50_MultiDeleteCommand_Undo_RestoresRowsInOrder()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var rowA = table.AddRow(); rowA.SetValue("col1", "A");
            var rowB = table.AddRow(); rowB.SetValue("col1", "B");
            var rowC = table.AddRow(); rowC.SetValue("col1", "C");

            var command = new MultiDeleteCommand(table, new List<BalanceRow> { rowA, rowC });
            command.Execute();
            command.Undo();

            Assert.AreEqual(3, table.Rows.Count);
            Assert.AreEqual("A", table.Rows[0].GetValue("col1").ToString());
            Assert.AreEqual("C", table.Rows[2].GetValue("col1").ToString());
        }

        [Test]
        public void Test51_CommandDescriptions_MatchExpected()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("hp", "HP", ColumnType.Integer));
            var row = table.AddRow();

            ICommand addCmd    = new AddRowCommand(table, new BalanceRow());
            ICommand deleteCmd = new DeleteRowCommand(table, row);
            ICommand editCmd   = new EditCellCommand(table, row.RowId, "hp", 0, 100);
            ICommand multiCmd  = new MultiDeleteCommand(table, new List<BalanceRow> { row });

            Assert.AreEqual("Add Row",      addCmd.GetDescription());
            Assert.AreEqual("Delete Row",   deleteCmd.GetDescription());
            Assert.AreEqual("Edit Cell [hp]", editCmd.GetDescription());
            Assert.AreEqual("Delete 1 Rows", multiCmd.GetDescription());
        }

        #endregion

        #region UndoRedo Edge-Case Tests

        [Test]
        public void Test52_UndoRedoService_Clear_EmptiesBothStacks()
        {
            var service = new UndoRedoService();
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("col1", "old");

            service.ExecuteCommand(new EditCellCommand(table, row.RowId, "col1", "old", "new1"));
            service.ExecuteCommand(new EditCellCommand(table, row.RowId, "col1", "new1", "new2"));
            service.Undo();  // put one command into redo stack

            service.Clear();

            Assert.IsFalse(service.CanUndo());
            Assert.IsFalse(service.CanRedo());
        }

        [Test]
        public void Test53_UndoRedoService_NewCommandClearsRedoStack()
        {
            var service = new UndoRedoService();
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("col1", "original");

            service.ExecuteCommand(new EditCellCommand(table, row.RowId, "col1", "original", "v1"));
            service.Undo();
            Assert.IsTrue(service.CanRedo());

            // Execute a new command — redo stack must be cleared
            service.ExecuteCommand(new EditCellCommand(table, row.RowId, "col1", "original", "v2"));

            Assert.IsFalse(service.CanRedo());
        }

        #endregion

        #region CSV Export Tests

        [Test]
        public void Test54_CSVExporter_ProducesCorrectHeaders()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            table.AddColumn(new ColumnDefinition("hp",   "HP",   ColumnType.Integer));

            var exporter = new CSVExporter();
            var path = Path.Combine(Path.GetTempPath(), "bf_test_export.csv");
            exporter.Export(table, path);

            var lines = File.ReadAllLines(path);
            Assert.AreEqual("Name,HP", lines[0]);
            File.Delete(path);
        }

        [Test]
        public void Test55_CSVExporter_ProducesCorrectDataRow()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            table.AddColumn(new ColumnDefinition("hp",   "HP",   ColumnType.Integer));
            var row = table.AddRow();
            row.SetValue("name", "Warrior");
            row.SetValue("hp",   100);

            var exporter = new CSVExporter();
            var path = Path.Combine(Path.GetTempPath(), "bf_test_datarow.csv");
            exporter.Export(table, path);

            var lines = File.ReadAllLines(path);
            Assert.AreEqual("Warrior,100", lines[1]);
            File.Delete(path);
        }

        [Test]
        public void Test56_CSVExporter_QuotesFieldWithComma()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("desc", "Desc", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("desc", "Sword, Magic");

            var exporter = new CSVExporter();
            var path = Path.Combine(Path.GetTempPath(), "bf_test_comma.csv");
            exporter.Export(table, path);

            var content = File.ReadAllText(path);
            StringAssert.Contains("\"Sword, Magic\"", content);
            File.Delete(path);
        }

        [Test]
        public void Test57_CSVExporter_EscapesDoubleQuote()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("desc", "Desc", ColumnType.String));
            var row = table.AddRow();
            row.SetValue("desc", "He said \"hi\"");

            var exporter = new CSVExporter();
            var path = Path.Combine(Path.GetTempPath(), "bf_test_quote.csv");
            exporter.Export(table, path);

            var content = File.ReadAllText(path);
            StringAssert.Contains("\"He said \"\"hi\"\"\"", content);
            File.Delete(path);
        }

        [Test]
        public void Test58_CSVExporter_InvalidPath_ReturnsFalse()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("col1", "Col1", ColumnType.String));

            var exporter = new CSVExporter();
            var result = exporter.Export(table, "/nonexistent_dir/file.csv");

            Assert.IsFalse(result);
        }

        #endregion

        #region CSV Import Tests

        [Test]
        public void Test59_CSVImporter_CanImport_TrueForCsvExtension()
        {
            var importer = new CSVImporter();
            Assert.IsTrue(importer.CanImport("data.csv"));
            Assert.IsTrue(importer.CanImport("DATA.CSV"));
        }

        [Test]
        public void Test60_CSVImporter_CanImport_FalseForOtherExtension()
        {
            var importer = new CSVImporter();
            Assert.IsFalse(importer.CanImport("data.json"));
            Assert.IsFalse(importer.CanImport("data.txt"));
        }

        [Test]
        public void Test61_CSVImporter_Import_ReturnsNullForNonCsvPath()
        {
            var importer = new CSVImporter();
            var result = importer.Import("data.json");
            Assert.IsNull(result);
        }

        [Test]
        public void Test62_CSVImporter_InfersIntegerColumn()
        {
            var path = WriteTempCsv("Name,HP\nWarrior,100\nMage,80\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNotNull(table);
            Assert.AreEqual(2, table.Columns.Count);
            Assert.AreEqual(ColumnType.Integer, table.Columns[1].DataType);
            File.Delete(path);
        }

        [Test]
        public void Test63_CSVImporter_InfersFloatColumn()
        {
            var path = WriteTempCsv("Name,Speed\nWarrior,1.5\nMage,2.3\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNotNull(table);
            Assert.AreEqual(ColumnType.Float, table.Columns[1].DataType);
            File.Delete(path);
        }

        [Test]
        public void Test64_CSVImporter_InfersBooleanColumn()
        {
            var path = WriteTempCsv("Name,IsActive\nWarrior,True\nMage,False\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNotNull(table);
            Assert.AreEqual(ColumnType.Boolean, table.Columns[1].DataType);
            File.Delete(path);
        }

        [Test]
        public void Test65_CSVImporter_FallsBackToStringColumn()
        {
            var path = WriteTempCsv("Name,Tag\nWarrior,fire\nMage,ice\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNotNull(table);
            Assert.AreEqual(ColumnType.String, table.Columns[1].DataType);
            File.Delete(path);
        }

        [Test]
        public void Test66_CSVImporter_CorrectRowCount()
        {
            var path = WriteTempCsv("Name,HP\nWarrior,100\nMage,80\nRogue,70\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.AreEqual(3, table.Rows.Count);
            File.Delete(path);
        }

        [Test]
        public void Test67_CSVImporter_QuotedFieldWithComma_ParsedCorrectly()
        {
            var path = WriteTempCsv("Name,Desc\nWarrior,\"Sword, Shield\"\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNotNull(table);
            Assert.AreEqual("Sword, Shield", table.Rows[0].GetValue("col_1").ToString());
            File.Delete(path);
        }

        [Test]
        public void Test68_CSVImporter_ReturnsNullForHeaderOnlyFile()
        {
            var path = WriteTempCsv("Name,HP\n");
            var importer = new CSVImporter();

            var table = importer.Import(path);

            Assert.IsNull(table);
            File.Delete(path);
        }

        #endregion

        #region ClipboardService Tests

        [Test]
        public void Test69_ClipboardService_Copy_Paste_ReturnsSameValue()
        {
            ClipboardService.Clear();
            ClipboardService.Copy("hp", 42);

            var result = ClipboardService.Paste("hp");

            Assert.AreEqual(42, result);
        }

        [Test]
        public void Test70_ClipboardService_CanPaste_TrueAfterCopy()
        {
            ClipboardService.Clear();
            ClipboardService.Copy("hp", 100);

            Assert.IsTrue(ClipboardService.CanPaste("hp"));
        }

        [Test]
        public void Test71_ClipboardService_Clear_RemovesData()
        {
            ClipboardService.Copy("hp", 100);
            ClipboardService.Clear();

            // CanPaste falls back to GUIUtility.systemCopyBuffer — clear that too
            GUIUtility.systemCopyBuffer = "";

            Assert.IsFalse(ClipboardService.CanPaste("hp"));
        }

        [Test]
        public void Test72_ClipboardService_CopyMultiple_PasteMultiple_ReturnAllValues()
        {
            ClipboardService.Clear();
            var values = new Dictionary<string, object>
            {
                { "hp",   100 },
                { "name", "Warrior" }
            };
            ClipboardService.CopyMultiple(values);

            var result = ClipboardService.PasteMultiple();

            Assert.AreEqual(100,      result["hp"]);
            Assert.AreEqual("Warrior", result["name"]);
        }

        [Test]
        public void Test73_ClipboardService_Copy_OverwritesPreviousData()
        {
            ClipboardService.Clear();
            ClipboardService.Copy("hp", 50);
            ClipboardService.Copy("hp", 99);

            Assert.AreEqual(99, ClipboardService.Paste("hp"));
        }

        #endregion

        #region BalanceTable Additional Tests

        [Test]
        public void Test74_BalanceTable_ValidateData_PassesForValidData()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String, true));
            var row = table.AddRow();
            row.SetValue("name", "Warrior");

            var result = table.ValidateData();

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [Test]
        public void Test75_BalanceTable_RemoveColumn_RemovesFromAllRows()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            table.AddColumn(new ColumnDefinition("hp",   "HP",   ColumnType.Integer));
            var row = table.AddRow();
            row.SetValue("name", "Warrior");
            row.SetValue("hp", 100);

            table.RemoveColumn("hp");

            Assert.AreEqual(1, table.Columns.Count);
            var hpAfter = row.GetValue("hp");
            Assert.IsTrue(hpAfter == null || hpAfter.Equals(string.Empty),
                $"Expected null or empty string after column removal, but was: {hpAfter}");
        }

        [Test]
        public void Test76_BalanceTable_HasStructure_TrueForMatchingColumns()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            table.AddColumn(new ColumnDefinition("hp",   "HP",   ColumnType.Integer));

            Assert.IsTrue(table.HasStructure(new List<string> { "Name", "HP" }));
        }

        [Test]
        public void Test77_BalanceTable_HasStructure_FalseForMismatch()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));

            Assert.IsFalse(table.HasStructure(new List<string> { "Name", "HP" }));
            Assert.IsFalse(table.HasStructure(new List<string> { "WrongName" }));
        }

        [Test]
        public void Test78_BalanceTable_AddColumn_SetsDefaultOnExistingRows()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            var row = table.AddRow();

            // Add a second column after the row already exists
            table.AddColumn(new ColumnDefinition("hp", "HP", ColumnType.Integer, false, 50));

            Assert.AreEqual(50, row.GetValue("hp"));
        }

        [Test]
        public void Test79_BalanceTable_GetRow_ReturnsCorrectRow()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.AddColumn(new ColumnDefinition("name", "Name", ColumnType.String));
            var row0 = table.AddRow();
            var row1 = table.AddRow();

            Assert.AreEqual(row0, table.GetRow(0));
            Assert.AreEqual(row1, table.GetRow(1));
            Assert.IsNull(table.GetRow(99));
        }

        [Test]
        public void Test80_BalanceTable_GetColumn_ReturnsCorrectColumn()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            var col = new ColumnDefinition("hp", "HP", ColumnType.Integer);
            table.AddColumn(col);

            Assert.AreEqual(col, table.GetColumn("hp"));
            Assert.IsNull(table.GetColumn("nonexistent"));
        }

        #endregion

        #region CodeGen — ToIdentifier Tests

        [Test]
        public void Test81_ToIdentifier_SimpleWord_PascalCase()
        {
            Assert.AreEqual("Name", BalanceTableCodeGenerator.ToIdentifier("name"));
        }

        [Test]
        public void Test82_ToIdentifier_WordsWithSpace_PascalCase()
        {
            Assert.AreEqual("HpMax", BalanceTableCodeGenerator.ToIdentifier("hp max"));
        }

        [Test]
        public void Test83_ToIdentifier_WordsWithDash_PascalCase()
        {
            Assert.AreEqual("HpMax", BalanceTableCodeGenerator.ToIdentifier("hp-max"));
        }

        [Test]
        public void Test84_ToIdentifier_WordsWithUnderscores_PascalCase()
        {
            Assert.AreEqual("IsActive", BalanceTableCodeGenerator.ToIdentifier("is_active"));
        }

        [Test]
        public void Test85_ToIdentifier_StartsWithDigit_UnderscorePrefix()
        {
            Assert.AreEqual("_3dSpeed", BalanceTableCodeGenerator.ToIdentifier("3d_speed"));
        }

        [Test]
        public void Test86_ToIdentifier_AllSpecialChars_ReturnsColumn()
        {
            Assert.AreEqual("Column", BalanceTableCodeGenerator.ToIdentifier("---"));
        }

        [Test]
        public void Test87_ToIdentifier_EmptyString_ReturnsColumn()
        {
            Assert.AreEqual("Column", BalanceTableCodeGenerator.ToIdentifier(""));
        }

        [Test]
        public void Test88_ToIdentifier_Null_ReturnsColumn()
        {
            Assert.AreEqual("Column", BalanceTableCodeGenerator.ToIdentifier(null));
        }

        [Test]
        public void Test89_ToIdentifier_PreservesInnerUpperCase()
        {
            // Single token: first char uppercased, rest preserved as-is
            Assert.AreEqual("MyHPValue", BalanceTableCodeGenerator.ToIdentifier("myHPValue"));
        }

        [Test]
        public void Test90_ToIdentifier_MultipleConsecutiveDelimiters_Collapsed()
        {
            // "hp__max" — "__" is one separator (+ quantifier) → ["hp","max"] → "HpMax"
            Assert.AreEqual("HpMax", BalanceTableCodeGenerator.ToIdentifier("hp__max"));
        }

        #endregion

        #region CodeGen — GenerateCode Argument Validation Tests

        [Test]
        public void Test91_GenerateCode_NullTable_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                BalanceTableCodeGenerator.GenerateCode(null, "Assets/BalanceForge/Tests/TempCodeGen/"));
        }

        [Test]
        public void Test92_GenerateCode_EmptyOutputDir_ThrowsArgumentException()
        {
            var table = CreateCodeGenTable();
            Assert.Throws<System.ArgumentException>(() =>
                BalanceTableCodeGenerator.GenerateCode(table, ""));
        }

        #endregion

        #region CodeGen — GenerateCode File Content Tests

        [Test]
        public void Test93_GenerateCode_CreatesFileOnDisk()
        {
            var table = CreateCodeGenTable();
            string dir = UniqueTempCodeGenDir();
            try
            {
                BalanceTableCodeGenerator.GenerateCode(table, dir);
                Assert.IsTrue(File.Exists(GetCodeGenOsPath(dir + "CharacterStatsData.cs")));
            }
            finally { CleanTempCodeGenDir(dir); }
        }

        [Test]
        public void Test94_GenerateCode_ReturnedPathEndsWithDataCs()
        {
            var table = CreateCodeGenTable();
            string dir = UniqueTempCodeGenDir();
            try
            {
                string path = BalanceTableCodeGenerator.GenerateCode(table, dir);
                StringAssert.EndsWith("CharacterStatsData.cs", path);
            }
            finally { CleanTempCodeGenDir(dir); }
        }

        [Test]
        public void Test95_GenerateCode_ContainsCorrectClassDeclaration()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("public class CharacterStatsData : ScriptableObject", code);
        }

        [Test]
        public void Test96_GenerateCode_NoRowClassEmitted()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            // SoA has no separate Row class
            StringAssert.DoesNotContain("class CharacterStatsRow", code);
        }

        [Test]
        public void Test97_GenerateCode_CorrectNamespace()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("namespace BalanceForge.Generated", code);
        }

        [Test]
        public void Test98_GenerateCode_IntegerColumn_EmitsIntArray()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("int[] Hp", code);
        }

        [Test]
        public void Test99_GenerateCode_FloatColumn_EmitsFloatArray()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("float[] Speed", code);
        }

        [Test]
        public void Test100_GenerateCode_BoolColumn_EmitsBoolArray()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("bool[] IsActive", code);
        }

        [Test]
        public void Test101_GenerateCode_StringColumn_EmitsStringArray()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("string[] Name", code);
        }

        [Test]
        public void Test102_GenerateCode_DefaultIsArrayEmpty()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("System.Array.Empty<int>()",    code);
            StringAssert.Contains("System.Array.Empty<float>()",  code);
            StringAssert.Contains("System.Array.Empty<string>()", code);
            StringAssert.Contains("System.Array.Empty<bool>()",   code);
        }

        [Test]
        public void Test103_GenerateCode_ContainsCountProperty()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("public int Count =>", code);
        }

        [Test]
        public void Test104_GenerateCode_ContainsFindIndexMethod()
        {
            string code = GenerateCodeAndRead(CreateCodeGenTable());
            StringAssert.Contains("public int FindIndex(Predicate<int> predicate)", code);
        }

        [Test]
        public void Test105_GenerateCode_DuplicateColumnIds_AreDeduplicated()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = "DupTest";
            table.AddColumn(new ColumnDefinition("hp", "HP First",  ColumnType.Integer));
            table.AddColumn(new ColumnDefinition("hp", "HP Second", ColumnType.Integer));

            string code = GenerateCodeAndRead(table);

            StringAssert.Contains("int[] Hp ",   code);  // first occurrence
            StringAssert.Contains("int[] Hp_2 ", code);  // deduplicated second
        }

        [Test]
        public void Test106_GenerateCode_AllColumnTypes_EmitCorrectArrayTypes()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = "AllTypesTable";
            table.AddColumn(new ColumnDefinition("str", "Str", ColumnType.String));
            table.AddColumn(new ColumnDefinition("i",   "I",   ColumnType.Integer));
            table.AddColumn(new ColumnDefinition("f",   "F",   ColumnType.Float));
            table.AddColumn(new ColumnDefinition("b",   "B",   ColumnType.Boolean));
            table.AddColumn(new ColumnDefinition("v2",  "V2",  ColumnType.Vector2));
            table.AddColumn(new ColumnDefinition("v3",  "V3",  ColumnType.Vector3));
            table.AddColumn(new ColumnDefinition("col", "Col", ColumnType.Color));
            table.AddColumn(new ColumnDefinition("e",   "E",   ColumnType.Enum));
            table.AddColumn(new ColumnDefinition("ar",  "Ar",  ColumnType.AssetReference));

            string code = GenerateCodeAndRead(table);

            StringAssert.Contains("string[]",             code);
            StringAssert.Contains("int[]",                code);
            StringAssert.Contains("float[]",              code);
            StringAssert.Contains("bool[]",               code);
            StringAssert.Contains("Vector2[]",            code);
            StringAssert.Contains("Vector3[]",            code);
            StringAssert.Contains("Color[]",              code);
            StringAssert.Contains("UnityEngine.Object[]", code);
        }

        [Test]
        public void Test107_GenerateCode_EmptyTable_StillCompilableOutput()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = "EmptyTable";
            // No columns — should produce a valid class without crashing

            string code = GenerateCodeAndRead(table);

            StringAssert.Contains("public class EmptyTableData : ScriptableObject", code);
            // Count property is omitted when there are no columns
            StringAssert.DoesNotContain("public int Count =>", code);
        }

        #endregion

        #region CodeGen — BakeData Argument Validation Tests

        [Test]
        public void Test108_BakeData_NullSource_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                BalanceTableCodeGenerator.BakeData(null, "Assets/BalanceForge/Generated/Test.asset"));
        }

        [Test]
        public void Test109_BakeData_EmptyAssetPath_ThrowsArgumentException()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            Assert.Throws<System.ArgumentException>(() =>
                BalanceTableCodeGenerator.BakeData(table, ""));
        }

        #endregion

        #region Helper Methods

        private List<BalanceRow> CreateTestRows()
        {
            var rows = new List<BalanceRow>();

            var row1 = new BalanceRow();
            row1.SetValue("name", "Item1");
            row1.SetValue("value", 10);
            rows.Add(row1);

            var row2 = new BalanceRow();
            row2.SetValue("name", "Item2");
            row2.SetValue("value", 20);
            rows.Add(row2);

            var row3 = new BalanceRow();
            row3.SetValue("name", "Item3");
            row3.SetValue("value", 30);
            rows.Add(row3);

            return rows;
        }

        private string WriteTempCsv(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), $"bf_import_{System.Guid.NewGuid():N}.csv");
            File.WriteAllText(path, content);
            return path;
        }

        // ── CodeGen helpers ───────────────────────────────────────

        private BalanceTable CreateCodeGenTable()
        {
            var table = ScriptableObject.CreateInstance<BalanceTable>();
            table.TableName = "CharacterStats";
            table.AddColumn(new ColumnDefinition("name",      "Name",     ColumnType.String));
            table.AddColumn(new ColumnDefinition("hp",        "Hp",       ColumnType.Integer));
            table.AddColumn(new ColumnDefinition("speed",     "Speed",    ColumnType.Float));
            table.AddColumn(new ColumnDefinition("is_active", "IsActive", ColumnType.Boolean));
            return table;
        }

        /// <summary>Returns a unique Assets-relative temp directory for each test.</summary>
        private string UniqueTempCodeGenDir() =>
            "Assets/BalanceForge/Tests/TempCodeGen_" + System.Guid.NewGuid().ToString("N").Substring(0, 8) + "/";

        /// <summary>Converts an Assets-relative path to an OS absolute path.</summary>
        private string GetCodeGenOsPath(string assetsRelPath)
        {
            string projectRoot = Application.dataPath.Replace('\\', '/');
            projectRoot = projectRoot.Substring(0, projectRoot.Length - "Assets".Length);
            return projectRoot + assetsRelPath;
        }

        /// <summary>Deletes a temp codegen directory if it exists.</summary>
        private void CleanTempCodeGenDir(string dir)
        {
            string osDir = GetCodeGenOsPath(dir);
            if (Directory.Exists(osDir))
                Directory.Delete(osDir, true);
        }

        /// <summary>
        /// Generates code for a table into a unique temp directory, reads the file
        /// contents, cleans up, and returns the source text.
        /// </summary>
        private string GenerateCodeAndRead(BalanceTable table)
        {
            string dir = UniqueTempCodeGenDir();
            try
            {
                BalanceTableCodeGenerator.GenerateCode(table, dir);
                string className = BalanceTableCodeGenerator.ToIdentifier(table.TableName);
                if (string.IsNullOrEmpty(className)) className = "BalanceTable";
                return File.ReadAllText(GetCodeGenOsPath(dir + className + "Data.cs"));
            }
            finally
            {
                CleanTempCodeGenDir(dir);
            }
        }

        #endregion
    }
}
