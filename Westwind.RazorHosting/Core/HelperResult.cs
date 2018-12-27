#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Helped class used by Razor to render generated code.
    /// </summary>
    public class HelperResult : IHtmlString
    {
        private readonly Action<TextWriter> action;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelperResult"/> class,
        /// with the provided <paramref name="action"/>.
        /// </summary>
        /// <param name="action">The action that should be used to produce the result.</param>
        public HelperResult(Action<TextWriter> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }

            this.action = action;
        }

        /// <summary>
        /// Returns a HTML formatted <see cref="string"/> that represents the current <see cref="HelperResult"/>.
        /// </summary>
        /// <returns>A HTML formatted <see cref="string"/> that represents the current <see cref="HelperResult"/>.</returns>
        public string ToHtmlString()
        {
            return this.ToString();
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="HelperResult"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> that represents the current <see cref="HelperResult"/>.</returns>
        public override string ToString()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                this.action(stringWriter);
                return stringWriter.ToString();
            }
        }

        /// <summary>
        /// Writes the output of the <see cref="HelperResult"/> to the provided <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> instance that the output should be written to.</param>
        public void WriteTo(TextWriter writer)
        {
            this.action(writer);
        }


        /// <summary>
        /// Writes the output of the <see cref="HelperResult"/> to the provided <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">A <see cref="TextWriter"/> instance that the output should be written to.</param>
        /// <param name="val"></param>
        public void WriteTo(TextWriter writer, object val)
        {
            writer.Write(val);
        }
    }

    public interface IHtmlString
    {
        /// <summary>
        /// Returns an HTML-encoded string.
        /// </summary>
        /// <returns>An HTML-encoded string.</returns>
        string ToHtmlString();
    }
}
