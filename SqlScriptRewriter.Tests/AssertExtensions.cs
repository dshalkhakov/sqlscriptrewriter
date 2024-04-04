using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace SqlScriptRewriter.Tests
{
    public static class AssertExtensions
    {
        public static void AreEqualIgnoringSymbols(string a, string b)
        {
            var msg = string.Format("Expected: <{0}>. Actual: <{1}>.", a, b);
            Assert.AreEqual(0, string.Compare(a, b, CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols), msg);
        }
    }
}
