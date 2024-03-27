using System;
using Mustache;

namespace SqlScriptRewriter
{
    public class Templater : ITemplater
    {
        public string ExpandConditionalComment(string template, object environment)
        {
            var formatCompiler = new FormatCompiler();
            formatCompiler.RemoveNewLines = false;

            formatCompiler.RegisterTag(new TemplaterTags.CommentIfTagDefinition(), true);
            formatCompiler.RegisterTag(new TemplaterTags.CommentIfEndTagDefinition(), true);

            formatCompiler.RegisterTag(new TemplaterTags.UncommentIfTagDefinition(), true);
            formatCompiler.RegisterTag(new TemplaterTags.UncommentIfEndTagDefinition(), true);

            var generator = formatCompiler.Compile(template);
            var ret = generator.Render(environment);

            // this text needs to be turned into tokens. then we put those tokens instead of the original comment token.
            return ret;
        }

        /// <summary>
        /// Returns true if string looks like a template. Time complexity is O(n), where n is the length of the string.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public Tuple<bool, string> IsConditionalComment(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                return Tuple.Create(false, string.Empty);
            }
            var hasBegin = template.Contains("{{");
            var hasEnd = template.Contains("}}");
            if (hasBegin && hasEnd)
            {
                return Tuple.Create(true, string.Empty);
            }
            if ((hasBegin && !hasEnd)
                || (hasEnd && !hasBegin))
            {
                return Tuple.Create(true, "Found a conditional comment with mismatched {{ }} tags");
            }
            return Tuple.Create(false, string.Empty);
        }
    }
}
