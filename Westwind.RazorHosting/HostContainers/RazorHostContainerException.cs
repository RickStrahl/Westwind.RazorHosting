using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Westwind.RazorHosting
{
    /// <summary>
    /// Exception thrown when rendering or compiling of a folder host
    /// render or initialization occurs
    /// </summary>
    public class RazorHostContainerException : Exception
    {
        public RazorHostContainerException()
        {
        }

        public RazorHostContainerException(string message) : base(message)
        {
        }

        public RazorHostContainerException(string message, string sourceCode, Exception lastException = null, object templatePerRequestConfigurationData = null) : base(message,lastException)
        {
            GeneratedSourceCode = sourceCode;
            RequestConfigurationData = templatePerRequestConfigurationData;
        }

        /// <summary>
        /// Source code for the template if available
        /// </summary>
        public string GeneratedSourceCode { get; set; }

        /// <summary>
        /// Template Engine Configuration Data
        /// </summary>
        public object RequestConfigurationData { get; set; }
    }
}
