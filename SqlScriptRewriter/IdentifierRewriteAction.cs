using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlScriptRewriter
{
    public class IdentifierRewriteAction : IRewriteAction
    {
        private readonly Func<string, string> _action;

        public IdentifierRewriteAction(Func<string, string> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public string ActionName { get => "Identifier Rewrite Action"; }

        public TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> nextToken)
        {
            switch (token.TokenType)
            {
                case TSqlTokenType.Identifier:
                case TSqlTokenType.QuotedIdentifier:
                    var newToken = IdentifierUtils.WithIdentifierToken(token, _action);
                    return new[] { newToken };

                default:
                    break;
            }
            return new[] { token };
        }
    }
}
