using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;

namespace SqlScriptRewriter
{
    public interface ISqlScriptRewriter
    {
        string Rewrite(string script, IRewriteAction rewriteAction, out IList<string> err);
    }
}