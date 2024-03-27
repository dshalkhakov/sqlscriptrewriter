using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
