using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Linq;

namespace SqlScriptRewriter
{
    public class ConditionalCommentsRewriteAction : IRewriteAction
    {
        private readonly ITemplater _templater;
        private readonly object _environment;
        private readonly Func<TSqlParser> _makeParser;

        public ConditionalCommentsRewriteAction(ITemplater templater, object environment)
            : this(templater, environment, () => new TSql110Parser(true))
        {
        }

        public ConditionalCommentsRewriteAction(ITemplater templater, object environment, Func<TSqlParser> makeParser)
        {
            _templater = templater ?? throw new ArgumentNullException(nameof(templater));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _makeParser = makeParser ?? throw new ArgumentNullException(nameof(makeParser));
        }

        public string ActionName { get => "Conditional Comments Expansion"; }

        public TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> nextToken)
        {
            switch (token.TokenType)
            {
                case TSqlTokenType.MultilineComment:
                    var isConditionalComment = _templater.IsConditionalComment(token.Text);
                    if (isConditionalComment.Item1 && !string.IsNullOrEmpty(isConditionalComment.Item2))
                    {
                        var msg = string.Format("Found errors in conditional comment '{0}': {1}", token.Text, isConditionalComment.Item2);
                        throw new InvalidOperationException(msg);
                    }

                    if (isConditionalComment.Item1)
                    {
                        var textWithoutSlashStars = RemoveSlashStars(token.Text);
                        var expanded = _templater.ExpandConditionalComment(textWithoutSlashStars, _environment);
                        if (ExpansionContainsUnmatchedMultiLineComments(expanded))
                        {
                            // TSqlParser simply can't parse that. so return as-is and hope for the best.
                            return new TSqlParserToken[] { new TSqlParserToken(TSqlTokenType.MultilineComment, expanded) };
                        }

                        var parser = _makeParser();

                        return ParseAndCheckForErrors(expanded, parser);
                    }
                    break;
                case TSqlTokenType.SingleLineComment:
                    // not handling single line comments at all
                    break;
            }

            return new[] { token };
        }

        private static TSqlParserToken[] ParseAndCheckForErrors(string expanded, TSqlParser parser)
        {
            // parse, check for errors, report them if found, return the tokens (depending on what we got)
            // ideally we just want the tokens, since we cannot know in advance if the returned fragment
            // makes any sense, especially if we splice it into the current one
            using (var stream = StreamUtils.GenerateStreamFromString(expanded))
            using (var streamReader = new System.IO.StreamReader(stream))
            {
                var tree = parser.Parse(streamReader, out var errors);
                if (tree == null || tree.ScriptTokenStream == null || tree.ScriptTokenStream.Count <= 1)
                {
                    Console.WriteLine("Error parsing '{0}' into TSQL:", expanded);
                    foreach (var error in errors)
                    {
                        Console.WriteLine("{0}:{1} - {2}", error.Line, error.Column, error.Message);
                    }
                    return new TSqlParserToken[0];
                }
                var tokens = tree.ScriptTokenStream
                    .Take(tree.ScriptTokenStream.Count - 1)
                    .ToArray();
                return tokens;
            }
        }

        private static string RemoveSlashStars(string tokenText)
        {
            return tokenText.Substring(0, tokenText.Length - 2)
                .Substring(2);
        }

        private static bool ExpansionContainsUnmatchedMultiLineComments(string expanded)
        {
            if (expanded == null)
            {
                return false;
            }
            var startComment = expanded.Contains("/*");
            var endComment = expanded.Contains("*/");
            if ((startComment && !endComment)
                || (!startComment && endComment))
            {
                return true;
            }
            return false;
        }
    }
}
