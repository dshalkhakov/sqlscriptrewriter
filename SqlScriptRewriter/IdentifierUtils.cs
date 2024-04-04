using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlScriptRewriter
{
    internal class IdentifierUtils
    {
        public static bool TokenIsIdentifier(TSqlParserToken token)
        {
            return token?.TokenType == TSqlTokenType.Identifier || token?.TokenType == TSqlTokenType.QuotedIdentifier;
        }

        public static string EnsureUnquoted(string s)
        {
            if (!string.IsNullOrEmpty(s) && s[0] == '[')
            {
                return UnquoteIdentifier(s);
            }
            else
            {
                return s;
            }
        }

        public static string QuoteIfNeeded(string s, string oldS)
        {
            if (!string.IsNullOrEmpty(oldS) && oldS[0] == '[')
            {
                return QuoteIdentifier(s);
            }
            else
            {
                return s;
            }
        }

        public static TSqlParserToken QuoteIfNeeded(string s, TSqlParserToken oldS)
        {
            if (oldS?.Text[0] == '[')
            {
                return new TSqlParserToken(TSqlTokenType.QuotedIdentifier, QuoteIdentifier(s));
            }
            else
            {
                return new TSqlParserToken(TSqlTokenType.Identifier, s);
            }
        }

        private static string UnquoteIdentifier(string s)
            => s.Substring(0, s.Length - 1)
                    .Substring(1);

        private static string QuoteIdentifier(string s)
            => "[" + s + "]";

        public static int PeekProcedureName(Func<int, TSqlParserToken> peekToken, out TSqlParserToken schemaName, out TSqlParserToken procedureName)
        {
            var ident1Token = peekToken(1);
            var nameDotToken = peekToken(2);
            var ident2Token = peekToken(3);

            var nameIncludesSchema = nameDotToken?.TokenType == TSqlTokenType.Dot;
            schemaName = nameIncludesSchema
                ? ident1Token
                : new TSqlParserToken(TSqlTokenType.Identifier, string.Empty);
            procedureName = nameIncludesSchema
                ? ident2Token
                : ident1Token;

            // return how many tokens of lookahead we needed to parse the procedure name
            return nameIncludesSchema ? 3 : 1;
        }

        public static void ParseModuleName(
            Func<int, TSqlParserToken> peekToken,
            Func<TSqlParserToken> consumeToken, 
            out TSqlParserToken dbName,
            out TSqlParserToken schemaName, out TSqlParserToken procedureName)
        {
            dbName = null;
            var ident1Token = consumeToken();
            var nameDotToken = peekToken(1);
            var nameDotToken2 = peekToken(2);
            if (nameDotToken.TokenType == TSqlTokenType.Dot && nameDotToken2.TokenType != TSqlTokenType.Dot)
            {
                consumeToken(); // consume dot
                var ident2Token = consumeToken();

                schemaName = ident1Token;
                procedureName = ident2Token;
            }
            else if (nameDotToken.TokenType == TSqlTokenType.Dot && nameDotToken2.TokenType == TSqlTokenType.Dot)
            {
                consumeToken(); // consume dot
                consumeToken(); // consume dot
                var ident2Token = consumeToken();

                dbName = ident1Token;
                schemaName = new TSqlParserToken(TSqlTokenType.Identifier, "dbo");
                procedureName = ident2Token;
            }
            else
            {
                schemaName = new TSqlParserToken(TSqlTokenType.Identifier, "dbo");
                procedureName = ident1Token;
            }
        }

        // m a -> (a -> b) -> m b
        public static TSqlParserToken WithIdentifierToken(TSqlParserToken token, Func<string, string> mapFunc)
        {
            switch (token.TokenType)
            {
                case TSqlTokenType.QuotedIdentifier:
                    var text = UnquoteIdentifier(token.Text);
                    var newText = mapFunc(text);
                    return new TSqlParserToken(TSqlTokenType.QuotedIdentifier, QuoteIdentifier(newText));

                case TSqlTokenType.Identifier:
                    return new TSqlParserToken(TSqlTokenType.Identifier, mapFunc(token.Text));

                default:
                    return token;
            }
        }
    }
}
