using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class IdempotentCreateRewriteActionTests
    {
        private SqlScriptRewriter _sut;
        private IRewriteAction _createToAlterAction;

        bool TestIdentifierMatcher(string schema, string procedureName)
        {
            var schemaEqualsDbo = schema.Length == 0 || string.CompareOrdinal(schema, "dbo") == 0;
            if (schemaEqualsDbo && procedureName.StartsWith("My"))
            {
                return true;
            }
            if (string.CompareOrdinal(schema, "test") == 0 && string.IsNullOrEmpty(procedureName))
            {
                return true;
            }
            return false;
        }

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
            _createToAlterAction = new IdempotentCreateRewriteAction(TestIdentifierMatcher);
        }

        #region Procedures

        [DataTestMethod]
        [DataRow("1", "MyProc")]
        [DataRow("2", "[MyProc]")]
        [DataRow("3", "dbo.MyProc")]
        [DataRow("4", "[dbo].MyProc")]
        [DataRow("5", "[dbo].[MyProc]")]
        public void RewritesCreateProcedureToAlterProcedure(string description, string procedureName)
        {
            // Arrange
            var sqlTemplate = @"CREATE PROCEDURE {0} AS
BEGIN
  SELECT 1
END;";
            var sql = string.Format(sqlTemplate, procedureName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT type_desc, type FROM sys.procedures WITH (nolock) WHERE name = 'MyProc' AND type = 'P')
BEGIN
    EXEC('CREATE PROCEDURE {0} AS')
END
GO
ALTER PROCEDURE {0} AS
BEGIN
  SELECT 1
END;";
            var outputSql = string.Format(outputSqlFormat, procedureName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyProc")]
        [DataRow("2", "[MyProc]")]
        [DataRow("3", "dbo.MyProc")]
        [DataRow("4", "[dbo].MyProc")]
        [DataRow("5", "[dbo].[MyProc]")]
        public void RewritesAlterProcedureToCreateWithAlterProcedure(string description, string procedureName)
        {
            // Arrange
            var sqlTemplate = @"ALTER PROCEDURE {0} AS
BEGIN
  SELECT 1
END;";
            var sql = string.Format(sqlTemplate, procedureName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT type_desc, type FROM sys.procedures WITH (nolock) WHERE name = 'MyProc' AND type = 'P')
BEGIN
    EXEC('CREATE PROCEDURE {0} AS')
END
GO
ALTER PROCEDURE {0} AS
BEGIN
  SELECT 1
END;";
            var outputSql = string.Format(outputSqlFormat, procedureName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion

        #region Schemas

        [DataTestMethod]
        [DataRow("1", "test")]
        [DataRow("2", "[test]")]
        public void RewritesCreateSchemaToCreateSchemaIfNotExists(string description, string schemaName)
        {
            // Arrange
            var sqlTemplate = @"CREATE SCHEMA {0}";
            var sql = string.Format(sqlTemplate, schemaName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT name FROM sys.schemas WITH (nolock) WHERE name = 'test')
  EXEC('CREATE SCHEMA [test]');
GO";
            var outputSql = string.Format(outputSqlFormat, schemaName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod] // alter schema is left without modification
        [DataRow("1", "test")]
        [DataRow("2", "[test]")]
        public void RewritesAlterSchemaToItself(string description, string schemaName)
        {
            // Arrange
            var sqlTemplate = @"ALTER SCHEMA {0} TRANSFER Person.Address;";
            var sql = string.Format(sqlTemplate, schemaName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"ALTER SCHEMA {0} TRANSFER Person.Address;";
            var outputSql = string.Format(outputSqlFormat, schemaName);
            Assert.AreEqual(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion

        #region Functions

        [DataTestMethod]
        [DataRow("1", "MyFunc")]
        [DataRow("2", "[MyFunc]")]
        [DataRow("3", "dbo.MyFunc")]
        [DataRow("4", "[dbo].MyFunc")]
        [DataRow("5", "[dbo].[MyFunc]")]
        public void RewritesCreateFunctionToDropCreate(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 10 END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF EXISTS (SELECT name FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type IN (N'FN',N'TF',N'IF',N'TF'))
  EXEC('DROP FUNCTION {0}')
GO
CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 10 END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyFunc")]
        [DataRow("2", "[MyFunc]")]
        [DataRow("3", "dbo.MyFunc")]
        [DataRow("4", "[dbo].MyFunc")]
        [DataRow("5", "[dbo].[MyFunc]")]
        public void RewritesAlterFunctionToDropCreate(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"ALTER FUNCTION {0}() RETURNS INT AS BEGIN RETURN 10 END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF EXISTS (SELECT name FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type IN (N'FN',N'TF',N'IF',N'TF'))
  EXEC('DROP FUNCTION {0}')
GO
CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 10 END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion

        #region Views

        [DataTestMethod]
        [DataRow("1", "MyView")]
        [DataRow("2", "[MyView]")]
        [DataRow("3", "dbo.MyView")]
        [DataRow("4", "[dbo].MyView")]
        [DataRow("5", "[dbo].[MyView]")]
        public void RewritesCreateViewToCreateAlter(string description, string viewName)
        {
            // Arrange
            var sqlTemplate = @"CREATE VIEW {0} AS SELECT FOO FROM BAR";
            var sql = string.Format(sqlTemplate, viewName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF OBJECT_ID(N'{0}', N'V') IS NULL
  EXEC('CREATE VIEW {0} AS SELECT 1 BAZ')
GO
ALTER VIEW {0} AS SELECT FOO FROM BAR";
            var outputSql = string.Format(outputSqlFormat, viewName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyView")]
        [DataRow("2", "[MyView]")]
        [DataRow("3", "dbo.MyView")]
        [DataRow("4", "[dbo].MyView")]
        [DataRow("5", "[dbo].[MyView]")]
        public void RewritesAlterViewToCreateAlter(string description, string viewName)
        {
            // Arrange
            var sqlTemplate = @"ALTER VIEW {0} AS SELECT FOO FROM BAR";
            var sql = string.Format(sqlTemplate, viewName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF OBJECT_ID(N'{0}', N'V') IS NULL
  EXEC('CREATE VIEW {0} AS SELECT 1 BAZ')
GO
ALTER VIEW {0} AS SELECT FOO FROM BAR";
            var outputSql = string.Format(outputSqlFormat, viewName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion
    }
}
