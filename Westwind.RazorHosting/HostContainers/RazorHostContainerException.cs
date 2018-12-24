using System;

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

        public RazorHostContainerException(string message, string sourceCode, Exception lastException = null, string activeTemplate = null, object templatePerRequestConfigurationData = null) : base(message, lastException)
        {
            GeneratedSourceCode = sourceCode;
            RequestConfigurationData = templatePerRequestConfigurationData;
            ActiveTemplate = activeTemplate;
        }

        /// <summary>
        /// Source code for the template if available
        /// </summary>
        public string GeneratedSourceCode { get; set; }


        /// <summary>
        /// The active template that is being rendered when
        /// rendering file templates
        /// </summary>
        public string ActiveTemplate { get; set; }

        /// <summary>
        /// Template Engine Configuration Data
        /// </summary>
        public object RequestConfigurationData { get; set; }


        /// <summary>
        /// Call stack when an actual exception occurred  - not always available
        /// </summary>
        public string CallStack { get; set; }
    }
}
