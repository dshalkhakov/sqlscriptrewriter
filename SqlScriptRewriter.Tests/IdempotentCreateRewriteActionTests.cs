using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            if (string.CompareOrdinal(schema, "evv") == 0 && string.IsNullOrEmpty(procedureName))
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
        [DataRow("1", "evv")]
        [DataRow("2", "[evv]")]
        public void RewritesCreateSchemaToCreateSchemaIfNotExists(string description, string schemaName)
        {
            // Arrange
            var sqlTemplate = @"CREATE SCHEMA {0}";
            var sql = string.Format(sqlTemplate, schemaName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT name FROM sys.schemas WITH (nolock) WHERE name = 'evv')
  EXEC('CREATE SCHEMA [evv]');
GO";
            var outputSql = string.Format(outputSqlFormat, schemaName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod] // alter schema is left without modification
        [DataRow("1", "evv")]
        [DataRow("2", "[evv]")]
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

        #region Scalar Functions (FN)

        [DataTestMethod]
        [DataRow("1", "MyScalarFunc")]
        [DataRow("2", "[MyScalarFunc]")]
        [DataRow("3", "dbo.MyScalarFunc")]
        [DataRow("4", "[dbo].MyScalarFunc")]
        [DataRow("5", "[dbo].[MyScalarFunc]")]
        public void RewritesCreateScalarFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 20 END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'FN')
  EXEC('CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 1 END')
GO
ALTER FUNCTION {0}() RETURNS INT AS BEGIN RETURN 20 END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyScalarFunc")]
        [DataRow("2", "[MyScalarFunc]")]
        [DataRow("3", "dbo.MyScalarFunc")]
        [DataRow("4", "[dbo].MyScalarFunc")]
        [DataRow("5", "[dbo].[MyScalarFunc]")]
        public void RewritesAlterScalarFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"ALTER FUNCTION {0}() RETURNS INT AS BEGIN RETURN 20 END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'FN')
  EXEC('CREATE FUNCTION {0}() RETURNS INT AS BEGIN RETURN 1 END')
GO
ALTER FUNCTION {0}() RETURNS INT AS BEGIN RETURN 20 END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion // !Scalar Functions (FN)

        #region Inline Table-Valued Functions (IF)

        [DataTestMethod]
        [DataRow("1", "MyITVFunc")]
        [DataRow("2", "[MyITVFunc]")]
        [DataRow("3", "dbo.MyITVFunc")]
        [DataRow("4", "[dbo].MyITVFunc")]
        [DataRow("5", "[dbo].[MyITVFunc]")]
        public void RewritesCreateInlineTableValuedFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"CREATE FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT * FROM FOO)";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'IF')
  EXEC('CREATE FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT 1 FOO)')
GO
ALTER FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT * FROM FOO)";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyITVFunc")]
        [DataRow("2", "[MyITVFunc]")]
        [DataRow("3", "dbo.MyITVFunc")]
        [DataRow("4", "[dbo].MyITVFunc")]
        [DataRow("5", "[dbo].[MyITVFunc]")]
        public void RewritesAlterInlineTableValuedFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"ALTER FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT * FROM FOO)";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'IF')
  EXEC('CREATE FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT 1 FOO)')
GO
ALTER FUNCTION {0}() RETURNS TABLE AS RETURN (SELECT * FROM FOO)";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion // !Inline Table-Valued Functions (IF)

        #region Table-Valued Functions (TF)

        [DataTestMethod]
        [DataRow("1", "MyTVFunc")]
        [DataRow("2", "[MyTVFunc]")]
        [DataRow("3", "dbo.MyTVFunc")]
        [DataRow("4", "[dbo].MyTVFunc")]
        [DataRow("5", "[dbo].[MyTVFunc]")]
        public void RewritesCreateTableValuedFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"CREATE FUNCTION {0}() RETURNS @ret TABLE (bar INT) AS BEGIN INSERT INTO @ret SELECT 10 BAR RETURN END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'TF')
  EXEC('CREATE FUNCTION {0}() RETURNS @ret TABLE (foo INT) AS BEGIN INSERT INTO @ret SELECT 1 FOO RETURN END')
GO
ALTER FUNCTION {0}() RETURNS @ret TABLE (bar INT) AS BEGIN INSERT INTO @ret SELECT 10 BAR RETURN END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("1", "MyITVFunc")]
        [DataRow("2", "[MyITVFunc]")]
        [DataRow("3", "dbo.MyITVFunc")]
        [DataRow("4", "[dbo].MyITVFunc")]
        [DataRow("5", "[dbo].[MyITVFunc]")]
        public void RewritesAlterTableValuedFunctionToCreateAlter(string description, string functionName)
        {
            // Arrange
            var sqlTemplate = @"ALTER FUNCTION {0}() RETURNS @ret TABLE (bar INT) AS BEGIN INSERT INTO @ret SELECT 10 BAR RETURN END";
            var sql = string.Format(sqlTemplate, functionName);

            // Act
            var output = _sut.Rewrite(sql, _createToAlterAction, out var errors);

            // Assert
            var outputSqlFormat = @"IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'{0}') AND type = N'TF')
  EXEC('CREATE FUNCTION {0}() RETURNS @ret TABLE (foo INT) AS BEGIN INSERT INTO @ret SELECT 1 FOO RETURN END')
GO
ALTER FUNCTION {0}() RETURNS @ret TABLE (bar INT) AS BEGIN INSERT INTO @ret SELECT 10 BAR RETURN END";
            var outputSql = string.Format(outputSqlFormat, functionName);
            AssertExtensions.AreEqualIgnoringSymbols(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        #endregion // !Table-Valued Functions (TF)

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
