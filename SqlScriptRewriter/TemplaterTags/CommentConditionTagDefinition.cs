using Mustache;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SqlScriptRewriter.TemplaterTags
{
    /// <summary>
    /// Defines a tag that conditionally prints it's content.
    /// </summary>
    public abstract class CommentConditionTagDefinition : TagDefinition
    {
        private const string conditionParameter = "condition";

        /// <summary>
        /// Initializes a new instance of a ConditionTagDefinition.
        /// </summary>
        /// <param name="tagName">The name of the tag.</param>
        protected CommentConditionTagDefinition(string tagName)
            : base(tagName)
        {
        }

        protected override IEnumerable<string> GetClosingTags()
        {
            return new string[0];
        }

        protected override bool GetHasContent()
        {
            return false;
        }

        /// <summary>
        /// Gets the parameters that can be passed to the tag.
        /// </summary>
        /// <returns>The parameters.</returns>
        protected override IEnumerable<TagParameter> GetParameters()
        {
            return new TagParameter[] { new TagParameter(conditionParameter) { IsRequired = true } };
        }

        public object GetConditionParameter(Dictionary<string, object> arguments)
        {
            return arguments[conditionParameter];
        }

        public bool IsConditionSatisfied(object condition)
        {
            if (condition == null || condition == DBNull.Value)
            {
                return false;
            }
            if (condition is IEnumerable enumerable)
            {
                return enumerable.Cast<object>().Any();
            }
            if (condition is Char)
            {
                return (Char)condition != '\0';
            }
            try
            {
                decimal number = (decimal)Convert.ChangeType(condition, typeof(decimal));
                return number != 0.0m;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the parameters that are used to create a new child context.
        /// </summary>
        /// <returns>The parameters that are used to create a new child context.</returns>
        public override IEnumerable<TagParameter> GetChildContextParameters()
        {
            return new TagParameter[0];
        }
    }
}
