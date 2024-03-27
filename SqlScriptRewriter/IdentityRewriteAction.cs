using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace SqlScriptRewriter
{
    public class IdentityRewriteAction : IRewriteAction
    {
        public string ActionName { get => "Identity Rewrite Action"; }

        public TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> nextToken)
        {
            return new[] { token };
        }
    }
}
