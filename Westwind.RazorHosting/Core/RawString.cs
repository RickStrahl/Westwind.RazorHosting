using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Class that wraps a string and returns it as a raw
    /// non-encoded string.
    /// </summary>
    public class RawString
    {
        protected string _Text;

        public RawString(string text)
        {
            _Text = text;
        }

        public override string ToString()
        {
            return _Text;
        }
    }
}