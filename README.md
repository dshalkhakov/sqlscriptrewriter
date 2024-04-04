# SqlScriptRewriter

# SQL Script Rewriting Examples

## Idempotent CREATE/ALTER PROCEDURE

This procedure

```
CREATE PROCEDURE dbo.Foo AS
BEGIN
  SELECT 1
END;
```

Will be rewritten to

```
IF NOT EXISTS (SELECT type_desc, type FROM sys.procedures WITH (nolock) WHERE name = 'Foo' AND type = 'P')
BEGIN
    EXEC('CREATE PROCEDURE dbo.Foo AS')
END
GO
ALTER PROCEDURE dbo.Foo AS
BEGIN
  SELECT 1
END;
```

For more information, please see the `SqlScriptRewriter.Tests` project.

## Idempotent CREATE SCHEMA

```
CREATE SCHEMA foo
```

will be rewritten to

```
IF NOT EXISTS (SELECT name FROM sys.schemas WITH (nolock) WHERE name = 'foo')
  EXEC('CREATE SCHEMA [foo]');
GO
```

Any other syntactic variants would result in rewrite errors.
So if you need to include something else into your `CREATE SCHEMA` statement, please do the rewriting manually using dynamic SQL, as above.

For more information, please see the `SqlScriptRewriter.Tests` project.

## Idempotent CREATE/ALTER VIEW

Both `CREATE VIEW` and `ALTER VIEW` would be rewritten to the idempotent style in the same fashion as `CREATE/ALTER PROCEDURE` above.

For more information, please see the `SqlScriptRewriter.Tests` project.

## Idempotent CREATE/ALTER FUNCTION

`CREATE/ALTER FUNCTION` is tricky. There are three different function types: IF (inline table valued function), TF (table valued function), FN (scalar function).
All of them are rewritten to idempotent style. E.g. a scalar function

```
CREATE FUNCTION Foo() RETURNS INT AS BEGIN RETURN 10 END
```

will be rewritten to

```
IF NOT EXISTS (SELECT 1 FROM sys.objects WITH (nolock) WHERE object_id = OBJECT_ID(N'Foo') AND type = N'FN')
  EXEC('CREATE FUNCTION Foo() RETURNS INT AS BEGIN RETURN 1 END')
ALTER FUNCTION Foo() RETURNS INT AS BEGIN RETURN 10 END
```

**NOTE:**: if the type of the function changes (e.g. a scalar function was modified and became a table-valued one), a separate migration script dropping the previous definition should be added.

## Conditional comments

Multi-line comments are allowed to have Mustache templates embedded into them. The templating is achieved with the (Mustache# library)[https://github.com/jehugaleahsa/mustache-sharp].

This query

```
SELECT 1
 /* {{#if DEVTEST}}
 , foo
 {{/if}} */
 FROM bar
```

will be expanded to 

```
SELECT 1

 , foo
 
 FROM bar
```

on DEVTEST enviroment. While for Production the expansion would look like this:

```
SELECT 1
 
 
 
 FROM bar
```

Currently DEVTEST is hardcoded to be true.

## Commenting conditionally with comment_if/end_comment_if tags

You can use matching `#comment_if`/`#end_comment_if` tags to conditionally insert comments into the processed text.
E.g. this query

```
SELECT 1
 /* {{#comment_if DEVTEST}} */
 , foo
 /* {{#end_comment_if DEVTEST}} */
 FROM bar
```

will be expanded to 

```
SELECT 1
/*
 , foo
*/
 FROM bar
```

## Un-commenting conditionally with uncomment_if/end_uncomment_if tags

Conversely, if you want the code to be uncommented in DEVTEST, you can use the matching `#uncomment_if`/`#end_uncomment_if` tags.
E.g. this query

```
SELECT 1
 /* {{#uncomment_if DEVTEST}} */
 , foo
 /* {{#end_uncomment_if DEVTEST}} */
 FROM bar
```

will be expanded to 

```
SELECT 1

 , foo

 FROM bar
```

## Keeping prod version uncommented and DEVTEST commented

We actually want the prod version to be always uncommented in the source text. And yet when deployed to DEVTEST, the DEVTEST should be uncommented and prod version should be commented out.
We can achieve that by combining the `comment_if` and `uncomment_if` tags:

```
select
  -- snip
	/* {{#uncomment_if DEVTEST}}
	, 'foo' [Bar]
	{{#end_uncomment_if DEVTEST}} */
	/* {{#comment_if DEVTEST}} */
	,'baz' [Bar]
	/* {{#end_comment_if DEVTEST}} */
  -- snip
```

This query expands to the following for DEVTEST:

```
select
  -- snip
	 
	, 'foo' [Bar]
	 
	 /* 
	,'baz' [Bar]
	 */ 
  -- snip
```

How does this work?

1. Since the first multi-line comment contains a `{{`, then it's a conditional comment.
2. Expanding it, we get the following tokens:

```
	 
	, 'foo' [Bar]
	 
```

3. We replace the original multi-line comment token with those tokens.
4. Then we see the next multi-line comment. This one expands to ` /* `, so we replace the original comment with this one.
5. Then we process next tokens (`foo` and later), and we bump into the last multi-line comment.
6. We expand it, getting ` */ `. We replace the original comment with this one.
7. After all those expansions, we have arrived at the final program text.
