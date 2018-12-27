#pragma warning disable CS1591

using System.Net;
using System.Text;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Class that wraps a string and returns it as a raw
    /// non-encoded string.
    /// </summary>
    public class RawString : IHtmlString
    {
        protected string _Text;

        public RawString(string text)
        {
            _Text = text ?? string.Empty;
        }

        public RawString()
        {
            _Text = string.Empty;
        }

        public RawString(StringBuilder sb)
        {
            _Text = sb.ToString();
        }


        
        /// <summary>
        /// 
        /// </summary>
        public static RawString Empty => _empty;
        private static readonly RawString _empty = new RawString();

        public override string ToString()
        {
            return _Text;
        }

        public string ToHtmlString()
        {            
            return WebUtility.HtmlEncode(_Text);
        }
    }
}
