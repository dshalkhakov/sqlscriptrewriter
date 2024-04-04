using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using SqlScriptRewriter.Enums;

namespace SqlScriptRewriter
{
    // O Parsec, where is thy conciseness...
    public static class FunctionUtils
    {
        // Precondition: parser has consumed ALTER/CREATE, FUNCTION, function name, and is now on the '(' token
        public static FunctionType IsFunction(Func<int, TSqlParserToken> peekToken)
        {
            int ofs = 1;
            var leftParenToken = peekToken(ofs);
            int level = 1;
            if (leftParenToken != null && leftParenToken.TokenType == TSqlTokenType.LeftParenthesis)
            {
                if (!ConsumeUntilRightParen(ref ofs, ref level, peekToken))
                {
                    return FunctionType.Bad;
                }
                if (level < 0)
                {
                    return FunctionType.Bad; // underflow
                }
                else if (level > 1)
                {
                    return FunctionType.Bad; // unbalanced parens
                }

                // we're on RETURNS
                var returnsToken = peekToken(ofs + 1);
                if (returnsToken != null && returnsToken.TokenType == TSqlTokenType.Identifier && string.Equals(returnsToken.Text, "RETURNS", StringComparison.InvariantCultureIgnoreCase))
                {
                    var tableToken = peekToken(ofs + 2);
                    var afterTableToken = peekToken(ofs + 3);

                    // TABLE -> inline table valued function
                    if (tableToken != null && tableToken.TokenType == TSqlTokenType.Table)
                    {
                        return FunctionType.IF;
                    }
                    // variable followed by table -> table valued function
                    if (tableToken != null && tableToken.TokenType == TSqlTokenType.Variable
                        && afterTableToken != null && afterTableToken.TokenType == TSqlTokenType.Table)
                    {
                        return FunctionType.TF;
                    }
                    // everything else is a scalar function
                    return FunctionType.FN;
                }
            }
            return FunctionType.Bad;
        }

        private static bool ConsumeUntilRightParen(ref int ofs, ref int level, Func<int, TSqlParserToken> peekToken)
        {
            while (true)
            {
                ofs++;
                var token = peekToken(ofs);
                if (token == null)
                {
                    return false;
                }
                else if (token.TokenType == TSqlTokenType.LeftParenthesis)
                {
                    level++;
                }
                else if (token.TokenType == TSqlTokenType.RightParenthesis)
                {
                    if (level <= 1)
                    {
                        break;
                    }
                    level--;
                }
            }

            return true;
        }
    }
}
