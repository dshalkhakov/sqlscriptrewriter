using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SqlScriptRewriter
{
    public class SqlScriptRewriter : ISqlScriptRewriter
    {
        public string Rewrite(string script, IRewriteAction rewriteAction, out IList<string> err)
        {
            if (rewriteAction == null)
            {
                rewriteAction = new IdentityRewriteAction();
            }

            var tree = Parse(script, out var errors);
            if (errors != null && errors.Count > 0)
            {
                err = ConvertErrors(errors);
                return script;
            }

            err = new List<string>();
            if (tree != null)
            {
                var nodes = RewriteTokens(tree, rewriteAction);
                var output = this.TokensToText(nodes);
                return output;
            }
            return script;
        }

        private static IList<string> ConvertErrors(IList<ParseError> errors)
        {
            IList<string> err = new List<string>();
            foreach (var error in errors)
            {
                err.Add($"{error.Line}:{error.Column}: {error.Message}");
            }
            return err;
        }

        public TSqlFragment Parse(string script, out IList<ParseError> errors)
        {
            errors = new List<ParseError>();
            var parser = new TSql110Parser(true); // TODO should be 100. but we have a script which has IIF, available since 110
            using (var stream = StreamUtils.GenerateStreamFromString(script))
            using (var streamReader = new System.IO.StreamReader(stream))
            {
                return parser.Parse(streamReader, out errors);
            }
        }

        public TSqlParserToken PeekToken(TSqlFragment fragment, ref int current, int offset)
        {
            var ofs = 0;
            for (var i = 0; i < offset; i++)
            {
                ofs++;
                // skip whitepace
                while (true)
                {
                    if (current + ofs > fragment.LastTokenIndex)
                        return null; // EOF
                    if (fragment.ScriptTokenStream[current + ofs].TokenType == TSqlTokenType.WhiteSpace)
                    {
                        ofs++;
                        continue;
                    }
                    break;
                }
                // found the token
            }
            return fragment.ScriptTokenStream[current + ofs];
        }

        private TSqlParserToken NextToken(TSqlFragment fragment, ref int i)
        {
            i++;
            while (true)
            {
                // skip whitespace
                if (i > fragment.LastTokenIndex)
                    return null; // EOF
                if (fragment.ScriptTokenStream[i].TokenType == TSqlTokenType.WhiteSpace)
                {
                    i++;
                    continue;
                }
                break;
            }
            return fragment.ScriptTokenStream[i];
        }

        private List<TSqlParserToken> RewriteTokens(TSqlFragment fragment, IRewriteAction rewriteAction)
        {
            var outTokens = new List<TSqlParserToken>();
            var tokenStream = fragment.ScriptTokenStream;

            for (var i = fragment.FirstTokenIndex; i <= fragment.LastTokenIndex; i++)
            {
                var token = tokenStream[i];
                var outputTokens = rewriteAction.DoRewrite(
                    token,
                    offset => PeekToken(fragment, ref i, offset),
                    () => NextToken(fragment, ref i));
                outTokens.AddRange(outputTokens);
            }
            return outTokens;
        }

        public string TokensToText(List<TSqlParserToken> tokens)
        {
            var sb = new StringBuilder();
            foreach (var token in tokens)
            {
                sb.Append(token.Text);
            }
            return sb.ToString();
        }
    }
}
