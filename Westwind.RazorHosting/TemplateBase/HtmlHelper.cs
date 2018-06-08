namespace Westwind.RazorHosting
{
    public class HtmlHelper
    {
        /// <summary>
        /// Output a string without formatting
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public RawString Raw(string html)
        {
            return new RawString(html);
        }

        /// <summary>
        /// Outputs an unencoded string from a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public RawString HtmlString(object value)
        {
            if (value == null)
                return null;

            return new RawString(value.ToString());
        }

        public string Encode(string value)
        {
            if (value == null)
                return string.Empty;
            return Utilities.HtmlEncode(value);
        }

        public string Encode(object value)
        {
            if (value == null)
                return string.Empty;

            return Utilities.HtmlEncode(value);
        }
    }
}
