using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class ProcedureNameRewriteActionTests
    {
        private SqlScriptRewriter _sut;
        private ProcedureNameRewriteAction _action;

        private (string, string) RewriteFunc(string schema, string name)
        {
            return (schema, "prefix" + name);
        }

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
            _action = new ProcedureNameRewriteAction(RewriteFunc);
        }

        [DataTestMethod]
        [DataRow("CREATE", "MyProc", "dbo.prefixMyProc")]
        [DataRow("CREATE", "[MyProc]", "dbo.[prefixMyProc]")]
        [DataRow("CREATE", "dbo.MyProc", "dbo.prefixMyProc")]
        [DataRow("CREATE", "dbo . MyProc", "dbo.prefixMyProc")]
        [DataRow("CREATE", "dbo.  MyProc", "dbo.prefixMyProc")]
        [DataRow("CREATE", "[dbo].MyProc", "[dbo].prefixMyProc")]
        [DataRow("CREATE", "[dbo].[MyProc]", "[dbo].[prefixMyProc]")]
        [DataRow("ALTER", "MyProc", "dbo.prefixMyProc")]
        [DataRow("ALTER", "[MyProc]", "dbo.[prefixMyProc]")]
        [DataRow("ALTER", "dbo.MyProc", "dbo.prefixMyProc")]
        [DataRow("ALTER", "dbo . MyProc", "dbo.prefixMyProc")]
        [DataRow("ALTER", "dbo.  MyProc", "dbo.prefixMyProc")]
        [DataRow("ALTER", "[dbo].MyProc", "[dbo].prefixMyProc")]
        [DataRow("ALTER", "[dbo].[MyProc]", "[dbo].[prefixMyProc]")]
        public void CreateAlterProcedure_RewritesProcedureName(string verb, string identifier, string expectedIdentifier)
        {
            // Arrange
            var inputSqlTemplate = @"{0} PROCEDURE {1} AS
BEGIN
  SELECT 1
END;";
            var inputSql = string.Format(inputSqlTemplate, verb, identifier);

            // Act
            var output = _sut.Rewrite(inputSql, _action, out var errors);

            // Assert
            var outputSql = string.Format(inputSqlTemplate, verb, expectedIdentifier);
            Assert.AreEqual(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("MyProc", "dbo.prefixMyProc")]
        [DataRow("[MyProc]", "dbo.[prefixMyProc]")]
        [DataRow("dbo.MyProc", "dbo.prefixMyProc")]
        [DataRow("dbo.  MyProc", "dbo.prefixMyProc")]
        [DataRow("dbo . MyProc", "dbo.prefixMyProc")]
        [DataRow("[dbo].MyProc", "[dbo].prefixMyProc")]
        [DataRow("[dbo].[MyProc]", "[dbo].[prefixMyProc]")]
        [DataRow("MyProc", "dbo.prefixMyProc")]
        [DataRow("[MyProc]", "dbo.[prefixMyProc]")]
        [DataRow("dbo.MyProc", "dbo.prefixMyProc")]
        [DataRow("[dbo].MyProc", "[dbo].prefixMyProc")]
        [DataRow("[dbo].[MyProc]", "[dbo].[prefixMyProc]")]
        public void Exec_RewritesProcedureName(string identifier, string expectedIdentifier)
        {
            // Arrange
            var inputSqlTemplate = @"EXEC {0};";
            var inputSql = string.Format(inputSqlTemplate, identifier);

            // Act
            var output = _sut.Rewrite(inputSql, _action, out var errors);

            // Assert
            var outputSql = string.Format(inputSqlTemplate, expectedIdentifier);
            Assert.AreEqual(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("MyProc", "dbo.prefixMyProc", "MyOtherProc", "dbo.prefixMyOtherProc")]
        [DataRow("[MyProc]", "dbo.[prefixMyProc]", "[MyOtherProc]", "dbo.[prefixMyOtherProc]")]
        public void StoreProcWithExec_RewritesProcedureName(string identifier, string expectedIdentifier, string innerIdentifier, string expectedInnerIdentifier)
        {
            // Arrange
            var inputSqlTemplate = @"CREATE PROCEDURE {0} AS
BEGIN
  EXEC {1};
END;";
            var inputSql = string.Format(inputSqlTemplate, identifier, innerIdentifier);

            // Act
            var output = _sut.Rewrite(inputSql, _action, out var errors);

            // Assert
            var outputSql = string.Format(inputSqlTemplate, expectedIdentifier, expectedInnerIdentifier);
            Assert.AreEqual(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }

        [DataTestMethod]
        [DataRow("MyProc", "dbo.prefixMyProc", "MyOtherProc", "dbo.prefixMyOtherProc")]
        [DataRow("[MyProc]", "dbo.[prefixMyProc]", "[MyOtherProc]", "dbo.[prefixMyOtherProc]")]
        [DataRow("MyProc", "dbo.prefixMyProc", "DbName..MyOtherProc", "DbName.dbo.prefixMyOtherProc")]
        public void StoreProcWithInsertIntoExec_RewritesProcedureName(string identifier, string expectedIdentifier, string innerIdentifier, string expectedInnerIdentifier)
        {
            // Arrange
            var inputSqlTemplate = @"CREATE PROCEDURE {0} AS
BEGIN
  INSERT INTO #Temp EXEC {1}
END;";
            var inputSql = string.Format(inputSqlTemplate, identifier, innerIdentifier);

            // Act
            var output = _sut.Rewrite(inputSql, _action, out var errors);

            // Assert
            var outputSql = string.Format(inputSqlTemplate, expectedIdentifier, expectedInnerIdentifier);
            Assert.AreEqual(outputSql, output);
            Assert.AreEqual(0, errors.Count);
        }
    }
}
