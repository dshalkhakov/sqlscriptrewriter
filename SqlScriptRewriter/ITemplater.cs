using System;

namespace SqlScriptRewriter
{
    /// <summary>
    /// Conditional comments template engine.
    /// </summary>
    public interface ITemplater
    {
        /// <summary>
        /// Does this code comment looks like a conditional comment in Mustache syntax?
        /// </summary>
        /// <param name="comment">Comment token text.</param>
        /// <returns>True if comment text contains '{{', false otherwise.</returns>
        Tuple<bool, string> IsConditionalComment(string comment);

        /// <summary>
        /// Renders the template into text, given the environment, and returns the result.
        /// </summary>
        /// <param name="comment">Comment token text.</param>
        /// <param name="environment">Environment the Mustache template will be rendered in (the 'hash').</param>
        /// <returns>Expanded comment result.</returns>
        string ExpandConditionalComment(string comment, object environment);
    }
}