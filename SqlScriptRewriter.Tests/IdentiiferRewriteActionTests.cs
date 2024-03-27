using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class IdentiiferRewriteActionTests
    {
        private SqlScriptRewriter _sut;
        private IRewriteAction _prefixAction;

        static Func<string, string> conditionalAddSuffixAction = s =>
        {
            if (s != null && s.StartsWith("myprefix"))
            {
                return s + "V2";
            }
            else
            {
                return s;
            }
        };
        private IRewriteAction _conditionalSuffixAction;

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
            _prefixAction = new IdentifierRewriteAction(s => "prefix" + s);
            _conditionalSuffixAction = new IdentifierRewriteAction(conditionalAddSuffixAction);
        }

        [TestMethod]
        public void RewritesIdentifier()
        {
            // Arrange, Act
            var output = _sut.Rewrite(@"CREATE PROCEDURE dbo.MyProc AS
BEGIN
  SELECT 1
END;", _prefixAction, out var errors);

            // Assert
            Assert.AreEqual(@"CREATE PROCEDURE prefixdbo.prefixMyProc AS
BEGIN
  SELECT 1
END;", output);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void RewritesQuotedIdentifier()
        {
            // Arrange, Act
            var output = _sut.Rewrite(@"CREATE PROCEDURE dbo.[MyProc] AS
BEGIN
  SELECT 1
END;", _prefixAction, out var errors);

            // Assert
            Assert.AreEqual(@"CREATE PROCEDURE prefixdbo.[prefixMyProc] AS
BEGIN
  SELECT 1
END;", output);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void RewritesQuotedIdentifier2()
        {
            // Arrange, Act
            var output = _sut.Rewrite(@"CREATE PROCEDURE [dbo].[MyProc] AS
BEGIN
  SELECT 1
END;", _prefixAction, out var errors);

            // Assert
            Assert.AreEqual(@"CREATE PROCEDURE [prefixdbo].[prefixMyProc] AS
BEGIN
  SELECT 1
END;", output);
            Assert.AreEqual(0, errors.Count);
        }

        // there are rogue cases when identifiers include spaces. just to be sure
        // this case is handled gracefully
        [TestMethod]
        public void RewritesQuotedIdentifier_WhitespaceSuffixPreserved()
        {
            // Arrange, Act
            var output = _sut.Rewrite(@"CREATE PROCEDURE dbo.[MyProc ] AS
BEGIN
  SELECT 1
END;", _prefixAction, out var errors);

            // Assert
            Assert.AreEqual(@"CREATE PROCEDURE prefixdbo.[prefixMyProc ] AS
BEGIN
  SELECT 1
END;", output);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void RewritesConditionally()
        {
            // Arrange
            // Act
            var output = _sut.Rewrite(@"CREATE PROCEDURE dbo.myprefixFoo AS
BEGIN
  EXEC OtherDatabase..Foo
END;", this._conditionalSuffixAction, out var errors);

            // Assert
            Assert.IsNotNull(errors);
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(@"CREATE PROCEDURE dbo.myprefixFooV2 AS
BEGIN
  EXEC OtherDatabase..Foo
END;", output);
        }

    }
}
