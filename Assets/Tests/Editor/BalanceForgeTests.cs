using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// Direct namespace imports - no assembly references needed
using BalanceForge.Core.Data;
using BalanceForge.Data.Operations;
using BalanceForge.Services;

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
        
        #endregion
    }
}
