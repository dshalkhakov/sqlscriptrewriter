using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class TemplaterTests
    {
        [TestMethod]
        public void IsConditionalComment_HappyPath_ReturnsTrue()
        {
            // Arrange
            var sut = new Templater();
            var template = @"/* {{#comment_if DEVTEST}} */";

            var ret = sut.IsConditionalComment(template);

            Assert.IsTrue(ret.Item1);
            Assert.IsTrue(string.IsNullOrEmpty(ret.Item2), "Expected return error to be null or empty, but it's not the case");
        }

        [TestMethod]
        [DataRow(@"/* {#comment_if DEVTEST}} */")]
        [DataRow(@"/* {{#comment_if DEVTEST} */")]
        public void IsConditionalComment_MismatchedMustacheTags_ReportedAsError(string template)
        {
            // Arrange
            var sut = new Templater();

            // Act, Assert
            var ret = sut.IsConditionalComment(template);

            // Assert
            Assert.IsTrue(ret.Item1);
            Assert.IsFalse(string.IsNullOrEmpty(ret.Item2), "Expected return error to be populated, but it's not the case");
        }

        [TestMethod]
        [ExpectedException(typeof(System.Collections.Generic.KeyNotFoundException))]
        public void HashPropertiesAreCaseSensitive()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if devtest}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{/if}}";
            // Act, Assert
            sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = true });
        }

        [TestMethod]
        public void If_WhenTrue()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if DEVTEST}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{/if}}";
            var expectedOutput = @"
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
";
            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = true });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void If_WhenFalse()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if DEVTEST}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{/if}}";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = false });

            // Assert
            Assert.AreEqual(string.Empty, output);
        }

        [TestMethod]
        public void IfElse_ElseBranch()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if DEVTEST}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{#else}}
-- nothing in prod
{{/if}}";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = false });

            // Assert
            Assert.AreEqual(@"
-- nothing in prod
", output);
        }

        [TestMethod]
        public void IfElseIf_ElseIfBranch()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if DEVTEST}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{#elif Production}}
-- nothing in prod
{{#else}}
-- should not happen
{{/if}}";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = false, Production = true });

            // Assert
            Assert.AreEqual(@"
-- nothing in prod
", output);
        }

        [TestMethod]
        public void IfElse_IfBranch()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#if DEVTEST}}
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
{{#else}}
-- nothing in prod
{{/if}}";
            var expectedOutput = @"
,'FederalTaxID'[OfficeQualifier]
,'231943113' [OfficeIdentifier]
";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = true });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        #region comment_if

        [TestMethod]
        public void CommentIf_WhenTrue_CommentsOutLines()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#comment_if DEVTEST}}
, 'FederalTaxID' [OfficeQualifier]
{{#end_comment_if DEVTEST}}";
            var expectedOutput = @"/*
, 'FederalTaxID' [OfficeQualifier]
*/";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = true });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void CommentIf_WhenFalse_LeavesLinesAsIs()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#comment_if DEVTEST}}
, 'FederalTaxID' [OfficeQualifier]
{{#end_comment_if DEVTEST}}";
            var expectedOutput = @"
, 'FederalTaxID' [OfficeQualifier]
";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = false });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        #endregion // !comment_if

        #region uncomment_if

        [TestMethod]
        public void UncommentIf_WhenFalse_CommentsOutLines()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#uncomment_if DEVTEST}}
, 'FederalTaxID' [OfficeQualifier]
{{#end_uncomment_if DEVTEST}}";
            var expectedOutput = @"/*
, 'FederalTaxID' [OfficeQualifier]
*/";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = false });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void UncommentIf_WhenTrue_LeavesLinesAsIs()
        {
            // Arrange
            var sut = new Templater();
            var template = @"{{#uncomment_if DEVTEST}}
, 'FederalTaxID' [OfficeQualifier]
{{#end_uncomment_if DEVTEST}}";
            var expectedOutput = @"
, 'FederalTaxID' [OfficeQualifier]
";

            // Act
            var output = sut.ExpandConditionalComment(template, new TemplaterTestEnvironment { DEVTEST = true });

            // Assert
            Assert.AreEqual(expectedOutput, output);
        }

        #endregion // !uncomment_if
    }
}
