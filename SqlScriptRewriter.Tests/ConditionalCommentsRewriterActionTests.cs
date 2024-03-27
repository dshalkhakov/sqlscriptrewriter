using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlScriptRewriter.Tests
{
    [TestClass]
    public class ConditionalCommentsRewriterActionTests
    {
        SqlScriptRewriter _sut;
        ConditionalCommentsRewriteAction _action;
        Mock<ITemplater> _templaterMock;
        TemplaterTestEnvironment _environment;

        [TestInitialize]
        public void SetUp()
        {
            _sut = new SqlScriptRewriter();
            _templaterMock = new Mock<ITemplater>();
            _environment = new TemplaterTestEnvironment();
            _action = new ConditionalCommentsRewriteAction(_templaterMock.Object, _environment,
                () => new TSql110Parser(true));
        }

        [TestMethod]
        public void WhenMultilineComment_HasConditionalCommentWithTrueCondition_CommentIsExpanded()
        {
            // Arrange
            var input = @"SELECT 1
/* {{#if DEVTEST}}
   , foo
   {{/if}} */
   FROM bar";
            var expectedOutput = @"SELECT 1

   , foo

   FROM bar";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(It.IsAny<string>(), It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"
   , foo
");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void WhenMultilineComment_HasConditionalCommentWithFalseCondition_CommentIsExpanded()
        {
            // Arrange
            var input = @"SELECT 1
/* {{#if DEVTEST}}
   , foo
   {{/if}} */
   FROM bar";
            var expectedOutput = @"SELECT 1


   FROM bar";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(It.IsAny<string>(), It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"
");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void WhenMultilineComment_HasConditionalComment_StarSlashesAreStrippedBeforeTemplating()
        {
            // Arrange
            var input = @"SELECT 1
/* {{#if DEVTEST}}
   , foo
   {{/if}} */
   FROM bar";
            var expectedTemplateInput = @" {{#if DEVTEST}}
   , foo
   {{/if}} ";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(It.IsAny<string>(), It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"
   , foo
");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            _templaterMock.Verify(
                m => m.ExpandConditionalComment(It.Is<string>(s => s == expectedTemplateInput), It.IsAny<TemplaterTestEnvironment>()),
                "Expected /* and */ to be stripped prior to templating, but it didn't occur");
        }

        [TestMethod]
        public void WhenConditionalCommentHasNestedMultiLineSqlComments_SqlCommentsGetWrittenIntoOutput()
        {
            // Arrange
            // DS: yes, TSQL allows multiline comments to be nested (I am surprised just as you are)
            var input = @"SELECT 1
/* {{#if DEVTEST}}
   /*, foo */
   {{/if}} */
   FROM bar";
            var expectedOutput = @"SELECT 1

   /*, foo */

   FROM bar";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(It.IsAny<string>(), It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"
   /*, foo */
");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void WhenConditionalCommentHasEmbeddedSingleLineSqlComments_SqlCommentsGetWrittenIntoOutput()
        {
            // Arrange
            var input = @"SELECT 1
/* {{#if DEVTEST}}
   --, foo
   {{/if}} */
   FROM bar";
            var expectedOutput = @"SELECT 1

   --, foo

   FROM bar";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(It.IsAny<string>(), It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"
   --, foo
");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(expectedOutput, output);
        }

        [TestMethod]
        public void WhenConditionalCommentExpandsToUnbalancedComment_SqlCommentsGetWrittenIntoOutput()
        {
            // Arrange
            var input = @"SELECT 1
/* {{#comment_if DEVTEST}} */
   , foo
/* {{#end_comment_if DEVTEST}} */
   FROM bar";
            var expectedOutput = @"SELECT 1
/*
   , foo
*/
   FROM bar";
            _templaterMock.Setup(m => m.IsConditionalComment(It.IsAny<string>()))
                .Returns(Tuple.Create(true, string.Empty));
            _templaterMock.Setup(m => m.ExpandConditionalComment(
                    It.Is<string>(expr => expr == " {{#comment_if DEVTEST}} "),
                    It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"/*");
            _templaterMock.Setup(m => m.ExpandConditionalComment(
                    It.Is<string>(expr => expr == " {{#end_comment_if DEVTEST}} "),
                    It.IsAny<TemplaterTestEnvironment>()))
                .Returns(@"*/");

            // Act
            var output = _sut.Rewrite(input, _action, out var errors);

            // Assert
            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(expectedOutput, output);
        }
    }
}
