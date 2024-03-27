using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Xml.Linq;

namespace SqlScriptRewriter
{
    public class IdempotentCreateRewriteAction : IRewriteAction
    {
        private readonly Func<string, string, bool> _identifierPredicate;

        public IdempotentCreateRewriteAction(Func<string, string, bool> predicate)
        {
            _identifierPredicate = predicate;
        }

        public string ActionName { get => "Idempotent CREATE/ALTER PROCEDURE, VIEW, FUNCTION or SCHEMA"; }

        public TSqlParserToken[] DoRewrite(TSqlParserToken token, Func<int, TSqlParserToken> peekToken, Func<TSqlParserToken> nextToken)
        {
            var procedureToken = peekToken(1);

            IdentifierUtils.PeekProcedureName(tok => peekToken(tok+1), out var schemaName, out var procedureName);

            switch (token.TokenType)
            {
                case TSqlTokenType.Create:
                case TSqlTokenType.Alter:
                    if (procedureToken?.TokenType == TSqlTokenType.Procedure
                         && IdentifierUtils.TokenIsIdentifier(procedureName))
                    {
                        return RewriteCreateOrAlterProcedure(token, schemaName, procedureName);
                    }
                    else if (procedureToken?.TokenType == TSqlTokenType.Schema
                        && token.TokenType == TSqlTokenType.Create
                        && IdentifierUtils.TokenIsIdentifier(procedureName))
                    {
                        return RewriteCreateSchema(token, nextToken, procedureName);
                    }
                    else if (procedureToken?.TokenType == TSqlTokenType.Function
                         && IdentifierUtils.TokenIsIdentifier(procedureName))
                    {
                        return RewriteCreateOrAlterFunction(token, schemaName, procedureName);
                    }
                    else if (procedureToken?.TokenType == TSqlTokenType.View
                        && IdentifierUtils.TokenIsIdentifier(procedureName))
                    {
                        return RewriteCreateOrAlterView(token, schemaName, procedureName);
                    }
                    break;
                default:
                    break;
            }
            return new[] { token };
        }

        private TSqlParserToken[] RewriteCreateOrAlterProcedure(TSqlParserToken token, TSqlParserToken schemaName, TSqlParserToken procedureName)
        {
            var schemaNameText = IdentifierUtils.EnsureUnquoted(schemaName.Text);
            var procedureNameText = IdentifierUtils.EnsureUnquoted(procedureName.Text);
            if (_identifierPredicate(schemaNameText, procedureNameText))
            {
                var newSchemaNameText = IdentifierUtils.QuoteIfNeeded(schemaNameText, schemaName.Text);
                var newProcedureNameText = IdentifierUtils.QuoteIfNeeded(procedureNameText, procedureName.Text);
                return MakeCreateOrAlterProcedure(newSchemaNameText, newProcedureNameText, procedureNameText);
            }
            else
            {
                return new[] { token };
            }
        }

        private TSqlParserToken[] RewriteCreateOrAlterFunction(TSqlParserToken token, TSqlParserToken schemaName, TSqlParserToken procedureName)
        {
            var schemaNameText = IdentifierUtils.EnsureUnquoted(schemaName.Text);
            var procedureNameText = IdentifierUtils.EnsureUnquoted(procedureName.Text);
            if (_identifierPredicate(schemaNameText, procedureNameText))
            {
                var newSchemaNameText = IdentifierUtils.QuoteIfNeeded(schemaNameText, schemaName.Text);
                var newProcedureNameText = IdentifierUtils.QuoteIfNeeded(procedureNameText, procedureName.Text);
                return MakeDropCreateFunction(newSchemaNameText, newProcedureNameText, procedureNameText);
            }
            else
            {
                return new[] { token };
            }
        }

        private TSqlParserToken[] RewriteCreateSchema(TSqlParserToken token, Func<TSqlParserToken> nextToken, TSqlParserToken procedureName)
        {
            var schemaNameText = IdentifierUtils.EnsureUnquoted(procedureName.Text);
            if (_identifierPredicate(schemaNameText, string.Empty))
            {
                nextToken(); // consume SCHEMA
                nextToken(); // consume [schema_name]
                return MakeCreateSchema(schemaNameText);
            }
            else
            {
                return new[] { token };
            }
        }

