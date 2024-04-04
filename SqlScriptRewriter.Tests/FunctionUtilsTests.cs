using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlScriptRewriter.Enums;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class FunctionUtilsTests
    {
        private SqlScriptRewriter _sut;

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
        }

        [DataTestMethod]
        [DataRow("CREATE FUNCTION ident () RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident (@param1 int) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100)) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100)) RETURNS VARCHAR(100) AS BEGIN RETURN 'foo' END")]

        [DataRow("ALTER FUNCTION ident () RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident (@param1 int) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100)) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS INT AS BEGIN RETURN 1 END")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100)) RETURNS VARCHAR(100) AS BEGIN RETURN 'foo' END")]
        public void IsFunction_DeterminesScalarFunction(string input)
        {
            var tree = _sut.Parse(input, out var errors);

            int current = 3;
            var result = FunctionUtils.IsFunction(tok => _sut.PeekToken(tree, ref current, tok + 1));

            Assert.AreEqual(0, errors.Count, "Expected zero errors, but it's not the case");
            Assert.AreEqual(FunctionType.FN, result);
        }

        [DataTestMethod]
        [DataRow("CREATE FUNCTION ident () RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident ( @param1 int ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident (@param1 int) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100)) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]

        [DataRow("ALTER FUNCTION ident () RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident ( @param1 int ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident (@param1 int) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100)) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS TABLE AS RETURN ( SELECT 1 foo )")]
        public void IsFunction_DeterminesInlineTableValuedFunction(string input)
        {
            var tree = _sut.Parse(input, out var errors);

            int current = 3;
            var result = FunctionUtils.IsFunction(tok => _sut.PeekToken(tree, ref current, tok + 1));

            Assert.AreEqual(0, errors.Count, "Expected zero errors, but it's not the case");
            Assert.AreEqual(FunctionType.IF, result);
        }

        [DataTestMethod]
        [DataRow("CREATE FUNCTION ident () RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident (@param1 int) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100)) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("CREATE FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]

        [DataRow("ALTER FUNCTION ident () RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident (@param1 int) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident ( @param1 int, @param2 bit = null ) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100)) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        [DataRow("ALTER FUNCTION ident (@param1 VARCHAR(100), @param2 NVARCHAR(10)) RETURNS @ret TABLE (foo int) AS BEGIN SELECT 1 foo END")]
        public void IsFunction_DeterminesTableValuedFunction(string input)
        {
            var tree = _sut.Parse(input, out var errors);

            int current = 3;
            var result = FunctionUtils.IsFunction(tok => _sut.PeekToken(tree, ref current, tok + 1));

            Assert.AreEqual(0, errors.Count, "Expected zero errors, but it's not the case");
            Assert.AreEqual(FunctionType.TF, result);
        }
    }
}
