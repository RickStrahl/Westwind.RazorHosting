using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Westwind.RazorHosting
{
    public class HtmlHelper
    {
        /// <summary>
        /// Output a string without formatting
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public HtmlString Raw(string html)
        {
            return new HtmlString(html);
        }

        /// <summary>
        /// Outputs an unencoded string from a value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public HtmlString HtmlString(object value)
        {
            if (value == null)
                return null;

            return new HtmlString(value.ToString());
        }

        public string Encode(string value)
        {
            if (value == null)
                return string.Empty;
            return HttpUtility.HtmlEncode(value);
        }

        public string Encode(object value)
        {
            if (value == null)
                return string.Empty;

            return HttpUtility.HtmlEncode(value);
        }
    }
}