        private TSqlParserToken[] RewriteCreateOrAlterView(TSqlParserToken token, TSqlParserToken schemaName, TSqlParserToken viewName)
        {
            var schemaNameText = IdentifierUtils.EnsureUnquoted(schemaName.Text);
            var viewNameText = IdentifierUtils.EnsureUnquoted(viewName.Text);
            if (_identifierPredicate(schemaNameText, viewNameText))
            {
                var newSchemaNameText = IdentifierUtils.QuoteIfNeeded(schemaNameText, schemaName.Text);
                var newViewNameText = IdentifierUtils.QuoteIfNeeded(viewNameText, viewName.Text);
                return MakeCreateOrAlterView(newSchemaNameText, newViewNameText, viewNameText);
            }
            else
            {
                return new[] { token };
            }
        }

        private static TSqlParserToken MakeWhitespace() => new TSqlParserToken(TSqlTokenType.WhiteSpace, " ");

        private static TSqlParserToken[] MakeCreateOrAlterProcedure(string schemaName, string procedureName, string procedureNameUnquoted)
        {
            var createProcedureDynamicSqlLiteral = string.IsNullOrEmpty(schemaName)
                ? $"'CREATE PROCEDURE {procedureName} AS'"
                : $"'CREATE PROCEDURE {schemaName}.{procedureName} AS'";

            return new[]
            {
                    // IF NOT EXISTS (SELECT type_desc, type FROM sys.procedures WITH (nolock) WHERE name = 'MyProc' and type = 'P')
                    new TSqlParserToken(TSqlTokenType.If, "IF"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Not, "NOT"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Exists, "EXISTS"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.Select, "SELECT"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Identifier, "type_desc"),
                    new TSqlParserToken(TSqlTokenType.Comma, ","),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Identifier, "type"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.From, "FROM"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Identifier, "sys"),
                    new TSqlParserToken(TSqlTokenType.Dot, "."),
                    new TSqlParserToken(TSqlTokenType.Identifier, "procedures"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.With, "WITH"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.Identifier, "nolock"),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Where, "WHERE"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Identifier, "name"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.EqualsSign, "="),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, $"'{procedureNameUnquoted}'"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.And, "AND"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Identifier, "type"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.EqualsSign, "="),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, "'P'"),

                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                    // BEGIN
                    new TSqlParserToken(TSqlTokenType.Begin, "BEGIN"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n    "),
                    //    EXEC('CREATE PROCEDURE dbo.MyProc AS')
                    new TSqlParserToken(TSqlTokenType.Exec, "EXEC"),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, createProcedureDynamicSqlLiteral),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                    // END
                    new TSqlParserToken(TSqlTokenType.End, "END"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                    // GO
                    new TSqlParserToken(TSqlTokenType.End, "GO"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),

                    new TSqlParserToken(TSqlTokenType.Alter, "ALTER")
                };
        }

        private static TSqlParserToken[] MakeCreateSchema(string schemaNameUnquoted)
        {
            var createSchemaDynamicSqlLiteral = $"'CREATE SCHEMA [{schemaNameUnquoted}]'";

            return new[]
            {
                // IF NOT EXISTS (SELECT name FROM sys.schemas WITH (nolock) WHERE name = 'schema_name')
                new TSqlParserToken(TSqlTokenType.If, "IF"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Not, "NOT"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Exists, "EXISTS"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                new TSqlParserToken(TSqlTokenType.Select, "SELECT"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Identifier, "name"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.From, "FROM"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Identifier, "sys"),
                new TSqlParserToken(TSqlTokenType.Dot, "."),
                new TSqlParserToken(TSqlTokenType.Identifier, "schemas"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.With, "WITH"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                new TSqlParserToken(TSqlTokenType.Identifier, "nolock"),
                new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Where, "WHERE"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.Identifier, "name"),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.EqualsSign, "="),
                MakeWhitespace(),
                new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, $"'{schemaNameUnquoted}'"),
                new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n  "),
                // BEGIN
                //new TSqlParserToken(TSqlTokenType.Begin, "BEGIN"),
                //new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n    "),
                new TSqlParserToken(TSqlTokenType.Exec, "EXEC"),
                new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, createSchemaDynamicSqlLiteral),
                new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                new TSqlParserToken(TSqlTokenType.Semicolon, ";"),
                new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                // END
                //new TSqlParserToken(TSqlTokenType.End, "END"),
                //new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                // GO
                new TSqlParserToken(TSqlTokenType.End, "GO"),
            };
        }

        private static TSqlParserToken[] MakeDropCreateFunction(string schemaName, string procedureName, string procedureNameUnquoted)
        {
            var objectName = string.IsNullOrEmpty(schemaName)
                ? procedureName
                : $"{schemaName}.{procedureName}";
            var createFunctionDynamicSqlLiteral = $"'DROP FUNCTION {objectName}'";

            return new[]
            {
                    // IF EXISTS (SELECT name FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID('MyProc') and type IN ('FN', 'TF', 'IF', 'FN'))
                    new TSqlParserToken(TSqlTokenType.If, "IF"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Exists, "EXISTS"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.Select, "SELECT"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.Identifier, "name"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.From, "FROM"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.Identifier, "sys"),
                        new TSqlParserToken(TSqlTokenType.Dot, "."),
                        new TSqlParserToken(TSqlTokenType.Identifier, "objects"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.With, "WITH"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                        new TSqlParserToken(TSqlTokenType.Identifier, "nolock"),
                        new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Where, "WHERE"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.Identifier, "object_id"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.EqualsSign, "="),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.Function, "OBJECT_ID"),
                            new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                            new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, $"N'{objectName}'"),
                            new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.And, "AND"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.Identifier, "type"),
                        MakeWhitespace(),
                        new TSqlParserToken(TSqlTokenType.In, "IN"),
                        MakeWhitespace(),
                            new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                            new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, "N'FN'"),
                            new TSqlParserToken(TSqlTokenType.Comma, ","),
                            new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, "N'TF'"),
                            new TSqlParserToken(TSqlTokenType.Comma, ","),
                            new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, "N'IF'"),
                            new TSqlParserToken(TSqlTokenType.Comma, ","),
                            new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, "N'TF'"),
                        new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n  "),
                    //    EXEC('DROP FUNCTION dbo.MyProc')
                    new TSqlParserToken(TSqlTokenType.Exec, "EXEC"),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, createFunctionDynamicSqlLiteral),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                    // GO
                    new TSqlParserToken(TSqlTokenType.End, "GO"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),

                    new TSqlParserToken(TSqlTokenType.Create, "CREATE")
                };
        }

        private static TSqlParserToken[] MakeCreateOrAlterView(string schemaName, string viewName, string viewNameUnquoted)
        {
            var objectName = string.IsNullOrEmpty(schemaName)
                ? viewName
                : $"{schemaName}.{viewName}";
            var createViewDynamicSqlLiteral =
                $"'CREATE VIEW {objectName} AS SELECT 1 BAZ'";

            return new[]
            {
                    // IF OBJECT_ID(N'foo', 'V') IS NULL
                    new TSqlParserToken(TSqlTokenType.If, "IF"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Function, "OBJECT_ID"),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, $"N'{objectName}'"),
                    new TSqlParserToken(TSqlTokenType.Comma, ","),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, $"N'V'"),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Is, "IS"),
                    MakeWhitespace(),
                    new TSqlParserToken(TSqlTokenType.Null, "NULL"),

                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n  "),
                    //    EXEC('CREATE PROCEDURE dbo.MyProc AS')
                    new TSqlParserToken(TSqlTokenType.Exec, "EXEC"),
                    new TSqlParserToken(TSqlTokenType.LeftParenthesis, "("),
                    new TSqlParserToken(TSqlTokenType.AsciiStringLiteral, createViewDynamicSqlLiteral),
                    new TSqlParserToken(TSqlTokenType.RightParenthesis, ")"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),
                    // GO
                    new TSqlParserToken(TSqlTokenType.End, "GO"),
                    new TSqlParserToken(TSqlTokenType.WhiteSpace, "\r\n"),

                    new TSqlParserToken(TSqlTokenType.Alter, "ALTER")
                };
        }
    }
}
