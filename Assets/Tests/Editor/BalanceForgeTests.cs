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
        

    }
}
