using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Linq;

namespace SqlScriptRewriter
{
    public class ProcedureNameRewriteAction : IRewriteAction
    {
        private readonly Func<string, string, (string, string)> _rewriteFunc;

        public ProcedureNameRewriteAction(Func<string, string, (string, string)> rewriteFunc)
        {
            _rewriteFunc = rewriteFunc ?? throw new ArgumentNullException(nameof(rewriteFunc));
        }

        public string ActionName { get => "Procedure Name Rewrite"; }

        public TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> consumeToken)
        {
            switch (token.TokenType)
            {
                case TSqlTokenType.Procedure:
                case TSqlTokenType.Exec:
                case TSqlTokenType.Execute:
                    var nextToken = peekToken(1);
                    if (IdentifierUtils.TokenIsIdentifier(nextToken))
                    {
                        IdentifierUtils.ParseModuleName(peekToken, consumeToken, out var dbName, out var schemaName, out var procedureName);
                        var (newSchema, newName) = _rewriteFunc(
                            IdentifierUtils.EnsureUnquoted(schemaName.Text),
                            IdentifierUtils.EnsureUnquoted(procedureName.Text));
                        return MakeModuleName(token, dbName, schemaName, procedureName, newSchema, newName);
                    }
                    break;
                default:
                    break;
            }

            return new[] { token };
        }

        private static TSqlParserToken[] MakeModuleName(TSqlParserToken token, TSqlParserToken dbName, TSqlParserToken schemaName, TSqlParserToken procedureName, string newSchema, string newName)
        {
            return new[]
            {
                // <token> [<dbName?>.]<schemaName>.<identifier>
                token,
                new TSqlParserToken(TSqlTokenType.WhiteSpace, " "),
                dbName ?? null,
                dbName != null ? new TSqlParserToken(TSqlTokenType.Dot, ".") : null,
                IdentifierUtils.QuoteIfNeeded(newSchema, schemaName),
                new TSqlParserToken(TSqlTokenType.Dot, "."),
                IdentifierUtils.QuoteIfNeeded(newName, procedureName),
            }.Where(t => t != null).ToArray();
        }
    }
}
