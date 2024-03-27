using System.Collections.Generic;
using System.IO;
using Mustache;

namespace SqlScriptRewriter.TemplaterTags
{
    /// <summary>
    /// Defines a tag that renders '/*' if the argument is true.
    /// </summary>
    public sealed class CommentIfTagDefinition : CommentConditionTagDefinition
    {
        /// <summary>
        /// Initializes a new instance of a CommentIfTagDefinition.
        /// </summary>
        public CommentIfTagDefinition()
            : base("comment_if")
        {
        }

        /// <summary>
        /// Gets the text to output.
        /// </summary>
        /// <param name="writer">The writer to write the output to.</param>
        /// <param name="arguments">The arguments passed to the tag.</param>
        /// <param name="context">Extra data passed along with the context.</param>
        public override void GetText(TextWriter writer, Dictionary<string, object> arguments, Scope context)
        {
            var condition = GetConditionParameter(arguments);
            if (IsConditionSatisfied(condition))
            {
                writer.Write("/*");
            }
        }
    }
}
