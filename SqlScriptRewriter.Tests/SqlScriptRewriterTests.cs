using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlScriptRewriter;
using System;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class SqlScriptRewriterTests
    {
        private SqlScriptRewriter _sut;

        private static IRewriteAction _identityAction = new IdentityRewriteAction();
        private static IRewriteAction _prefixAction = new IdentifierRewriteAction(s => "prefix" + s);

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
        }

        [DataTestMethod]
        [DataRow("null input", null)]
        [DataRow("empty string", "")]
        public void WhenBadInput_GracefullyHandled(string description, string input)
        {
            // Arrange, Act
            var output = _sut.Rewrite(input, _prefixAction, out var errors);

            // Assert
            Assert.AreEqual(string.Empty, output);
            Assert.IsNotNull(errors);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void WhenInputHasErrors_TheyAreReported()
        {
            // Arrange, Act
            var _ =  _sut.Rewrite(@"CREATE PROCEDURE dbo.Foo AS
SELECT 1
END;", _identityAction, out var errors);

            // Assert
            Assert.IsNotNull(errors);
            Assert.AreEqual(1, errors.Count, "Expected errors to be returned, but it was not the case");
            Assert.AreEqual("3:4: Incorrect syntax near ;.", errors[0]);
        }

        [TestMethod]
        public void WhenInputHasErrors_IdentityAction_ScriptIsReturnedUnchanged()
        {
            // Arrange, Act
            var input = @"CREATE PROCEDURE dbo.Foo AS
SELECT 1
END;";
            var output = _sut.Rewrite(input, _identityAction, out var _);

            // Assert
            Assert.AreEqual(output, input, "Expected script to be unchanged in case of errors, but this is not the case");
        }

        [TestMethod] // should be strict 100 (2008), but we have a script using IIF, available since 110 (2012)
        public void WorksWithSqlServer2012_Iif()
        {
            var _ = _sut.Rewrite(@"CREATE PROCEDURE dbo.Foo AS
BEGIN
SELECT IIF(1 is null, 1, 0)
END;", _identityAction, out var errors);

            // Assert
            Assert.IsNotNull(errors);
            Assert.AreEqual(0, errors.Count);
        }
    }
}
