using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;

namespace SqlScriptRewriter
{
    public interface IRewriteAction
    {
        /// <summary>
        /// This method should dispatch on the current token and then return either the
        /// current token or a list of new ones. The list might be empty as well.
        /// </summary>
        /// <param name="token">Current token.</param>
        /// <param name="peekToken">Function for look-ahead. Returns null if reached EOF.</param>
        /// <param name="nextToken">Consume next token. Returns null if reached EOF.</param>
        /// <returns>Either the current token or a replacement(s).</returns>
        TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> nextToken);

        /// <summary>
        /// Name of the action.
        /// </summary>
        string ActionName { get; }
    }
}
